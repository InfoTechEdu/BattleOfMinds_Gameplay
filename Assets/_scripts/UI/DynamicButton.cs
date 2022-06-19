using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicButton : MonoBehaviour
{
    [SerializeField] private Color loadingColor;
    [SerializeField] private Color loadedColor;
    [SerializeField] private Color errorColor;

    [SerializeField] private string loadingText;
    [SerializeField] private string loadedText;
    [SerializeField] private string errorText;

    private Text btnText;
    private Image btnImage;

    private string initMessage;
    private Color initColor;

    public string LoadingText { get => loadingText; set => loadingText = value; }
    public string LoadedText { get => loadedText; set => loadedText = value; }
    public string ErrorText { get => errorText; set => errorText = value; }

    private void Awake()
    {
        btnText = GetComponentInChildren<Text>();
        btnImage = GetComponent<Image>();
    }

    public void OnClicked()
    {
        GetComponent<Button>().interactable = false;

        UpdateView(loadingText, loadingColor);
    }
    public void OnDataLoaded(bool success)
    {
        DisableIfInteractable();

        if (success)
            UpdateView(loadedText, loadedColor);
        else
            UpdateView(errorText, errorColor);
    }

    private bool configured = false;
    public void Configure(string hexColor, string text)
    {
        initMessage = text;
        ColorUtility.TryParseHtmlString(hexColor, out initColor);

        configured = true;
    }
    public void Reload()
    {
        if (!configured)
        {
            Debug.LogWarning("Dynamic button reload declined. Reason - not configured.");
            return;
        }

        GetComponent<Button>().interactable = true;

        btnText.text = initMessage;
        btnImage.color = initColor;
    }

    private void UpdateView(string text, Color color)
    {
        btnText.text = text;
        btnImage.color = color;
    }
    private void DisableIfInteractable()
    {
        Button btn = GetComponent<Button>();
        if (btn.IsInteractable())
            btn.interactable = false;
    }
}
