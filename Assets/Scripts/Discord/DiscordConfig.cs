using UnityEngine;

namespace VoiceMeter.Discord
{
    [CreateAssetMenu(fileName = "DiscordConfig", menuName = "Discord Config", order = 100)]
    public class DiscordConfig : ScriptableObject
    {
        public ulong applicationId;
    }
}