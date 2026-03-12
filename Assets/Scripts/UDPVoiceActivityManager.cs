using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using VoiceMeter.Discord;

namespace VoiceMeter
{
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class UDPVoiceActivityManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioReceiverUDP _audioReceiver;
        [SerializeField] private UserStreamDisplay _userStreamDisplayPrefab;

        [Header("Settings")]
        [SerializeField] private float _displayWindowInSeconds = 30f;
        [SerializeField] private float _activityThreshold = 0.001f;

        private readonly Dictionary<string, UserStreamDisplay> _userDisplays = new();
        private readonly ConcurrentQueue<(string userId, DateTime timestamp)> _eventQueue = new();
        private readonly ConcurrentQueue<(string username, string eventType)> _userEventQueue = new();

        private void Awake()
        {
            if (_audioReceiver == null)
            {
                _audioReceiver = FindFirstObjectByType<AudioReceiverUDP>();
            }
        }

        private void OnEnable()
        {
            if (_audioReceiver != null)
            {
                _audioReceiver.OnDataReceived += HandleDataReceived;
                _audioReceiver.OnUserEvent += HandleUserEvent;
            }
        }

        private void OnDisable()
        {
            if (_audioReceiver != null)
            {
                _audioReceiver.OnDataReceived -= HandleDataReceived;
                _audioReceiver.OnUserEvent -= HandleUserEvent;
            }
        }

        private void HandleUserEvent(string username, string eventType)
        {
            if (string.IsNullOrEmpty(username)) return;

            _userEventQueue.Enqueue((username, eventType));
        }

        private void ProcessUserEvents()
        {
            while (_userEventQueue.TryDequeue(out (string username, string eventType) evt))
            {
                string username = evt.username;
                string eventType = evt.eventType;

                if (eventType == "joined")
                {
                    if (_userDisplays.ContainsKey(username))
                    {
                        if (_userDisplays[username] != null)
                        {
                            Destroy(_userDisplays[username].gameObject);
                        }
                        _userDisplays.Remove(username);
                        Debug.Log($"Destroyed stale display for left user: {username}");
                    }

                    UserStreamDisplay spawnedDisplay = SpawnUserDisplay(username);
                    _userDisplays[username] = spawnedDisplay;
                    Debug.Log($"Created display for joined user: {username}");
                }
                else if (eventType == "left")
                {
                    if (_userDisplays.TryGetValue(username, out UserStreamDisplay display))
                    {
                        if (display != null && display.gameObject != null)
                        {
                            Destroy(display.gameObject);
                        }
                        _userDisplays.Remove(username);
                        Debug.Log($"Destroyed display for left user: {username}");
                    }
                }
            }
        }

        private void HandleDataReceived(IPEndPoint remoteEp, string username, byte[] data)
        {
            if (data == null || data.Length == 0 || string.IsNullOrEmpty(username)) return;

            DateTime now = DateTime.Now;

            // Treat as raw audio data and check for activity
            bool isActive = CheckActivity(data);

            if (isActive)
            {
                _eventQueue.Enqueue((username, now));
            }
        }

        private bool CheckActivity(byte[] data)
        {
            if (data == null || data.Length == 0) return false;

            int sampleCount = data.Length / 2;
            float sumSquared = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(data, i * 2);
                float fSample = sample / 32768f;
                sumSquared += fSample * fSample;
            }

            float rms = Mathf.Sqrt(sumSquared / sampleCount);
            return rms > _activityThreshold;
        }

        private void Update()
        {
            ProcessUserEvents();
            ProcessEventQueue();
            UpdateTimeEquity();
        }

        private string GetUsername(string userId)
        {
            return userId;  // Since userId is now the username
        }

        private void ProcessEventQueue()
        {
            while (_eventQueue.TryDequeue(out (string userId, DateTime timestamp) evt))
            {
                if (!_userDisplays.TryGetValue(evt.userId, out UserStreamDisplay display))
                {
                    display = SpawnUserDisplay(evt.userId);
                    _userDisplays[evt.userId] = display;
                }

                string username = GetUsername(evt.userId);

                // Create a VoiceReceiveEvent for the existing UserStreamDisplay logic
                var voiceEvent = new VoiceReceiveEvent(null)
                {
                    UserId = display.UserId,
                    TimeStamp = evt.timestamp,
                    User = new User { Username = username }
                };

                display.VoiceEventCallback(voiceEvent);
            }
        }

        private UserStreamDisplay SpawnUserDisplay(string userId)
        {
            UserStreamDisplay display = Instantiate(_userStreamDisplayPrefab, transform);

            display.Username.text = userId;  // userId is username
            display.UserId = userId.GetHashCode();
            display.Visualizer.TimeWindow = _displayWindowInSeconds;
            display.Context = null; // No Discord listener context
            return display;
        }

        private void UpdateTimeEquity()
        {
            float sum = 0;
            foreach (UserStreamDisplay display in _userDisplays.Values)
            {
                sum += display.ProcessedFrameCount;
            }

            if (sum <= 0) return;

            foreach (UserStreamDisplay display in _userDisplays.Values)
            {
                display.EquityMeter.DisplayPercent(display.ProcessedFrameCount / sum);
            }
        }
    }
}