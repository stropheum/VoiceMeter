using System;
using System.Linq;
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
        
        private const float GapIntervalThreshold = 25f;

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
            
            DateTime modelEnd = model.TimeStamp + TimeSpan.FromMilliseconds(20f);            

            if (Visualizer.Models.Count == 0)
            {
                Visualizer.Models.Add(new StreamSegmentModel
                {
                    Start = model.TimeStamp,
                    End = modelEnd
                });
                return;
            }

            StreamSegmentModel lastSegment = Visualizer.Models.Last();
            
            TimeSpan gapInterval = model.TimeStamp - lastSegment.End;
            if (gapInterval.TotalMilliseconds <= GapIntervalThreshold)
            {
                Visualizer.Models[^1] = new StreamSegmentModel
                {
                    Start = lastSegment.Start,
                    End = modelEnd
                };
            }
            else
            {
                //TODO: instead of just creating models that are all rendered by one script, instantiate individual segments, spawn them at the correct offset, and just animate them across until they exit the left side of the mask
                //TODO: Stop making shit so complicated
                Visualizer.Models.Add(new StreamSegmentModel
                {
                    Start = model.TimeStamp,
                    End = modelEnd
                });
            }
        }
    }
}
