using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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
    private static readonly Color CardBackground = new Color(0.1647f, 0.1647f, 0.1647f, 0.85f);
    private static readonly Color CardTextColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color TrackShellColor = new Color(0.82f, 0.86f, 0.91f, 0.92f);
    private static readonly Color TrackInnerShadow = new Color(0.42f, 0.48f, 0.56f, 0.22f);
    private static readonly Color AccentBlue = new Color(0.231f, 0.509f, 0.964f, 1f);
    private static readonly Color AccentBlueSoft = new Color(0.376f, 0.631f, 0.98f, 1f);
    private static readonly Color MicLowColor = new Color(0.72f, 0.88f, 0.2f, 1f);
    private static readonly Color MicMidColor = new Color(0.98f, 0.88f, 0.2f, 1f);
    private static readonly Color MicWarmColor = new Color(0.98f, 0.6f, 0.18f, 1f);
    private static readonly Color MicHighColor = new Color(0.92f, 0.24f, 0.2f, 1f);
    private static readonly Color ProgressIdleColor = new Color(0.30f, 0.62f, 0.38f, 0.9f);
    private static readonly Color ProgressActiveColor = new Color(0.36f, 0.78f, 0.45f, 1f);
    private static readonly Color ProgressDividerColor = new Color(1f, 1f, 1f, 0.15f);
    private static readonly Color MutedCoral = new Color(0.96f, 0.42f, 0.38f, 1f);
    private static readonly Color FabBackground = new Color(0.1647f, 0.1647f, 0.1647f, 0.92f);
    private static readonly Color FabBackgroundActive = new Color(0.21f, 0.27f, 0.37f, 0.96f);

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

    [Header("Layout en Editor")]
    [Tooltip("Muestra y actualiza este layout también fuera de Play Mode.")]
    public bool previewLayoutInEditor = true;
    [Tooltip("Si está activo, la disposición visual se vuelve a aplicar desde código.")]
    public bool lockLayoutToCode = true;
    [Tooltip("Si está activo, en Play Mode el script puede reconstruir elementos faltantes del layout.")]
    public bool rebuildLayoutAtRuntime = false;

    // ─────────────────────────────────────────────────────────────
    //  Micrófono
    // ─────────────────────────────────────────────────────────────
    [Header("Control de Micrófono")]
    public Button micButton;
    public TMP_Text micButtonText;
    public Image micButtonIcon;
    public Sprite micMutedIcon;
    public Sprite micUnmutedIcon;
    public Color micMutedColor = Color.white;
    public Color micUnmutedColor = new Color(0.75f, 1f, 0.75f, 1f);

    // ─────────────────────────────────────────────────────────────
    //  Configuración (panel de debug/ajustes)
    // ─────────────────────────────────────────────────────────────
    [Header("Panel de Configuración")]
    public Slider candySlider;
    public Slider impulseSlider;
    public Slider sensitivitySlider;
    public Slider capSpeedSlider;
    public Button restartButton;
    public Sprite restartButtonSprite;
    public Sprite metricsCardSprite;

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
    private TMP_Text soundLevelLabel;
    private TMP_Text restartButtonLabel;
    private Image generatedMicBody;
    private Image generatedMicStem;
    private Image generatedMicBase;
    private Image generatedMicSlash;
    private int lastEscapedCount = -1;
    private float progressActiveUntil = -1f;
    private Font projectFont;
    private TMP_FontAsset projectTmpFont;

    // Umbral a partir del cual el sonido se considera "descontrolado"
    private const float RMS_HIGH_THRESHOLD = 0.7f;
    private const float RMS_MID_THRESHOLD = 0.4f;

    // ─────────────────────────────────────────────────────────────
    //  Inicialización
    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        EnsureMicAssetsLoaded();
        EnsureProjectFontAssetsLoaded();

        if (hudPanel == null)
            hudPanel = gameObject;

        if (micButton != null)
        {
            RectTransform micRect = micButton.GetComponent<RectTransform>();
            bool hasCustomMicSprites = micMutedIcon != null || micUnmutedIcon != null;
            EnsureMicButtonIcon(micRect, hasCustomMicSprites);
            SyncMicVisualHierarchy(hasCustomMicSprites);
        }

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

        ApplyProjectFonts();
        SetupWinScreenLayout();
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
                targetColor = MicHighColor;
            else if (smoothedRms >= RMS_MID_THRESHOLD)
                targetColor = Color.Lerp(MicWarmColor, MicHighColor,
                    (smoothedRms - RMS_MID_THRESHOLD) / (RMS_HIGH_THRESHOLD - RMS_MID_THRESHOLD));
            else if (smoothedRms >= 0.2f)
                targetColor = Color.Lerp(MicMidColor, MicWarmColor,
                    (smoothedRms - 0.2f) / (RMS_MID_THRESHOLD - 0.2f));
            else
                targetColor = Color.Lerp(MicLowColor, MicMidColor,
                    smoothedRms / 0.2f);

            soundBarFill.color = Color.Lerp(soundBarFill.color, targetColor, Time.deltaTime * 8f);
        }

        // Indicador de pulso: activo solo cuando hay nota válida
        bool hasNote = !string.IsNullOrEmpty(note);
        if (soundPulseIndicator != null)
            soundPulseIndicator.SetActive(hasNote && smoothedRms > 0.05f);

        // Etiqueta de nota sobre la barra
        if (soundNoteLabel != null)
            soundNoteLabel.text = hasNote ? note : "—";

        if (soundLevelLabel != null)
            soundLevelLabel.text = $"{Mathf.RoundToInt(smoothedRms * 100f)}%";
    }

    // ─────────────────────────────────────────────────────────────
    //  Barra de progreso
    // ─────────────────────────────────────────────────────────────
    private void UpdateProgressBar(int escaped, int total)
    {
        if (total <= 0) return;

        float targetProgress = (float)escaped / total;

        if (lastEscapedCount < 0)
            lastEscapedCount = escaped;

        if (escaped > lastEscapedCount)
            progressActiveUntil = Time.time + 0.4f;

        lastEscapedCount = escaped;

        // Suavizado para que la barra suba progresivamente
        smoothedProgress = Mathf.Lerp(smoothedProgress, targetProgress, Time.deltaTime * 4f);

        if (progressBar != null)
            progressBar.value = smoothedProgress;

        // Color interpolado según avance
        if (progressBarFill != null)
        {
            Color progressColor = Time.time < progressActiveUntil ? ProgressActiveColor : ProgressIdleColor;
            progressBarFill.color = Color.Lerp(progressBarFill.color, progressColor, Time.deltaTime * 8f);
        }

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
            noteLabel.text = $"<b>NOTA</b>\n{(string.IsNullOrEmpty(note) ? "—" : note)}";
        if (freqLabel != null)
            freqLabel.text = freq > 0f ? $"<b>HZ</b>\n{freq:F1}" : "<b>HZ</b>\n—";
        if (forceLabel != null)
            forceLabel.text = $"<b>FUERZA</b>\n{force:F2}";
    }

    // ─────────────────────────────────────────────────────────────
    //  Pantalla de victoria
    // ─────────────────────────────────────────────────────────────
    public void ShowWinScreen()
    {
        if (winScreen == null) return;

        SetupWinScreenLayout();
        SetHudVisible(false);

        // Detener corrutina anterior si existía
        if (winFadeCoroutine != null)
            StopCoroutine(winFadeCoroutine);

        winScreen.SetActive(true);

        if (winLogoImage != null)
            winLogoImage.gameObject.SetActive(true);

        if (!isActiveAndEnabled)
        {
            if (winCanvasGroup != null)
                winCanvasGroup.alpha = 1f;
        }
        else
        {
            winFadeCoroutine = StartCoroutine(FadeInWinScreen());
        }

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

        if (winLogoImage != null)
            winLogoImage.gameObject.SetActive(false);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            restartButton.interactable = true;
        }

        // Restaurar el HUD al reiniciar
        SetHudVisible(true);
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

    public void SetMicButtonState(bool isActive, bool hasMicrophone = true)
    {
        EnsureMicAssetsLoaded();
        SyncMicVisualHierarchy(micMutedIcon != null || micUnmutedIcon != null);
        SetMicButtonText(hasMicrophone ? (isActive ? "Mic On" : "Mic Off") : "Sin Mic");

        if (micButton != null && micButton.targetGraphic is Image buttonBackground)
        {
            bool hasCustomMicSprites = micMutedIcon != null || micUnmutedIcon != null;
            buttonBackground.color = hasCustomMicSprites
                ? Color.clear
                : (hasMicrophone && isActive ? FabBackgroundActive : FabBackground);
        }

        if (micButtonIcon != null)
        {
            Sprite stateSprite = hasMicrophone
                ? (isActive ? micUnmutedIcon : micMutedIcon)
                : micMutedIcon;

            if (stateSprite != null)
                micButtonIcon.sprite = stateSprite;

            micButtonIcon.gameObject.SetActive(stateSprite != null);
            ApplyMicIconSizing();
            micButtonIcon.color = hasMicrophone && isActive ? micUnmutedColor : micMutedColor;
        }

        UpdateGeneratedMicIcon(isActive, hasMicrophone);
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

    private void SetHudVisible(bool isVisible)
    {
        if (hudPanel == null)
            return;

        if (hudPanel != gameObject)
        {
            hudPanel.SetActive(isVisible);
            return;
        }

        Transform root = hudPanel.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (winScreen != null && child.gameObject == winScreen)
                continue;

            child.gameObject.SetActive(isVisible);
        }
    }

    private void SetupWinScreenLayout()
    {
        if (winScreen == null) return;

        RectTransform winRoot = winScreen.transform as RectTransform;
        if (winRoot == null) return;

        if (winCanvasGroup == null)
            winCanvasGroup = winScreen.GetComponent<CanvasGroup>();

        if (winLogoImage != null)
        {
            RectTransform logoRect = winLogoImage.rectTransform;
            logoRect.SetParent(winRoot, false);
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = new Vector2(0f, 10f);
            logoRect.sizeDelta = new Vector2(460f, 460f);
            logoRect.localScale = Vector3.one;
            winLogoImage.preserveAspect = true;
            winLogoImage.raycastTarget = false;
            winLogoImage.gameObject.SetActive(false);
            winLogoImage.transform.SetAsLastSibling();
        }

    }

    [ContextMenu("Aplicar Layout a la Escena")]
    private void BakeLayoutToScene()
    {
        previewLayoutInEditor = true;
        ApplyBentoLayout();
        lockLayoutToCode = false;
        MarkSceneDirtyInEditor();
    }

    [ContextMenu("Desbloquear Layout para Edicion Manual")]
    private void UnlockLayoutForManualEditing()
    {
        previewLayoutInEditor = true;
        lockLayoutToCode = false;
        MarkSceneDirtyInEditor();
    }

    private void ApplyBentoLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null) return;

        soundColorLow = MicLowColor;
        soundColorMid = MicMidColor;
        soundColorHigh = MicHighColor;
        progressColorStart = ProgressIdleColor;
        progressColorEnd = ProgressIdleColor;

        SetupInfoCard(root);
        SetupMetricsCard(root);
        SetupMicFab(root);
        SetupGameplayRestartButton(root);
    }

    private void SetupGameplayRestartButton(RectTransform root)
    {
        if (restartButton == null)
            return;

        RectTransform buttonRect = restartButton.GetComponent<RectTransform>();
        buttonRect.SetParent(root, false);
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-42f, 42f);
        buttonRect.sizeDelta = new Vector2(340f, 108f);
        buttonRect.localScale = Vector3.one;

        if (restartButton.targetGraphic is Image background)
            ConfigureRestartButtonVisuals(background, restartButton);

        restartButtonLabel ??= restartButton.GetComponentInChildren<TMP_Text>(true);
        if (restartButtonLabel != null)
        {
            bool hasCustomSprite = restartButtonSprite != null;
            restartButtonLabel.gameObject.SetActive(!hasCustomSprite);

            if (!hasCustomSprite)
            {
                restartButtonLabel.text = "Reiniciar";
                restartButtonLabel.alignment = TextAlignmentOptions.Center;
                restartButtonLabel.fontSize = 26f;
                restartButtonLabel.color = Color.white;
                restartButtonLabel.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        restartButton.gameObject.SetActive(true);
        restartButton.interactable = true;
        restartButton.transform.SetAsLastSibling();
    }

    private void SetupInfoCard(RectTransform root)
    {
        RectTransform card = CreateCard("TopInfoCard", root, new Vector2(28f, -28f), new Vector2(360f, 112f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        ApplyMetricsCardSprite(card);
        EnsureTopInfoCardEditable(card);

        StyleInfoText(noteLabel, card, new Vector2(70f, -56f), new Vector2(86f, 70f));
        StyleInfoText(freqLabel, card, new Vector2(180f, -56f), new Vector2(86f, 70f));
        StyleInfoText(forceLabel, card, new Vector2(290f, -56f), new Vector2(86f, 70f));
    }

    private void SetupMetricsCard(RectTransform root)
    {
        RectTransform card = CreateCard("BottomMetricsCard", root, new Vector2(28f, 28f), new Vector2(440f, 132f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        ApplyMetricsCardSprite(card);

        CreateOrUpdateSoundLevelLabel(card);
        CreateValueFrame("SoundValueFrame", card, new Vector2(368f, -32f), new Vector2(62f, 34f));
        CreateValueFrame("ProgressValueFrame", card, new Vector2(260f, -84f), new Vector2(170f, 34f));
        StyleMetricText(progressCountLabel, card, new Vector2(278f, -84f), new Vector2(96f, 24f));
        StyleMetricText(progressPercentLabel, card, new Vector2(380f, -84f), new Vector2(50f, 24f));

        StyleBar(soundBar, soundBarFill, card, new Vector2(22f, -32f), new Vector2(332f, 34f), false);
        StyleBar(progressBar, progressBarFill, card, new Vector2(22f, -84f), new Vector2(332f, 34f), false);
    }

    private void SetupMicFab(RectTransform root)
    {
        if (micButton == null) return;

        EnsureMicAssetsLoaded();
        bool hasCustomMicSprites = micMutedIcon != null || micUnmutedIcon != null;
        if (hasCustomMicSprites)
            RemoveLegacyMicIconRoot();

        RectTransform rect = micButton.GetComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-60f, 60f);
        rect.sizeDelta = new Vector2(500f, 500f);

        if (micButton.targetGraphic is Image background)
        {
            background.sprite = hasCustomMicSprites ? null : background.sprite;
            background.type = hasCustomMicSprites ? Image.Type.Simple : Image.Type.Sliced;
            background.color = hasCustomMicSprites ? Color.clear : FabBackground;
            background.raycastTarget = true;
        }

        ColorBlock colors = micButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.98f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.fadeDuration = 0.2f;
        micButton.colors = colors;

        if (micButtonText != null)
            micButtonText.gameObject.SetActive(false);

        EnsureMicButtonIcon(rect, hasCustomMicSprites);
        SyncMicVisualHierarchy(hasCustomMicSprites);
        if (micButtonIcon != null)
        {
            micButtonIcon.gameObject.SetActive(hasCustomMicSprites);
            RectTransform iconRect = micButtonIcon.rectTransform;
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            ApplyMicIconSizing();
        }

        EnsureGeneratedMicIcon();
        SetMicButtonState(false);
    }

    private void StyleInfoText(TMP_Text label, RectTransform parent, Vector2 defaultPosition, Vector2 defaultSize)
    {
        if (label == null) return;

        bool alreadyUnderParent = label.rectTransform.parent == parent;
        label.rectTransform.SetParent(parent, false);
        ReleaseTopInfoRect(label.rectTransform, !alreadyUnderParent, defaultPosition, defaultSize);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 22f;
        label.color = CardTextColor;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.fontWeight = FontWeight.Regular;
        ApplyProjectFont(label);

        RemoveComponentIfExists<LayoutElement>(label.gameObject);
    }

    private void ReleaseTopInfoRect(RectTransform rect, bool forceDefaults, Vector2 defaultPosition, Vector2 defaultSize)
    {
        bool isStretching = rect.anchorMin != rect.anchorMax;

        if (forceDefaults || isStretching)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = defaultPosition;
            rect.sizeDelta = defaultSize;
        }
    }

    private void EnsureTopInfoCardEditable(RectTransform card)
    {
        if (card == null)
            return;

        RemoveComponentIfExists<HorizontalLayoutGroup>(card.gameObject);
        RemoveComponentIfExists<ContentSizeFitter>(card.gameObject);

        if (noteLabel != null)
            RemoveComponentIfExists<LayoutElement>(noteLabel.gameObject);
        if (freqLabel != null)
            RemoveComponentIfExists<LayoutElement>(freqLabel.gameObject);
        if (forceLabel != null)
            RemoveComponentIfExists<LayoutElement>(forceLabel.gameObject);
    }

    private void StyleMetricText(TMP_Text label, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        if (label == null) return;

        RectTransform rect = label.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        label.alignment = TextAlignmentOptions.Right;
        label.fontSize = 18f;
        label.color = CardTextColor;
        label.fontWeight = FontWeight.SemiBold;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        ApplyProjectFont(label);
    }

    private void CreateOrUpdateSoundLevelLabel(RectTransform parent)
    {
        if (soundLevelLabel == null)
        {
            GameObject labelObject = new GameObject("SoundLevelValue", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            soundLevelLabel = labelObject.GetComponent<TextMeshProUGUI>();

            TMP_Text source = progressPercentLabel != null ? progressPercentLabel : micButtonText;
            if (source != null)
            {
                soundLevelLabel.font = source.font;
                soundLevelLabel.fontSharedMaterial = source.fontSharedMaterial;
            }
        }

        RectTransform rect = soundLevelLabel.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(382f, -30f);
        rect.sizeDelta = new Vector2(48f, 24f);

        soundLevelLabel.alignment = TextAlignmentOptions.Right;
        soundLevelLabel.fontSize = 18f;
        soundLevelLabel.color = CardTextColor;
        soundLevelLabel.fontWeight = FontWeight.SemiBold;
        soundLevelLabel.textWrappingMode = TextWrappingModes.NoWrap;
        soundLevelLabel.text = "0%";
        ApplyProjectFont(soundLevelLabel);
    }

    private void StyleBar(Slider slider, Image fill, RectTransform parent, Vector2 anchoredPosition, Vector2 size, bool showHandle)
    {
        if (slider == null) return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.SetParent(parent, false);
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = size;

        if (slider.targetGraphic is Image background)
        {
            if (background.sprite == null)
                background.sprite = ResolveDefaultSprite();
            background.type = Image.Type.Sliced;
            background.color = TrackShellColor;

            RectTransform bgRect = background.rectTransform;
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0f, 0f);

            Outline outline = GetOrAdd<Outline>(background.gameObject);
            outline.effectColor = new Color(0.42f, 0.5f, 0.58f, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            Shadow shellShadow = GetOrAdd<Shadow>(background.gameObject);
            shellShadow.effectColor = new Color(1f, 1f, 1f, 0.18f);
            shellShadow.effectDistance = new Vector2(0f, 1f);
        }

        StyleAreaRect(slider.fillRect != null ? slider.fillRect.parent as RectTransform : null, new Vector2(10f, 0f), new Vector2(-24f, -10f));
        StyleAreaRect(slider.handleRect != null ? slider.handleRect.parent as RectTransform : null, new Vector2(10f, 0f), new Vector2(-24f, -10f));

        if (fill != null)
        {
            if (fill.sprite == null)
                fill.sprite = ResolveDefaultSprite();
            fill.type = Image.Type.Sliced;
            fill.color = AccentBlue;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Shadow fillShadow = GetOrAdd<Shadow>(fill.gameObject);
            fillShadow.effectColor = new Color(1f, 1f, 1f, 0.14f);
            fillShadow.effectDistance = new Vector2(0f, 1f);
        }

        if (slider.handleRect != null)
        {
            RectTransform handleRect = slider.handleRect;
            handleRect.sizeDelta = showHandle ? new Vector2(10f, 42f) : new Vector2(10f, 10f);

            Image handleImage = handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                if (handleImage.sprite == null)
                    handleImage.sprite = ResolveDefaultSprite();
                handleImage.type = Image.Type.Sliced;
                handleImage.color = new Color(0.95f, 0.97f, 1f, showHandle ? 1f : 0f);
            }

            Shadow shadow = GetOrAdd<Shadow>(handleRect.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, showHandle ? 0.18f : 0f);
            shadow.effectDistance = new Vector2(0f, -2f);

            Outline handleOutline = GetOrAdd<Outline>(handleRect.gameObject);
            handleOutline.effectColor = new Color(0.45f, 0.53f, 0.61f, showHandle ? 0.45f : 0f);
            handleOutline.effectDistance = new Vector2(1f, -1f);

            handleRect.gameObject.SetActive(showHandle);
        }

        EnsureTrackInset(slider, showHandle);

        if (!showHandle)
            EnsureProgressDividers(sliderRect);
        else
            RemoveProgressDividers(sliderRect);
    }

    private RectTransform CreateCard(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Transform existing = parent.Find(name);
        if (existing == null && Application.isPlaying && !rebuildLayoutAtRuntime)
            return null;
        GameObject cardObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = cardObject.GetComponent<Image>();
        if (metricsCardSprite != null)
        {
            image.sprite = metricsCardSprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }
        else
        {
            if (image.sprite == null)
                image.sprite = ResolveDefaultSprite();
            image.color = CardBackground;
            image.type = Image.Type.Sliced;
        }
        image.raycastTarget = false;

        return rect;
    }

    private void ApplyMetricsCardSprite(RectTransform card)
    {
        if (card == null || metricsCardSprite == null)
            return;

        Image image = card.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = metricsCardSprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
    }

    private RectTransform CreateValueFrame(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing == null && Application.isPlaying && !rebuildLayoutAtRuntime)
            return null;
        GameObject frameObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = frameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = frameObject.GetComponent<Image>();
        if (image.sprite == null)
            image.sprite = ResolveDefaultSprite();
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.08f);
        image.raycastTarget = false;

        Outline outline = GetOrAdd<Outline>(frameObject);
        outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
        outline.effectDistance = new Vector2(1f, -1f);

        return rect;
    }

    private void EnsureGeneratedMicIcon()
    {
        if (micButton == null || generatedMicBody != null) return;

        RectTransform buttonRect = micButton.GetComponent<RectTransform>();
        Sprite sprite = micButton.targetGraphic is Image buttonImage ? buttonImage.sprite : null;

        RectTransform iconRoot = CreateIconRect("MicIconRoot", buttonRect, Vector2.zero, new Vector2(34f, 34f));
        if (iconRoot == null) return;
        generatedMicBody = CreateIconImage("Body", iconRoot, sprite, new Vector2(0f, 4f), new Vector2(14f, 18f), true);
        generatedMicStem = CreateIconImage("Stem", iconRoot, sprite, new Vector2(0f, -7f), new Vector2(4f, 10f), false);
        generatedMicBase = CreateIconImage("Base", iconRoot, sprite, new Vector2(0f, -13f), new Vector2(16f, 3f), false);
        generatedMicSlash = CreateIconImage("Slash", iconRoot, sprite, new Vector2(8f, 1f), new Vector2(4f, 28f), false);
        if (generatedMicBody == null || generatedMicStem == null || generatedMicBase == null || generatedMicSlash == null)
            return;

        generatedMicSlash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -35f);
    }

    private void UpdateGeneratedMicIcon(bool isActive, bool hasMicrophone)
    {
        if (generatedMicBody == null)
            return;

        bool hasCustomMicSprites = micMutedIcon != null || micUnmutedIcon != null;
        if (generatedMicBody.transform.parent != null)
            generatedMicBody.transform.parent.gameObject.SetActive(!hasCustomMicSprites);

        if (hasCustomMicSprites)
            return;

        Color iconColor = hasMicrophone && isActive ? AccentBlueSoft : CardTextColor;
        Color slashColor = hasMicrophone ? MutedCoral : MutedCoral;

        generatedMicBody.color = iconColor;
        generatedMicStem.color = iconColor;
        generatedMicBase.color = iconColor;
        generatedMicSlash.color = slashColor;
        generatedMicSlash.gameObject.SetActive(!hasMicrophone || !isActive);
    }

    private void EnsureMicButtonIcon(RectTransform buttonRect, bool hasCustomMicSprites)
    {
        if (!hasCustomMicSprites || micButton != null && micButtonIcon != null)
            return;

        Transform existing = buttonRect.Find("MicStateIcon");
        GameObject iconObject = existing != null
            ? existing.gameObject
            : new GameObject("MicStateIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        iconObject.transform.SetParent(buttonRect, false);
        micButtonIcon = iconObject.GetComponent<Image>();
        micButtonIcon.raycastTarget = false;
    }

    private void SyncMicVisualHierarchy(bool hasCustomMicSprites)
    {
        if (micButton == null)
            return;

        Transform legacyRoot = micButton.transform.Find("MicIconRoot");
        if (legacyRoot != null)
            legacyRoot.gameObject.SetActive(!hasCustomMicSprites);

        if (micButtonIcon != null)
            micButtonIcon.gameObject.SetActive(hasCustomMicSprites);
    }

    private void RemoveLegacyMicIconRoot()
    {
        if (micButton == null)
            return;

        Transform legacyRoot = micButton.transform.Find("MicIconRoot");
        if (legacyRoot == null)
            return;

        if (Application.isPlaying)
            Destroy(legacyRoot.gameObject);
        else
            DestroyObjectInEditorSafely(legacyRoot.gameObject);
    }

    private void ApplyMicIconSizing()
    {
        if (micButtonIcon == null)
            return;

        RectTransform iconRect = micButtonIcon.rectTransform;
        Sprite sprite = micButtonIcon.sprite;
        micButtonIcon.preserveAspect = true;

        if (sprite == null)
        {
            iconRect.sizeDelta = new Vector2(34f, 34f);
            return;
        }

        Rect spriteRect = sprite.rect;
        float aspect = spriteRect.height > 0.001f ? spriteRect.width / spriteRect.height : 1f;
        float height = 180f;
        float width = height * aspect;
        const float maxWidth = 240f;

        if (width > maxWidth)
        {
            width = maxWidth;
            height = width / Mathf.Max(0.001f, aspect);
        }

        iconRect.sizeDelta = new Vector2(width, height);
    }

    private RectTransform CreateIconRect(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing == null && Application.isPlaying && !rebuildLayoutAtRuntime)
            return null;
        GameObject obj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private Image CreateIconImage(string name, RectTransform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 size, bool sliced)
    {
        Transform existing = parent.Find(name);
        if (existing == null && Application.isPlaying && !rebuildLayoutAtRuntime)
            return null;
        GameObject obj;
        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    private T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = target.AddComponent<T>();
        return component;
    }

    private void RemoveComponentIfExists<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            return;

        if (Application.isPlaying)
            Destroy(component);
        else
            DestroyObjectInEditorSafely(component);
    }

    private void StyleAreaRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void EnsureTrackInset(Slider slider, bool showHandle)
    {
        if (slider.targetGraphic is not Image background)
            return;

        RectTransform bgRect = background.rectTransform;
        Transform existing = bgRect.Find("Inset");
        GameObject insetObject = existing != null ? existing.gameObject : new GameObject("Inset", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform insetRect = insetObject.GetComponent<RectTransform>();
        insetRect.SetParent(bgRect, false);
        insetRect.anchorMin = new Vector2(0f, 0.5f);
        insetRect.anchorMax = new Vector2(1f, 0.5f);
        insetRect.pivot = new Vector2(0.5f, 0.5f);
        insetRect.anchoredPosition = Vector2.zero;
        insetRect.sizeDelta = new Vector2(-12f, -12f);

        Image insetImage = insetObject.GetComponent<Image>();
        if (insetImage.sprite == null)
            insetImage.sprite = ResolveDefaultSprite();
        insetImage.type = Image.Type.Sliced;
        insetImage.color = showHandle ? new Color(0.83f, 0.87f, 0.92f, 0.44f) : TrackInnerShadow;
        insetImage.raycastTarget = false;
    }

    private void EnsureProgressDividers(RectTransform sliderRect)
    {
        RectTransform container = CreateIconRect("ProgressDividers", sliderRect, new Vector2(0f, 0f), new Vector2(0f, 0f));
        container.anchorMin = new Vector2(0f, 0f);
        container.anchorMax = new Vector2(1f, 1f);
        container.offsetMin = new Vector2(24f, 8f);
        container.offsetMax = new Vector2(-36f, -8f);

        for (int i = 0; i < 5; i++)
        {
            string name = $"Divider_{i}";
            Transform existing = container.Find(name);
            GameObject dividerObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
            dividerRect.SetParent(container, false);
            dividerRect.anchorMin = new Vector2((i + 1) / 6f, 0.18f);
            dividerRect.anchorMax = new Vector2((i + 1) / 6f, 0.82f);
            dividerRect.pivot = new Vector2(0.5f, 0.5f);
            dividerRect.sizeDelta = new Vector2(2f, 0f);
            dividerRect.anchoredPosition = Vector2.zero;

            Image dividerImage = dividerObject.GetComponent<Image>();
            dividerImage.color = ProgressDividerColor;
            dividerImage.raycastTarget = false;
        }
    }

    private void RemoveProgressDividers(RectTransform sliderRect)
    {
        Transform existing = sliderRect.Find("ProgressDividers");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyObjectInEditorSafely(existing.gameObject);
        }
    }

    private Sprite ResolveDefaultSprite()
    {
        if (micButton != null && micButton.targetGraphic is Image micImage && micImage.sprite != null)
            return micImage.sprite;

        if (soundBarFill != null && soundBarFill.sprite != null)
            return soundBarFill.sprite;

        if (progressBarFill != null && progressBarFill.sprite != null)
            return progressBarFill.sprite;

        return null;
    }

    private void ConfigureRestartButtonVisuals(Image background, Button button)
    {
        if (restartButtonSprite != null)
        {
            background.sprite = restartButtonSprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.color = Color.white;
        }
        else
        {
            background.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
            background.type = Image.Type.Sliced;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.72f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.58f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.72f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        HoverScaleButton hoverScale = button.GetComponent<HoverScaleButton>();
        if (hoverScale == null)
            hoverScale = button.gameObject.AddComponent<HoverScaleButton>();
        hoverScale.hoverScale = 1.08f;
        hoverScale.pressedScale = 1.03f;
        hoverScale.transitionSpeed = 14f;
    }

    private void EnsureMicAssetsLoaded()
    {
#if UNITY_EDITOR
        if (micMutedIcon == null)
            micMutedIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Props/MicOff.png");

        if (micUnmutedIcon == null)
            micUnmutedIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Props/MicOn.png");
#endif
    }

    private void EnsureProjectFontAssetsLoaded()
    {
        if (projectFont == null)
            projectFont = Resources.Load<Font>("Fonts/ShareTechMono-Regular");

        if (projectTmpFont == null && projectFont != null)
            projectTmpFont = TMP_FontAsset.CreateFontAsset(projectFont);
    }

    private void ApplyProjectFonts()
    {
        ApplyProjectFont(noteLabel);
        ApplyProjectFont(freqLabel);
        ApplyProjectFont(forceLabel);
        ApplyProjectFont(soundNoteLabel);
        ApplyProjectFont(progressPercentLabel);
        ApplyProjectFont(progressCountLabel);
        ApplyProjectFont(micButtonText);
        ApplyProjectFont(errorText);
        ApplyProjectFont(soundLevelLabel);
        ApplyProjectFont(restartButtonLabel);
    }

    private void ApplyProjectFont(TMP_Text label)
    {
        if (label == null)
            return;

        EnsureProjectFontAssetsLoaded();
        if (projectTmpFont == null)
            return;

        label.font = projectTmpFont;
        if (projectTmpFont.material != null)
            label.fontSharedMaterial = projectTmpFont.material;
    }

    private static void DestroyObjectInEditorSafely(Object obj)
    {
        if (obj == null)
            return;

#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (obj == null)
                return;

            DestroyImmediate(obj);
        };
#else
        Destroy(obj);
#endif
    }

    private void QueueEditorLayoutRefresh()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying)
                return;

            RectTransform root = transform as RectTransform;
            RectTransform topInfoCard = root != null ? root.Find("TopInfoCard") as RectTransform : null;
            EnsureTopInfoCardEditable(topInfoCard);
        };
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (restartButtonSprite == null)
            restartButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Props/Restart.png");

        if (metricsCardSprite == null)
            metricsCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Props/Barra.png");
        EnsureMicAssetsLoaded();

        if (!Application.isPlaying)
            QueueEditorLayoutRefresh();
    }
#endif

    private void MarkSceneDirtyInEditor()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}
