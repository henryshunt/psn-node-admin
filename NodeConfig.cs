using Newtonsoft.Json;
using System;

namespace psn_node_admin
{
    public class NodeConfig
    {
        [JsonProperty("id")]
        public string Identifier { get; set; }

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
        public UInt16 LoggerPort { get; set; }

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
            string json = "{ \"nent\": " + Convert.ToInt16(IsEnterpriseNetwork)
                + ", \"nnam\": \"" + NetworkName + "\", \"nunm\": \"" + NetworkUsername
                + "\", \"npwd\": \"" + NetworkPassword + "\", \"ladr\": \""
                + LoggerAddress + "\", \"lprt\": " + LoggerPort + ", \"tncn\": "
                + NetworkConnectTimeout + ", \"tlcn\": " + LoggerConnectTimeout
                + ", \"tlsb\": " + LoggerSubscribeTimeout + ", \"tlss\": "
                + LoggerSessionTimeout + ", \"tlrp\": " + LoggerReportTimeout + " }";

            json = json.Replace("\"\"", "null");
            return json;
        }
    }
}
