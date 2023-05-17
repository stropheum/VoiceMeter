using System;
using Newtonsoft.Json;

namespace VoiceMeter.Discord
{
    [Serializable]
    public class MessageLogModel
    {
        [JsonProperty] public string Name { get; set; }
        [JsonProperty] public string Payload { get; set; }
    }
}