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

        public DiscordVoiceListener Context
        {
            get => _context;
            set
            {
                UnregisterVoiceEventCallback(_context);
                _context = value;
                RegisterVoiceEventCallback(_context);
            }
        }
        public long UserId {  get; set; }
        private List<VoiceReceiveEvent> _userVoiceEvents = new();
        private DiscordVoiceListener _context;

        private void Awake()
        {
            Debug.Assert(Username != null);
            Debug.Assert(Visualizer != null);
        }

        private void Start()
        {
            Debug.Assert(Context != null);
            Visualizer.TimeWindow = Context.DisplayWindowInSeconds;
        }

        private void Update()
        {
            Visualizer.Models = _userVoiceEvents;
        }

        private void RegisterVoiceEventCallback(DiscordVoiceListener context)
        {
            if (context == null)
            {
                return;
            }
            context.OnVoiceReceive += VoiceEventCallback;
        }

        private void UnregisterVoiceEventCallback(DiscordVoiceListener context)
        {
            if (context == null)
            {
                return;
            }
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
