using Newtonsoft.Json;

namespace PSNNodeAdmin.Routines
{
    internal class DeviceConfig
    {
        [JsonProperty("madr")]
        public string MACAddress { get; set; }

        [JsonProperty("nnam")]
        public string NetworkName { get; set; }
        [JsonProperty("nent")]
        public bool IsEnterpriseNetwork { get; set; }
        [JsonProperty("nunm")]
        public string NetworkUsername { get; set; }
        [JsonProperty("npwd")]
        public string NetworkPassword { get; set; }

        [JsonProperty("ladr")]
        public string LoggerAddress { get; set; }
        [JsonProperty("lprt")]
        public int LoggerPort { get; set; }

        [JsonProperty("tnet")]
        public int NetworkTimeout { get; set; }
        [JsonProperty("tlog")]
        public int LoggerTimeout { get; set; }


        public override string ToString()
        {
            string jsonTemplate = "{{\"nnam\":\"{0}\",\"nent\":{1},\"nunm\":\"{2}\"," +
                "\"npwd\":\"{3}\",\"ladr\":\"{4}\",\"lprt\":{5},\"tnet\":{6},\"tlog\":{7}}}";

            return string.Format(jsonTemplate, NetworkName, IsEnterpriseNetwork ? "true" : "false",
                NetworkUsername, NetworkPassword, LoggerAddress, LoggerPort, NetworkTimeout,
                LoggerTimeout);
        }
    }

    internal class DeviceTime
    {
        [JsonProperty("time")]
        public string Time { get; set; }
        [JsonProperty("tvld")]
        public bool IsTimeValid { get; set; }
    }
}
