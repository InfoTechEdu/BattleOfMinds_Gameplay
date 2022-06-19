using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationsViewController : MonoBehaviour
{
    public Transform gameEndNotificationPanel;
    public Transform friendshipInviteNotificationPanel;
    public Transform friendshipStatusNotificationPanel;
    public Transform gameInviteNotificationPanel;

    private float timeOutDelay = 7f;

    private void Start()
    {
        gameEndNotificationPanel.gameObject.SetActive(false);
        friendshipInviteNotificationPanel.gameObject.SetActive(false);
        friendshipStatusNotificationPanel.gameObject.SetActive(false);
        gameInviteNotificationPanel.gameObject.SetActive(false);
    }

    public void ShowGameInviteNotification(UserData opponent, NotificationData notification, Action<string, UserData> onAccept, Action<string, UserData> onDecline)
    {
        Image profilePhoto = gameInviteNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
        profilePhoto.sprite = opponent.ProfilePhoto;

        Text messageText = gameInviteNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
        messageText.text = "Приглашает вас в игру";

        Text nameText = gameInviteNotificationPanel.FindDeepChild("NameValue").GetComponent<Text>();
        nameText.text = $"Игрок {opponent.Name}";

        Button acceptGameButton = gameInviteNotificationPanel.FindDeepChild("AcceptButton").GetComponent<Button>();
        acceptGameButton.onClick.RemoveAllListeners();
        acceptGameButton.onClick.AddListener(() =>
        {
            onAccept.Invoke(notification.gameId, opponent);
            notification.OnReacted();
            gameInviteNotificationPanel.gameObject.SetActive(false);
        });

        Button declineGameButton = gameInviteNotificationPanel.FindDeepChild("DeclineButton").GetComponent<Button>();
        declineGameButton.onClick.RemoveAllListeners();
        declineGameButton.onClick.AddListener(() =>
        {
            onDecline.Invoke(notification.gameId, opponent);
            notification.OnReacted();
            gameInviteNotificationPanel.gameObject.SetActive(false);
        });



        gameInviteNotificationPanel.gameObject.SetActive(true);
    }

    public void ShowGameEndNotification(UserData opponent, NotificationData notification, Action onSubmitted = null)
    {
        Image opponentProfilePhoto = gameEndNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
        opponentProfilePhoto.sprite = opponent.ProfilePhoto;

        Text opponentName = gameEndNotificationPanel.FindDeepChild("OpponentFullName").GetComponent<Text>();
        opponentName.text = opponent.FullName;

        Text messageText = gameEndNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
        if (notification.winStatus == "lose")
        {
            messageText.text = "К сожалению, вы проиграли. \r\n Ваш соперник:";
        }
        else if (notification.winStatus == "draw")
        {
            messageText.text = "Поздравляем! У вас ничья. \r\n Ваш соперник:";
        }
        else if (notification.winStatus == "win")
        {
            messageText.text = "Поздравляем! Вы выиграли! \r\n Ваш соперник:";
        }

        Button closeButton = gameEndNotificationPanel.FindDeepChild("CloseButton").GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => {
            onSubmitted?.Invoke();
            notification.OnReacted();
          });

        gameEndNotificationPanel.gameObject.SetActive(true);
    }

    public void ShowFriendInviteNotification(UserData friend, NotificationData notification, Action<object, string, bool, Action<bool>> onResponseGet)
    {
        Image profilePhoto = friendshipInviteNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
        profilePhoto.sprite = friend.ProfilePhoto;

        Text nameText = friendshipInviteNotificationPanel.FindDeepChild("NameValue").GetComponent<Text>();
        nameText.text = $"Игрое {friend.Name}";
        Text messageText = friendshipInviteNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
        messageText.text = "Хочет добавить вас в друзья";

        Button acceptFriendshipButton = friendshipInviteNotificationPanel.FindDeepChild("AcceptButton").GetComponent<Button>();
        acceptFriendshipButton.onClick.RemoveAllListeners();
        acceptFriendshipButton.onClick.AddListener(() => {
            onResponseGet.Invoke(this, friend.id, true, null);
            notification.OnReacted();
            friendshipInviteNotificationPanel.gameObject.SetActive(false);
        }); //refactor.

        Button declineFriendshipButton = friendshipInviteNotificationPanel.FindDeepChild("DeclineButton").GetComponent<Button>();
        declineFriendshipButton.onClick.RemoveAllListeners();
        declineFriendshipButton.onClick.AddListener(() => {
            onResponseGet.Invoke(this, friend.id, false, null);
            notification.OnReacted();
            friendshipInviteNotificationPanel.gameObject.SetActive(false);
        });

        friendshipInviteNotificationPanel.gameObject.SetActive(true);
    }

    public void ShowFriendshipStatusNotification(UserData friend, NotificationData notification, Action onSubmitted = null)
    {
        Image profilePhoto = friendshipStatusNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
        profilePhoto.sprite = friend.ProfilePhoto;

        Text nameText = friendshipStatusNotificationPanel.FindDeepChild("NameValue").GetComponent<Text>();
        nameText.text = $"Игрок {friend.Name}";
        Text messageText = friendshipStatusNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
        messageText.text = notification.isAccepted ? "Принял ваше предложение о дружбе" : "Отклонил ваше предложение о дружбе";

        Button okButton = friendshipStatusNotificationPanel.FindDeepChild("OkButton").GetComponent<Button>();
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() => {
            onSubmitted?.Invoke();
            notification.OnReacted();
        });

        friendshipStatusNotificationPanel.gameObject.SetActive(true);
    }
    public void ShowGameInvitationStatusNotification(UserData friend, NotificationData notification, Action onSubmitted = null)
    {
        Image profilePhoto = friendshipStatusNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
        profilePhoto.sprite = friend.ProfilePhoto;

        Text nameText = friendshipStatusNotificationPanel.FindDeepChild("NameValue").GetComponent<Text>();
        nameText.text = $"Игрок {friend.Name}";
        Text messageText = friendshipStatusNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
        messageText.text = notification.isAccepted ? "Принял вашу игру" : "Отклонил вашу игру";

        Button okButton = friendshipStatusNotificationPanel.FindDeepChild("OkButton").GetComponent<Button>();
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() => {
            onSubmitted?.Invoke();
            notification.OnReacted();
        });

        friendshipStatusNotificationPanel.gameObject.SetActive(true);
    }


    //public void ShowGameEndNotification(UserData opponent, string gameStatus, Action onSubmitted)
    //{
    //    gameEndNotificationPanel.gameObject.SetActive(true);

    //    Text messageText = gameEndNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
    //    if (gameStatus == "lost")
    //    {
    //        messageText.text = "К сожалению, вы проиграли. \r\n Ваш соперник:";
    //    }
    //    if (gameStatus == "draw")
    //    {
    //        messageText.text = "Поздравляем! У вас ничья. \r\n Ваш соперник:";
    //    }
    //    if (gameStatus == "win")
    //    {
    //        messageText.text = "Поздравляем! Вы выиграли! \r\n Ваш соперник:";
    //    }

    //    Button closeButton = gameEndNotificationPanel.FindDeepChild("CloseButton").GetComponent<Button>();
    //    closeButton.onClick.AddListener(()=> onSubmitted());

    //    Text opponentName = gameEndNotificationPanel.FindDeepChild("OpponentFullName").GetComponent<Text>();
    //    opponentName.text = opponent.FullName;
    //    Image opponentProfilePhoto = gameEndNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
    //    opponentProfilePhoto.sprite = opponent.ProfilePhoto;

    //    gameEndNotificationPanel.gameObject.SetActive(true);
    //}

    //public void ShowFriendInviteNotification(UserData friend, Action<object, string, bool, Action<bool>> onResponseGet)
    //{
    //    Image profilePhoto = friendshipInviteNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
    //    profilePhoto.sprite = friend.ProfilePhoto;

    //    Text messageText = friendshipInviteNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
    //    messageText.text = "Хочет добавить вас в друзья";

    //    Button acceptFriendshipButton = friendshipInviteNotificationPanel.FindDeepChild("AcceptButton").GetComponent<Button>();
    //    acceptFriendshipButton.onClick.AddListener(()=> { 
    //        onResponseGet.Invoke(this, friend.id, true, null);
    //        friendshipInviteNotificationPanel.gameObject.SetActive(false);
    //    }); //refactor.

    //    Button declineFriendshipButton = friendshipInviteNotificationPanel.FindDeepChild("DeclineButton").GetComponent<Button>();
    //    declineFriendshipButton.onClick.AddListener(() => {
    //        onResponseGet.Invoke(this, friend.id, false, null);
    //        friendshipInviteNotificationPanel.gameObject.SetActive(false);
    //    });

    //    friendshipInviteNotificationPanel.gameObject.SetActive(true);
    //}

    //public void ShowFriendshipStatusNotification(UserData friend, bool isFriendshipAccepted, Action onSubmitted)
    //{
    //    Button okButton = gameEndNotificationPanel.FindDeepChild("OkButton").GetComponent<Button>();
    //    okButton.onClick.AddListener(() => onSubmitted());

    //    Image profilePhoto = friendshipStatusNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
    //    profilePhoto.sprite = friend.ProfilePhoto;

    //    Text messageText = friendshipStatusNotificationPanel.FindDeepChild("Message").GetComponent<Text>();
    //    messageText.text = isFriendshipAccepted? "Принял ваше предложение о дружбе" : "Отклонил ваше предложение о дружбе";

    //    friendshipStatusNotificationPanel.gameObject.SetActive(true);
    //}

    //public void ShowGameInviteNotification(UserData opponent, string sessionId, Action<string, UserData> onAccept, Action<string, UserData> onDecline)
    //{
    //    Image profilePhoto = gameInviteNotificationPanel.FindDeepChild("ProfilePhoto").GetComponent<Image>();
    //    profilePhoto.sprite = opponent.ProfilePhoto;

    //    Button acceptGameButton = gameInviteNotificationPanel.FindDeepChild("AcceptButton").GetComponent<Button>();
    //    acceptGameButton.onClick.AddListener(() =>
    //    {
    //        onAccept.Invoke(sessionId, opponent);

    //    });

    //    Button declineGameButton = gameInviteNotificationPanel.FindDeepChild("DeclineButton").GetComponent<Button>();
    //    declineGameButton.onClick.AddListener(() =>
    //    {
    //        onDecline.Invoke(sessionId, opponent);
    //        gameInviteNotificationPanel.gameObject.SetActive(false);
    //    });

    //    gameInviteNotificationPanel.gameObject.SetActive(true);
    //}
}
