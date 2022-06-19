using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FriendView : MonoBehaviour
{
    UserData friendData;

    private Image profilePhoto;
    private Text nameText;
    private Text statusText;

    private void Awake()
    {
        nameText = transform.Find("Name").GetComponent<Text>();
        statusText = transform.Find("Status").GetComponent<Text>();

        profilePhoto = transform.FindDeepChild("ProfilePhoto").GetComponent<Image>();

        nameText.text = "Идет загрузка";
        statusText.text = "Идет загрузка";
    }

    //private IEnumerator Start()
    //{
    //    if(friendData == null || friendData.ProgressData == null)
    //        yield return null;

    //    updateView();
    //}

    private void Start()
    {
        UpdateView();
    }

    public void LoadAndUpdateViewData(UserData ud)
    {
        friendData = ud;
        UpdateView();
    }

    public void UpdateView()
    {
        if (friendData == null || friendData.ProgressData == null)
        {
            Debug.Log("Can not update UserSearchResultInfoView. friend data is null");
            return;
        }

        if (!gameObject.activeInHierarchy)
            return;

        nameText.text = friendData.FullName;

        Debug.Log("[temp] friend status - " + friendData.status);
        switch (friendData.status)
        {
            case "searching":
                statusText.text = "Ищет игру";
                break;
            case "online":
                statusText.text = "В сети";
                break;
            default:
                statusText.text = string.Empty;
                break;
        }

        profilePhoto.sprite = friendData.ProfilePhoto;

        //if(gameObject.activeInHierarchy)
        //    StartCoroutine(DownloadAndSetPhoto());
    }

    private IEnumerator DownloadAndSetPhoto()
    {
        Debug.Log("Downloading texture with url - " + friendData.ProfilePhotoUrl);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(friendData.ProfilePhotoUrl);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;

            profilePhoto.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }
}
