using CamundaClientLibrary.Dto;
using CamundaClientLibrary.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaClientLibrary.Test.Workers
{
    [ExternalTask("Process_CamundaEngineLibrary.Test", "Activity_A")]
    public class TaskCAdapter : IExternalTaskAdapter
    {

        public void Execute(ExternalTask externalTask, ref Dictionary<string, object> resultVariables)
        {
            resultVariables.Add("Task C Result", DateTime.Today.ToLongTimeString());
        }
    }
}
