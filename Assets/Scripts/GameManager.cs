using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Efectos Visuales")]
    [Tooltip("Controlador de brillo del jarron - asignar el GameObject del jarron")]
    public JarGlowController jarGlow;

    [Header("Scene Flow")]
    public string victorySceneName = "Victory";

    private bool won;

    void Awake()
    {
        ConfigureUIButtonHandlers();
    }

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

        // Pulso de brillo cada vez que hay fuerza activa
        if (jarGlow != null && pitchDetector.LastForce > 0.01f)
            jarGlow.TriggerNotePulse();

        if (candyManager.IsWon())
        {
            won = true;

            micCapture.StopCapture();
            ui.SetMicButtonState(false);

            // Primero la secuencia visual del jarron y, al terminar, la escena de victoria.
            if (jarGlow != null)
                jarGlow.TriggerExplosion(LoadVictoryScene);
            else
                LoadVictoryScene();
        }
    }

    public void StartGame()
    {
        won = false;
        micCapture.StopCapture();
        ui.SetMicButtonState(false);

        // Reiniciar brillo del jarron
        if (jarGlow != null)
            jarGlow.Reset();

        candyManager.Spawn(config.candyCount, () => {
            bool ok = micCapture.StartCapture();
            if (!ok) Debug.LogWarning("[GameManager] No microphone found.");
            ui.SetMicButtonState(ok, ok);
        });

        capController.ResetAngle();
        ui.HideWinScreen();
    }

    public void Restart()
    {
        won = false;
        ApplyUIConfigValues();

        micCapture.StopCapture();
        ui.SetMicButtonState(false);

        // Reiniciar brillo del jarron
        if (jarGlow != null)
            jarGlow.Reset();

        candyManager.Spawn(config.candyCount, () => {
            bool ok = micCapture.StartCapture();
            if (!ok) Debug.LogWarning("[GameManager] No microphone found.");
            ui.SetMicButtonState(ok, ok);
        });

        capController.ResetAngle();
        ui.HideWinScreen();
    }

    public void OnMicButtonClicked()
    {
        if (micCapture.IsActive)
        {
            micCapture.StopCapture();
            ui.SetMicButtonState(false);
        }
        else
        {
            bool ok = micCapture.StartCapture();
            ui.SetMicButtonState(ok, ok);
        }
    }

    private void ConfigureUIButtonHandlers()
    {
        if (ui == null) return;

        if (ui.restartButton != null)
        {
            ui.restartButton.onClick.RemoveListener(Restart);
            ui.restartButton.onClick.AddListener(Restart);
        }

        if (ui.micButton != null)
        {
            ui.micButton.onClick.RemoveListener(OnMicButtonClicked);
            ui.micButton.onClick.AddListener(OnMicButtonClicked);
        }
    }

    private void ApplyUIConfigValues()
    {
        if (ui == null) return;

        if (ui.candySlider != null)
            config.candyCount = Mathf.RoundToInt(ui.candySlider.value);

        if (ui.impulseSlider != null)
            config.baseImpulse = ui.impulseSlider.value;

        if (ui.sensitivitySlider != null)
            config.micSensitivity = ui.sensitivitySlider.value;

        if (ui.capSpeedSlider != null)
            config.capRotationSpeed = ui.capSpeedSlider.value;
    }

    private void LoadVictoryScene()
    {
        SceneManager.LoadScene(victorySceneName);
    }
}
