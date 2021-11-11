using System;
using System.Collections.Generic;
using System.Text;

namespace CamundaClientLibrary.Requests
{
    public class PendingExternalTasksRequest
    {

        public string externalTaskId { get; set; }
        public string topicName { get; set; }
        public string workerId { get; set; }
        public bool? locked { get; set; }
        public bool? notLocked { get; set; }
        public bool? withRetriesLeft { get; set; }
        public bool? noRetriesLeft { get; set; }
        public DateTime? lockExpirationAfter { get; set; }
        public DateTime? lockExpirationBefore { get; set; }
        public string activityId { get; set; }
        public string executionId { get; set; }
        public string processInstanceId { get; set; }
        public string processDefinitionId { get; set; }
        public string tenantIdIn { get; set; }
        public bool? active { get; set; }
        public bool? suspended { get; set; }
        public long? priorityHigherThanOrEquals { get; set; }
        public long? priorityLowerThanOrEquals { get; set; }
        //public string sorting { get; set; }

    }
}
