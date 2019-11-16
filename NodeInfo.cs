using Newtonsoft.Json;

namespace psn_node_admin
{
    public class NodeInfo
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
        public int LoggerPort { get; set; }

        [JsonProperty("tncn")]
        public int NetworkConnectTimeout { get; set; }
        [JsonProperty("tlcn")]
        public int LoggerConnectTimeout { get; set; }
        [JsonProperty("tlsb")]
        public int LoggerSubscribeTimeout { get; set; }
        [JsonProperty("tlss")]
        public int LoggerSessionTimeout { get; set; }
        [JsonProperty("tlrp")]
        public int LoggerReportTimeout { get; set; }
    }
}
