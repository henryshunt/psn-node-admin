using Newtonsoft.Json;
using System;

namespace psn_node_admin
{
    public class NodeConfig
    {
        [JsonProperty("madr")]
        public string MACAddress { get; set; }

        [JsonProperty("nent")]
        public bool IsEnterpriseNetwork { get; set; }
        [JsonProperty("nnam")]
        public string NetworkName { get; set; }
        [JsonProperty("nunm")]
        public string NetworkUsername { get; set; }
        [JsonProperty("npwd")]
        public string NetworkPassword { get; set; }

        [JsonProperty("ladr")]
        public string LoggerAddress { get; set; }
        [JsonProperty("lprt")]
        public ushort LoggerPort { get; set; }

        [JsonProperty("tncn")]
        public byte NetworkConnectTimeout { get; set; }
        [JsonProperty("tlcn")]
        public byte LoggerConnectTimeout { get; set; }
        [JsonProperty("tlsb")]
        public byte LoggerSubscribeTimeout { get; set; }
        [JsonProperty("tlss")]
        public byte LoggerSessionTimeout { get; set; }
        [JsonProperty("tlrp")]
        public byte LoggerReportTimeout { get; set; }


        public override string ToString()
        {
            string jsonTemplate = "{{ \"nent\": {0}, \"nnam\": \"{1}\", \"nunm\": "
                + "\"{2}\", \"npwd\": \"{3}\", \"ladr\": \"{4}\", \"lprt\": {5}, "
                + "\"tncn\": {6}, \"tlcn\": {7}, \"tlsb\": {8}, \"tlss\": {9}, "
                + "\"tlrp\": {10} }}";

            string json = string.Format(jsonTemplate,
                Convert.ToInt16(IsEnterpriseNetwork), NetworkName, NetworkUsername,
                NetworkPassword, LoggerAddress, LoggerPort, NetworkConnectTimeout,
                LoggerConnectTimeout, LoggerSubscribeTimeout, LoggerSessionTimeout,
                LoggerReportTimeout);
            return json;
        }
    }
}
