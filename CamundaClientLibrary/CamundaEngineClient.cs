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

        private IList<ExternalTaskWorker> _workers = new List<ExternalTaskWorker>();
        private CamundaClientHelper _camundaClientHelper;

        public CamundaEngineClient() : this(new Uri(DEFAULT_URL), null, null) { }

        public CamundaEngineClient(Uri restUrl, string userName, string password)
        {
            _camundaClientHelper = new CamundaClientHelper(restUrl, userName, password);
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

        public void StartWorkerListener()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var externalTaskWorkers = RetrieveExternalTaskWorkerInfo(assembly);

            this.CheckWorkers(externalTaskWorkers);

            var externalTaskListener = new ExternalTaskListener(ExternalTaskService, externalTaskWorkers);
        }

        public void CheckWorkers(IEnumerable<ExternalTaskWorkerInfo> workerInfos)
        {
            var exceptionWorkerInfosGroupping = workerInfos.GroupBy(x => x.TopicName).Where(grp => grp.Count() > 1);
            var exceptionTopicNames = exceptionWorkerInfosGroupping.Select(grp => grp.Key);

            if (exceptionTopicNames.Count() > 0)
            {                
                var exceptionWorkerInfos = exceptionWorkerInfosGroupping.SelectMany(x => x);
                var exceptionWorkerAssemblyNamesString = string.Join(@",", exceptionWorkerInfos.Select(x => x.Type.FullName));
                var exceptionTopicNamesString = string.Join(@",", exceptionTopicNames);

                var message = string.Format(@"The assembly name {0} is configured same topic name {1}", exceptionWorkerAssemblyNamesString, exceptionTopicNamesString);
                throw new ConfigurationException(message);
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
            var externalTaskWorkers = RetrieveExternalTaskWorkerInfo(assembly);

            this.CheckWorkers(externalTaskWorkers);

            foreach (var taskWorkerInfo in externalTaskWorkers)
            {
                Console.WriteLine($"Register Task Worker for Topic '{taskWorkerInfo.TopicName}'");
                ExternalTaskWorker worker = new ExternalTaskWorker(ExternalTaskService, taskWorkerInfo);
                _workers.Add(worker);
                worker.StartWork();
            }
        }

        private static IEnumerable<Dto.ExternalTaskWorkerInfo> RetrieveExternalTaskWorkerInfo(System.Reflection.Assembly assembly)
        {
            // find all classes with CustomAttribute [ExternalTask("name")]
            var externalTaskWorkers =
                from t in assembly.GetTypes()
                let externalTaskTopicAttribute = t.GetCustomAttributes(typeof(ExternalTaskTopicAttribute), true).FirstOrDefault() as ExternalTaskTopicAttribute
                let externalTaskVariableRequirements = t.GetCustomAttributes(typeof(ExternalTaskVariableRequirementsAttribute), true).FirstOrDefault() as ExternalTaskVariableRequirementsAttribute
                where externalTaskTopicAttribute != null
                select new Dto.ExternalTaskWorkerInfo
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