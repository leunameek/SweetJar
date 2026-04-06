using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Config")]
    public GameConfig config;

    [Header("References")]
    public CandyManager candyManager;
    public CapController capController;
    public MicrophoneCapture micCapture;
    public PitchDetector pitchDetector;
    public UIController ui;

    private bool won;

    void Start()
    {
        Debug.Log($"[GameManager] micCapture={micCapture}, pitchDetector={pitchDetector}");
        Debug.Log($"[GameManager] Microphone devices: {Microphone.devices.Length}");
        foreach (var d in Microphone.devices)
            Debug.Log($"  device: {d}");

        pitchDetector.mic = micCapture;
        StartGame();
    }

    void Update()
    {
        if (won) return;

        candyManager.CheckEscaped();

        ui.UpdateHUD(
            pitchDetector.CurrentNote,
            pitchDetector.CurrentFreq,
            pitchDetector.CurrentRms,
            pitchDetector.LastForce,
            candyManager.escapedCount,
            candyManager.totalCandies
        );

        if (candyManager.IsWon())
        {
            won = true;
            ui.ShowWinScreen();
        }
    }

    public void StartGame()
    {
        won = false;
        micCapture.StopCapture();
        candyManager.Spawn(config.candyCount, () => {
            bool ok = micCapture.StartCapture();
            if (!ok) Debug.LogWarning("[GameManager] No microphone found.");
        });
        capController.ResetAngle();
        ui.HideWinScreen();
    }

    public void Restart()
    {
        won = false;
        config.candyCount = Mathf.RoundToInt(ui.candySlider.value);
        config.baseImpulse = ui.impulseSlider.value;
        config.micSensitivity = ui.sensitivitySlider.value;
        config.capRotationSpeed = ui.capSpeedSlider.value;

        micCapture.StopCapture();
        candyManager.Spawn(config.candyCount, () => {
            bool ok = micCapture.StartCapture();
            if (!ok) Debug.LogWarning("[GameManager] No microphone found.");
        });
        capController.ResetAngle();
        ui.HideWinScreen();
    }

    public void OnMicButtonClicked()
    {
        if (micCapture.IsActive)
        {
            micCapture.StopCapture();
            ui.SetMicButtonText("Start Mic");
        }
        else
        {
            bool ok = micCapture.StartCapture();
            ui.SetMicButtonText(ok ? "Stop Mic" : "No Mic Found");
        }
    }
}
