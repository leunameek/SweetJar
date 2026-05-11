using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Maneja toda la interfaz de usuario del juego:
/// - Barra de percepción de sonido (visualizador de voz animado)
/// - Barra de progreso de dulces escapados
/// - Pantalla de victoria con logo
/// - HUD de debug (nota, frecuencia, fuerza)
/// - Controles de configuración
/// </summary>
public class UIController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  HUD principal
    // ─────────────────────────────────────────────────────────────
    [Header("HUD – Debug Info (opcional)")]
    [Tooltip("Etiqueta que muestra la nota detectada (puede dejarse vacío)")]
    public TMP_Text noteLabel;
    [Tooltip("Etiqueta que muestra la frecuencia en Hz (puede dejarse vacío)")]
    public TMP_Text freqLabel;
    [Tooltip("Etiqueta que muestra la fuerza aplicada (puede dejarse vacío)")]
    public TMP_Text forceLabel;

    // ─────────────────────────────────────────────────────────────
    //  Barra de percepción de sonido
    // ─────────────────────────────────────────────────────────────
    [Header("Barra de Percepción de Sonido")]
    [Tooltip("Slider que representa el nivel RMS del micrófono (0-1)")]
    public Slider soundBar;
    [Tooltip("Imagen de relleno del slider de sonido (para cambiar color según intensidad)")]
    public Image soundBarFill;
    [Tooltip("Color cuando el sonido es bajo / controlado")]
    public Color soundColorLow = new Color(0.2f, 0.8f, 1f);
    [Tooltip("Color cuando el sonido es medio")]
    public Color soundColorMid = new Color(0.4f, 1f, 0.4f);
    [Tooltip("Color cuando el sonido es demasiado fuerte / descontrolado")]
    public Color soundColorHigh = new Color(1f, 0.3f, 0.2f);
    [Tooltip("Objeto con animación de 'pulso' cuando se detecta nota válida")]
    public GameObject soundPulseIndicator;
    [Tooltip("Etiqueta que muestra la nota activa sobre la barra de sonido")]
    public TMP_Text soundNoteLabel;

    // ─────────────────────────────────────────────────────────────
    //  Barra de progreso de dulces
    // ─────────────────────────────────────────────────────────────
    [Header("Progreso de Dulces")]
    [Tooltip("Slider que muestra el porcentaje de dulces escapados")]
    public Slider progressBar;
    [Tooltip("Imagen de relleno de la barra de progreso")]
    public Image progressBarFill;
    [Tooltip("Etiqueta de porcentaje (ej: '42%')")]
    public TMP_Text progressPercentLabel;
    [Tooltip("Etiqueta de conteo (ej: '126 / 300')")]
    public TMP_Text progressCountLabel;
    [Tooltip("Color inicial de la barra de progreso")]
    public Color progressColorStart = new Color(0.4f, 0.8f, 1f);
    [Tooltip("Color final de la barra de progreso (al llegar al 100%)")]
    public Color progressColorEnd = new Color(1f, 0.9f, 0.2f);

    // ─────────────────────────────────────────────────────────────
    //  Pantalla de victoria
    // ─────────────────────────────────────────────────────────────
    [Header("Pantalla de Victoria")]
    [Tooltip("GameObject raíz de la pantalla de victoria (Canvas o panel)")]
    public GameObject winScreen;
    [Tooltip("Panel del HUD que se oculta al ganar (arrastra el GameObject HUD)")]
    public GameObject hudPanel;
    [Tooltip("Imagen del logo '25' con VHS y gafas VR")]
    public Image winLogoImage;
    [Tooltip("Grupo CanvasGroup del panel de victoria para fade-in")]
    public CanvasGroup winCanvasGroup;
    [Tooltip("Duración del fade-in de la pantalla de victoria")]
    public float winFadeDuration = 1.5f;
    [Tooltip("Efecto de partículas o animación al ganar (opcional)")]
    public ParticleSystem winParticles;

    // ─────────────────────────────────────────────────────────────
    //  Micrófono
    // ─────────────────────────────────────────────────────────────
    [Header("Control de Micrófono")]
    public Button micButton;
    public TMP_Text micButtonText;

    // ─────────────────────────────────────────────────────────────
    //  Configuración (panel de debug/ajustes)
    // ─────────────────────────────────────────────────────────────
    [Header("Panel de Configuración")]
    public Slider candySlider;
    public Slider impulseSlider;
    public Slider sensitivitySlider;
    public Slider capSpeedSlider;
    public Button restartButton;

    // ─────────────────────────────────────────────────────────────
    //  Error Overlay
    // ─────────────────────────────────────────────────────────────
    [Header("Overlay de Error")]
    public GameObject errorOverlay;
    public TMP_Text errorText;

    // ─────────────────────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────────────────────
    private float smoothedRms = 0f;
    private float smoothedProgress = 0f;
    private Coroutine winFadeCoroutine;

    // Umbral a partir del cual el sonido se considera "descontrolado"
    private const float RMS_HIGH_THRESHOLD = 0.7f;
    private const float RMS_MID_THRESHOLD = 0.4f;

    // ─────────────────────────────────────────────────────────────
    //  Inicialización
    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (winScreen != null)
            winScreen.SetActive(false);
        if (errorOverlay != null)
            errorOverlay.SetActive(false);
        if (soundPulseIndicator != null)
            soundPulseIndicator.SetActive(false);

        // Asegurar que el CanvasGroup del win screen empieza en 0
        if (winCanvasGroup != null)
            winCanvasGroup.alpha = 0f;

        // Configurar barra de sonido
        if (soundBar != null)
        {
            soundBar.minValue = 0f;
            soundBar.maxValue = 1f;
            soundBar.value = 0f;
            soundBar.interactable = false;
        }

        // Configurar barra de progreso
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
            progressBar.interactable = false;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  UpdateHUD — llamado desde GameManager cada frame
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Actualiza todos los elementos de la interfaz con los datos actuales del juego.
    /// </summary>
    /// <param name="note">Nota detectada (ej: "C", "G") o null si no hay</param>
    /// <param name="freq">Frecuencia en Hz</param>
    /// <param name="rms">Amplitud RMS del micrófono (0-1 aprox)</param>
    /// <param name="force">Fuerza aplicada a los dulces</param>
    /// <param name="escaped">Dulces que han escapado</param>
    /// <param name="total">Total de dulces</param>
    public void UpdateHUD(string note, float freq, float rms, float force, int escaped, int total)
    {
        UpdateDebugLabels(note, freq, force);
        UpdateSoundBar(note, rms);
        UpdateProgressBar(escaped, total);
    }

    // ─────────────────────────────────────────────────────────────
    //  Barra de sonido
    // ─────────────────────────────────────────────────────────────
    private void UpdateSoundBar(string note, float rms)
    {
        // Suavizado del RMS para que la barra no salte bruscamente
        smoothedRms = Mathf.Lerp(smoothedRms, Mathf.Clamp01(rms * 10f), Time.deltaTime * 12f);

        if (soundBar != null)
            soundBar.value = smoothedRms;

        // Color según intensidad
        if (soundBarFill != null)
        {
            Color targetColor;
            if (smoothedRms >= RMS_HIGH_THRESHOLD)
                targetColor = soundColorHigh;
            else if (smoothedRms >= RMS_MID_THRESHOLD)
                targetColor = Color.Lerp(soundColorMid, soundColorHigh,
                    (smoothedRms - RMS_MID_THRESHOLD) / (RMS_HIGH_THRESHOLD - RMS_MID_THRESHOLD));
            else
                targetColor = Color.Lerp(soundColorLow, soundColorMid,
                    smoothedRms / RMS_MID_THRESHOLD);

            soundBarFill.color = Color.Lerp(soundBarFill.color, targetColor, Time.deltaTime * 8f);
        }

        // Indicador de pulso: activo solo cuando hay nota válida
        bool hasNote = !string.IsNullOrEmpty(note);
        if (soundPulseIndicator != null)
            soundPulseIndicator.SetActive(hasNote && smoothedRms > 0.05f);

        // Etiqueta de nota sobre la barra
        if (soundNoteLabel != null)
            soundNoteLabel.text = hasNote ? note : "—";
    }

    // ─────────────────────────────────────────────────────────────
    //  Barra de progreso
    // ─────────────────────────────────────────────────────────────
    private void UpdateProgressBar(int escaped, int total)
    {
        if (total <= 0) return;

        float targetProgress = (float)escaped / total;

        // Suavizado para que la barra suba progresivamente
        smoothedProgress = Mathf.Lerp(smoothedProgress, targetProgress, Time.deltaTime * 4f);

        if (progressBar != null)
            progressBar.value = smoothedProgress;

        // Color interpolado según avance
        if (progressBarFill != null)
            progressBarFill.color = Color.Lerp(progressColorStart, progressColorEnd, smoothedProgress);

        // Porcentaje
        if (progressPercentLabel != null)
            progressPercentLabel.text = $"{Mathf.RoundToInt(smoothedProgress * 100f)}%";

        // Conteo
        if (progressCountLabel != null)
            progressCountLabel.text = $"{escaped} / {total}";
    }

    // ─────────────────────────────────────────────────────────────
    //  Labels de debug
    // ─────────────────────────────────────────────────────────────
    private void UpdateDebugLabels(string note, float freq, float force)
    {
        if (noteLabel != null)
            noteLabel.text = $"Nota: {(string.IsNullOrEmpty(note) ? "—" : note)}";
        if (freqLabel != null)
            freqLabel.text = freq > 0f ? $"{freq:F1} Hz" : "—";
        if (forceLabel != null)
            forceLabel.text = $"Fuerza: {force:F2}";
    }

    // ─────────────────────────────────────────────────────────────
    //  Pantalla de victoria
    // ─────────────────────────────────────────────────────────────
    public void ShowWinScreen()
    {
        if (winScreen == null) return;

        // Ocultar todo el HUD al ganar
        if (hudPanel != null)
            hudPanel.SetActive(false);

        // Detener corrutina anterior si existía
        if (winFadeCoroutine != null)
            StopCoroutine(winFadeCoroutine);

        winScreen.SetActive(true);
        winFadeCoroutine = StartCoroutine(FadeInWinScreen());

        if (winParticles != null)
            winParticles.Play();
    }

    public void HideWinScreen()
    {
        if (winFadeCoroutine != null)
            StopCoroutine(winFadeCoroutine);

        if (winScreen != null)
            winScreen.SetActive(false);

        if (winCanvasGroup != null)
            winCanvasGroup.alpha = 0f;

        if (winParticles != null)
            winParticles.Stop();

        // Restaurar el HUD al reiniciar
        if (hudPanel != null)
            hudPanel.SetActive(true);
    }

    private IEnumerator FadeInWinScreen()
    {
        if (winCanvasGroup == null) yield break;

        winCanvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < winFadeDuration)
        {
            elapsed += Time.deltaTime;
            winCanvasGroup.alpha = Mathf.Clamp01(elapsed / winFadeDuration);
            yield return null;
        }

        winCanvasGroup.alpha = 1f;
    }

    // ─────────────────────────────────────────────────────────────
    //  Micrófono
    // ─────────────────────────────────────────────────────────────
    public void SetMicButtonText(string text)
    {
        if (micButtonText != null)
            micButtonText.text = text;
    }

    // ─────────────────────────────────────────────────────────────
    //  Error Overlay
    // ─────────────────────────────────────────────────────────────
    public void ShowError(string message)
    {
        if (errorText != null) errorText.text = message;
        if (errorOverlay != null) errorOverlay.SetActive(true);
    }

    public void HideError()
    {
        if (errorOverlay != null) errorOverlay.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  Acceso público al progreso suavizado (para JarGlowController)
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Retorna el progreso actual suavizado (0-1), útil para JarGlowController.
    /// </summary>
    public float SmoothedProgress => smoothedProgress;
}