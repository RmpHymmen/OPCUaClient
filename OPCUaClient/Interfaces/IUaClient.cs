using Opc.Ua.Client;
using OPCUaClient.Objects;

namespace OPCUaClient.Interfaces
{
    public interface IUaClient
    {
        ///
        /// Zusammenfassung:
        ///     Indicates if the instance is connected to the server.
        bool IsConnected { get; }

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
        void Connect(uint timeOut = 5u, bool keepAlive = false);

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
        Task ConnectAsync(uint timeOut = 5u, bool keepAlive = false);

        ///
        /// Zusammenfassung:
        ///     Close the connection with the OPC UA Server
        void Disconnect();

        ///
        /// Zusammenfassung:
        ///     Close the connection with the OPC UA Server
        Task DisconnectAsync();

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
        void Write(string address, object value, ushort namespaceID);

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
        void Write(Tag tag, ushort namespaceID);

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
        Tag Read(string address, ushort namespaceID);

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
        ///     If the status of read action is not good StatusCodes
        ///
        ///   T:System.NotSupportedException:
        ///     If the type is not supported.
        TValue Read<TValue>(string address, ushort namespaceID);

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
        void Write(List<Tag> tags, ushort namespaceID);

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
        List<Tag> Read(List<string> address, ushort namespaceID);

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
        void Monitoring(string address, int miliseconds, ushort namespaceID, MonitoredItemNotificationEventHandler monitor);

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
        List<Device> Devices(ushort namespaceID, bool recursive = false);

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
        List<Group> Groups(string address, ushort namespaceID, bool recursive = false);

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
        List<Tag> Tags(string address, ushort namespaceID);

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
        Task<List<Device>> DevicesAsync(ushort namespaceID, bool recursive = false);

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
        Task<List<Group>> GroupsAsync(string address, ushort namespaceID, bool recursive = false);

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
        Task<List<Tag>> TagsAsync(string address, ushort namespaceID);

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
        Task<Tag> WriteAsync(string address, object value, ushort namespaceID);

        ///
        /// Zusammenfassung:
        ///     Write a value on a tag
        ///
        /// Parameter:
        ///   tag:
        ///     OPCUaClient.Objects.Tag
        Task<Tag> WriteAsync(Tag tag, ushort namespaceID);

        ///
        /// Zusammenfassung:
        ///     Write a lis of values
        ///
        /// Parameter:
        ///   tags:
        ///     OPCUaClient.Objects.Tag
        Task<List<Tag>> WriteAsync(List<Tag> tags, ushort namespaceID);

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
        Task<Tag> ReadAsync(string address, ushort namespaceID);

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
        ///     If the status of read action is not good StatusCodes
        ///
        ///   T:System.NotSupportedException:
        ///     If the type is not supported.
        Task<TValue> ReadAsync<TValue>(string address, ushort namespaceID);

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
        Task<List<Tag>> ReadAsync(List<string> address, ushort namespaceID);
    }
}