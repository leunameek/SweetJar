using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Controla el brillo del jarrón y la irradiación global de la pantalla.
///
/// Flujo:
/// - A medida que escapedCount / totalCandies aumenta, el jarrón emite más luz.
/// - En paralelo, la pantalla empieza a blanquearse progresivamente.
/// - Al llegar al win ratio, la escena entra en blanco total, se sostiene unos segundos
///   y luego recién cambia a la escena de victoria manteniendo ese fondo blanco.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class JarGlowController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Renderer del jarrón translúcido (glass)")]
    public Renderer jarRenderer;

    [Tooltip("UIController para leer el progreso suavizado")]
    public UIController ui;

    [Tooltip("CandyManager para leer escaped / total directamente")]
    public CandyManager candyManager;

    [Header("Brillo del Jarrón")]
    [Tooltip("Color de emisión a intensidad mínima (jarrón lleno)")]
    public Color glowColorMin = new Color(0.5f, 0.8f, 1f);

    [Tooltip("Color de emisión a intensidad alta antes del clímax final")]
    public Color glowColorMax = new Color(1f, 0.95f, 0.5f);

    [Tooltip("Intensidad mínima de emisión (HDR, en escala lineal)")]
    public float glowIntensityMin = 0f;

    [Tooltip("Intensidad máxima durante el progreso normal")]
    public float glowIntensityMax = 3f;

    [Tooltip("Intensidad máxima que debe alcanzar el jarrón al llegar al win ratio")]
    public float glowIntensityAtWin = 10f;

    [Tooltip("Velocidad de interpolación del brillo (más alto = más reactivo)")]
    public float glowSmoothSpeed = 3f;

    [Header("Secuencia de Victoria")]
    [Tooltip("Color del jarrón cuando alcanza su punto máximo")]
    public Color victoryGlowColor = Color.white;

    [Tooltip("Luz puntual opcional que refuerza el efecto en la escena")]
    public Light explosionPointLight;

    [Tooltip("Intensidad máxima de la luz puntual durante la victoria")]
    public float explosionLightIntensity = 5f;

    [Header("Irradiación de Pantalla")]
    [Tooltip("Porcentaje relativo al win ratio a partir del cual la pantalla empieza a blanquearse")]
    [Range(0f, 1f)] public float screenGlowStart = 0.45f;

    [Tooltip("Curva del blanqueo progresivo de pantalla. Más alto = más suave al inicio, más agresivo al final")]
    public float screenGlowExponent = 2.4f;

    [Tooltip("Opacidad máxima del blanco antes de llegar a la victoria")]
    [Range(0f, 1f)] public float maxPreWinScreenAlpha = 0.82f;

    [Tooltip("Tiempo que tarda en llegar de blanco fuerte a blanco total al ganar")]
    public float screenFlashRampDuration = 0.22f;

    [Tooltip("Tiempo que la pantalla permanece completamente blanca antes de cambiar de escena")]
    public float fullWhiteHoldDuration = 0.8f;

    [Header("Pulso por Nota")]
    [Tooltip("Intensidad extra de emisión cuando se detecta una nota y se aplica fuerza")]
    public float notePulseIntensity = 1.5f;

    [Tooltip("Velocidad de decaimiento del pulso por nota")]
    public float notePulseDecay = 8f;

    private Material jarMaterial;
    private float currentGlowIntensity;
    private float notePulse;
    private bool exploded;
    private Coroutine victorySequenceCoroutine;
    private Color originalBaseColor = Color.white;
    private int baseColorPropertyId = -1;
    private bool hasBaseColorProperty;
    private Canvas overlayCanvas;
    private Image screenOverlay;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        if (jarRenderer == null)
            jarRenderer = GetComponent<Renderer>();

        if (jarRenderer != null)
        {
            jarMaterial = jarRenderer.material;
            jarMaterial.EnableKeyword("_EMISSION");

            if (jarMaterial.HasProperty("_BaseColor"))
                baseColorPropertyId = Shader.PropertyToID("_BaseColor");
            else if (jarMaterial.HasProperty("_Color"))
                baseColorPropertyId = Shader.PropertyToID("_Color");

            hasBaseColorProperty = baseColorPropertyId != -1;
            if (hasBaseColorProperty)
                originalBaseColor = jarMaterial.GetColor(baseColorPropertyId);
        }

        if (explosionPointLight != null)
        {
            explosionPointLight.intensity = 0f;
            explosionPointLight.enabled = false;
        }

        EnsureScreenOverlay();
        SetScreenOverlayAlpha(0f);
    }

    void Update()
    {
        if (jarMaterial == null || exploded)
            return;

        float progressToWin = GetProgressToWin();
        float targetIntensity = Mathf.Lerp(glowIntensityMin, glowIntensityAtWin, progressToWin);
        float regularCap = Mathf.Lerp(glowIntensityMin, glowIntensityMax, progressToWin);
        targetIntensity = Mathf.Max(targetIntensity * 0.65f, regularCap);

        notePulse = Mathf.Max(0f, notePulse - notePulseDecay * Time.deltaTime);
        targetIntensity += notePulse;

        currentGlowIntensity = Mathf.Lerp(currentGlowIntensity, targetIntensity, Time.deltaTime * glowSmoothSpeed);

        Color glowColor = Color.Lerp(glowColorMin, glowColorMax, progressToWin);
        if (progressToWin > 0.8f)
            glowColor = Color.Lerp(glowColor, victoryGlowColor, (progressToWin - 0.8f) / 0.2f * 0.65f);

        SetEmission(glowColor, currentGlowIntensity);
        UpdateVictoryLight(progressToWin);
        UpdateScreenGlow(progressToWin);
    }

    public void TriggerNotePulse()
    {
        notePulse = notePulseIntensity;
    }

    public void TriggerExplosion(Action onSequenceCompleted = null)
    {
        if (exploded)
            return;

        exploded = true;

        if (victorySequenceCoroutine != null)
            StopCoroutine(victorySequenceCoroutine);

        victorySequenceCoroutine = StartCoroutine(VictorySequence(onSequenceCompleted));
    }

    public void Reset()
    {
        if (victorySequenceCoroutine != null)
        {
            StopCoroutine(victorySequenceCoroutine);
            victorySequenceCoroutine = null;
        }

        exploded = false;
        notePulse = 0f;
        currentGlowIntensity = 0f;

        if (jarRenderer != null)
            jarRenderer.enabled = true;

        if (jarMaterial != null)
        {
            SetEmission(glowColorMin, 0f);
            SetJarOpacity(1f);
        }

        if (explosionPointLight != null)
        {
            explosionPointLight.intensity = 0f;
            explosionPointLight.enabled = false;
        }

        SetScreenOverlayAlpha(0f);
    }

    private IEnumerator VictorySequence(Action onSequenceCompleted)
    {
        float elapsed = 0f;
        float startIntensity = currentGlowIntensity;
        float startOverlayAlpha = GetScreenOverlayAlpha();
        float startLightIntensity = explosionPointLight != null ? explosionPointLight.intensity : 0f;

        if (explosionPointLight != null)
            explosionPointLight.enabled = true;

        while (elapsed < screenFlashRampDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, screenFlashRampDuration));

            currentGlowIntensity = Mathf.Lerp(startIntensity, glowIntensityAtWin, t);
            SetEmission(victoryGlowColor, currentGlowIntensity);
            SetScreenOverlayAlpha(Mathf.Lerp(startOverlayAlpha, 1f, t));

            if (explosionPointLight != null)
                explosionPointLight.intensity = Mathf.Lerp(startLightIntensity, explosionLightIntensity, t);

            yield return null;
        }

        currentGlowIntensity = glowIntensityAtWin;
        SetEmission(victoryGlowColor, currentGlowIntensity);
        SetScreenOverlayAlpha(1f);
        SetJarOpacity(0f);

        if (jarRenderer != null)
            jarRenderer.enabled = false;

        if (explosionPointLight != null)
        {
            explosionPointLight.enabled = true;
            explosionPointLight.intensity = explosionLightIntensity;
        }

        yield return new WaitForSeconds(fullWhiteHoldDuration);

        onSequenceCompleted?.Invoke();
        victorySequenceCoroutine = null;
    }

    private void SetEmission(Color color, float intensity)
    {
        Color hdrColor = color * Mathf.Pow(2f, intensity);
        jarMaterial.SetColor(EmissionColorID, hdrColor);
    }

    private float GetProgressToWin()
    {
        float progress = 0f;

        if (ui != null)
            progress = ui.SmoothedProgress;
        else if (candyManager != null && candyManager.totalCandies > 0)
            progress = (float)candyManager.escapedCount / candyManager.totalCandies;

        float winRatio = 1f;
        if (candyManager != null && candyManager.config != null)
            winRatio = Mathf.Max(0.0001f, candyManager.config.winRatio);

        return Mathf.Clamp01(progress / winRatio);
    }

    private void UpdateVictoryLight(float progressToWin)
    {
        if (explosionPointLight == null)
            return;

        float targetLight = Mathf.Lerp(0f, explosionLightIntensity * 0.6f, progressToWin);
        explosionPointLight.enabled = targetLight > 0.01f;
        explosionPointLight.intensity = Mathf.Lerp(explosionPointLight.intensity, targetLight, Time.deltaTime * glowSmoothSpeed);
    }

    private void UpdateScreenGlow(float progressToWin)
    {
        EnsureScreenOverlay();
        if (screenOverlay == null)
            return;

        float normalized = Mathf.InverseLerp(screenGlowStart, 1f, progressToWin);
        normalized = Mathf.Clamp01(normalized);
        float curved = Mathf.Pow(normalized, Mathf.Max(0.01f, screenGlowExponent));
        SetScreenOverlayAlpha(curved * maxPreWinScreenAlpha);
    }

    private void SetJarOpacity(float alpha)
    {
        if (!hasBaseColorProperty || jarMaterial == null)
            return;

        Color color = originalBaseColor;
        color.a *= Mathf.Clamp01(alpha);
        jarMaterial.SetColor(baseColorPropertyId, color);
    }

    private void EnsureScreenOverlay()
    {
        if (screenOverlay != null)
            return;

        GameObject canvasObject = new GameObject("JarGlowOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.hideFlags = HideFlags.DontSave;

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("ScreenWhiteOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        screenOverlay = imageObject.GetComponent<Image>();
        screenOverlay.color = new Color(1f, 1f, 1f, 0f);
        screenOverlay.raycastTarget = false;

        RectTransform rect = screenOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetScreenOverlayAlpha(float alpha)
    {
        if (screenOverlay == null)
            return;

        Color color = screenOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        screenOverlay.color = color;
    }

    private float GetScreenOverlayAlpha()
    {
        return screenOverlay != null ? screenOverlay.color.a : 0f;
    }

    private void OnDestroy()
    {
        if (overlayCanvas == null)
            return;

        if (Application.isPlaying)
            Destroy(overlayCanvas.gameObject);
        else
            DestroyImmediate(overlayCanvas.gameObject);
    }
}
