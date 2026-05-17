using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public GameObject rootPanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public GameObject continueIndicator;

    public void ShowUI(bool show)
    {
        if (rootPanel != null)
            rootPanel.SetActive(show);
    }

    public void SetLine(string speakerName, string text, Sprite portrait)
    {
        if (speakerNameText != null)
            speakerNameText.text = speakerName;

        if (dialogueText != null)
            dialogueText.text = text;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;

            // Force le comportement
            portraitImage.preserveAspect = true;

            // Ajuste dynamiquement le ratio
            var fitter = portraitImage.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            if (fitter != null && portrait != null)
            {
                float ratio = (float)portrait.texture.width / portrait.texture.height;
                fitter.aspectRatio = ratio;
            }
        }
    }

    public void SetContinueIndicator(bool show)
    {
        if (continueIndicator != null)
            continueIndicator.SetActive(show);
    }
}