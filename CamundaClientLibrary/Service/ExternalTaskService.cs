using CamundaClientLibrary.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using CamundaClientLibrary.Requests;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using System.Threading;
using CamundaClientLibrary.DTO;
using Common.Logging;

namespace CamundaClientLibrary.Service
{

    public class ExternalTaskService
    {

        private ILog logger = LogManager.GetLogger(typeof(ExternalTaskService));

        private CamundaClientHelper helper;

        public ExternalTaskService(CamundaClientHelper client)
        {
            this.helper = client;
        }

        public bool GetServerStatus()
        {
            var http = helper.HttpClient();
            var response = http.GetAsync(helper.RestUrl + "engine").Result;
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public IList<PendingExternalTask> GetPendingExternalTasks
            (string externalTaskId = null, string topicName = null, string workerId= null, 
            bool? locked = null, bool? notLocked = null, bool? withRetriesLeft = null, bool? noRetriesLeft = null, 
            DateTime? lockExpirationAfter = null, DateTime? lockExpirationBefore = null, string activityId = null, 
            string executionId = null, string processInstanceId = null, string processDefinitionId = null, 
            string tenantIdIn = null, bool? active = null, bool? suspended = null, long? priorityHigherThanOrEquals = null, 
            long? priorityLowerThanOrEquals = null)
        {
            var request = new PendingExternalTasksRequest
            {
                externalTaskId = externalTaskId,
                topicName = topicName,
                workerId = workerId,
                locked = locked,
                notLocked = notLocked,
                withRetriesLeft = withRetriesLeft,
                noRetriesLeft = noRetriesLeft,
                lockExpirationAfter = lockExpirationAfter,
                lockExpirationBefore = lockExpirationBefore,
                activityId = activityId,
                executionId = executionId,
                processInstanceId = processInstanceId,
                processDefinitionId = processDefinitionId,
                tenantIdIn = tenantIdIn,
                active = active,
                suspended = suspended,
                priorityHigherThanOrEquals = priorityHigherThanOrEquals,
                priorityLowerThanOrEquals = priorityLowerThanOrEquals,
            };

            return this.GetPendingExternalTasks(request);
        }

        public IList<PendingExternalTask> GetPendingExternalTasks(PendingExternalTasksRequest request)
        {
            var http = helper.HttpClient();
            var requestContent = new StringContent(JsonConvert.SerializeObject(request, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }), Encoding.UTF8, CamundaClientHelper.CONTENT_TYPE_JSON);
            var response = http.PostAsync(helper.RestUrl + "external-task", requestContent).Result;
            if (response.IsSuccessStatusCode)
            {
                var tasks = JsonConvert.DeserializeObject<IEnumerable<PendingExternalTask>>(response.Content.ReadAsStringAsync().Result);
                return new List<PendingExternalTask>(tasks);
            }
            else
            {
                throw new EngineException("Could not get pending external tasks: " + response.ReasonPhrase + " and the http status code return " + response.StatusCode);
            }
        }

        public IList<ExternalTask> FetchAndLockTasks(string workerId, int maxTasks, string topicName, long lockDurationInMilliseconds, IEnumerable<string> variablesToFetch = null)
        { 
            return FetchAndLockTasks(workerId, maxTasks, new List<string> { topicName }, lockDurationInMilliseconds, variablesToFetch);
        }

        public IList<ExternalTask> FetchAndLockTasks(string workerId, int maxTasks, IEnumerable<string> topicNames, long lockDurationInMilliseconds, IEnumerable<string> variablesToFetch = null)
        {
            var lockRequest = new FetchAndLockRequest
            {
                WorkerId = workerId,
                MaxTasks = maxTasks
            };
            //if (longPolling)
            //{
            //    lockRequest.AsyncResponseTimeout = 1 * 60 * 1000; // 1 minute
            //}
            foreach (var topicName in topicNames)
            {
                var lockTopic = new FetchAndLockTopic
                {
                    TopicName = topicName,
                    LockDuration = lockDurationInMilliseconds,
                    Variables = variablesToFetch
                };
                lockRequest.Topics.Add(lockTopic);
            }

            return FetchAndLockTasks(lockRequest);
        }

        public IList<ExternalTask> FetchAndLockTasks(FetchAndLockRequest fetchAndLockRequest)
        {
            var http = helper.HttpClient();
            try
            {
                var requestContent = new StringContent(JsonConvert.SerializeObject(fetchAndLockRequest, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }), Encoding.UTF8, CamundaClientHelper.CONTENT_TYPE_JSON);
                var response = http.PostAsync("external-task/fetchAndLock", requestContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    var tasks = JsonConvert.DeserializeObject<IEnumerable<ExternalTask>>(response.Content.ReadAsStringAsync().Result);
                    return new List<ExternalTask>(tasks);
                }
                else
                {
                    throw new EngineException("Could not fetch and lock tasks: " + response.ReasonPhrase + " and the http status code return " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // TODO: Handle Exception, add back off
                logger.Error(ex);
                throw;
            }
        }    

        public void Complete(string workerId, string externalTaskId, Dictionary<string, object> variablesToPassToProcess = null)
        {
            var http = helper.HttpClient();

            var request = new CompleteRequest();
            request.WorkerId = workerId;
            request.Variables = CamundaClientHelper.ConvertVariables(variablesToPassToProcess);

            var requestContent = new StringContent(JsonConvert.SerializeObject(request, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }), Encoding.UTF8, CamundaClientHelper.CONTENT_TYPE_JSON);
            var response = http.PostAsync("external-task/" + externalTaskId + "/complete", requestContent).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new EngineException("Could not complete external Task: " + response.ReasonPhrase + " and the http status code return " + response.StatusCode);
            }
        }

        public void Error(string workerId, string externalTaskId, string errorCode)
        {
            var http = helper.HttpClient();

            var request = new BpmnErrorRequest();
            request.WorkerId = workerId;
            request.ErrorCode = errorCode;

            var requestContent = new StringContent(JsonConvert.SerializeObject(request, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }), Encoding.UTF8, CamundaClientHelper.CONTENT_TYPE_JSON);
            var response = http.PostAsync("external-task/" + externalTaskId + "/bpmnError", requestContent).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new EngineException("Could not report BPMN error for external Task: " + response.ReasonPhrase + " and the http status code return " + response.StatusCode);
            }
        }

        public void Failure(string workerId, string externalTaskId, string errorMessage, int retries, long retryTimeout)
        {
            var http = helper.HttpClient();

            var request = new FailureRequest();
            request.WorkerId = workerId;
            request.ErrorMessage = errorMessage;
            request.Retries = retries;
            request.RetryTimeout = retryTimeout;

            var requestContent = new StringContent(JsonConvert.SerializeObject(request, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }), Encoding.UTF8, CamundaClientHelper.CONTENT_TYPE_JSON);
            var response = http.PostAsync("external-task/" + externalTaskId + "/failure", requestContent).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new EngineException("Could not report failure for external Task: " + response.ReasonPhrase + " and the http status code return " + response.StatusCode);
            }
        }

    }
}
