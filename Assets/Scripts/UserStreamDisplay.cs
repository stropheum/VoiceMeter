using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            //TODO: move this stuff below into a helper method to process from the queue on update
            // if (model.UserId != UserId)
            // {
            //     return;
            // }
            //
            // DateTime modelEnd = model.TimeStamp + TimeSpan.FromMilliseconds(20f);            
            //
            // if (Visualizer.StreamSegments.Count == 0)
            // {
            //     // Visualizer.StreamSegments.Add(new StreamSegmentModel
            //     // {
            //     //     Start = model.TimeStamp,
            //     //     End = modelEnd
            //     // });
            //     Visualizer.GenerateSegment(new StreamSegmentModel
            //     {
            //         Start = model.TimeStamp,
            //         End = modelEnd
            //     });
            //     return;
            // }
            //
            // StreamSegment lastSegment = Visualizer.StreamSegments.Last();
            //
            // TimeSpan gapInterval = model.TimeStamp - lastSegment.Model.End;
            // if (gapInterval.TotalMilliseconds <= GapIntervalThresholdInMillis)
            // {
            //     Visualizer.StitchLastSegment(new StreamSegmentModel
            //     {
            //         Start = lastSegment.Model.Start,
            //         End = modelEnd
            //     });
            //     // Visualizer.StreamSegments[^1].Model = new StreamSegmentModel
            //     // {
            //     //     Start = lastSegment.Model.Start,
            //     //     End = modelEnd
            //     // };
            // }
            // else
            // {
            //     //TODO: instead of just creating models that are all rendered by one script, instantiate individual segments, spawn them at the correct offset, and just animate them across until they exit the left side of the mask
            //     //TODO: Stop making shit so complicated
            //     Visualizer.GenerateSegment(new StreamSegmentModel
            //     {
            //         Start = model.TimeStamp,
            //         End = modelEnd
            //     });
            // }
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
                    // Visualizer.StreamSegments.Add(new StreamSegmentModel
                    // {
                    //     Start = model.TimeStamp,
                    //     End = modelEnd
                    // });
                    Visualizer.GenerateSegment(_streamSegmentPrefab, new StreamSegmentModel
                    {
                        Start = model.TimeStamp,
                        End = modelEnd
                    });
                    return;
                }
            
                StreamSegment lastSegment = Visualizer.StreamSegments.Last();
            
                TimeSpan gapInterval = model.TimeStamp - lastSegment.Model.End;
                if (gapInterval.TotalMilliseconds <= GapIntervalThresholdInMillis)
                {
                    Visualizer.StitchLastSegment(new StreamSegmentModel
                    {
                        Start = lastSegment.Model.Start,
                        End = modelEnd
                    });
                    // Visualizer.StreamSegments[^1].Model = new StreamSegmentModel
                    // {
                    //     Start = lastSegment.Model.Start,
                    //     End = modelEnd
                    // };
                }
                else
                {
                    //TODO: instead of just creating models that are all rendered by one script, instantiate individual segments, spawn them at the correct offset, and just animate them across until they exit the left side of the mask
                    //TODO: Stop making shit so complicated
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
