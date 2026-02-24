using NAudio.Wave;
using NETConnect.Shared.Packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Audio
{
    public record AudioQueueHelper(byte[] AudioChunk, DateTime Received);

    public class Audio
    {
        private static WaveOutEvent waveOut;
        private static BufferedWaveProvider bufferedWaveProvider;

        private static List<AudioQueueHelper> betterQueue = new List<AudioQueueHelper>();
        private static ConcurrentQueue<byte[]> audioQueue = new ConcurrentQueue<byte[]>();
        private static bool isRunning = false;

        /// <summary>
        /// Call this once at startup to initialize playback
        /// </summary>
        public static void Init(int sampleRate = 16000, int bits = 16, int channels = 1)
        {
            if (isRunning) return;

            var waveFormat = new WaveFormat(sampleRate, bits, channels);
            bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
            {
                DiscardOnBufferOverflow = true,
                //BufferDuration = TimeSpan.FromSeconds(3), // 3 sec buffer prevents gaps

            };

            waveOut = new WaveOutEvent();
            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();

            isRunning = true;

            // Start a background thread to process queued audio
            Thread playbackThread = new Thread(ProcessQueue)
            {
                IsBackground = true
            };
            playbackThread.Start();
        }

        /// <summary>
        /// Call this whenever you have a new audio chunk
        /// </summary>
        public static void QueueAudio(byte[] audioChunk)
        {
            if (!isRunning) throw new InvalidOperationException("AudioPlayer not initialized. Call Init() first.");
            if (audioChunk == null || audioChunk.Length == 0) return;

            //audioQueue
            betterQueue.Add(new AudioQueueHelper(audioChunk, DateTime.Now));
            //betterQueue.Enqueue();
        }



        /// <summary>
        /// Background thread: takes queued chunks and feeds them to the playback buffer
        /// </summary>
        private static void ProcessQueue()
        {
            int AfterProcessTimer = 1;
            while (isRunning)
            {
                
                AudioQueueHelper[] SafeQueue = betterQueue.Where(x => DateTime.Now > x.Received.AddMilliseconds(AfterProcessTimer)).ToArray();

                // Play all that are older than 3 seconds //
                foreach (var item in SafeQueue.Where(x => DateTime.Now > x.Received.AddMilliseconds(AfterProcessTimer)))
                {
                    betterQueue.Remove(item);
                    //audioQueue.Enqueue(item.AudioChunk);
                }

                byte[] chunk = SafeQueue.SelectMany(x => x.AudioChunk).ToArray();

                bufferedWaveProvider.AddSamples(chunk, 0, chunk.Length);

                Thread.Sleep(5); // small sleep to prevent CPU spinning
            }
        }

        /// <summary>
        /// Call this to stop playback cleanly
        /// </summary>
        public static void Stop()
        {
            isRunning = false;
            waveOut?.Stop();
            waveOut?.Dispose();
        }


        private static WaveInEvent waveIn;

        /// <summary>
        /// Call this to start streaming from the mic.
        /// Audio chunks are fed to StreamingAudio.QueueChunk
        /// </summary>
        public static void StartStreaming(ref PacketHelper PacketHelper)
        {
            // Initialize the persistent playback queue
            Init();

            // Setup microphone capture
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1), // 16kHz, 16-bit mono
                BufferMilliseconds = 50 // ~1600 bytes per chunk
            };


            var Helper = PacketHelper;
            waveIn.DataAvailable += (s, e) =>
            {

                //if (ContainsVoice(e.Buffer, e.BytesRecorded))
                //{
                //    // Copy only the actual recorded bytes
                //    byte[] voiceChunk = new byte[e.BytesRecorded];
                //    Array.Copy(e.Buffer, 0, voiceChunk, 0, e.BytesRecorded);
                //    // Feed into the persistent audio queue
                //    //QueueAudio(voiceChunk);


                //    // Optional: send over your network here
                //    // YourNetwork.Send(voiceChunk);
                //    Helper.SendVoicePacket(voiceChunk);

                //    //Console.WriteLine($"Captured {e.BytesRecorded} bytes");
                //}

                // Prevents recording after token canceled - this will probably need changed when sharing audio from server directly to client rather than client to 
                if (Helper.Token.IsCancellationRequested)
                {
                    StopStreaming();
                    return;
                }

                DateTime lastVoiceTime = DateTime.MinValue;
                int hangMs = 300; //default 300

                if (ContainsVoice(e.Buffer, e.BytesRecorded))
                {
                    lastVoiceTime = DateTime.UtcNow;
                }

                if ((DateTime.UtcNow - lastVoiceTime).TotalMilliseconds < hangMs)
                {
                    // Copy only the actual recorded bytes
                    byte[] voiceChunk = new byte[e.BytesRecorded];
                    Array.Copy(e.Buffer, 0, voiceChunk, 0, e.BytesRecorded);
                    Helper.SendVoicePacket(voiceChunk);
                }
            };

            waveIn.StartRecording();
            //Console.WriteLine("Microphone streaming started. Press Enter to stop.");
        }

        //public void HandleDataAvailable(object s, WaveInEventArgs e)
        //{

        //}

        public static bool ContainsVoice(byte[] buffer, int bytesRecorded, short threshold = 500)
        {
            for (int i = 0; i < bytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                if (Math.Abs(sample) > threshold)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Stop the microphone streaming
        /// </summary>
        public static void StopStreaming()
        {
            waveIn?.StopRecording();
            waveIn?.Dispose();
            //StreamingAudio.Stop();
            Console.WriteLine("Microphone streaming stopped.");
        }


        public static void PlayAudio(byte[] audioBytes)
        {
            if (audioBytes == null || audioBytes.Length == 0)
            {
                Console.WriteLine("No audio to play");
                return;
            }

            var waveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono

            using (var waveOut = new WaveOutEvent())
            {
                var bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                {
                    DiscardOnBufferOverflow = true
                };

                waveOut.Init(bufferedWaveProvider);
                waveOut.Play();

                // Feed the bytes in small chunks for reliability
                int chunkSize = 1024;
                int offset = 0;

                while (offset < audioBytes.Length)
                {
                    int bytesToWrite = Math.Min(chunkSize, audioBytes.Length - offset);
                    bufferedWaveProvider.AddSamples(audioBytes, offset, bytesToWrite);
                    offset += bytesToWrite;

                    // Give NAudio time to process
                    System.Threading.Thread.Sleep(5);
                }

                // Wait until all audio is played
                while (bufferedWaveProvider.BufferedBytes > 0)
                {
                    System.Threading.Thread.Sleep(50);
                }

                waveOut.Stop();
            }
        }
    }
}
