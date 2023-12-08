using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OPCUaClient.Exceptions;
using OPCUaClient.Interfaces;
using OPCUaClient.Objects;
using System.Net;
using ObjectIds = Opc.Ua.ObjectIds;

namespace OPCUaClient
{
    ///
    /// Zusammenfassung:
    ///     Client for OPCUA Server
    public class UaClient : IUaClient
    {
        private readonly ConfiguredEndpoint _endpoint;

        private Session? _session = null;

        private readonly UserIdentity _userIdentity;

        private readonly ApplicationConfiguration _appConfig;

        private const int ReconnectPeriod = 10000;

        private readonly object _lock = new object();

        private SessionReconnectHandler? _reconnectHandler;

        ///
        /// Zusammenfassung:
        ///     Indicates if the instance is connected to the server.
        public bool IsConnected => _session?.Connected ?? false;

        private void KeepAlive(Session session, KeepAliveEventArgs e)
        {
            try
            {
                if (!ServiceResult.IsBad(e.Status))
                {
                    return;
                }

                lock (_lock)
                {
                    if (_reconnectHandler == null)
                    {
                        _reconnectHandler = new SessionReconnectHandler(reconnectAbort: true);
                        _reconnectHandler!.BeginReconnect(_session, ReconnectPeriod, Reconnect);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void Reconnect(object sender, EventArgs e)
        {
            if (sender != _reconnectHandler)
            {
                return;
            }

            lock (_lock)
            {
                if (_reconnectHandler!.Session != null)
                {
                    _session = _reconnectHandler!.Session;
                }

                _reconnectHandler!.Dispose();
                _reconnectHandler = null;
            }
        }

        private Subscription Subscription(int miliseconds)
        {
            return new Subscription
            {
                PublishingEnabled = true,
                PublishingInterval = miliseconds,
                Priority = 1,
                KeepAliveCount = 10u,
                LifetimeCount = 20u,
                MaxNotificationsPerPublish = 1000u
            };
        }

        ///
        /// Zusammenfassung:
        ///     Create a new instance
        ///
        /// Parameter:
        ///   appName:
        ///     Name of the application
        ///
        ///   serverUrl:
        ///     Url of server
        ///
        ///   security:
        ///     Enable or disable the security
        ///
        ///   untrusted:
        ///     Accept untrusted certificates
        ///
        ///   user:
        ///     User of the OPC UA Server
        ///
        ///   password:
        ///     Password of the user
        public UaClient(string appName, string serverUrl, bool security, bool untrusted, string user = "", string password = "")
        {
            string text = Path.Combine(Directory.GetCurrentDirectory(), "Certificates");
            Directory.CreateDirectory(text);
            Directory.CreateDirectory(Path.Combine(text, "Application"));
            Directory.CreateDirectory(Path.Combine(text, "Trusted"));
            Directory.CreateDirectory(Path.Combine(text, "TrustedPeer"));
            Directory.CreateDirectory(Path.Combine(text, "Rejected"));
            string hostName = Dns.GetHostName();
            if (user.Length > 0)
            {
                _userIdentity = new UserIdentity(user, password);
            }
            else
            {
                _userIdentity = new UserIdentity();
            }

            _appConfig = new ApplicationConfiguration
            {
                ApplicationName = appName,
                ApplicationUri = Utils.Format("urn:{0}" + appName, hostName),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(text, "Application"),
                        SubjectName = "CN=" + appName + ", DC=" + hostName
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(text, "Trusted")
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(text, "TrustedPeer")
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(text, "Rejected")
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true,
                    RejectSHA1SignedCertificates = false
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 20000
                },
                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 5000
                },
                TraceConfiguration = new TraceConfiguration
                {
                    DeleteOnLoad = true
                },
                DisableHiResClock = false
            };
            _appConfig.Validate(ApplicationType.Client).GetAwaiter().GetResult();
            if (_appConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                _appConfig.CertificateValidator.CertificateValidation += delegate (CertificateValidator s, CertificateValidationEventArgs ee)
                {
                    ee.Accept = ee.Error.StatusCode == 2149187584u && untrusted;
                };
            }

            ApplicationInstance applicationInstance = new ApplicationInstance
            {
                ApplicationName = appName,
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = _appConfig
            };
            Utils.SetTraceMask(0);
            applicationInstance.CheckApplicationInstanceCertificate(silent: true, 2048).GetAwaiter().GetResult();
            EndpointDescription description = CoreClientUtils.SelectEndpoint(_appConfig, serverUrl, security);
            EndpointConfiguration configuration = EndpointConfiguration.Create(_appConfig);
            _endpoint = new ConfiguredEndpoint(null, description, configuration);
        }

        ///
        /// Zusammenfassung:
        ///     Open the connection with the OPC UA Server
        ///
        /// Parameter:
        ///   timeOut:
        ///     Timeout to try to connect with the server in seconds.
        ///
        ///   keepAlive:
        ///     Sets whether to try to connect to the server in case the connection is lost.
        ///
        /// Ausnahmen:
        ///   T:OPCUaClient.Exceptions.ServerException:
        public void Connect(uint timeOut = 5u, bool keepAlive = false)
        {
            Disconnect();
            _session = Task.Run(async () => await Session.Create(_appConfig, _endpoint, updateBeforeConnect: false, checkDomain: false, _appConfig.ApplicationName, timeOut * 1000, _userIdentity, null)).GetAwaiter().GetResult();
            if (keepAlive)
            {
                _session!.KeepAlive += KeepAlive;
            }

            if (_session == null || !_session!.Connected)
            {
                throw new ServerException("Error creating a session on the server");
            }
        }

        ///
        /// Zusammenfassung:
        ///     Open the connection with the OPC UA Server
        ///
        /// Parameter:
        ///   timeOut:
        ///     Timeout to try to connect with the server in seconds.
        ///
        ///   keepAlive:
        ///     Sets whether to try to connect to the server in case the connection is lost.
        ///
        /// Ausnahmen:
        ///   T:OPCUaClient.Exceptions.ServerException:
        public async Task ConnectAsync(uint timeOut = 5u, bool keepAlive = false)
        {
            await DisconnectAsync();
            _session = await Session.Create(_appConfig, _endpoint, updateBeforeConnect: false, checkDomain: false, _appConfig.ApplicationName, timeOut * 1000, _userIdentity, null);
            if (keepAlive)
            {
                Session session = _session;
                if (session != null)
                {
                    session.KeepAlive += KeepAlive;
                }
            }

            if (_session == null || !_session!.Connected)
            {
                throw new ServerException("Error creating a session on the server");
            }
        }

        ///
        /// Zusammenfassung:
        ///     Close the connection with the OPC UA Server
        public void Disconnect()
        {
            if (!(_session?.Connected ?? false))
            {
                return;
            }

            if (_session!.Subscriptions != null && _session!.Subscriptions.Any())
            {
                foreach (Subscription subscription in _session!.Subscriptions)
                {
                    subscription.Delete(silent: true);
                }
            }

            _session!.Close();
            _session!.Dispose();
            _session = null;
        }

        ///
        /// Zusammenfassung:
        ///     Close the connection with the OPC UA Server
        public Task DisconnectAsync()
        {
            if (_session?.Connected ?? false)
            {
                if (_session!.Subscriptions != null && _session!.Subscriptions.Any())
                {
                    foreach (Subscription subscription in _session!.Subscriptions)
                    {
                        subscription.Delete(silent: true);
                    }
                }

                _session!.Close();
                _session!.Dispose();
                _session = null;
            }

            return Task.CompletedTask;
        }

        ///
        /// Zusammenfassung:
        ///     Write a value on a tag
        ///
        /// Parameter:
        ///   address:
        ///     Address of the tag
        ///
        ///   value:
        ///     Value to write
        ///
        /// Ausnahmen:
        ///   T:OPCUaClient.Exceptions.WriteException:
        public void Write(string address, object value, ushort namespaceID)
        {
            WriteValueCollection writeValueCollection = new WriteValueCollection();
            WriteValue writeValue = new WriteValue
            {
                NodeId = new NodeId(address, namespaceID),
                AttributeId = 13u,
                Value = new DataValue()
            };
            writeValue.Value.Value = value;
            writeValueCollection.Add(writeValue);
            _session!.Write(null, writeValueCollection, out var results, out var _);
            if (!StatusCode.IsGood(results[0]))
            {
                throw new WriteException("Error writing value. Code: " + results[0].Code);
            }
        }

        ///
        /// Zusammenfassung:
        ///     Write a value on a tag
        ///
        /// Parameter:
        ///   tag:
        ///     OPCUaClient.Objects.Tag
        ///
        /// Ausnahmen:
        ///   T:OPCUaClient.Exceptions.WriteException:
        public void Write(Tag tag, ushort namespaceID)
        {
            Write(tag.Address, tag.Value, namespaceID);
        }

        ///
        /// Zusammenfassung:
        ///     Read a tag of the sepecific address
        ///
        /// Parameter:
        ///   address:
        ///     Address of the tag
        ///
        /// Rückgabewerte:
        ///     OPCUaClient.Objects.Tag
        public Tag Read(string address, ushort namespaceID)
        {
            Tag tag = new Tag
            {
                Address = address,
                Value = null
            };
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId
                {
                    NodeId = new NodeId(address, namespaceID),
                    AttributeId = 13u
                }
            };
            _session!.Read(null, 0.0, TimestampsToReturn.Both, nodesToRead, out var results, out var _);
            tag.Value = results[0].Value;
            tag.Code = results[0].StatusCode;
            return tag;
        }

        ///
        /// Zusammenfassung:
        ///     Read an address
        ///
        /// Parameter:
        ///   address:
        ///     Address to read.
        ///
        /// Typparameter:
        ///   TValue:
        ///     Type of value to read.
        ///
        /// Ausnahmen:
        ///   T:OPCUaClient.Exceptions.ReadException:
        ///     If the status of read action is not good Opc.Ua.StatusCodes
        ///
        ///   T:System.NotSupportedException:
        ///     If the type is not supported.
        public TValue Read<TValue>(string address, ushort namespaceID)
        {
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId
                {
                    NodeId = new NodeId(address, namespaceID),
                    AttributeId = 13u
                }
            };
            _session!.Read(null, 0.0, TimestampsToReturn.Both, nodesToRead, out var results, out var _);
            if (results[0].StatusCode != 0u)
            {
                throw new ReadException(results[0].StatusCode.Code.ToString());
            }

            if (typeof(TValue) == typeof(bool))
            {
                return (TValue)(object)Convert.ToBoolean(results[0].Value);
            }

            if (typeof(TValue) == typeof(byte))
            {
                return (TValue)(object)Convert.ToByte(results[0].Value);
            }

            if (typeof(TValue) == typeof(ushort))
            {
                return (TValue)(object)Convert.ToUInt16(results[0].Value);
            }

            if (typeof(TValue) == typeof(uint))
            {
                return (TValue)(object)Convert.ToUInt32(results[0].Value);
            }

            if (typeof(TValue) == typeof(ulong))
            {
                return (TValue)(object)Convert.ToUInt64(results[0].Value);
            }

            if (typeof(TValue) == typeof(short))
            {
                return (TValue)(object)Convert.ToInt16(results[0].Value);
            }

            if (typeof(TValue) == typeof(int))
            {
                return (TValue)(object)Convert.ToInt32(results[0].Value);
            }

            if (typeof(TValue) == typeof(long))
            {
                return (TValue)(object)Convert.ToInt64(results[0].Value);
            }

            if (typeof(TValue) == typeof(float))
            {
                return (TValue)(object)Convert.ToSingle(results[0].Value);
            }

            if (typeof(TValue) == typeof(double))
            {
                return (TValue)(object)Convert.ToDouble(results[0].Value);
            }

            if (typeof(TValue) == typeof(decimal))
            {
                return (TValue)(object)Convert.ToDecimal(results[0].Value);
            }

            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)Convert.ToString(results[0].Value);
            }

