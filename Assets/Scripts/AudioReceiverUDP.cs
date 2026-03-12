using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace VoiceMeter
{
    public class AudioReceiverUDP : MonoBehaviour
    {
        [Header("UDP Settings")]
        [SerializeField] private int _listenPort = 8765;

        [Header("Audio Settings")]
        [SerializeField] private int _sampleRate = 48000;
        [SerializeField] private int _channels = 2;
        [SerializeField] private int _bufferSeconds = 5;

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private readonly ConcurrentQueue<(string username, byte[] audio)> _pcmQueue = new();
        // private AudioSource _audioSource;
        private AudioClip _streamingClip;

        private float[] _audioBuffer;
        private int _writePos;
        private int _readPos;
        private bool _hasData;

        private long _totalReceivedBytes = 0;
        public event Action<IPEndPoint, byte[]> OnDataReceived;

        private void Start()
        {
            // _audioSource = gameObject.AddComponent<AudioSource>();
            // _audioSource.loop = true;

            int totalSamples = _sampleRate * _bufferSeconds * _channels;
            _streamingClip = AudioClip.Create("DiscordStream", totalSamples, _channels, _sampleRate, true, OnAudioRead);
            // _audioSource.clip = _streamingClip;
            // _audioSource.Play();

            _audioBuffer = new float[totalSamples];

            // Start UDP listener
            _udpClient = new UdpClient(_listenPort);
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            Debug.Log($"UDP listening on port {_listenPort}");
        }

        private void ReceiveLoop()
        {
            IPEndPoint remoteEp = null;
            while (true)
            {
                try
                {
                    byte[] packet = _udpClient.Receive(ref remoteEp);
                    if (packet == null || packet.Length < 4)
                    {
                        Debug.LogWarning($"Dropped tiny/invalid packet ({packet?.Length ?? 0} bytes)");
                        continue;
                    }

                    // Read big-endian (network order) 4-byte length
                    int userLen = (packet[0] << 24) | (packet[1] << 16) | (packet[2] << 8) | packet[3];

                    if (userLen < 0 || userLen > 100 || packet.Length < 4 + userLen)
                    {
                        Debug.LogWarning($"Incomplete/malformed packet: userLen={userLen}, totalLen={packet.Length}");
                        continue;
                    }

                    string username = System.Text.Encoding.UTF8.GetString(packet, 4, userLen);
                    byte[] audioBytes = new byte[packet.Length - 4 - userLen];
                    Array.Copy(packet, 4 + userLen, audioBytes, 0, audioBytes.Length);

                    _totalReceivedBytes += audioBytes.Length;
                    Debug.Log($"UDP received {audioBytes.Length} bytes from {username} (total: {_totalReceivedBytes})");

                    _pcmQueue.Enqueue((username, audioBytes));
                    OnDataReceived?.Invoke(remoteEp, audioBytes);
                }
                catch (Exception e)
                {
                    Debug.LogError($"UDP receive error: {e.Message}");
                }
            }
        }

        private void Update()
        {
            while (_pcmQueue.TryDequeue(out (string username, byte[] audio) tuple))
            {
                string username = tuple.username;
                byte[] pcmBytes = tuple.audio;

                if (pcmBytes == null || pcmBytes.Length == 0)
                {
                    continue;
                }

                Debug.Log($"Processing {pcmBytes.Length} audio bytes from user: {username}");

                int sampleCount = pcmBytes.Length / 2;
                for (int i = 0; i < sampleCount && _writePos < _audioBuffer.Length; i++)
                {
                    short sample = BitConverter.ToInt16(pcmBytes, i * 2);
                    _audioBuffer[_writePos] = sample / 32768f;
                    _writePos = (_writePos + 1) % _audioBuffer.Length;
                }

                _hasData = true;
            }
        }

        private void OnAudioRead(float[] data)
        {
            if (!_hasData)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }

            int needed = data.Length;
            int available = (_writePos - _readPos + _audioBuffer.Length) % _audioBuffer.Length;

            if (available < needed)
            {
                Array.Clear(data, 0, data.Length);
                for (int i = 0; i < available; i++)
                {
                    data[i] = _audioBuffer[_readPos];
                    _readPos = (_readPos + 1) % _audioBuffer.Length;
                }
            }
            else
            {
                for (int i = 0; i < needed; i++)
                {
                    data[i] = _audioBuffer[_readPos];
                    _readPos = (_readPos + 1) % _audioBuffer.Length;
                }
            }
        }

        private void OnDestroy()
        {
            _receiveThread?.Abort();
            _udpClient?.Close();
        }
    }
}