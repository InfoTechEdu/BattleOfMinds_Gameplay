using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FriendSessionController : MonoBehaviour
{
    [SerializeField] private FriendSessionViewController friendshipViewController;

    private DataController dataController;

    public UserData GetUserData() => dataController.GetUserData();
    public List<UserData> GetFriendsList() => dataController.FriendshipController.GetActiveFriends();
    public void GetUserDataById(string id, Action<UserData> onGet) => dataController.DownloadUserDataREST(id, onGet, true);
    public void UpdateFriendData(UserData friend) => dataController.FriendshipController.UpdateActiveFriendData(friend);
    public void UpdateSessionDataWithFriend(UserData friend) {
        SessionData sd = dataController.LobbiesController.GetSessionByOpponentId(friend.Id);
        dataController.LobbiesController.UpdateSessionDataById(sd.Id);
    }
    public string GetOpponentIdFromSession(SessionData session)
    {
        return session.GetOpponentId(dataController.GetUserId());
    }
    public void UpdateSessionData(string sessionId) => dataController.LobbiesController.UpdateSessionDataById(sessionId);
    public UserData SearchFriend(string friendName)
    {
        //return dataController.FriendshipController.GetActiveFriendById()
        return dataController.FriendshipController.GetActiveFriends().Find(f => f.Name == friendName);
    }

    public List<UserData> SearchFriends(string friendName)
    {
        List<UserData> allFriends = dataController.FriendshipController.GetActiveFriends();
        List<UserData> serchFriends = new List<UserData>();

        foreach (UserData friend in allFriends)
        {
            if (friend.progressData.name.ToUpper().Contains(friendName.ToUpper()))
            {
                serchFriends.Add(friend);
            }
        }

        return serchFriends;
    }

    public bool FriendsDataWasLoaded { get => dataController.FriendshipController.Loaded; }
    //Нет необходимости, так как они грузятся в MenuScreen
    //public void UpdateFriendsData() => dataController.FriendshipController.UpdateActiveFriendsData();

    private void Awake()
    {
        dataController = FindObjectOfType<DataController>();

        //notifications listeners
        Messenger.AddListener<NotificationData>(NotificationEvents.OnNewNotification, OnNewNotification);
    }
    private void OnDestroy()
    {
        //notifications listeners
        Messenger.RemoveListener<NotificationData>(NotificationEvents.OnNewNotification, OnNewNotification);
    }
    public void InviteToGame(string friendId, Action<string> callback)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("game", "battleofminds");
        @params.Add("inviterId", dataController.GetUserId());
        @params.Add("invitingId", friendId);

        FirebaseManager.Instance.Functions.CallCloudFunction("InviteToGame", @params, (data) =>
        {
            Debug.Log($"Success calling function InviteToGame");
            callback.Invoke(data.body);
        }, (exception) =>
        {
            Debug.LogError($"Error while calling InviteToGame");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling InviteToGame data. Message - {exception.Message}");
            callback.Invoke(null);
        });
    }
    public void StartNewFriendlyGame(UserData opponent, string sessionId)
    {
        dataController.AcceptGameInvitation(sessionId, opponent.Id, "battleofminds", (success) => Debug.Log($"Accepting game success status - {success}"));
        dataController.UpdateCurrentOpponentData(opponent);
        dataController.UpdateCurrentOnlineSession(new SessionData(sessionId));

        SceneManager.LoadScene("SessionScreen");
    }
    public void AcceptGameInvitation(string invitingFriendId, Action<string> onReady) //bad code
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("userId", dataController.GetUserId());
        @params.Add("inviterId", invitingFriendId);
        @params.Add("game", "battleofminds");
        @params.Add("isAccepted", true);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineGameInvitation", @params, (data) =>
        {
            if (data.statusCode != 400)
            {
                Debug.Log($"Success calling function AcceptDeclineGameInvitation");
                onReady.Invoke(data.body);
            }
            else
            {
                Debug.LogError($"Failed calling funtcion AcceptDeclineGameInvitation. Reason - ${data.body}");
                onReady.Invoke(null);

                StartNewFriendlyGame(dataController.FriendshipController.GetActiveFriendById(invitingFriendId), data.body);
            }
        }, (exception) =>
        {
            Debug.LogError($"Error while calling AcceptDeclineGameInvitation. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling AcceptDeclineGameInvitation data. Message - {exception.Message}");
            onReady.Invoke(null);
        });
    }
    public void DeclineGameInvitation(string rejectedFriendId, Action<string> onReady)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("userId", dataController.GetUserId());
        @params.Add("inviterId", rejectedFriendId);
        @params.Add("game", "battleofminds");
        @params.Add("isAccepted", false);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineGameInvitation", @params, (data) =>
        {
            if (data.statusCode != 400)
            {
                Debug.Log($"Success calling function AcceptDeclineGameInvitation");
                onReady.Invoke(data.body);
            }
            else
            {
                Debug.LogError($"Failed calling funtcion AcceptDeclineGameInvitation. Reason - ${data.body}");
                GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Error while calling AcceptDeclineGameInvitation data. Reason - {data.body}");
                onReady.Invoke(null);
            }
        }, (exception) =>
        {
            Debug.LogError($"Error while calling AcceptDeclineGameInvitation. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling AcceptDeclineGameInvitation data. Message - {exception.Message}");
            onReady.Invoke(null);
        });
    }

    NotificationData activeNotification;
    private void OnNewNotification(NotificationData notification)
    {
        activeNotification = notification;

        switch (notification.Type)
        {
            case "GameEnd":
                dataController.DownloadUserDataREST(notification.sender, (loadedOpponent) =>
                {
                    friendshipViewController.ShowGameEndNotification(loadedOpponent, notification, null);
                }, true);

                break;
            case "FriendInvitation":
                dataController.DownloadUserDataREST(notification.sender, (loadedFriend) =>
                {
                    friendshipViewController.ShowFriendInviteNotification(loadedFriend, notification, OnFriendshipResponseReceived);
                }, true);
                //edit;
                break;
            case "FriendshipStatus":
                dataController.DownloadUserDataREST(notification.sender, (loadedFriend) =>
                {
                    friendshipViewController.ShowFriendshipStatusNotification(loadedFriend, notification, null);
                }, true);
                //dataController.NotificationsController.onNotificationWasShown(activeNotification); //old
                break;
            case "GameInvitation":
                dataController.DownloadUserDataREST(notification.sender, (loadedOpponent) =>
                {
                    friendshipViewController.ShowGameInvititationNotification(loadedOpponent, notification, onGameAccepted, onGameDeclined);
                    dataController.UpdateCurrentOpponentData(loadedOpponent);
                }, true);
                //edit
                break;
            case "GameInvitationStatus":
                dataController.DownloadUserDataREST(activeNotification.sender, (loadedOpponent) =>
                {
                    friendshipViewController.ShowGameInvitationStatusNotification(loadedOpponent, notification, null);
                }, true);
                break;
            default:
                break;
        }
    }
    private void OnFriendshipResponseReceived(object sender, string friendId, bool isAccepted, Action<bool> onReady = null)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("userId", dataController.GetUserId());
        @params.Add("wishingId", friendId);
        @params.Add("game", "battleofminds");
        @params.Add("isAccepted", isAccepted);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineFriendInvitation", @params, (data) =>
        {
            if (data.statusCode != 400)
            {
                Debug.Log($"Success calling function AcceptDeclineGameInvitation");
                onReady?.Invoke(true);
            }
        }, (exception) =>
        {
            Debug.LogError($"Error while calling AcceptDeclineGameInvitation. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling AcceptDeclineGameInvitation data. Message - {exception.Message}");
            //onReady?.Invoke(false);
        });
    }
    private void onGameAccepted(string sessionId, UserData inviter)
    {
        activeNotification.OnReacted();
        StartNewFriendlyGame(inviter, sessionId);
       
    }
    private void onGameDeclined(string sessionId, UserData inviter)
    {
        activeNotification.OnReacted();
        dataController.DeclineGameInvitation(sessionId, inviter.Id, "battleofminds", (success) => { Debug.Log("Game decined status = " + success); });
    }



}


