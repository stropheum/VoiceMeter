using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using VoiceMeter.Discord;

namespace VoiceMeter
{
    public class UserStreamDisplay : MonoBehaviour
    {
        [field: SerializeField][UsedImplicitly] public TextMeshProUGUI Username { get; set; }
        [field: SerializeField][UsedImplicitly] public StreamVisualizer Visualizer { get; set; }
        public long UserId {  get; set; }
        private List<VoiceReceiveEvent> _userVoiceEvents = new();

        private void Awake()
        {
            Debug.Assert(Username != null);
            Debug.Assert(Visualizer != null);
        }

        public void RegisterVoiceEventCallback(DiscordVoiceListener context)
        {
            context.OnVoiceReceive += VoiceEventCallback;
        }

        public void UnregisterVoiceEventCallback(DiscordVoiceListener context)
        {
            context.OnVoiceReceive -= VoiceEventCallback;
        }
        
        private void VoiceEventCallback(VoiceReceiveEvent model)
        {
            if (model.UserId != UserId)
            {
                return;
            }
            _userVoiceEvents.Add(model);
        }
    }
}