            throw new NotSupportedException();
        }

        ///
        /// Zusammenfassung:
        ///     Write a lis of values
        ///
        /// Parameter:
        ///   tags:
        ///     OPCUaClient.Objects.Tag
        ///
        /// Ausnahmen:
        ///   T:OPCUaClient.Exceptions.WriteException:
        public void Write(List<Tag> tags, ushort namespaceID)
        {
            WriteValueCollection writeValueCollection = new WriteValueCollection();
            writeValueCollection.AddRange(tags.Select((Tag tag) => new WriteValue
            {
                NodeId = new NodeId(tag.Address, namespaceID),
                AttributeId = 13u,
                Value = new DataValue
                {
                    Value = tag.Value
                }
            }));
            _session!.Write(null, writeValueCollection, out var results, out var _);
            if (results.Where((StatusCode sc) => !StatusCode.IsGood(sc)).Any())
            {
                throw new WriteException("Error writing value. Code: " + results.Where((StatusCode sc) => !StatusCode.IsGood(sc)).First().Code);
            }
        }

        ///
        /// Zusammenfassung:
        ///     Read a list of tags on the OPCUA Server
        ///
        /// Parameter:
        ///   address:
        ///     List of address to read.
        ///
        /// Rückgabewerte:
        ///     A list of tags OPCUaClient.Objects.Tag
        public List<Tag> Read(List<string> address, ushort namespaceID)
        {
            List<Tag> list = new List<Tag>();
            ReadValueIdCollection readValueIdCollection = new ReadValueIdCollection();
            readValueIdCollection.AddRange(address.Select((string a) => new ReadValueId
            {
                NodeId = new NodeId(a, namespaceID),
                AttributeId = 13u
            }));
            _session!.Read(null, 0.0, TimestampsToReturn.Both, readValueIdCollection, out var results, out var _);
            for (int i = 0; i < address.Count; i++)
            {
                list.Add(new Tag
                {
                    Address = address[i],
                    Value = results[i].Value,
                    Code = results[i].StatusCode
                });
            }

            return list;
        }

        ///
        /// Zusammenfassung:
        ///     Monitoring a tag and execute a function when the value change
        ///
        /// Parameter:
        ///   address:
        ///     Address of the tag
        ///
        ///   miliseconds:
        ///     Sets the time to check changes in the tag
        ///
        ///   monitor:
        ///     Function to execute when the value changes.
        public void Monitoring(string address, int miliseconds, ushort namespaceID, MonitoredItemNotificationEventHandler monitor)
        {
            Monitoring(address, miliseconds, namespaceID, monitor, null);
        }
        ///
        /// Zusammenfassung:
        ///     Monitoring a tag and execute a function when the value change
        ///
        /// Parameter:
        ///   address:
        ///     Address of the tag
        ///
        ///   miliseconds:
        ///     Sets the time to check changes in the tag
        ///
        ///   monitor:
        ///     Function to execute when the value changes.
        public void Monitoring(string address, int miliseconds, ushort namespaceID, MonitoredItemNotificationEventHandler monitor, object myObject)
        {
            Subscription subscription = Subscription(miliseconds);
            CustomMonitoredItem monitoredItem = new CustomMonitoredItem();
            monitoredItem.StartNodeId = new NodeId(address, namespaceID);
            monitoredItem.AttributeId = 13u;
            monitoredItem.Notification += monitor;
            monitoredItem.MyObject = myObject;
            subscription.AddItem(monitoredItem);
            _session!.AddSubscription(subscription);
            subscription.Create();
            subscription.ApplyChanges();
        }

        ///
        /// Zusammenfassung:
        ///     Scan root folder of OPC UA server and get all devices
        ///
        /// Parameter:
        ///   recursive:
        ///     Indicates whether to search within device groups
        ///
        /// Rückgabewerte:
        ///     List of OPCUaClient.Objects.Device
        public List<Device> Devices(ushort namespaceID, bool recursive = false)
        {
            var browser = new Browser(_session);
            browser.BrowseDirection = BrowseDirection.Forward;
            browser.NodeClassMask = 3;
            browser.ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;
            ReferenceDescriptionCollection source = browser.Browse(ObjectIds.ObjectsFolder);
            var list = (from d in source
                        where d.ToString() != "Server"
                        select d into b
                        select new Device
                        {
                            Address = b.ToString()
                        }).ToList();

            foreach (var ele in list)
            {
                ele.Groups = Groups(ele.Address, namespaceID, recursive);
                ele.Tags = Tags(ele.Address, namespaceID);
            }

            return list.Where(ele => ele.Tags.Count() > 0 || ele.Groups.Count() > 0).ToList();
        }

        ///
        /// Zusammenfassung:
        ///     Scan an address and retrieve the tags and groups
        ///
        /// Parameter:
        ///   address:
        ///     Address to search
        ///
        ///   recursive:
        ///     Indicates whether to search within group groups
        ///
        /// Rückgabewerte:
        ///     List of OPCUaClient.Objects.Group
        public List<Group> Groups(string address, ushort namespaceID, bool recursive = false)
        {
            List<Group> list = new List<Group>();
            Browser browser = new Browser(_session);
            browser.BrowseDirection = BrowseDirection.Forward;
            browser.NodeClassMask = 3;
            browser.ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;
            try
            {
                ReferenceDescriptionCollection referenceDescriptionCollection = browser.Browse(new NodeId(address, namespaceID));
                for (int i = 0; i < referenceDescriptionCollection.Count; i++)
                {
                    ReferenceDescription referenceDescription = referenceDescriptionCollection[i];
                    if (referenceDescription.NodeClass == NodeClass.Object)
                    {
                        Group group = new Group();
                        group.Address = address + "." + referenceDescription.ToString();
                        group.Groups = Groups(group.Address, namespaceID, recursive);
                        group.Tags = Tags(group.Address, namespaceID);
                        list.Add(group);
                    }
                }
            }
            catch
            {
                return list;
            }

            return list;
        }

        ///
        /// Zusammenfassung:
        ///     Scan an address and retrieve the tags.
        ///
        /// Parameter:
        ///   address:
        ///     Address to search
        ///
        /// Rückgabewerte:
        ///     List of OPCUaClient.Objects.Tag
        public List<Tag> Tags(string address, ushort namespaceID)
        {
            List<Tag> list = new List<Tag>();
            Browser browser = new Browser(_session);
            browser.BrowseDirection = BrowseDirection.Forward;
            browser.NodeClassMask = 3;
            browser.ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;
            try
            {
                ReferenceDescriptionCollection referenceDescriptionCollection = browser.Browse(new NodeId(address, namespaceID));
                for (int i = 0; i < referenceDescriptionCollection.Count; i++)
                {
                    ReferenceDescription referenceDescription = referenceDescriptionCollection[i];
                    if (referenceDescription.NodeClass == NodeClass.Variable)
                    {
                        list.Add(new Tag
                        {
                            Address = address + "." + referenceDescription.ToString()
                        });
                    }
                }
            }
            catch
            {
                return list;
            }

            return list;
        }

        ///
        /// Zusammenfassung:
        ///     Scan root folder of OPC UA server and get all devices
        ///
        /// Parameter:
        ///   recursive:
        ///     Indicates whether to search within device groups
        ///
        /// Rückgabewerte:
        ///     List of OPCUaClient.Objects.Device
        public async Task<List<Device>> DevicesAsync(ushort namespaceID, bool recursive = false)
        {
            return await Task.Run(() => Devices(namespaceID, recursive));
        }

        ///
        /// Zusammenfassung:
        ///     Scan an address and retrieve the tags and groups
        ///
        /// Parameter:
        ///   address:
        ///     Address to search
        ///
        ///   recursive:
        ///     Indicates whether to search within group groups
        ///
        /// Rückgabewerte:
        ///     List of OPCUaClient.Objects.Group
        public async Task<List<Group>> GroupsAsync(string address, ushort namespaceID, bool recursive = false)
        {
            return await Task.Run(() => Groups(address, namespaceID, recursive));
        }

        ///
        /// Zusammenfassung:
        ///     Scan an address and retrieve the tags.
        ///
        /// Parameter:
        ///   address:
        ///     Address to search
        ///
        /// Rückgabewerte:
        ///     List of OPCUaClient.Objects.Tag
        public async Task<List<Tag>> TagsAsync(string address, ushort namespaceID)
        {
            return await Task.Run(() => Tags(address, namespaceID));
        }

        ///
        /// Zusammenfassung:
        ///     Write a value on a tag
        ///
        /// Parameter:
        ///   address:
        ///     Address of the tag
        ///
        ///   value:
        ///     Value to write
        public async Task<Tag> WriteAsync(string address, object value, ushort namespaceID)
        {
            WriteValueCollection writeValues = new WriteValueCollection();
            WriteValue writeValue = new WriteValue
            {
                NodeId = new NodeId(address, namespaceID),
                AttributeId = 13u,
                Value = new DataValue()
            };
            writeValue.Value.Value = value;
            writeValues.Add(writeValue);
            WriteResponse response = await _session!.WriteAsync(null, writeValues, default(CancellationToken));
            return new Tag
            {
                Address = address,
                Value = value,
                Code = response.Results[0].Code
            };
        }

        ///
        /// Zusammenfassung:
        ///     Write a value on a tag
        ///
        /// Parameter:
        ///   tag:
        ///     OPCUaClient.Objects.Tag
        public async Task<Tag> WriteAsync(Tag tag, ushort namespaceID)
        {
            return await WriteAsync(tag.Address, tag.Value, namespaceID);
        }

        ///
        /// Zusammenfassung:
        ///     Write a lis of values
        ///
        /// Parameter:
        ///   tags:
        ///     OPCUaClient.Objects.Tag
        public async Task<List<Tag>> WriteAsync(List<Tag> tags, ushort namespaceID)
        {
            WriteValueCollection writeValues = new WriteValueCollection();
            writeValues.AddRange(tags.Select((Tag tag) => new WriteValue
            {
                NodeId = new NodeId(tag.Address, namespaceID),
                AttributeId = 13u,
                Value = new DataValue
                {
                    Value = tag.Value
                }
            }));
            WriteResponse response = await _session!.WriteAsync(null, writeValues, default(CancellationToken));
            for (int i = 0; i < response.Results.Count; i++)
            {
                tags[i].Code = response.Results[i].Code;
            }

            return tags;
        }

        ///
        /// Zusammenfassung:
        ///     Read a tag of the sepecific address
        ///
        /// Parameter:
        ///   address:
        ///     Address of the tag
        ///
        /// Rückgabewerte:
        ///     OPCUaClient.Objects.Tag
        public async Task<Tag> ReadAsync(string address, ushort namespaceID)
        {
            Tag tag = new Tag
            {
                Address = address,
                Value = null
            };
            ReadValueIdCollection readValues = new ReadValueIdCollection
            {
                new ReadValueId
                {
                    NodeId = new NodeId(address, namespaceID),
                    AttributeId = 13u
                }
            };
            ReadResponse dataValues = await _session!.ReadAsync(null, 0.0, TimestampsToReturn.Both, readValues, default(CancellationToken));
            tag.Value = dataValues.Results[0].Value;
            tag.Code = dataValues.Results[0].StatusCode;
            return tag;
        }

        ///
        /// Zusammenfassung:
        ///     Read an address
        ///
        /// Parameter:
        ///   address:
        ///     Address to read.
        ///
        /// Typparameter:
        ///   TValue:
        ///     Type of value to read.
        ///
        /// Ausnahmen:
        ///   T:OPCUaClient.Exceptions.ReadException:
        ///     If the status of read action is not good Opc.Ua.StatusCodes
        ///
        ///   T:System.NotSupportedException:
        ///     If the type is not supported.
        public Task<TValue> ReadAsync<TValue>(string address, ushort namespaceID)
        {
            return Task.Run(() => Read<TValue>(address, namespaceID));
        }

        ///
        /// Zusammenfassung:
        ///     Read a list of tags on the OPCUA Server
        ///
        /// Parameter:
        ///   address:
        ///     List of address to read.
        ///
        /// Rückgabewerte:
        ///     A list of tags OPCUaClient.Objects.Tag
        public async Task<List<Tag>> ReadAsync(List<(string, ushort)> address)
        {
            List<Tag> tags = new List<Tag>();
            ReadValueIdCollection readValues = new ReadValueIdCollection();
            readValues.AddRange(address.Select(((string, ushort) a) => new ReadValueId
            {
                NodeId = new NodeId(a.Item1, a.Item2),
                AttributeId = 13u
            }));
            ReadResponse dataValues = await _session!.ReadAsync(null, 0.0, TimestampsToReturn.Both, readValues, default(CancellationToken));
            for (int i = 0; i < dataValues.Results.Count; i++)
            {
                tags.Add(new Tag
                {
                    Address = address[i].Item1,
                    Value = dataValues.Results[i].Value,
                    Code = dataValues.Results[i].StatusCode
                });
            }

            return tags;
        }
    }
}