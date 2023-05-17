using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VoiceMeter.Discord
{
    public class DiscordVoiceListener : MonoBehaviour
    {
        private Process _process;
        private StreamReader _processOutputStream;
        private Dictionary<long, List<VoiceReceiveEvent>> _userVoiceData = new();
        
        public event Action<Dictionary<long, List<VoiceReceiveEvent>>> OnVoiceReceive;

        private void Start()
        {
            StartCoroutine(Connect());
        }

        private IEnumerator Connect()
        {
            const string libPath = @"E:\Users\strop\RiderProjects\VoiceMeterBot\VoiceMeterBot\bin\Debug\net7.0";
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
                RedirectStandardInput = true,
            };
            _process = Process.Start(startInfo);
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
            try
            {
                var message = JsonConvert.DeserializeObject<MessageLogModel>(e.Data);
                if (message.Name == "VoiceReceive")
                {
                    var model = JsonConvert.DeserializeObject<VoiceReceiveEvent>(message.Payload);
                    RecordVoiceEvent(model);
                    Debug.Log(JsonConvert.SerializeObject(model));
                }
            }
            catch (Exception)
            {
                Debug.Log($"Non-event log: {e.Data}");
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
            if (!_userVoiceData.ContainsKey(model.UserId))
            {
                _userVoiceData[model.UserId] = new List<VoiceReceiveEvent>();
            }
            _userVoiceData[model.UserId].Add(model);
            OnVoiceReceive?.Invoke(_userVoiceData);
        }
    }
}