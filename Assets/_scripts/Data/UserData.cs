

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using FullSerializer;

public class UserData
{
    [fsProperty] public string id;
    [fsProperty] public string status;
    [fsProperty] public string userClass;

    [fsProperty] public UserProgressData progressData;
    [fsProperty] public StatisticsData statistics;

    [fsProperty] private Dictionary<string, string> sessions = new Dictionary<string, string>();

    [fsProperty] private Sprite profilePhotoSprite;
    [fsProperty] private string profilePhoto;

    public UserData()
    {
    }

    public UserData(string id)
    {
        this.id = id;
    }
    public UserData(string id, UserProgressData progressData)
    {
        this.id = id;
        this.progressData = progressData;
    }

    public void setId(string id)
    {
        this.id = id;
    }
    public void setStatus(string status)
    {
        this.status = status;
    }
    public void setUserClass(string _class)
    {
        this.userClass = _class;
    }
    public void setProfilePhotoUrl(string url)
    {
        this.profilePhoto = url;
    }
    public void setProfilePhotoSprite(Sprite photo)
    {
        this.profilePhotoSprite = photo;
    }
    public void setSessions(Dictionary<string, string> sessions)
    {
        this.sessions = sessions;
    }
    public void setStatistics(StatisticsData statistics)
    {
        this.statistics = statistics;
    }
    public void setProgressData(UserProgressData data)
    {
        this.progressData = data;
    }
    public void updatePublicData(UserProgressData progressData, StatisticsData statistics)
    {
        this.progressData = progressData;
        this.statistics = statistics;
    }

    public void updateProfilePhoto(Sprite newPhoto)
    {
        profilePhotoSprite = newPhoto;
    }

    public void CopyFrom(UserData data)
    {
        setId(data.id);
        setStatus(data.status);
        setUserClass(data.userClass);
        setSessions(data.sessions);
        setProfilePhotoUrl(data.profilePhoto);
        updatePublicData(data.progressData, data.statistics);
        updateProfilePhoto(data.profilePhotoSprite);
    }

    private void updateProfilePhoto(string url)
    {
        profilePhoto = url;
        //downloadProfilePhoto();
    }

    //private System.Collections.IEnumerator downloadProfilePhoto()
    //{
    //    var opponentDataTask = LoadProfileImage();
    //    yield return new WaitUntil(() => opponentDataTask.IsCompleted);
    //}
    //private async System.Threading.Tasks.Task LoadProfileImage()
    //{
    //    Texture2D tex = await ImageLoader.LoadImage(profilePhotoUrl);
    //    Sprite downloadedPhotoSprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
    //    profilePhoto = downloadedPhotoSprite;
    //}
    //private void downloadPrivatePhotoUsingCoroutine(string phoroUrl)
    //{
    //    this.profilePhotoUrl = phoroUrl;
    //    ImageLoaderCoroutine.DownloadImage(phoroUrl, ref profilePhoto);
    //    StartCoroutine(GetSpriteFromURL(phoroUrl));
    //}
    //private System.Collections.IEnumerator GetSpriteFromURL(string url)
    //{
    //    Debug.Log("Downloading profile photo texture with url - " + url);
    //    UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
    //    yield return www.SendWebRequest();

    //    if (www.isNetworkError || www.isHttpError)
    //    {
    //        Debug.Log(www.error);
    //    }
    //    else
    //    {
    //        Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
    //        profilePhoto = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
    //    }
    //}
    [fsIgnore] public string Id { get => id; set => id = value; }
    [fsIgnore] public UserProgressData ProgressData { get => progressData; set => progressData = value; }
    //internal SessionData[] Sessions { get => sessions; set => sessions = value; }
    [fsIgnore] internal StatisticsData Statistics { get => statistics; set => statistics = value; }
    [fsIgnore] public Sprite ProfilePhoto { get => profilePhotoSprite; set => profilePhotoSprite = value; }

    [fsIgnore] public string ProfilePhotoUrl
    {
        get => profilePhoto;
        set => profilePhoto = value;
        //set => updateProfilePhoto(value);
    }
    [fsIgnore] public string FullName { get => progressData.Name + " " + progressData.Surname; }
    [fsIgnore] public string Name { get => progressData.Name; }
    [fsIgnore] public string Surname { get => progressData.Surname; }
    [fsIgnore] public string UserClass { get => userClass; set => userClass = value; }

    public override string ToString()
    {
        return string.Format("UserData : [id = {0}, progressData = {1}, statistics = {2}]", 
            id, progressData, statistics);
    }
}
