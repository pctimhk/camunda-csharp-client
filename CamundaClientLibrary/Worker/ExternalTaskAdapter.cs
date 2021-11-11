using CamundaClientLibrary.Dto;
using System.Collections.Generic;

namespace CamundaClientLibrary.Worker
{

    public interface IExternalTaskAdapter
    {
        void Execute(ExternalTask externalTask, ref Dictionary<string, object> resultVariables);
    }


}
