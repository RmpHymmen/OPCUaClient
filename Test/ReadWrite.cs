namespace Test
{
    public class ReadWrite
    {
        private ushort _namespaceID = 2;

        [Test]
        public void Booolean()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);

            client.Write("NexusMeter.Test.Boolean", true, _namespaceID);
            var tag = client.Read("NexusMeter.Test.Boolean", _namespaceID);
            Assert.AreEqual(true, tag.Value);

            client.Write("NexusMeter.Test.Boolean", false, _namespaceID);
            tag = client.Read("NexusMeter.Test.Boolean", _namespaceID);
            Assert.AreEqual(false, tag.Value);

            client.Disconnect();

        }

        [Test]
        public void UInteger()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            UInt32 value = client.Read<UInt32>("NexusMeter.Test.UInteger", _namespaceID);
            client.Disconnect();
            Assert.That(value, Is.EqualTo(12337));
        }

        [Test]
        public void Integer()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            int value = new Random().Next(int.MinValue, int.MaxValue);
            client.Write("NexusMeter.Test.Integer", value, _namespaceID);
            var tag = client.Read("NexusMeter.Test.Integer", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            value = new Random().Next(int.MinValue, int.MaxValue);
            client.Write("NexusMeter.Test.Integer", value, _namespaceID);
            tag = client.Read("NexusMeter.Test.Integer", _namespaceID);
            int read = client.Read<int>("NexusMeter.Test.Integer", _namespaceID);
            Assert.AreEqual(value, tag.Value);
            Assert.AreEqual(value, read);

            client.Disconnect();
        }

        [Test]
        public void Double()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            double value = new Random().NextDouble();
            client.Write("NexusMeter.Test.Double", value, _namespaceID);
            var tag = client.Read("NexusMeter.Test.Double", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            value = new Random().NextDouble();
            client.Write("NexusMeter.Test.Double", value, _namespaceID);
            tag = client.Read("NexusMeter.Test.Double", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            client.Disconnect();
        }

        [Test]
        public void Float()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            float value = new Random().NextSingle();
            client.Write("NexusMeter.Test.Float", value, _namespaceID);
            var tag = client.Read("NexusMeter.Test.Float", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            value = new Random().NextSingle();
            client.Write("NexusMeter.Test.Float", value, _namespaceID);
            tag = client.Read("NexusMeter.Test.Float", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            client.Disconnect();
        }

        [Test]
        public void String()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            String value = "";

            while (value.Length < 10)
            {
                value += (char)new Random().Next(32, 166);
            }

            client.Write("NexusMeter.Test.String", value, _namespaceID);
            var tag = client.Read("NexusMeter.Test.String", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            while (value.Length < 10)
            {
                value += (char)new Random().Next(32, 166);
            }
            client.Write("NexusMeter.Test.String", value, _namespaceID);
            tag = client.Read("NexusMeter.Test.String", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            client.Disconnect();
        }


        [Test]
        public void Multiple()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            var values = new List<Tag>
            {
                new Tag
                {
                    Address = "NexusMeter.Test.Boolean",
                    Value = new Random().Next(0, _namespaceID) == 1
                },
                new Tag
                {
                    Address = "NexusMeter.Test.Double",
                    Value = new Random().NextDouble()
                },
                new Tag
                {
                    Address = "NexusMeter.Test.Float",
                    Value = new Random().NextSingle()
                },
                new Tag
                {
                    Address = "NexusMeter.Test.Integer",
                    Value = new Random().Next(int.MinValue, int.MaxValue)
                },
                new Tag
                {
                    Address = "NexusMeter.Test.String",
                    Value = "Hello, World!"
                }
            };

            client.Connect();

            client.Write(values, _namespaceID);

            var read = client.Read(values.Select(v => v.Address).ToList(), _namespaceID);
            Assert.AreEqual(values.Count, read.Count);

            for (int i = 0; i < read.Count; i++)
            {
                Assert.AreEqual(values[i].Value, read[i].Value);
            }
            client.Disconnect();
        }




        [Test]
        public async Task BoooleanAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);

            await client.WriteAsync("NexusMeter.Test.Boolean", true, _namespaceID);
            var tag = await client.ReadAsync("NexusMeter.Test.Boolean", _namespaceID);
            Assert.AreEqual(true, tag.Value);

            await client.WriteAsync("NexusMeter.Test.Boolean", false, _namespaceID);
            tag = await client.ReadAsync("NexusMeter.Test.Boolean", _namespaceID);
            Assert.AreEqual(false, tag.Value);

            client.Disconnect();

        }

        [Test]
        public async Task UIntegerAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            UInt32 value = await client.ReadAsync<UInt32>("NexusMeter.Test.UInteger", _namespaceID);
            client.Disconnect();
            Assert.That(value, Is.EqualTo(12337));
        }

        [Test]
        public async Task IntegerAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            int value = new Random().Next(int.MinValue, int.MaxValue);
            await client.WriteAsync("NexusMeter.Test.Integer", value, _namespaceID);
            var tag = await client.ReadAsync("NexusMeter.Test.Integer", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            value = new Random().Next(int.MinValue, int.MaxValue);
            client.Write("NexusMeter.Test.Integer", value, _namespaceID);
            tag = client.Read("NexusMeter.Test.Integer", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            client.Disconnect();
        }

        [Test]
        public async Task DoubleAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            double value = new Random().NextDouble();
            await client.WriteAsync("NexusMeter.Test.Double", value, _namespaceID);
            var tag = await client.ReadAsync("NexusMeter.Test.Double", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            value = new Random().NextDouble();
            await client.WriteAsync("NexusMeter.Test.Double", value, _namespaceID);
            tag = await client.ReadAsync("NexusMeter.Test.Double", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            client.Disconnect();
        }

        [Test]
        public async Task FloatAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            float value = new Random().NextSingle();
            await client.WriteAsync("NexusMeter.Test.Float", value, _namespaceID);
            var tag = await client.ReadAsync("NexusMeter.Test.Float", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            value = new Random().NextSingle();
            await client.WriteAsync("NexusMeter.Test.Float", value, _namespaceID);
            tag = await client.ReadAsync("NexusMeter.Test.Float", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            client.Disconnect();
        }

        [Test]
        public async Task StringAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            client.Connect(30);
            String value = "";

            while (value.Length < 10)
            {
                value += (char)new Random().Next(32, 166);
            }

            await client.WriteAsync("NexusMeter.Test.String", value, _namespaceID);
            var tag = await client.ReadAsync("NexusMeter.Test.String", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            while (value.Length < 10)
            {
                value += (char)new Random().Next(32, 166);
            }
            await client.WriteAsync("NexusMeter.Test.String", value, _namespaceID);
            tag = await client.ReadAsync("NexusMeter.Test.String", _namespaceID);
            Assert.AreEqual(value, tag.Value);

            client.Disconnect();
        }


        [Test]
        public async Task MultipleAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            var values = new List<Tag>
            {
                new Tag
                {
                    Address = "NexusMeter.Test.Boolean",
                    Value = new Random().Next(0, _namespaceID) == 1
                },
                new Tag
                {
                    Address = "NexusMeter.Test.Double",
                    Value = new Random().NextDouble()
                },
                new Tag
                {
                    Address = "NexusMeter.Test.Float",
                    Value = new Random().NextSingle()
                },
                new Tag
                {
                    Address = "NexusMeter.Test.Integer",
                    Value = new Random().Next(int.MinValue, int.MaxValue)
                },
                new Tag
                {
                    Address = "NexusMeter.Test.String",
                    Value = "Hello, World!"
                }
            };

            client.Connect();

            var result = await client.WriteAsync(values, _namespaceID);

            var read = await client.ReadAsync(values.Select(v => v.Address).ToList(), _namespaceID);
            Assert.AreEqual(values.Count, read.Count);

            for (int i = 0; i < read.Count; i++)
            {
                Assert.AreEqual(values[i].Value, read[i].Value);
            }
            client.Disconnect();
        }

        [Test]
        public async Task MultipleFailAsync()
        {
            UaClient client = new UaClient("testingRead", "opc.tcp://localhost:52240", true, true);
            var values = new List<Tag>();

            for (int i = 0; i < 1000; i++)
            {
                if (i < 10)
                {
                    values.Add(new Tag
                    {
                        Address = $"NexusMeter.Async.Tag00{i}",
                        Value = "Hello"
                    });
                }
                else if (i < 100)
                {
                    values.Add(new Tag
                    {
                        Address = $"NexusMeter.Async.Tag0{i}",
                        Value = "Hello"
                    });
                }
                else
                {
                    values.Add(new Tag
                    {
                        Address = $"NexusMeter.Async.Tag{i}",
                        Value = "Hello"
                    });
                }
            }

            client.Connect();

            var tags = await client.WriteAsync(values, _namespaceID);
            client.Disconnect();
            var tag = tags.Where(t => t.Name == "Tag989").First();
            Assert.IsFalse(tag.Quality);
        }
    }
}