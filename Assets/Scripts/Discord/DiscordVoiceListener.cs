using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace VoiceMeter.Discord
{
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class DiscordVoiceListener : MonoBehaviour
    {
        [field: SerializeField] public float DisplayWindowInSeconds { get; private set; } = 30f;
        [SerializeField] private UserStreamDisplay _userStreamDisplayPrefab;
        private Process _process;
        private StreamReader _processOutputStream;
        private readonly Dictionary<long, UserStreamDisplay> _userStreamDisplays = new();
        private readonly ConcurrentQueue<VoiceReceiveEvent> _newUserInitialEventQueue = new();
        private Coroutine _processNewUserQueueCoroutine;
        private bool _processCoroutineRunning = false;

        public event Action<VoiceReceiveEvent> OnVoiceReceive;

        private void Awake()
        {
            Debug.Assert(_userStreamDisplayPrefab != null);
        }

        private void Start()
        {
            StartCoroutine(Connect());
        }

        private void Update()
        {
            if (!_processCoroutineRunning && !_newUserInitialEventQueue.IsEmpty)
            {
                _processNewUserQueueCoroutine = StartCoroutine(ProcessNewUserStreamQueue());
            }

            UpdateTimeEquity();
        }

        private IEnumerator Connect()
        {
            const string libPath = @"E:\Users\strop\Documents\GitHub\VoiceMeterBot\VoiceMeterBot\bin\Debug\net7.0";
            const string processName = "VoiceMeterBot.exe";
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = libPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(libPath, processName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };
            _process = Process.Start(startInfo);
            Debug.Assert(_process != null);
            _process.OutputDataReceived += ProcessOnOutputDataReceived;
            _process.ErrorDataReceived += ProcessOnErrorDataReceived;
            _process.BeginOutputReadLine();

            Debug.Log("starting read loop");
            while (!_process.HasExited)
            {
                yield return null;
            }

            Debug.Log("exited read loop");
            _process.WaitForExit();
        }

        private static void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.Log(JsonConvert.SerializeObject(e));
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            
            try
            {
                var message = JsonConvert.DeserializeObject<MessageLogModel>(e.Data);
                if (message.Name != "VoiceReceive")
                {
                    return;
                }

                try
                {
                    var model = JsonConvert.DeserializeObject<VoiceReceiveEvent>(message.Payload);
                    Debug.Log($"[DiscordVoiceListener] De-serialized payload: User={model.User?.Username}, IP={model.IP}");
                    RecordVoiceEvent(model);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[DiscordVoiceListener] Payload error: {exception.Message} (Payload: {message.Payload})");
                    throw;
                }
            }
            catch (Exception _)
            {
                // This will catch non-JSON lines (logs)
                // Debug.Log($"Non-event log: {e.Data}");
            }
        }

        private void OnDestroy()
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
            }
        }

        private void OnApplicationQuit()
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
            }
        }

        private void RecordVoiceEvent(VoiceReceiveEvent model)
        {
            if (!_userStreamDisplays.ContainsKey(model.UserId))
            {
                _newUserInitialEventQueue.Enqueue(model);
            }

            if (OnVoiceReceive != null)
            {
                // Debug.Log($"[DiscordVoiceListener] Invoking OnVoiceReceive for user {model.UserId}");
                OnVoiceReceive.Invoke(model);
            }
        }

        private IEnumerator ProcessNewUserStreamQueue()
        {
            _processCoroutineRunning = true;
            while (!_newUserInitialEventQueue.IsEmpty)
            {
                if (_newUserInitialEventQueue.TryDequeue(out VoiceReceiveEvent model))
                {
                    if (!_userStreamDisplays.ContainsKey(model.UserId))
                    {
                        SpawnNewUserStreamDisplay(model);
                    }
                }

                yield return null;
            }

            _processCoroutineRunning = false;
        }

        private void SpawnNewUserStreamDisplay(VoiceReceiveEvent initialEvent)
        {
            UserStreamDisplay newUserStream = Instantiate(_userStreamDisplayPrefab, transform);
            newUserStream.Context = this;
            newUserStream.UserId = initialEvent.UserId;
            newUserStream.Username.text = initialEvent.User.Username;
            _userStreamDisplays[initialEvent.UserId] = newUserStream;
        }

        private void UpdateTimeEquity()
        {
            float sum = 0;
            foreach (UserStreamDisplay user in _userStreamDisplays.Values)
            {
                sum += user.ProcessedFrameCount;
            }

            foreach (UserStreamDisplay user in _userStreamDisplays.Values)
            {
                user.EquityMeter.DisplayPercent(user.ProcessedFrameCount / sum);
            }
        }
    }
}