using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;

namespace CamundaClientLibrary.Test
{
    public class Tests
    {

        public CamundaEngineClient camundaEngineClient { get; set; }

        [SetUp]
        public void Setup()
        {
            camundaEngineClient = new CamundaEngineClient(new System.Uri(@"http://192.168.17.158:29090//engine-rest/engine/default/"), null, null);

            // deploy BPMN
            //camundaEngineClient.RepositoryService.Deploy(@"CamundaEngineLibraryTest", @"BPMN\CamundaEngineLibraryTest.bpmn");
        }

        [Test]
        public void TestStartupWithSingleThreadPolling()
        {
            camundaEngineClient.StartupWithSingleThreadPolling();
        }
    }
}