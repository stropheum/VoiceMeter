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
        [SerializeField] private List<UserMapping> _userMappings = new();
        private readonly Dictionary<string, string> _dynamicMappings = new();

        [Serializable]
        public class UserMapping
        {
            public string IPAddress;
            public string Username;
        }

        private readonly Dictionary<string, UserStreamDisplay> _userDisplays = new();
        private readonly ConcurrentQueue<(string userId, DateTime timestamp)> _eventQueue = new();

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
            }
        }

        private void OnDisable()
        {
            if (_audioReceiver != null)
            {
                _audioReceiver.OnDataReceived -= HandleDataReceived;
            }

            var listener = FindFirstObjectByType<DiscordVoiceListener>();
            if (listener != null)
            {
                listener.OnVoiceReceive -= HandleDiscordVoiceReceive;
            }
        }

        private void HandleDiscordVoiceReceive(VoiceReceiveEvent evt)
        {
            Debug.Log($"[UDPVoiceActivityManager] Received Discord Voice Event: User={evt.User?.Username}, IP={evt.IP}");
            if (!string.IsNullOrEmpty(evt.IP) && evt.User != null && !string.IsNullOrEmpty(evt.User.Username))
            {
                _dynamicMappings[evt.IP] = evt.User.Username;
                Debug.Log($"[UDPVoiceActivityManager] Dynamic mapping updated: {evt.IP} -> {evt.User.Username}");
            }
        }

        private void HandleDataReceived(IPEndPoint remoteEp, string username, byte[] data)
        {
            if (data == null || data.Length == 0) return;

            string remoteAddress = remoteEp.Address.ToString();
            string remoteAddressAndPort = remoteEp.ToString();
            DateTime now = DateTime.Now;

            // 1. Update dynamic mapping from the provided username (which came from the UDP packet prefix)
            if (!string.IsNullOrEmpty(username))
            {
                _dynamicMappings[remoteAddress] = username;
                
                // Also update any existing display for this IP:port
                if (_userDisplays.TryGetValue(remoteAddressAndPort, out var display))
                {
                    if (display.Username.text != username)
                    {
                        display.Username.text = username;
                    }
                }
            }

            // 2. Try to see if this is a JSON metadata message first (fallback/legacy)
            if (data[0] == (byte)'{' || data[0] == (byte)'[')
            {
                try
                {
                    string json = System.Text.Encoding.UTF8.GetString(data);
                    var message = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageLogModel>(json);

                    if (message != null && message.Name == "VoiceReceive" && !string.IsNullOrEmpty(message.Payload))
                    {
                        var model = Newtonsoft.Json.JsonConvert.DeserializeObject<VoiceReceiveEvent>(message.Payload);
                        if (model != null && model.User != null && !string.IsNullOrEmpty(model.User.Username))
                        {
                            // Map the username to the IP address (without port)
                            _dynamicMappings[remoteAddress] = model.User.Username;
                            Debug.Log($"[UDPVoiceActivityManager] Dynamic mapping updated from UDP JSON: {remoteAddress} -> {model.User.Username}");
                            return; // Don't process as audio
                        }
                    }
                }
                catch
                {
                    // Not valid JSON or doesn't match our model, fall back to audio check
                }
            }

            // 2. Otherwise, treat as raw audio data and check for activity
            bool isActive = CheckActivity(data);

            if (isActive)
            {
                _eventQueue.Enqueue((remoteAddressAndPort, now));
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
            ProcessEventQueue();
            UpdateTimeEquity();
        }

        private string GetUsername(string userId)
        {
            // First attempt: Check dynamic mappings from Discord events (IP or IP:port)
            if (_dynamicMappings.TryGetValue(userId, out string dynamicName))
            {
                return dynamicName;
            }

            // Second attempt: try to match only the IP address if the userId contains a port
            int colonIndex = userId.LastIndexOf(':');
            if (colonIndex != -1)
            {
                string ipOnly = userId.Substring(0, colonIndex);
                if (_dynamicMappings.TryGetValue(ipOnly, out string dynamicIpName))
                {
                    return dynamicIpName;
                }
            }

            // Third attempt: match the full userId in static mappings (IP:port)
            foreach (var mapping in _userMappings)
            {
                if (mapping.IPAddress == userId)
                {
                    return mapping.Username;
                }
            }

            // Fourth attempt: match only the IP address in static mappings
            if (colonIndex != -1)
            {
                string ipOnly = userId.Substring(0, colonIndex);
                foreach (var mapping in _userMappings)
                {
                    if (mapping.IPAddress == ipOnly)
                    {
                        return mapping.Username;
                    }
                }
            }
            
            return userId;
        }

        private void ProcessEventQueue()
        {
            while (_eventQueue.TryDequeue(out var evt))
            {
                if (!_userDisplays.TryGetValue(evt.userId, out var display))
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

            display.Username.text = GetUsername(userId);
            display.UserId = (long)userId.GetHashCode();
            display.Visualizer.TimeWindow = _displayWindowInSeconds;
            display.Context = null; // No Discord listener context
            return display;
        }

        private void UpdateTimeEquity()
        {
            float sum = 0;
            foreach (var display in _userDisplays.Values)
            {
                sum += display.ProcessedFrameCount;
            }

            if (sum <= 0) return;

            foreach (var display in _userDisplays.Values)
            {
                display.EquityMeter.DisplayPercent(display.ProcessedFrameCount / sum);
            }
        }
    }
}
