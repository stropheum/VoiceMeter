using System;
using Newtonsoft.Json;

namespace VoiceMeter.Discord
{
    [Serializable]
    public class VoiceReceiveEvent
    {
        [JsonProperty] public DateTime TimeStamp { get; set; }
        [JsonProperty] public User User { get; set; }
        [JsonProperty] public int Ssrc { get; set; }
        [JsonProperty] public long UserId { get; set; }
        [JsonProperty] public string Frame { get; set; }

        public VoiceReceiveEvent(VoiceReceiveEvent rhs)
        {
            if (rhs == null || rhs == this)
            {
                return;
            }
            
            TimeStamp = rhs.TimeStamp;
            User = rhs.User;
            Ssrc = rhs.Ssrc;
            UserId = rhs.UserId;
            Frame = rhs.Frame;
        }
    }
    
    [Serializable]
    public class User
    {
        [JsonProperty] public long Id { get; set; }
        [JsonProperty] public string Username { get; set; }
        [JsonProperty] public int Discriminator { get; set; }
        [JsonProperty] public string AvatarHash { get; set; }
        [JsonProperty] public bool IsBot { get; set; }
        [JsonProperty] public object IsSystemUser { get; set; }
        [JsonProperty] public object MfaEnabled { get; set; }
        [JsonProperty] public object BannerHash { get; set; }
        [JsonProperty] public AccentColor AccentColor { get; set; }
        [JsonProperty] public object Locale { get; set; }
        [JsonProperty] public object Verified { get; set; }
        [JsonProperty] public object Email { get; set; }
        [JsonProperty] public int Flags { get; set; }
        [JsonProperty] public object PremiumType { get; set; }
        [JsonProperty] public int PublicFlags { get; set; }
        [JsonProperty] public object AvatarDecorationHash { get; set; }
        [JsonProperty] public bool HasAvatar { get; set; }
        [JsonProperty] public bool HasBanner { get; set; }
        [JsonProperty] public bool HasAvatarDecoration { get; set; }
        [JsonProperty] public DefaultAvatarUrl DefaultAvatarUrl { get; set; }
    }
    
    [Serializable]
    public class AccentColor
    {
        [JsonProperty] public int RawValue { get; set; }
        [JsonProperty] public int Red { get; set; }
        [JsonProperty] public int Green { get; set; }
        [JsonProperty] public int Blue { get; set; }
    }

    [Serializable]
    public class DefaultAvatarUrl
    {
    }
}