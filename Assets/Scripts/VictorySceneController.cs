using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VictorySceneController : MonoBehaviour
{
    private const string ProjectFontResourcePath = "Fonts/ShareTechMono-Regular";

    [Header("Scene Flow")]
    public string gameplaySceneName = "CandyRain";

    [Header("Assets")]
    public Sprite backgroundSprite;
    public Sprite logoSprite;
    public Sprite restartButtonSprite;

    [Header("Layout")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public Color backgroundTint = Color.white;
    public float backgroundScale = 1.22f;
    public Color glowColor = new Color(1f, 0.97f, 0.78f, 0.85f);
    public Vector2 logoSize = new Vector2(640f, 640f);
    public Vector2 logoPosition = new Vector2(0f, 20f);
    public Vector2 glowSize = new Vector2(620f, 620f);
    public Vector2 restartSize = new Vector2(1080f, 348f);
    public Vector2 restartPosition = new Vector2(0f, -320f);
    public float contentRevealDelay = 0f;
    public float contentFadeDuration = 0.16f;

    private static Sprite cachedButtonSprite;
    private static Sprite cachedGlowSprite;
    private CanvasGroup contentGroup;
    private Image flashOverlay;
    private Font projectFont;

    void Awake()
    {
        EnsureProjectFontLoaded();
        EnsureEventSystem();
        BuildVictoryUi();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void BuildVictoryUi()
    {
        GameObject canvasObject = new GameObject("VictoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        BuildBackground(canvasRect);

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(CanvasGroup));
        contentObject.transform.SetParent(canvasRect, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        Stretch(contentRect);
        contentGroup = contentObject.GetComponent<CanvasGroup>();
        contentGroup.alpha = 0f;

        BuildCenteredLogo(contentRect);
        BuildRestartButton(contentRect);
        BuildFlashOverlay(canvasRect);

        StartCoroutine(RevealContent());
    }

    private void BuildBackground(RectTransform canvasRect)
    {
        Image background = CreateImage("Background", canvasRect, backgroundSprite, backgroundTint);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = referenceResolution * backgroundScale;
        background.preserveAspect = true;
    }

    private void BuildCenteredLogo(RectTransform parent)
    {
        Image glow = CreateImage("LogoGlow", parent, GetGlowSprite(), glowColor);
        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = logoPosition + new Vector2(0f, -10f);
        glowRect.sizeDelta = glowSize;
        glow.raycastTarget = false;

        if (logoSprite != null)
        {
            Image logo = CreateImage("Logo", parent, logoSprite, Color.white);
            RectTransform logoRect = logo.rectTransform;
            logo.preserveAspect = true;
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = logoPosition;
            logoRect.sizeDelta = logoSize;
            logo.raycastTarget = false;
        }
    }

    private void BuildRestartButton(RectTransform parent)
    {
        Button restartButton = CreateButton(parent);
        RectTransform buttonRect = restartButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = restartPosition;
        buttonRect.sizeDelta = restartSize;
        restartButton.onClick.AddListener(RestartGame);
    }

    private void BuildFlashOverlay(RectTransform parent)
    {
        flashOverlay = CreateImage("FlashOverlay", parent, null, Color.white);
        Stretch(flashOverlay.rectTransform);
        flashOverlay.raycastTarget = false;
    }

    private Button CreateButton(RectTransform parent)
    {
        Font buttonFont = projectFont != null
            ? projectFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        bool hasCustomSprite = restartButtonSprite != null;
        image.sprite = hasCustomSprite ? restartButtonSprite : GetButtonSprite();
        image.type = hasCustomSprite ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = hasCustomSprite;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.72f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.55f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.72f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.targetGraphic = image;

        HoverScaleButton hoverScale = buttonObject.AddComponent<HoverScaleButton>();
        hoverScale.hoverScale = 1.08f;
        hoverScale.pressedScale = 1.03f;
        hoverScale.transitionSpeed = 14f;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        Text text = textObject.GetComponent<Text>();
        text.text = "Reiniciar";
        text.font = buttonFont;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.gameObject.SetActive(!hasCustomSprite);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        Stretch(textRect);

        return button;
    }

    private Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Sprite GetButtonSprite()
    {
        if (cachedButtonSprite != null)
            return cachedButtonSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "VictoryButtonSprite";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedButtonSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return cachedButtonSprite;
    }

    private Sprite GetGlowSprite()
    {
        if (cachedGlowSprite != null)
            return cachedGlowSprite;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "VictoryGlowSprite";

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * 0.95f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        cachedGlowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedGlowSprite;
    }

    private IEnumerator RevealContent()
    {
        if (contentGroup == null)
            yield break;

        if (contentRevealDelay > 0f)
            yield return new WaitForSeconds(contentRevealDelay);

        float elapsed = 0f;
        while (elapsed < contentFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, contentFadeDuration));
            contentGroup.alpha = t;
            SetFlashOverlayAlpha(1f - t);
            yield return null;
        }

        contentGroup.alpha = 1f;
        SetFlashOverlayAlpha(0f);
    }

    private void EnsureProjectFontLoaded()
    {
        if (projectFont == null)
            projectFont = Resources.Load<Font>(ProjectFontResourcePath);
    }

    private void SetFlashOverlayAlpha(float alpha)
    {
        if (flashOverlay == null)
            return;

        Color color = flashOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        flashOverlay.color = color;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (backgroundSprite == null)
            backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Props/fondo.jpeg");

        if (restartButtonSprite == null)
            restartButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Props/Restart.png");

        if (logoSprite == null)
            logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Props/Logo.png");
    }
#endif
}
