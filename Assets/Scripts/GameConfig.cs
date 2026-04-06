using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Config")]
public class GameConfig : ScriptableObject
{
    [Header("Jar / Box")]
    public float boxHeight = 8f;
    public float escapeMargin = 0.5f;
    public float escapeHoleRadius = 1.6f;

    [Header("Cap")]
    public float capRotationSpeed = 0.5f;  // rad/s

    [Header("Candies")]
    public int candyCount = 300;
    public float candyRadius = 0.18f;
    public float spawnRadius = 2f;
    public float spawnHeight = 9.5f;
    public float spawnInterval = 0.05f;
    public int layerSize = 40;

    [Header("Physics")]
    public float candyBounciness = 0.3f;
    public float candyFriction = 0.4f;

    [HideInInspector] public string preferredMicDevice = "";

    [Header("Audio / Pitch")]
    public int fftSize = 2048;
    public float yinThreshold = 0.15f;
    public float minRms = 0.01f;
    public float noteStableMs = 100f;
    public float forceDebounceMs = 120f;

    [Header("Force")]
    public float baseImpulse = 1f;
    public float micSensitivity = 1f;
    public float maxImpulse = 4f;
    public float maxCandySpeedY = 5f;  // max upward speed after any impulse (prevents tunneling)
    public float holeAttractionStrength = 0.15f;
    public float lateralJitter = 0.05f;

    [Header("Note Strengths (C Major)")]
    public float noteC = 0.30f;
    public float noteD = 0.40f;
    public float noteE = 0.50f;
    public float noteF = 0.60f;
    public float noteG = 0.70f;
    public float noteA = 0.85f;
    public float noteB = 1.00f;

    [Header("Win")]
    [Range(0f, 1f)] public float winRatio = 0.9f;

    public Dictionary<string, float> NoteStrengths => new Dictionary<string, float>
    {
        { "C", noteC }, { "D", noteD }, { "E", noteE }, { "F", noteF },
        { "G", noteG }, { "A", noteA }, { "B", noteB }
    };
}

#if UNITY_EDITOR
[CustomEditor(typeof(GameConfig))]
[CanEditMultipleObjects]
public class GameConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameConfig cfg = (GameConfig)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Microphone Device", EditorStyles.boldLabel);

        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            EditorGUILayout.HelpBox("No microphone devices detected.", MessageType.Warning);
            return;
        }

        string[] options = new string[devices.Length + 1];
        options[0] = "(Auto)";
        for (int i = 0; i < devices.Length; i++)
            options[i + 1] = devices[i];

        int currentIndex = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i] == cfg.preferredMicDevice)
            {
                currentIndex = i + 1;
                break;
            }
        }

        int newIndex = EditorGUILayout.Popup("Device", currentIndex, options);
        if (newIndex != currentIndex)
        {
            Undo.RecordObject(cfg, "Change Mic Device");
            cfg.preferredMicDevice = newIndex == 0 ? "" : devices[newIndex - 1];
            EditorUtility.SetDirty(cfg);
        }
    }
}
#endif
