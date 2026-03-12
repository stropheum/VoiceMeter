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
        private ConcurrentQueue<byte[]> _pcmQueue = new();
        private AudioSource _audioSource;
        private AudioClip _streamingClip;

        private float[] _audioBuffer;
        private int _writePos = 0;
        private int _readPos = 0;
        private bool _hasData = false;

        private long _totalReceivedBytes = 0;

        private void Start()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;

            int totalSamples = _sampleRate * _bufferSeconds * _channels;
            _streamingClip = AudioClip.Create("DiscordStream", totalSamples, _channels, _sampleRate, true, OnAudioRead);
            _audioSource.clip = _streamingClip;
            _audioSource.Play();

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
                    byte[] data = _udpClient.Receive(ref remoteEp);
                    if (data.Length > 0)
                    {
                        _totalReceivedBytes += data.Length;
                        Debug.Log($"UDP received {data.Length} bytes (total: {_totalReceivedBytes})");
                        _pcmQueue.Enqueue(data);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"UDP receive error: {e.Message}");
                }
            }
        }

        private void Update()
        {
            while (_pcmQueue.TryDequeue(out byte[] pcmBytes))
            {
                if (pcmBytes == null || pcmBytes.Length == 0)
                {
                    continue;
                }

                int sampleCount = pcmBytes.Length / 2;
                for (int i = 0; i < sampleCount && _writePos < _audioBuffer.Length; i++)
                {
                    short sample = System.BitConverter.ToInt16(pcmBytes, i * 2);
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
                System.Array.Clear(data, 0, data.Length);
                return;
            }

            int needed = data.Length;
            int available = (_writePos - _readPos + _audioBuffer.Length) % _audioBuffer.Length;

            if (available < needed)
            {
                System.Array.Clear(data, 0, data.Length);
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