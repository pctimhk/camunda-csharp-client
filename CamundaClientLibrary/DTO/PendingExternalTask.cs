using System;
using System.Collections.Generic;
using System.Text;

namespace CamundaClientLibrary.DTO
{
    public class PendingExternalTask
    {

        public string activityId { get; set; }
        public string activityInstanceId { get; set; }
        public string errorMessage { get; set; }
        public string executionId { get; set; }
        public string id { get; set; }
        public string lockExpirationTime { get; set; }
        public string processDefinitionId { get; set; }
        public string processDefinitionKey { get; set; }
        public string processDefinitionVersionTag { get; set; }
        public string processInstanceId { get; set; }
        public string retries { get; set; }
        public bool suspended { get; set; }
        public string workerId { get; set; }
        public string topicName { get; set; }
        public string tenantId { get; set; }
        public int priority { get; set; }
        public string businessKey { get; set; }

    }
}
