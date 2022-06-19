using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.ServerSentEvents;

public interface INotificationController<T>
{
    SortedList<T, INotification<T>> NotificationList { get; set; }
    EventSource Listener { get; set; }
    string NotificationsPath { get; set; }

    void Setup(string listenerUrl);
    void StartListen();
    void PauseListen();
    void StopListen();
    void LoadNotifications();
    void OnNewNotification(INotification<T> notification);
    void AddNotification(INotification<T> notification);
    void RemoveNotification(INotification<T> notification);
    INotification<T> GetLastNotification();
    void Sort();

}
