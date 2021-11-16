namespace CamundaClientLibrary.Worker
{
    [System.AttributeUsage(System.AttributeTargets.Class |
                           System.AttributeTargets.Struct)
    ]
    public sealed class ExternalTaskAttribute : System.Attribute
    {
        public string ProcessId { get; }
        public string ActivityId { get; }
        public int Retries { get; } = 5; // default: 5 times
        public long RetryTimeout { get; } = 10 * 1000; // default: 10 seconds

        public ExternalTaskAttribute(string processId, string activityId)
        {
            ProcessId = processId;
            ActivityId = activityId;
        }

        public ExternalTaskAttribute(string processId, string activityId, int retries, long retryTimeout)
        {
            ProcessId = processId;
            ActivityId = activityId;
            Retries = retries;
            RetryTimeout = retryTimeout;
        }
    }
}
