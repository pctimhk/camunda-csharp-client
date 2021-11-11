using System.Collections.Generic;

namespace CamundaClientLibrary.Dto
{
    public class ProcessInstance
    {
        public string Id { get; set; }
        public string BusinessKey { get; set; }

        public override string ToString() => $"ProcessInstance [Id={Id}, BusinessKey={BusinessKey}]";
    }

}
