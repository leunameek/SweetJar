using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PitchDetector : MonoBehaviour
{
    public GameConfig config;
    public MicrophoneCapture mic;
    public CandyManager candyManager;

    // Output
    public string CurrentNote { get; private set; }
    public float CurrentFreq { get; private set; }
    public float CurrentRms { get; private set; }
    public float LastForce { get; private set; }

    // Stability / debounce
    private string stableNote;
    private float noteStableSince;
    private float lastForceTime;

    private static readonly string[] NOTE_NAMES =
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    private static readonly HashSet<string> C_MAJOR =
        new HashSet<string> { "C", "D", "E", "F", "G", "A", "B" };
    private static readonly Dictionary<string, string> SNAP =
        new Dictionary<string, string>
        { {"C#","C"}, {"D#","D"}, {"F#","F"}, {"G#","G"}, {"A#","A"} };

    private string pendingNote;
    private float pendingRms;
    private bool hasPendingForce;

    void Update()
    {
        ProcessPitch();
    }

    void FixedUpdate()
    {
        if (!hasPendingForce) return;
        ApplyNoteForce(pendingNote, pendingRms);
        hasPendingForce = false;
    }

    void ProcessPitch()
    {
        if (mic == null)        { Debug.LogError("[PitchDetector] mic is not assigned!"); return; }
        if (!mic.IsActive)      return;

        float[] buffer = mic.GetBuffer();
        if (buffer == null)
        {
            Debug.LogWarning("[PitchDetector] buffer is null (not enough mic data yet)");
            CurrentNote = null; CurrentFreq = 0; CurrentRms = 0;
            return;
        }

        // RMS
        float sumSq = 0f;
        for (int i = 0; i < buffer.Length; i++)
            sumSq += buffer[i] * buffer[i];
        float rms = Mathf.Sqrt(sumSq / buffer.Length);
        CurrentRms = rms;

        if (rms < config.minRms)
        {
            CurrentNote = null; CurrentFreq = 0; stableNote = null;
            return;
        }
        Debug.Log($"[Mic] rms={rms:F4} freq={CurrentFreq:F1} note={CurrentNote}");

        float freq = DetectPitch(buffer, mic.SampleRate);
        if (freq < 60f || freq > 900f)
        {
            CurrentNote = null; CurrentFreq = 0; stableNote = null;
            return;
        }

        CurrentFreq = freq;
        string note = FrequencyToNote(freq);
        CurrentNote = note;

        // Stability
        float now = Time.time * 1000f;
        if (note != stableNote)
        {
            stableNote = note;
            noteStableSince = now;
            return;
        }
        if (now - noteStableSince < config.noteStableMs) return;

        // Debounce
        if (now - lastForceTime < config.forceDebounceMs) return;
        lastForceTime = now;

        pendingNote = note;
        pendingRms = rms;
        hasPendingForce = true;
    }

    float DetectPitch(float[] buffer, int sampleRate)
    {
        int len = buffer.Length;
        int halfLen = len / 2;

        // Difference function
        float[] diff = new float[halfLen];
        for (int tau = 0; tau < halfLen; tau++)
        {
            float sum = 0f;
            for (int i = 0; i < halfLen; i++)
            {
                float d = buffer[i] - buffer[i + tau];
                sum += d * d;
            }
            diff[tau] = sum;
        }

        // Cumulative mean normalized difference
        float[] cmndf = new float[halfLen];
        cmndf[0] = 1f;
        float runningSum = 0f;
        for (int tau = 1; tau < halfLen; tau++)
        {
            runningSum += diff[tau];
            cmndf[tau] = (runningSum > 0f) ? diff[tau] * tau / runningSum : 1f;
        }

        // Absolute threshold search
        int minTau = Mathf.FloorToInt(sampleRate / 900f);
        int maxTau = Mathf.Min(Mathf.FloorToInt(sampleRate / 60f), halfLen);
        int tauEstimate = -1;

        for (int tau = minTau; tau < maxTau; tau++)
        {
            if (cmndf[tau] < config.yinThreshold)
            {
                while (tau + 1 < halfLen && cmndf[tau + 1] < cmndf[tau])
                    tau++;
                tauEstimate = tau;
                break;
            }
        }

        if (tauEstimate == -1) return -1f;

        // Parabolic interpolation
        float betterTau = tauEstimate;
        if (tauEstimate > 0 && tauEstimate < halfLen - 1)
        {
            float s0 = cmndf[tauEstimate - 1];
            float s1 = cmndf[tauEstimate];
            float s2 = cmndf[tauEstimate + 1];
            float denom = 2f * (s0 - 2f * s1 + s2);
            if (denom != 0f)
                betterTau = tauEstimate + (s0 - s2) / denom;
        }

        return sampleRate / betterTau;
    }

    string FrequencyToNote(float freq)
    {
        float midi = 12f * Mathf.Log(freq / 440f, 2f) + 69f;
        int semitone = Mathf.RoundToInt(midi) % 12;
        string name = NOTE_NAMES[((semitone % 12) + 12) % 12];
        if (C_MAJOR.Contains(name)) return name;
        return SNAP.ContainsKey(name) ? SNAP[name] : "C";
    }

    void ApplyNoteForce(string note, float rms)
    {
        var strengths = config.NoteStrengths;
        float strength = strengths.ContainsKey(note) ? strengths[note] : 0.5f;
        float loudness = Mathf.Min(rms * config.micSensitivity * 10f, 1.5f);
        float impulseY = Mathf.Min(config.baseImpulse * strength * loudness, config.maxImpulse);
        LastForce = impulseY;

        var topLayer = candyManager.candyBodies
            .Where(rb => rb != null)
            .OrderByDescending(rb => rb.position.y)
            .Take(config.layerSize);

        foreach (var rb in topLayer)
        {
            Vector3 pos = rb.position;
            float dx = -pos.x;
            float dz = -pos.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            float ix = 0f, iz = 0f;
            if (dist > 0.1f)
            {
                float attr = config.holeAttractionStrength * strength;
                ix = (dx / dist) * attr;
                iz = (dz / dist) * attr;
            }

            ix += (Random.value - 0.5f) * config.lateralJitter;
            iz += (Random.value - 0.5f) * config.lateralJitter;

            rb.AddForce(new Vector3(ix, impulseY, iz), ForceMode.Impulse);
            Vector3 v = rb.linearVelocity;
            if (v.y > config.maxCandySpeedY) { v.y = config.maxCandySpeedY; rb.linearVelocity = v; }
        }
    }
}
