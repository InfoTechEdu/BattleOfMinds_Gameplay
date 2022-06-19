
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UserLeaderboardView : MonoBehaviour
{
    int position;
    UserData userData;

    //private Text positionTxt; //old code
    private Image profilePhotoImg;

    private Text nameTxt;
    private Text pointsTxt;
    private Text winCountTxt;

    private void Awake()
    {
        //positionTxt = transform.Find("Position").GetComponentInChildren<Text>(); //old code
        nameTxt = transform.Find("Name").GetComponent<Text>();
        pointsTxt = transform.Find("Points").GetComponent<Text>();
        winCountTxt = transform.Find("WinCount").GetComponent<Text>();

        profilePhotoImg = transform.FindDeepChild("ProfilePhoto").GetComponent<Image>();
    }

    private void Start()
    {
        if (!gameObject.activeInHierarchy)
            return;

        updateView();
        //Debug.Log("start end()");
    }

    public void loadViewData(int position, UserData ud)
    {
        this.position = position;
        userData = ud;


        //Debug.Log("laod view data end");
    }

    private void updateView()
    {

        Debug.Log("[debug] parsing userdata to UserLeaderboardView. Data - " + userData);
        //bad code. refactor. Костыль так как данный метод вызывается 6 раз, если три объекта с userData null
        if(userData == null)
        {

            Debug.Log("Can not update UserLeaderboardView. User data is null");
            return;
        }

        //positionTxt.text = position.ToString(); //old

        nameTxt.text = userData.ProgressData.Name;
        pointsTxt.text = userData.ProgressData.Points.ToString();
        winCountTxt.text = userData.Statistics.WinCount.ToString();

        //temp uncomment
        //StartCoroutine(DownloadProfilePhoto(userData.ProfilePhotoUrl, (downloadedSprite) => profilePhotoImg.sprite = downloadedSprite));
        
        //Texture2D tex = await ImageLoader.LoadImage(userData.ProfilePhotoUrl);
        //profilePhotoImg.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);


        StartCoroutine(DownloadAndSetPhoto(userData.ProfilePhotoUrl));        
    }

    private IEnumerator DownloadAndSetPhoto(string url)
    {
        Debug.Log("Downloading texture with url - " + url);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;

            profilePhotoImg.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }

    private IEnumerator DownloadProfilePhoto(string url, Action<Sprite> onLoaded)
    {
        Debug.Log("Downloading texture with url - " + url);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError($"Error downloading texture with url {url}. Error - {www.error}");
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite downloadedSprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            onLoaded.Invoke(downloadedSprite);
        }

    }
}
