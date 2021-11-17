using CamundaClientLibrary.Dto;
using CamundaClientLibrary.DTO;
using CamundaClientLibrary.Service;
using CamundaClientLibrary.Worker;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamundaClientLibrary
{

    public class CamundaEngineClient
    {

        private ILog logger = LogManager.GetLogger(typeof(CamundaEngineClient));

        public static string DEFAULT_URL = "http://localhost:8080/engine-rest/engine/default/";
        public static string COCKPIT_URL = "http://localhost:8080/camunda/app/cockpit/default/";

        private ExternalTaskListener _listener;
        private IList<ExternalTaskWorker> _workers = new List<ExternalTaskWorker>();
        private CamundaClientHelper _camundaClientHelper;

        public CamundaEngineClient() : this(new Uri(DEFAULT_URL), null, null, null) { }

        public CamundaEngineClient(Uri restUrl, string userName, string password, System.Reflection.Assembly assembly = null)
        {
            _camundaClientHelper = new CamundaClientHelper(restUrl, userName, password, assembly);
        }

        public BpmnWorkflowService BpmnWorkflowService => new BpmnWorkflowService(_camundaClientHelper);

        public HumanTaskService HumanTaskService => new HumanTaskService(_camundaClientHelper);

        public RepositoryService RepositoryService => new RepositoryService(_camundaClientHelper);

        public ExternalTaskService ExternalTaskService => new ExternalTaskService(_camundaClientHelper);

        /// <summary>
        /// Startup the 
        /// </summary>
        public void StartupWithSingleThreadPolling()
        {
            logger.Info("call StartupWithSingleThreadPolling");
            this.StartWorkerListener();
        }

        public void ShutdownSingleThreadPolling()
        {
            logger.Info("call ShutdownSingleThreadPolling");
            this.StopWorkerListener();
        }

        public void StartWorkerListener()
        {
            System.Reflection.Assembly assembly = this._camundaClientHelper.Assembly;
            if (assembly == null)
                assembly = System.Reflection.Assembly.GetEntryAssembly();
            var externalTaskWorkers = RetrieveExternalTaskWorkerInfo(assembly);

            this.CheckWorkers(externalTaskWorkers);

            _listener = new ExternalTaskListener(ExternalTaskService, externalTaskWorkers);
            _listener.StartWork();
        }


        public void StopWorkerListener()
        {
            _listener.StopWork();
        }

        private static IEnumerable<Type> GetTypesWithAttribute(System.Reflection.Assembly assembly, Type attribute)
        {
            return assembly.GetTypes().Where(type => type.GetCustomAttributes(attribute, true).Length > 0).ToList();
        }

        private static IEnumerable<Dto.ExternalTaskWorkerInfo> RetrieveExternalTaskWorkerInfo(System.Reflection.Assembly assembly)
        {
            // find all classes with CustomAttribute [ExternalTask("name")]
            var externalTaskTypes = GetTypesWithAttribute(assembly, typeof(ExternalTaskAttribute));

            var workInfos = new List<ExternalTaskWorkerInfo>();

            foreach (var externalTaskType in externalTaskTypes)
            {
                var externalTaskAttributes = externalTaskType.GetCustomAttributes(typeof(ExternalTaskAttribute), true) as ExternalTaskAttribute[];
                var externalTaskVariableRequirementsAttributes = externalTaskType.GetCustomAttributes(typeof(ExternalTaskVariableRequirementsAttribute), true) as ExternalTaskVariableRequirementsAttribute[];

                foreach  (var externalTaskAttributeObject in externalTaskAttributes)
                {
                    var externalTaskAttribute = externalTaskAttributeObject as ExternalTaskAttribute;
                    var workInfo = new ExternalTaskWorkerInfo();
                    workInfo.Type = externalTaskType;
                    workInfo.TaskAdapter = externalTaskType.GetConstructor(Type.EmptyTypes)?.Invoke(null) as IExternalTaskAdapter;
                    workInfo.ProcessId = externalTaskAttribute.ProcessId;
                    workInfo.ActivityId = externalTaskAttribute.ActivityId;
                    workInfo.Retries = externalTaskAttribute.Retries;
                    workInfo.RetryTimeout = externalTaskAttribute.RetryTimeout;
                    workInfo.VariablesToFetch = externalTaskVariableRequirementsAttributes?.Where(x => x.VariablesToFetch != null).SelectMany(x=> x.VariablesToFetch).ToList();
                    workInfos.Add(workInfo);
                }
            }

            return workInfos;
        }

        public void CheckWorkers(IEnumerable<ExternalTaskWorkerInfo> workerInfos)
        {

            var groupInfo = workerInfos.GroupBy(c=> new { c.ProcessId, c.ActivityId}).Select(g => new { ProcessId = g.Key.ProcessId, ActivityId = g.Key.ActivityId, Count = g.Count() });
            var exceptionInfos = groupInfo.Where(x => x.Count > 1);

            if (exceptionInfos.Count() > 0)
            {
                string message = "";

                foreach (var exceptionInfo in exceptionInfos)
                {
                    var exceptionWorkerAssemblyNamesString = string.Join(@",", workerInfos.Where(x=> x.ProcessId == exceptionInfo.ProcessId && x.ActivityId == exceptionInfo.ActivityId).Select(x => x.Type.FullName));
                    message += string.Format(@"The assembly name {0} is configured same process id {1} and same activity id {2}. This library is not support different assembly point to same topic.\n", exceptionWorkerAssemblyNamesString, exceptionInfo.ProcessId, exceptionInfo.ActivityId);
                }

                throw new NotSupportedException(message);
            }
        }

        public void Startup()
        {
            logger.Info("call Startup");
            this.StartWorkers();
            this.RepositoryService.AutoDeploy();
        }

        public void Shutdown()
        {
            this.StopWorkers();
        }

        public void StartWorkers()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var externalTaskWorkers = RetrieveExternalTaskTopicWorkerInfo(assembly);

            foreach (var taskWorkerInfo in externalTaskWorkers)
            {
                Console.WriteLine($"Register Task Worker for Topic '{taskWorkerInfo.TopicName}'");
                ExternalTaskWorker worker = new ExternalTaskWorker(ExternalTaskService, taskWorkerInfo);
                _workers.Add(worker);
                worker.StartWork();
            }
        }

        private static IEnumerable<Dto.ExternalTaskTopicWorkerInfo> RetrieveExternalTaskTopicWorkerInfo(System.Reflection.Assembly assembly)
        {
            // find all classes with CustomAttribute [ExternalTask("name")]
            var externalTaskWorkers =
                from t in assembly.GetTypes()
                let externalTaskTopicAttribute = t.GetCustomAttributes(typeof(ExternalTaskTopicAttribute), true).FirstOrDefault() as ExternalTaskTopicAttribute
                let externalTaskVariableRequirements = t.GetCustomAttributes(typeof(ExternalTaskVariableRequirementsAttribute), true).FirstOrDefault() as ExternalTaskVariableRequirementsAttribute
                where externalTaskTopicAttribute != null
                select new Dto.ExternalTaskTopicWorkerInfo
                {
                    Type = t,
                    TopicName = externalTaskTopicAttribute.TopicName,
                    Retries = externalTaskTopicAttribute.Retries,
                    RetryTimeout = externalTaskTopicAttribute.RetryTimeout,
                    VariablesToFetch = externalTaskVariableRequirements?.VariablesToFetch,
                    TaskAdapter = t.GetConstructor(Type.EmptyTypes)?.Invoke(null) as IExternalTaskAdapter
                };
            return externalTaskWorkers;
        }

        public void StopWorkers()
        {
            foreach (ExternalTaskWorker worker in _workers)
            {
                worker.StopWork();
            }
        }

        // HELPER METHODS

    }
}