using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using VoiceMeter.Discord;
using Debug = UnityEngine.Debug;

namespace VoiceMeter
{
    public class UserStreamDisplay : MonoBehaviour
    {
        [field: SerializeField][UsedImplicitly] public TextMeshProUGUI Username { get; set; }
        [field: SerializeField][UsedImplicitly] public StreamVisualizer Visualizer { get; set; }
        [SerializeField] private StreamSegment _streamSegmentPrefab;
        
        private const float GapIntervalThresholdInMillis = 25f;
        private Queue<VoiceReceiveEvent> _voiceReceiveEventQueue = new();

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
            Debug.Assert(_streamSegmentPrefab != null);
        }

        private void Start()
        {
            Debug.Assert(Context != null);
            Visualizer.TimeWindow = Context.DisplayWindowInSeconds;
        }

        private void Update()
        {
            ProcessVoiceReceiveEventQueue();
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
            _voiceReceiveEventQueue.Enqueue(model);
        }

        private void ProcessVoiceReceiveEventQueue()
        {
            while (_voiceReceiveEventQueue.Count > 0)
            {
                VoiceReceiveEvent model = _voiceReceiveEventQueue.Dequeue();
                if (model.UserId != UserId)
                {
                    return;
                }
            
                DateTime modelEnd = model.TimeStamp + TimeSpan.FromMilliseconds(20f);            

                if (Visualizer.StreamSegments.Count == 0)
                {
                    Visualizer.GenerateSegment(_streamSegmentPrefab, new StreamSegmentModel
                    {
                        Start = model.TimeStamp,
                        End = modelEnd
                    });
                    return;
                }
            
                StreamSegment lastSegment = Visualizer.StreamSegments.Last();
            
                TimeSpan gapInterval = model.TimeStamp - lastSegment.EndTime;
                if (gapInterval.TotalMilliseconds <= GapIntervalThresholdInMillis)
                {
                    Visualizer.StitchLastSegment(new StreamSegmentModel
                    {
                        Start = lastSegment.StartTime,
                        End = modelEnd
                    });
                }
                else
                {
                    Visualizer.GenerateSegment(_streamSegmentPrefab, new StreamSegmentModel
                    {
                        Start = model.TimeStamp,
                        End = modelEnd
                    });
                }
            }
        }
    }
}
