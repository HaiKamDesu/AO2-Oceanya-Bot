using System;
using System.Windows;
using System.Windows.Resources;
using NAudio.Wave;

namespace OceanyaClient.Utilities
{
    public static class AudioPlayer
    {
        /// <summary>
        /// Plays an embedded MP3 resource file asynchronously.
        /// </summary>
        /// <param name="resourcePath">Relative path to the resource (e.g., "Resources/MySound.mp3").</param>
        public static void PlayEmbeddedSound(string resourcePath, float volume = 1.0f)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
                StreamResourceInfo sri = Application.GetResourceStream(uri);

                if (sri == null)
                {
                    OceanyaMessageBox.Show($"Resource '{resourcePath}' not found.");
                    return;
                }

                var mp3Reader = new Mp3FileReader(sri.Stream);
                var waveOut = new WaveOutEvent();
                waveOut.Volume = volume;

                waveOut.Init(mp3Reader);
                waveOut.Play();

                waveOut.PlaybackStopped += (s, a) =>
                {
                    mp3Reader.Dispose();
                    waveOut.Dispose();
                };
            }
            catch (Exception ex)
            {
                OceanyaMessageBox.Show("Audio Playback Exception: " + ex.Message, "Error Playing Audio");
            }
        }
    }
}