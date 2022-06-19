using BestHTTP.ServerSentEvents;
using Proyecto26;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotificationEvents
{
    public const string OnFriendInvitationResponseGet = "OnFriendInvitationResponseGet";
    public const string OnGameInvitationResponseGet = "OnGameInvitationResponseGet";
    public const string OnNewNotification = "OnNewNotification";
    public const string OnNotificationsLoaded = "OnNotificationsLoaded"; //not used?
}

public class NotificationsController : MonoBehaviour, INotificationController<DateTime>
{
    private DataController dataController;

    public SortedList<DateTime, INotification<DateTime>> notificationsPool; //edit make private
    private EventSource notificationsListener;
    private string notificationsPath;

    public SortedList<DateTime, INotification<DateTime>> NotificationList { get => notificationsPool; set => notificationsPool = value; }
    public EventSource Listener { get => notificationsListener; set => notificationsListener = value; }
    public string NotificationsPath { get => notificationsPath; set => notificationsPath = value; }

    #region MonoBehaviour
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        dataController = FindObjectOfType<DataController>();
        Setup($"users/{dataController.GetUserId()}/private/notifications");
        //LoadNotifications();
    }
    private void OnDestroy()
    {
        StopListen();

        Messenger.RemoveListener<string>(NotificationEvents.OnFriendInvitationResponseGet, RemoveFriendInvitationNotification);
        Messenger.RemoveListener<string>(NotificationEvents.OnGameInvitationResponseGet, RemoveGameInvitationNotification);
    }
    #endregion

    public void RemoveFriendInvitationNotification(string friendId)
    {
        INotification<DateTime> removing = NotificationList.FirstOrDefault(n => ((NotificationData)n.Value).sender == friendId).Value;
        if (removing != null)
            RemoveNotification(removing);
        else
            Debug.LogWarning("Canceled removing notification. FriendId - " + friendId);
    }
    public void RemoveGameInvitationNotification(string gameInviter) //? is correct edit
    {
        INotification<DateTime> removing = NotificationList.FirstOrDefault(n => ((NotificationData)n.Value).sender == gameInviter).Value;
        if (removing != null)
            RemoveNotification(removing);
        else
            Debug.LogWarning("Canceled removing notification. GameInviter - " + gameInviter);
    }

    #region INotificationController API
    public void Setup(string path)
    {
        this.notificationsPath = path;
        notificationsPool = new SortedList<DateTime, INotification<DateTime>>();
        notificationsListener = FirebaseManager.Instance.Database.ListenForChildChanged(notificationsPath, (json) =>
        {
            DataParser.ParseNotificationsData(json, out List<NotificationData> notifications);
            foreach (var n in notifications)
            {
                n.SetController(this);
                AddNotification(n);
            }
                

            //get last notification and broadcast it
            if(notifications.Count > 0)
                Messenger.Broadcast(NotificationEvents.OnNewNotification, notifications[notifications.Count - 1]);
        }, (exception) =>
        {
            Debug.LogError($"Exception while listening {notificationsPath} data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while listening {notificationsPath} data. Message - {exception.Message}");
        });

        Messenger.MarkAsPermanent(NotificationEvents.OnFriendInvitationResponseGet);
        Messenger.AddListener<string>(NotificationEvents.OnFriendInvitationResponseGet, RemoveFriendInvitationNotification);
        Messenger.MarkAsPermanent(NotificationEvents.OnGameInvitationResponseGet);
        Messenger.AddListener<string>(NotificationEvents.OnGameInvitationResponseGet, RemoveGameInvitationNotification);
    }
    public void StartListen() => notificationsListener.Open();
    public void PauseListen() => notificationsListener.Dispose();
    public void StopListen() => notificationsListener.Close();
    public void LoadNotifications()
    {
        FirebaseManager.Instance.Database.GetJson(notificationsPath, (json) =>
        {
            DataParser.ParseNotificationsData(json, out List<NotificationData> notifications);
            foreach (var notification in notifications)
            {
                //if (notification.type == "FriendInvitation" && notification.FriendId != null)
                //{
                //    wishingToBeFriendsUsers.Add(new UserData(notification.FriendId));
                //}

                //refactor?
                //Не очень нравится сравнение по дате и времени, так как у уведомления может быть одна дата и время (хоть и редко)
                if (notificationsPool.TryGetValue(notification.Date, out INotification<DateTime> notUsed))
                    continue;

                notificationsPool.Add(notification.Date, notification);
            };

            //refactor Думаю такой подход дает проблемы, так как обработчик события может быть инициализирован позже
            if (notificationsPool.Count > 0)
                Messenger.Broadcast(NotificationEvents.OnNewNotification, GetLastNotification());
        }, (exception) =>
        {
            Debug.LogError($"Exception while checking notifications. Message - " + exception.Message);
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while checking notifications. Message - " + exception.Message);
        });
    }
    public void OnNewNotification(INotification<DateTime> notification)
    {
        AddNotification(notification);

        Messenger.Broadcast(NotificationEvents.OnNewNotification, (NotificationData) notification);
    }
    public void AddNotification(INotification<DateTime> notification)
    {
        DateTime dateTime = ((NotificationData)notification).Date;
        notificationsPool.Add(dateTime, notification);
    }
    public void RemoveNotification(INotification<DateTime> notification)
    {
        DateTime dateTime = ((NotificationData)notification).Date;
        notificationsPool.Remove(dateTime);

        string key = ((NotificationData)notification).Key;
        FirebaseManager.Instance.Database.Delete($"{notificationsPath}/{key}", () =>
        {
            Debug.Log($"Success removing notification with key - {key}");
        }, (exception) =>
        {
            Debug.LogError("Exception while removing notification. Message - " + exception.Message);
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Critical,
                "Error while removing notification. Message - " + exception.Message);
        });
    }
    public INotification<DateTime> GetLastNotification()
    {
        if (notificationsPool.Count == 0) return null;
        return notificationsPool.FirstOrDefault().Value;
    }
    public void Sort()
    {
        throw new NotImplementedException();
    }
    #endregion

}
