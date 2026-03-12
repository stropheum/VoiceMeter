using UnityEngine;
using NativeWebSocket;               // From the imported NativeWebSocket package
using System.Collections.Concurrent; // For thread-safe queue
using System;
using System.Threading.Tasks;

public class AudioReceiverNativeWS : MonoBehaviour
{
    [Header("WebSocket Settings")]
    [SerializeField] private string wsUrl = "ws://localhost:8765"; // Change to LAN IP if needed, e.g. ws://192.168.1.100:8765

    [Header("Audio Settings (Discord defaults)")]
    [SerializeField] private int sampleRate = 48000;
    [SerializeField] private int channels = 2;           // Stereo
    [SerializeField] private int bufferSeconds = 10;     // How much audio to buffer in the clip

    private WebSocket websocket;
    private AudioSource audioSource;
    private AudioClip streamingClip;

    // Thread-safe queue for incoming PCM bytes (from WebSocket → main thread)
    private readonly ConcurrentQueue<byte[]> pcmQueue = new ConcurrentQueue<byte[]>();

    // For smooth playback: ring buffer style
    private float[] audioBuffer;
    private int writePos = 0;
    private int readPos = 0;
    private bool hasData = false;

    async void Start()
    {
        // Create AudioSource and streaming clip
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        int totalSamples = sampleRate * bufferSeconds * channels;
        streamingClip = AudioClip.Create("DiscordVoiceStream", totalSamples, channels, sampleRate, true, OnAudioRead);
        audioSource.clip = streamingClip;

        // Initialize ring buffer (float samples)
        audioBuffer = new float[totalSamples];

        // Connect to WebSocket
        websocket = new WebSocket(wsUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log($"[NativeWebSocket] Connected to {wsUrl}");
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"[NativeWebSocket] Error: {e}");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log($"[NativeWebSocket] Closed: {e}");
        };

        websocket.OnMessage += (bytes) =>
        {
            // Received binary message → raw PCM bytes from Python bot
            pcmQueue.Enqueue(bytes);
            Debug.Log($"[NativeWebSocket] Received {bytes.Length} PCM bytes");
        };

        // Optional: auto-reconnect logic
        websocket.OnClose += async (e) =>
        {
            await Task.Delay(2000);
            Debug.Log("[NativeWebSocket] Attempting reconnect...");
            await websocket.Connect();
        };

        await websocket.Connect();
        audioSource.Play(); // Start playing the streaming clip
    }

    void Update()
    {
        // Drain queue on main thread and write to ring buffer
        while (pcmQueue.TryDequeue(out byte[] pcmBytes))
        {
            if (pcmBytes == null || pcmBytes.Length == 0) continue;

            // Convert 16-bit signed PCM → float[-1..1]
            int sampleCount = pcmBytes.Length / 2; // 2 bytes per sample
            for (int i = 0; i < sampleCount && writePos < audioBuffer.Length; i++)
            {
                short sample = BitConverter.ToInt16(pcmBytes, i * 2);
                audioBuffer[writePos] = sample / 32768f;
                writePos = (writePos + 1) % audioBuffer.Length;
            }

            hasData = true;
        }
    }

    // Called by Unity when the AudioClip needs more samples to play
    void OnAudioRead(float[] data)
    {
        if (!hasData)
        {
            // No data yet → silence
            Array.Clear(data, 0, data.Length);
            return;
        }

        int samplesNeeded = data.Length;
        int available = (writePos - readPos + audioBuffer.Length) % audioBuffer.Length;

        if (available < samplesNeeded)
        {
            // Underrun → fill with silence + what we have
            Array.Clear(data, 0, data.Length);
            for (int i = 0; i < available; i++)
            {
                data[i] = audioBuffer[readPos];
                readPos = (readPos + 1) % audioBuffer.Length;
            }
        }
        else
        {
            // Normal case: copy from ring buffer
            for (int i = 0; i < samplesNeeded; i++)
            {
                data[i] = audioBuffer[readPos];
                readPos = (readPos + 1) % audioBuffer.Length;
            }
        }
    }

    private async void OnDestroy()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }

    // Optional: Manual reconnect button / debug
    public async void Reconnect()
    {
        if (websocket != null)
        {
            await websocket.Close();
            await websocket.Connect();
        }
    }
}