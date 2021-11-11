using NUnit.Framework;

namespace CamundaClientLibrary.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestStartupWithSingleThreadPolling()
        {
            var camundaEngineClient = new CamundaEngineClient();
            camundaEngineClient.StartupWithSingleThreadPolling();

        }
    }
}