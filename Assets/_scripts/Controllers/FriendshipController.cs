using BestHTTP.ServerSentEvents;
using Proyecto26;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FriendshipNotifications
{
    public const string OnFriendDataLoaded = "OnFriendDataLoaded";
    public const string OnAllFriendsListsLoaded = "OnAllFriendsListsLoaded";
    public const string OnNewFriendship = "OnNewFriendship";

    public const string OnNewFriendshipInvitation = "OnNewFriendshipInvitation";
    public const string OnRemovedFromWishers = "OnRemovedFromWishers"; //accepted/declined by this or other user //refactor, rename? //Пока 
}

public enum FriendshipControllerStates
{
    Initialized,
    Loaded,
    Updating,
    Updated
}
public class FriendshipController : MonoBehaviour
{
    DataController dataController;

    List<UserData> wishingToBeFriendsUsers;
    List<UserData> activeFriendsList;
    List<UserData> expectedFriendsList;

    private EventSource friendsListener;

    private FriendshipControllerStates state;

    private bool preloded = false;
    private bool loaded = false;
    public bool Preloaded { get => preloded;}
    public bool Loaded { get => loaded;}

    //private bool initialized = false;
    //private bool loaded = false;
    //public bool Initialized { get => initialized;}
    //public bool Loaded { get => loaded; }
    //public FriendshipControllerStates State { get => state; set => state = value; }

    // Start is called before the first frame update
    void Start()
    {
        wishingToBeFriendsUsers = new List<UserData>();
        activeFriendsList = new List<UserData>();
        expectedFriendsList = new List<UserData>();
        //LoadFriendsLists(() =>
        //{
        //    Messenger.Broadcast(FriendshipNotifications.OnAllFriendsListsLoaded);
        //});

        //Danger. Может скачать данные 2 раза. Убери LoadFriendsLists если он при прослушивании сразу качает
        ListenFriendshipList($"users/{dataController.GetUserId()}/private/friends");
    }
    private void OnDestroy()
    {
        friendsListener.Close();
    }
    public void SetDataController(DataController controller)
    {
        dataController = controller;
    }

    //old
    //public void LoadFriendsLists(Action onReady)
    //{
    //    FirebaseManager.Instance.Database.GetJson($"users/{dataController.GetUserId()}/private/friends", OnFriendsListLoaded, (exception) =>
    //    {
    //        Debug.LogError($"Exception while downloading ... data. Message - {exception.Message}");
    //        GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading ... data. Message - {exception.Message}");

    //        onReady.Invoke();
    //    });
    //}
    private void OnFriendsListLoaded(string json)
    {
        if (json == "null" || json == null)
        {
            preloded = true;
            loaded = true;
            return;
        }

        //Debug.Log("FriendsList data was loaded");

        DataParser.ParseFriendsList(json, out activeFriendsList, out expectedFriendsList, out wishingToBeFriendsUsers);
        Debug.LogWarning($"[debug] Friends data parsed. Active - {Utils.CollectionUtils.ListToString(activeFriendsList)}" +
            $"Expected - {Utils.CollectionUtils.ListToString(expectedFriendsList)}");

        preloded = true;
        Debug.Log("FriendshipController. Data (lists) preloaded");
        Messenger.Broadcast(FriendshipNotifications.OnAllFriendsListsLoaded);

        LoadActiveFriendsData();
        //state = FriendshipControllerStates.Loaded;
    }
    public void ListenFriendshipList(string path)
    {
        friendsListener = FirebaseManager.Instance.Database.ListenForChildChanged(path, FriendsUpdatesHandler, (exception) =>
        {
            Debug.LogError($"Exception while listening {path} data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while listening {path} data. Message - {exception.Message}");
        });
    }
    private void FriendsUpdatesHandler(EventSource eSource, Message message)
    {
        //In first call after starting we get PUT event. Then we get the PATCH
        if (message.ToString().Contains("put"))
        {
            OnFriendsListLoaded(JSONNode.Parse(message.Data)["data"].ToString());
            return;
        }
            
        if (message.Data == null || message.Data == "null")
            return;

        JSONNode messageJsonObj = JSONNode.Parse(message.Data);
        string dataJson = messageJsonObj["data"].ToString();

        if (dataJson == null || dataJson == "null")
            return;

        // parsting json like {"expected/SomeUserId" : true} and getting data
        //bad code refactor. can not get key
        JSONNode dataJsonObj = JSON.Parse(dataJson);
        string key = "";
        foreach (var item in dataJsonObj)
            key = item.Key;

        string[] parts = key.Split('/');
        string friendId = parts[1];
        string friendType = parts[0];
        //not working
        //dataJsonObj.Keys.MoveNext();

        //string friendId = words[1];
        //string friendType = words[0];

        //old?
        //string friendId = path.Replace("active/", "").Replace("wishing/", ""); //refactor? Убирает либо active/ либо wishing/
        //string friendType = path.Substring(0, path.IndexOf("/"));

        switch (friendType)
        {
            case "active":
                if (dataJsonObj[key].ToString() == "null") RemoveFromActiveById(friendId); //checked
                else if (dataJsonObj[key].AsBool == true) OnNewFriendship(new UserData(friendId)); //checked
                break;

            case "expected":
                if (dataJsonObj[key].ToString() == "null") RemoveFromExpectedById(friendId); //checked
                else if (dataJsonObj[key].AsBool == true) AddToExpectedById(friendId); //checked
                break;

            case "wishing":
                if (dataJsonObj[key].ToString() == "null") { RemoveFromWishingById(friendId); Messenger.Broadcast(FriendshipNotifications.OnRemovedFromWishers, friendId); } //checked
                else if (dataJsonObj[key].AsBool == true) OnNewWishing(new UserData(friendId)); //checked
                break;

            default:
                Debug.LogWarning("Unexpected friend type. Type - " + friendType);
                break;
        }
    }

