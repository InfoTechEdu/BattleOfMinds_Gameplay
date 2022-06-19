using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbieView : MonoBehaviour
{
    DataController dataController; 
    /// <summary>
    /// session id as string parameter 
    /// </summary>
    Action<LobbieView> onClick; 

    SessionData sessionData;
    UserData opponentData;

    private Image opponentProfilePhoto;
    private Text opponentNameText;
    private Text statusText;

    public SessionData SessionData { get => sessionData; set => sessionData = value; }

    private void Awake()
    {
        dataController = FindObjectOfType<DataController>();

        opponentNameText = transform.Find("Name").GetComponent<Text>();
        statusText = transform.Find("Status").GetComponent<Text>();

        opponentProfilePhoto = transform.FindDeepChild("ProfilePhoto").GetComponent<Image>();

        opponentNameText.text = "Идет загрузка";
        statusText.text = "Идет загрузка";
    }

    public void UpdateView()
    {
        if (sessionData == null || sessionData.status == null)
        {
            GetComponent<Button>().interactable = false;
            Debug.Log("Can not update Lobbie View. Session or opponent data is null");
            return;
        }


        if (opponentData == null)
        {
            string currentUserId = dataController.GetUserId();
            dataController.DownloadUserDataREST(sessionData.GetOpponentId(currentUserId), (loaded) => { 
                opponentData = loaded;
                UpdateView(); //refactor. bad code. Не знаю подобает ли так писать. Еще есть вариант с coroutine
            }, true);

            GetComponent<Button>().interactable = false;
            return;
        }

        if (!gameObject.activeInHierarchy)
            return;

        GetComponent<Button>().interactable = true;
        opponentNameText.text = opponentData.FullName;
        opponentProfilePhoto.sprite = opponentData.ProfilePhoto;

        switch (sessionData.Status)
        {
            case "active":
                StateActive();
                break;
            case "expected":
                StateExpected();
                break;
            case "ended":
                StateEnded();
                break;
            default:
                statusText.text = string.Empty;
                statusText.color = Color.white;
                break;
        }
    }
    private void StateActive()
    {
        ClearView();

        int activeRound = sessionData.ActiveRoundIndex + 1;
        statusText.text = $"{activeRound}-й раунд";

        Color statusTextColor;
        if (sessionData.MovingPlayer == dataController.GetUserId())
            ColorUtility.TryParseHtmlString("#F38A08", out statusTextColor);
        else
            ColorUtility.TryParseHtmlString("#5D9DF2", out statusTextColor);
        statusText.color = statusTextColor;

    }
    private void StateEnded()
    {
        ClearView();

        Color color;
        string text;
        if (sessionData.SessionWinner == dataController.GetUserId())
        {
            text = "Победа";
            ColorUtility.TryParseHtmlString("#F38A08", out color);
        }
        else
        {
            text = "Поражение";
            ColorUtility.TryParseHtmlString("#5D9DF2", out color);
        }

        statusText.text = text;
        statusText.color = color;
    }
    private void StateExpected()
    {
        ClearView();

        //edit
        statusText.text = "Ожидание ответа";
        statusText.color = Color.white;
    }

    public void OnClicked()
    {
        onClick.Invoke(this);
    }

    public void LoadAndUpdateViewData(SessionData sd, Action<LobbieView> onLobbieClicked)
    {
        sessionData = sd;
        onClick = onLobbieClicked;

        UpdateView();
    }
    public void UpdateSessionData(SessionData updated) //refactor. Может сделать, чтобы при изменении данных сессии в dataController, они сразу менялись и везде? Сейчас у LobbieView и DataController получается разные объекты
    {
        this.sessionData = updated;
        UpdateView();
    }
    public void Block()
    {
        GetComponent<Button>().interactable = false;
    }
    public void Unblock()
    {
        GetComponent<Button>().interactable = true;
    }
    private void ClearView()
    {
        statusText.text = "";
        statusText.color = Color.white;//delete?
    }

    //private IEnumerator DownloadAndSetPhoto()
    //{
    //    yield return new WaitUntil(() => opponentData != null);
    //    Debug.Log("Downloading texture with url - " + opponentData.ProfilePhotoUrl);
    //    UnityWebRequest www = UnityWebRequestTexture.GetTexture(opponentData.ProfilePhotoUrl);
    //    yield return www.SendWebRequest();

    //    if (www.isNetworkError || www.isHttpError)
    //    {
    //        Debug.Log(www.error);
    //    }
    //    else
    //    {
    //        Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;

    //        opponentProfilePhoto.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
    //    }
    //}
}
