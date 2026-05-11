using UnityEngine;
using System.Collections;

/// <summary>
/// Controla el brillo (emisión) del material del jarrón.
///
/// SETUP en Inspector:
/// 1. Asigna el MeshRenderer del jarrón translúcido en "jarRenderer".
/// 2. El material del jarrón DEBE tener activado "Emission" en el shader
///    (Standard o URP/Lit) para que el brillo funcione.
/// 3. Asigna UIController para leer el progreso.
///
/// Flujo:
/// - A medida que escapedCount / totalCandies aumenta, el jarrón brilla más.
/// - Al llegar al 100%, se activa la explosión de luz y aparece el logo.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class JarGlowController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Referencias
    // ─────────────────────────────────────────────────────────────
    [Header("Referencias")]
    [Tooltip("Renderer del jarrón translúcido (glass)")]
    public Renderer jarRenderer;

    [Tooltip("UIController para leer el progreso suavizado")]
    public UIController ui;

    [Tooltip("CandyManager para leer escaped / total directamente")]
    public CandyManager candyManager;

    // ─────────────────────────────────────────────────────────────
    //  Configuración de brillo
    // ─────────────────────────────────────────────────────────────
    [Header("Brillo del Jarrón")]
    [Tooltip("Color de emisión a intensidad mínima (jarrón lleno)")]
    public Color glowColorMin = new Color(0.5f, 0.8f, 1f);

    [Tooltip("Color de emisión a intensidad máxima (jarrón casi vacío)")]
    public Color glowColorMax = new Color(1f, 0.95f, 0.5f);

    [Tooltip("Intensidad mínima de emisión (HDR, en escala lineal)")]
    public float glowIntensityMin = 0f;

    [Tooltip("Intensidad máxima de emisión antes de la explosión")]
    public float glowIntensityMax = 3f;

    [Tooltip("Velocidad de interpolación del brillo (más alto = más reactivo)")]
    public float glowSmoothSpeed = 3f;

    // ─────────────────────────────────────────────────────────────
    //  Configuración de explosión final
    // ─────────────────────────────────────────────────────────────
    [Header("Explosión de Luz Final")]
    [Tooltip("¿Ya se activó la explosión?")]
    private bool exploded = false;

    [Tooltip("Intensidad del flash de la explosión")]
    public float explosionPeakIntensity = 8f;

    [Tooltip("Duración del flash de la explosión (segundos)")]
    public float explosionDuration = 0.6f;

    [Tooltip("Color del flash de explosión")]
    public Color explosionColor = Color.white;

    [Tooltip("Luz puntual opcional que refuerza el efecto de explosión en la escena")]
    public Light explosionPointLight;

    [Tooltip("Intensidad máxima de la luz puntual durante la explosión")]
    public float explosionLightIntensity = 5f;

    // ─────────────────────────────────────────────────────────────
    //  Logo final (post-explosión)
    // ─────────────────────────────────────────────────────────────
    [Header("Logo Final")]
    [Tooltip("Objeto 3D o Canvas del logo '25 + VHS + gafas VR' (se activa tras la explosión)")]
    public GameObject logoObject;

    [Tooltip("Retardo entre el pico de la explosión y la aparición del logo")]
    public float logoDelay = 0.3f;

    // ─────────────────────────────────────────────────────────────
    //  Pulso de brillo al detectar nota
    // ─────────────────────────────────────────────────────────────
    [Header("Pulso por Nota")]
    [Tooltip("Intensidad extra de emisión cuando se detecta una nota y se aplica fuerza")]
    public float notePulseIntensity = 1.5f;

    [Tooltip("Velocidad de decaimiento del pulso por nota")]
    public float notePulseDecay = 8f;

    // ─────────────────────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────────────────────
    private Material jarMaterial;
    private float currentGlowIntensity = 0f;
    private float notePulse = 0f;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // ─────────────────────────────────────────────────────────────
    //  Inicialización
    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        // Usar el renderer del propio GameObject si no se asignó uno
        if (jarRenderer == null)
            jarRenderer = GetComponent<Renderer>();

        // Crear una copia del material para no modificar el asset original
        if (jarRenderer != null)
        {
            jarMaterial = jarRenderer.material; // Crea instancia automática
            // Habilitar keyword de emisión para que funcione en runtime
            jarMaterial.EnableKeyword("_EMISSION");
        }

        // Apagar la luz puntual al inicio
        if (explosionPointLight != null)
        {
            explosionPointLight.intensity = 0f;
            explosionPointLight.enabled = false;
        }

        // Ocultar logo al inicio
        if (logoObject != null)
            logoObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  Update — actualizar brillo cada frame
    // ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (jarMaterial == null || exploded) return;

        float progress = GetProgress();

        // Intensidad objetivo basada en el progreso
        float targetIntensity = Mathf.Lerp(glowIntensityMin, glowIntensityMax, progress);

        // Sumar el pulso por nota (decae rápido)
        notePulse = Mathf.Max(0f, notePulse - notePulseDecay * Time.deltaTime);
        targetIntensity += notePulse;

        // Suavizar la transición
        currentGlowIntensity = Mathf.Lerp(currentGlowIntensity, targetIntensity, Time.deltaTime * glowSmoothSpeed);

        // Color interpolado según progreso
        Color glowColor = Color.Lerp(glowColorMin, glowColorMax, progress);

        // Aplicar al material (multiplicar color por intensidad HDR)
        SetEmission(glowColor, currentGlowIntensity);
    }

    // ─────────────────────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Llamar desde PitchDetector o GameManager cuando se aplica una fuerza exitosa.
    /// Añade un pulso de brillo momentáneo al jarrón.
    /// </summary>
    public void TriggerNotePulse()
    {
        notePulse = notePulseIntensity;
    }

    /// <summary>
    /// Activa la explosión de luz final y muestra el logo.
    /// Llamado desde GameManager cuando IsWon() es verdadero.
    /// </summary>
    public void TriggerExplosion()
    {
        if (exploded) return;
        exploded = true;
        StartCoroutine(ExplosionSequence());
    }

    /// <summary>
    /// Reinicia el controlador de brillo (llamar al hacer Restart).
    /// </summary>
    public void Reset()
    {
        exploded = false;
        notePulse = 0f;
        currentGlowIntensity = 0f;

        if (jarMaterial != null)
            SetEmission(glowColorMin, 0f);

        if (explosionPointLight != null)
        {
            explosionPointLight.intensity = 0f;
            explosionPointLight.enabled = false;
        }

        if (logoObject != null)
            logoObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  Corrutina de explosión
    // ─────────────────────────────────────────────────────────────
    private IEnumerator ExplosionSequence()
    {
        // ── Fase 1: Flash hacia arriba ──────────────────────────
        float halfDuration = explosionDuration * 0.5f;
        float elapsed = 0f;

        if (explosionPointLight != null)
            explosionPointLight.enabled = true;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            SetEmission(explosionColor, Mathf.Lerp(currentGlowIntensity, explosionPeakIntensity, t));

            if (explosionPointLight != null)
                explosionPointLight.intensity = Mathf.Lerp(0f, explosionLightIntensity, t);

            yield return null;
        }

        // ── Fase 2: Flash hacia abajo ──────────────────────────
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            SetEmission(explosionColor, Mathf.Lerp(explosionPeakIntensity, 0f, t));

            if (explosionPointLight != null)
                explosionPointLight.intensity = Mathf.Lerp(explosionLightIntensity, 0f, t);

            yield return null;
        }

        // Apagar luz puntual
        if (explosionPointLight != null)
        {
            explosionPointLight.intensity = 0f;
            explosionPointLight.enabled = false;
        }

        // Apagar emisión del jarrón
        SetEmission(Color.black, 0f);

        // ── Mostrar logo tras el retardo ──────────────────────
        yield return new WaitForSeconds(logoDelay);

        if (logoObject != null)
            logoObject.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────
    private void SetEmission(Color color, float intensity)
    {
        // HDR: multiplicar el color por la intensidad (en escala lineal)
        Color hdrColor = color * Mathf.Pow(2f, intensity);
        jarMaterial.SetColor(EmissionColorID, hdrColor);

        // Actualizar el GI estático si el objeto es estático
        // (en escenas dinámicas esto no es necesario)
        // DynamicGI.SetEmissive(jarRenderer, hdrColor);
    }

    private float GetProgress()
    {
        // Prioridad 1: Usar UIController.SmoothedProgress (ya suavizado)
        if (ui != null)
            return ui.SmoothedProgress;

        // Prioridad 2: Calcular directo desde CandyManager
        if (candyManager != null && candyManager.totalCandies > 0)
            return (float)candyManager.escapedCount / candyManager.totalCandies;

        return 0f;
    }
}
