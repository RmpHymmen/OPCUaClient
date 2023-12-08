using Opc.Ua.Client;

namespace OPCUaClient.Objects
{
    public class CustomMonitoredItem : MonitoredItem
    {
        public object MyObject { get; set; }
    }
}
