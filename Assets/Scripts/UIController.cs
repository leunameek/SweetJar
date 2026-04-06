using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text noteLabel;
    public TMP_Text freqLabel;
    public Slider levelMeter;
    public TMP_Text forceLabel;
    public TMP_Text escapedLabel;
    public Button micButton;
    public TMP_Text micButtonText;

    [Header("Settings")]
    public Slider candySlider;
    public Slider impulseSlider;
    public Slider sensitivitySlider;
    public Slider capSpeedSlider;
    public Button restartButton;

    [Header("Win Screen")]
    public GameObject winScreen;

    [Header("Error Overlay")]
    public GameObject errorOverlay;
    public TMP_Text errorText;

    public void UpdateHUD(string note, float freq, float rms, float force, int escaped, int total)
    {
        if (noteLabel)     noteLabel.text     = $"Note: {(note ?? "—")}";
        if (freqLabel)     freqLabel.text     = $"{freq:F1} Hz";
        if (levelMeter)    levelMeter.value   = Mathf.Clamp01(rms * 10f);
        if (forceLabel)    forceLabel.text    = $"Force: {force:F2}";
        if (escapedLabel)  escapedLabel.text  = $"Escaped: {escaped} / {total}";
    }

    public void SetMicButtonText(string text) => micButtonText.text = text;

    public void ShowWinScreen() => winScreen.SetActive(true);
    public void HideWinScreen() => winScreen.SetActive(false);

    public void ShowError(string message)
    {
        errorText.text = message;
        errorOverlay.SetActive(true);
    }
    public void HideError() => errorOverlay.SetActive(false);
}
