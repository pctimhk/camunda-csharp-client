using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;

namespace CamundaClientLibrary.Test
{
    public class CamundaEngineClientTest
    {

        public CamundaEngineClient camundaEngineClient { get; set; }

        [SetUp]
        public void Setup()
        {
            camundaEngineClient = new CamundaEngineClient(new System.Uri(@"http://192.168.17.158:29090/engine-rest/engine/default/"), null, null, System.Reflection.Assembly.GetExecutingAssembly());

            // deploy BPMN
            //camundaEngineClient.RepositoryService.Deploy(@"CamundaEngineLibraryTest", @"BPMN\CamundaEngineLibraryTest.bpmn");
        }

        [Test]
        public void TestStartupWithSingleThreadPolling()
        {
            camundaEngineClient.StartupWithSingleThreadPolling();
            while (System.Diagnostics.Debugger.IsAttached)
            {
                // code or timeout value when running tests in debug mode
                System.Threading.Thread.Sleep(1000);
            }

            System.Threading.Thread.Sleep(30000);
        }
    }
}