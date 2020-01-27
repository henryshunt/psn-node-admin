using Newtonsoft.Json;
using System;

namespace PSNNodeAdmin.Routines
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

        [JsonProperty("tnet")]
        public byte NetworkTimeout { get; set; }
        [JsonProperty("tlog")]
        public byte LoggerTimeout { get; set; }


        public override string ToString()
        {
            string jsonTemplate = "{{ \"nent\": {0}, \"nnam\": \"{1}\", \"nunm\": "
                + "\"{2}\", \"npwd\": \"{3}\", \"ladr\": \"{4}\", \"lprt\": {5}, "
                + "\"tnet\": {6}, \"tlog\": {7} }}";

            string json = string.Format(jsonTemplate,
                Convert.ToInt16(IsEnterpriseNetwork), NetworkName, NetworkUsername,
                NetworkPassword, LoggerAddress, LoggerPort, NetworkTimeout,
                LoggerTimeout);
            return json;
        }
    }
}
