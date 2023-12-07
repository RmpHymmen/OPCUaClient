namespace Test
{
    public class Server
    {
        [Test]
        public void Connection()
        {

            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            client.Disconnect();
            Assert.Pass();
        }

        [Test]
        public void FullScan()
        {
            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            var devices = client.Devices(2, true);
            Assert.AreEqual(2, devices.Count);
            Assert.AreEqual(3, devices[0].Groups.Count);
            Assert.AreEqual("NexusMeter", devices[0].Name);
            Assert.AreEqual(1022, devices[0].Tags.Count);
        }

        [Test]
        public void Devices()
        {
            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            var devices = client.Devices(2);

            Assert.AreEqual(2, devices.Count);
            client.Disconnect();
        }

        [Test]
        public void Groups()
        {
            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            var groups = client.Groups("NexusMeter", 2, false);

            Assert.AreEqual(3, groups.Count);
            client.Disconnect();
        }


        [Test]
        public void Tags()
        {
            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);

            var tags = client.Tags("NexusMeter", 2);
            Assert.AreEqual(1022, tags.Count);

            tags = client.Tags("NexusMeter.Test", 2);
            Assert.AreEqual(6, tags.Count);
            
            client.Disconnect();
        }
        
       

        
        [Test]
        public async Task DevicesAsync()
        {
            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            var devices =  client.DevicesAsync(2);

            Assert.AreEqual(2, (await devices).Count);
            client.Disconnect();
        }

        [Test]
        public async Task GroupsAsync()
        {
            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            var groups = await client.GroupsAsync("NexusMeter", 2, false);

            Assert.AreEqual(3, groups.Count);
            client.Disconnect();
        }

        [Test]
        public async Task TagsAsync()
        {
            UaClient client = new UaClient("testingConect", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);

            var tags = await client.TagsAsync("NexusMeter", 2);
            Assert.AreEqual(1022, tags.Count);

            tags = client.Tags("NexusMeter.Test", 2);
            Assert.AreEqual(6, tags.Count);
            
            client.Disconnect();
        }
    }
}
