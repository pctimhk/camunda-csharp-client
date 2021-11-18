using CamundaClientLibrary;
using System;

namespace CamundaClientConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start the camunda client to listen the external task for the execution.");
            var camundaEngineClient = new CamundaEngineClient(new System.Uri(@"http://192.168.17.158:19090/engine-rest/engine/default/"), null, null, System.Reflection.Assembly.GetExecutingAssembly());
            camundaEngineClient.StartupWithSingleThreadPolling();
            Console.WriteLine("Press any key to stop the client");
            Console.ReadKey();
            camundaEngineClient.StopWorkerListener();
        }
    }
}
