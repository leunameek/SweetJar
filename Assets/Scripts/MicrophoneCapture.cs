using UnityEngine;

public class MicrophoneCapture : MonoBehaviour
{
    public GameConfig config;

    private AudioClip micClip;
    private string micDevice;
    private float[] readBuffer;

    public bool IsActive { get; private set; }

    public bool StartCapture()
    {
        micDevice = null;
        // Use exact device selected in Inspector
        if (!string.IsNullOrEmpty(config.preferredMicDevice))
            micDevice = config.preferredMicDevice;

        // Fallback: first non-virtual device
        if (micDevice == null)
            foreach (var d in Microphone.devices)
            {
                if (d.StartsWith("Monitor of")) continue;
                if (d == "Default Input Device") continue;
                if (d.EndsWith("Analog Stereo")) continue;
                micDevice = d;
                break;
            }

        if (micDevice == null)
        {
            Debug.LogWarning("[MicCapture] No usable microphone found.");
            return false;
        }

        int sampleRate = AudioSettings.outputSampleRate;
        try
        {
            micClip = Microphone.Start(micDevice, true, 1, sampleRate);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MicCapture] Failed to start '{micDevice}': {e.Message}");
            return false;
        }

        readBuffer = new float[config.fftSize];
        IsActive = true;
        Debug.Log($"[MicCapture] Started on: {micDevice} at {sampleRate}Hz");
        return true;
    }

    public void StopCapture()
    {
        if (micDevice != null)
            Microphone.End(micDevice);
        IsActive = false;
        micClip = null;
    }

    /// <summary>Returns the latest fftSize samples, or null if not enough data yet.</summary>
    public float[] GetBuffer()
    {
        if (!IsActive || micClip == null) return null;

        int micPos = Microphone.GetPosition(micDevice);
        if (micPos < config.fftSize) return null;

        int startPos = micPos - config.fftSize;
        if (startPos < 0) startPos += micClip.samples;

        micClip.GetData(readBuffer, startPos);
        return readBuffer;
    }

    public int SampleRate => AudioSettings.outputSampleRate;
}
