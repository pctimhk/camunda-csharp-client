using CamundaClientLibrary.Dto;
using CamundaClientLibrary.Service;
using Common.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace CamundaClientLibrary.Worker
{
    public class ExternalTaskListener
    {
        private string workerId = Guid.NewGuid().ToString();

        private ILog logger = LogManager.GetLogger(typeof(ExternalTaskListener));

        private long pollingIntervalInMilliseconds; // every 50 milliseconds
        private int maxDegreeOfParallelism;

        private long lockDurationInMilliseconds = 1 * 60 * 1000; // 1 minute
        private Timer taskQueryTimer;

        private ExternalTaskService externalTaskService;
        private IEnumerable<ExternalTaskWorkerInfo> workerInfos;

        public ExternalTaskListener(ExternalTaskService externalTaskService, IEnumerable<ExternalTaskWorkerInfo> workerInfos, int pollingIntervalInMilliseconds = 50, int maxDegreeOfParallelism = 32)
        {
            this.externalTaskService = externalTaskService;
            this.workerInfos = workerInfos;
            this.pollingIntervalInMilliseconds = pollingIntervalInMilliseconds;
            this.maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        public void StartWork(IEnumerable<ExternalTaskWorkerInfo> workerInfos)
        {

            this.taskQueryTimer = new Timer(_ => DoPolling(), null, pollingIntervalInMilliseconds, Timeout.Infinite);
        }

        public void StopWork()
        {
            this.taskQueryTimer.Dispose();
            this.taskQueryTimer = null;
        }

        public void Dispose()
        {
            if (this.taskQueryTimer != null)
            {
                this.taskQueryTimer.Dispose();
            }
        }

        public void DoPolling()
        {
            // Query External Tasks
            try
            {
                var camundaPendingTasks = this.externalTaskService.GetPendingExternalTasks(null, null, null, null,true);

                Parallel.ForEach(camundaPendingTasks, new ParallelOptions { MaxDegreeOfParallelism = this.maxDegreeOfParallelism }, camundaPendingTask =>
                {

                    try
                    {
                        // get the assembly in the list
                        if (!string.IsNullOrEmpty(camundaPendingTask.id) && !string.IsNullOrEmpty(camundaPendingTask.topicName))
                        {
                            // find the matched assembly
                            var workers = this.workerInfos.Where(x => x.TopicName == camundaPendingTask.topicName);

                            if (workers.Count() > 1)
                            {
                                throw new ConfigurationException("More than one worker found in the assembly");
                            }

                            var worker = workers.Single();
                            var tasks = externalTaskService.FetchAndLockTasks(workerId, 1, camundaPendingTask.topicName, lockDurationInMilliseconds, worker.VariablesToFetch);

                            if (tasks.Count() > 1)
                            {
                                throw new EngineException("More than one task return from Camunda");
                            }
                            else if (tasks.Count() <= 1)
                            {
                                logger.Warn(string.Format(@"another worker is locked for the task id {0}.", camundaPendingTask.id));
                            }

                            var task = tasks.Single();

                            this.Execute(task, worker);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        logger.Error(ex);
                    }
                });

            }
            catch (EngineException ex)
            {
                // Most probably server is not running or request is invalid
                Console.WriteLine(ex.Message);
                logger.Fatal(ex);
            }

            // schedule next run (if not stopped in between)
            if (taskQueryTimer != null)
            {
                taskQueryTimer.Change(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(Timeout.Infinite));
            }


        }

        private void Execute(ExternalTask externalTask, ExternalTaskWorkerInfo taskWorkerInfo)
        {
            Dictionary<string, object> resultVariables = new Dictionary<string, object>();

            logger.Info($"Execute External Task from topic '{taskWorkerInfo.TopicName}': {externalTask}...");
            try
            {
                taskWorkerInfo.TaskAdapter.Execute(externalTask, ref resultVariables);
                logger.Info($"...finished External Task {externalTask.Id}");
                externalTaskService.Complete(workerId, externalTask.Id, resultVariables);
            }
            catch (UnrecoverableBusinessErrorException ex)
            {
                logger.Warn($"...failed with business error code from External Task  {externalTask.Id}", ex);
                externalTaskService.Error(workerId, externalTask.Id, ex.BusinessErrorCode);
            }
            catch (Exception ex)
            {
                logger.Error($"...failed External Task  {externalTask.Id}", ex);
                var retriesLeft = taskWorkerInfo.Retries; // start with default
                if (externalTask.Retries.HasValue) // or decrement if retries are already set
                {
                    retriesLeft = externalTask.Retries.Value - 1;
                }
                externalTaskService.Failure(workerId, externalTask.Id, ex.Message, retriesLeft, taskWorkerInfo.RetryTimeout);
            }
        }

    }
}