    public void LoadActiveFriendsData()
    {
        if(activeFriendsList == null)
        {
            Debug.LogWarning("Active friends list is null. Data download denied");
            return;
        }

        int updatedCount = 0;
        int updatingCount = activeFriendsList.Count; //не удаляй. Если добавится еще друг, то activeFriendsList.Count увеличится. А так, мы сохраняем в буффер
        foreach (var active in activeFriendsList)
        {
            dataController.DownloadUserDataREST(active.Id, (data) =>
            {
                UserData updating = activeFriendsList.FirstOrDefault(f => f.Id == data.Id);
                updating.CopyFrom(data);

                Messenger.Broadcast(FriendshipNotifications.OnFriendDataLoaded, data.Id);

                updatedCount++;
                if (updatedCount >= updatingCount)
                {
                    Debug.Log("FriendshipController. Data loaded");
                    loaded = true;
                }
            }, true);
        }

        //foreach (var active in activeFriendsList)
        //{
        //    dataController.DownloadUserDataREST(active.Id, (data) =>
        //    {
        //        UserData updating = activeFriendsList.FirstOrDefault(f => f.Id == data.Id);
        //        updating.CopyFrom(data);

        //        Messenger.Broadcast(FriendshipNotifications.OnFriendDataLoaded, data.Id);
        //    }, true);
        //}

        //Пока не удаляю, может пригодиться
        //if (state == FriendshipControllerStates.Updating)
        //    return;

        //state = FriendshipControllerStates.Updating;
        //int loadedCount = 0;
        //for (int i = 0; i < activeFriendsList.Count; i++)
        //{
        //    dataController.DownloadUserDataREST(activeFriendsList[i].Id, (data) =>
        //    {
        //        UserData updating = activeFriendsList.FirstOrDefault(f => f.Id == data.Id);
        //        updating.CopyFrom(data);

        //        Messenger.Broadcast(FriendshipNotifications.OnFriendDataLoaded, data.Id);

        //        loadedCount++;
        //        if (loadedCount >= activeFriendsList.Count)
        //        {
        //            state = FriendshipControllerStates.Updated;
        //        }
        //    }, true);
        //}

    }
    public void UpdateActiveFriendData(UserData friend)
    {
        dataController.DownloadUserDataREST(friend.Id, (data) =>
        {
            UserData updating = activeFriendsList.FirstOrDefault(f => f.Id == data.Id);
            updating.CopyFrom(data);

            Messenger.Broadcast(FriendshipNotifications.OnFriendDataLoaded, data.Id);
        }, true);

        //old version
        //UserData old = activeFriendsList.Find(f => f.Id == updated.Id);
        //old.CopyFrom(updated);
    }
    public void UpdateWishingFriendsData()
    {
        if (wishingToBeFriendsUsers == null)
        {
            Debug.LogWarning("Wishing to be friends list is null. Data download denied");
            return;
        }

        foreach (var wishingId in wishingToBeFriendsUsers)
        {
            dataController.DownloadUserDataREST(wishingId.Id, (data) =>
            {
                UserData updating = wishingToBeFriendsUsers.FirstOrDefault(f => f.Id == data.Id);
                updating.CopyFrom(data);

                Messenger.Broadcast(FriendshipNotifications.OnFriendDataLoaded, data.Id);
            }, true);
        }
    }
    public void InviteFriend(string expectedFriendId, Action<object> callback, Action onFailed)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("game", "battleofminds");
        @params.Add("from", dataController.GetUserId());
        @params.Add("to", expectedFriendId);

        FirebaseManager.Instance.Functions.CallCloudFunction("SendFriendInvitation", @params, (data) =>
        {
            Debug.Log($"Success calling function SendFriendInvitation");
            callback.Invoke(data);
        }, (exception) =>
        {
            Debug.LogError($"Error while calling SendFriendInvitation. Exception - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling SendFriendInvitation data. Message - {exception.Message}");
            onFailed();
        });

