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

    [Header("Efectos Visuales")]
    [Tooltip("Controlador de brillo del jarrón – asignar el GameObject del jarrón")]
    public JarGlowController jarGlow;

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

        // Pulso de brillo cada vez que hay fuerza activa
        if (jarGlow != null && pitchDetector.LastForce > 0.01f)
            jarGlow.TriggerNotePulse();

        if (candyManager.IsWon())
        {
            won = true;

            // Primero la explosión visual del jarrón
            if (jarGlow != null)
                jarGlow.TriggerExplosion();

            // Luego la pantalla de victoria (el logo aparece dentro de JarGlowController
            // tras el flash; la win screen muestra el overlay de UI)
            ui.ShowWinScreen();
        }
    }

    public void StartGame()
    {
        won = false;
        micCapture.StopCapture();

        // Reiniciar brillo del jarrón
        if (jarGlow != null)
            jarGlow.Reset();

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

        // Reiniciar brillo del jarrón
        if (jarGlow != null)
            jarGlow.Reset();

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
            ui.SetMicButtonText("Iniciar Micrófono");
        }
        else
        {
            bool ok = micCapture.StartCapture();
            ui.SetMicButtonText(ok ? "Detener Micrófono" : "No se encontró micrófono");
        }
    }
}