        AddToExpectedById(expectedFriendId);
        //old
        //expectedFriendsList.Add(new UserData(expectedFriendId));
    }
    public void SearchFriend(string userName, Action<List<UserData>> onLoaded)
    {

        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("name", userName);
        @params.Add("game", "battleofminds");

        FirebaseManager.Instance.Functions.CallCloudFunction("SearchUser", @params, (data) =>
        {
            Debug.Log($"Success calling function SearchFriend");
            if (data.body == "null" || data.body == "[]")
            {
                Debug.LogWarning($"User data with id {userName} was not found!");
                onLoaded.Invoke(null);
                return;
            }
            else
            {
                JSONNode usersJsonArray = JSONNode.Parse(data.body);
                List<UserData> foundUsers = new List<UserData>();
                foreach (var user in usersJsonArray)
                {
                    DataParser.ParsePublicUserData(user.Value.ToString(), out UserData ud);
                    ud.setId(user.Value["id"]);
                    foundUsers.Add(ud);
                }

                onLoaded.Invoke(foundUsers);

                //old for single user
                //string foundUserId = nameIdJsonObj[userName].Value;
                //dataController.DownloadUserDataREST(foundUserId, (userdata) => onLoaded.Invoke(userdata));
            }

        }, (exception) =>
        {
            Debug.LogError($"Error while calling SearchFriend. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling SearchFriend data. Message - {exception.Message}");
            onLoaded.Invoke(null);
        });
    }

    public UserData GetActiveFriendById(string id)
    {
        return activeFriendsList.Find(f => f.Id == id);
    }
    public List<UserData> GetWishingToBeFriends()
    {
        return wishingToBeFriendsUsers;
    }
    public int GetFriendshipRequestsCount()
    {
        return wishingToBeFriendsUsers.Count;
    }
    public List<UserData> GetActiveFriends()
    {
        return activeFriendsList;
    }
    //old?
    //public void GetActiveFriendsShallow(Action<string> onGet)
    //{
    //    if(activeFriendsList == null)
    //    {
    //        FirebaseManager.Instance.Database.GetShallowTest($"users/{dataController.GetUserId()}/private/friends", (json) =>
    //        {
    //            Debug.Log("FriendsList count was loaded. Response - " + json);

    //            onGet.Invoke(0);
    //        }, (exception) =>
    //        {
    //            Debug.LogError($"Exception while downloading ... data. Message - {exception.Message}");
    //            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading ... data. Message - {exception.Message}");

    //            onGet.Invoke(-1);
    //        });
    //    }
    //    else
    //    {
    //        onGet(activeFriendsList.Count);
    //    }
        
    //}
    public int GetOnlineFriendsCount()
    {
        int counter = 0;
        foreach (var f in activeFriendsList)
        {
            if (f.status == "online")
            {
                counter++;
            }
        }

        return counter;
    }

    public bool HasFriendWithId(string id)
    {
        UserData friend = activeFriendsList.Find(f => f.Id == id);
        return friend != null;
    }
    public bool IsExpectedFriend(string id)
    {
        UserData expectedFriend = expectedFriendsList.Find(f => f.Id == id);
        return expectedFriend != null;
    }
    public bool IsWishingToBeFriend(string id)
    {
        UserData wishingToBeFriend = wishingToBeFriendsUsers.Find(f => f.Id == id);
        return wishingToBeFriend != null;
    }
    public bool IsActiveFriend(string id)
    {
        UserData activeFriend = activeFriendsList.Find(f => f.Id == id);
        return activeFriend != null;
    }
     
    public void OnNewFriendship(UserData newFriend)
    {
        activeFriendsList.Add(newFriend);

        //refactor. bad code
        RemoveFromExpectedById(newFriend.Id);
        RemoveFromWishingById(newFriend.Id);

        Messenger.Broadcast<UserData>(FriendshipNotifications.OnNewFriendship, newFriend);
    }
    public void OnNewWishing(UserData newWishing)
    {
        wishingToBeFriendsUsers.Add(newWishing);

        Messenger.Broadcast<UserData>(FriendshipNotifications.OnNewFriendshipInvitation, newWishing);
    }
    //refactor? Bad code? Создал, так как при приглашении в друзья происходит двойное добавление в лист. Через InviteFriend и через слушатель
    private void AddToExpectedById(string id)
    {
        if (IsExpectedFriend(id))
            return;

        expectedFriendsList.Add(new UserData(id));
    }
    public void RemoveFromWishingById(string newFriendId)
    {
        //wishingToBeFriendsUsers.Remove(newFriend); //not working
        UserData removing = wishingToBeFriendsUsers.Find(f => f.Id == newFriendId); //refactor. bad code
        wishingToBeFriendsUsers.Remove(removing);
    }
    public void RemoveFromExpectedById(string newFriendId)
    {
        //wishingToBeFriendsUsers.Remove(newFriend); //not working
        UserData removing = expectedFriendsList.Find(f => f.Id == newFriendId); //refactor. bad code
        expectedFriendsList.Remove(removing);
    }
    public void RemoveFromActiveById(string newFriendId)
    {
        //wishingToBeFriendsUsers.Remove(newFriend); //not working
        UserData removing = activeFriendsList.Find(f => f.Id == newFriendId); //refactor. bad code
        activeFriendsList.Remove(removing);
    }
}
