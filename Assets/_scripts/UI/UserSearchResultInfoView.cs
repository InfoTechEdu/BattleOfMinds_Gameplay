using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UserSearchResultInfoView : MonoBehaviour
{
    DataController dataController;

    UserData foundUserData;

    private Image profilePhoto;
    private Text nameText;
    private Text userStatusText;
    private Text viewStatusText;

    //private Button addToFriendButton;
    private DynamicButton addToFriendDynamicButton;
    private Transform acceptDeclineMenu;
    
    private void Awake()
    {
        dataController = FindObjectOfType<DataController>();

        nameText = transform.Find("Name").GetComponent<Text>();
        userStatusText = transform.Find("UserStatusText").GetComponent<Text>();
        viewStatusText = transform.Find("ViewStatusText").GetComponent<Text>();

        profilePhoto = transform.FindDeepChild("ProfilePhoto").GetComponent<Image>();

        acceptDeclineMenu = transform.FindDeepChild("AcceptDeclineMenu");

        nameText.text = "Идет загрузка";
        userStatusText.text = "Идет загрузка";

        viewStatusText.gameObject.SetActive(true);
        viewStatusText.text = "";

        addToFriendDynamicButton = transform.FindDeepChild("AddToFriendButton").GetComponent<DynamicButton>();
        addToFriendDynamicButton.gameObject.SetActive(false);
        acceptDeclineMenu = transform.FindDeepChild("AcceptDeclineMenu");
        acceptDeclineMenu.gameObject.SetActive(false);
    }
    public void LoadAndUpdateViewData(UserData ud, bool wishingToBeFriend)
    {
        foundUserData = ud;

        UpdateView();
    }

    public void UpdateView()
    {
        if (foundUserData == null || foundUserData.ProgressData == null)
        {
            Debug.Log("Can not update UserSearchResultInfoView. friend data is null");
            return;
        }

        //Может возникнуть, когда мы инстанциируем пустой view 
        if (!gameObject.activeInHierarchy)
            return;

        nameText.text = foundUserData.FullName;
        profilePhoto.sprite = foundUserData.ProfilePhoto;

        if (dataController.FriendshipController.IsWishingToBeFriend(foundUserData.Id)) //is wishing
        {
            StateIsWishing();
        }
        else if (dataController.FriendshipController.IsExpectedFriend(foundUserData.Id)) //is expected
        {
            StateIsExpected();
        }
        else if (dataController.FriendshipController.HasFriendWithId(foundUserData.Id)) //is active
        {
            StateIsFriend();
        }
        else //nothing
        {
            StateNoInfo();
        } 
    }
    //refactor? is bad code?
    public void OnProfilePhotoLoaded()
    {
        profilePhoto.sprite = foundUserData.ProfilePhoto;
    }

    private void StateNoInfo()
    {
        ClearView();
        addToFriendDynamicButton.gameObject.SetActive(true);

        addToFriendDynamicButton.LoadedText = "Заявка отправлена";
        addToFriendDynamicButton.LoadingText = "Отправка...";
        addToFriendDynamicButton.ErrorText = "Ошибка";
        addToFriendDynamicButton.GetComponent<Button>().onClick.RemoveAllListeners();
        addToFriendDynamicButton.GetComponent<Button>().onClick.AddListener(OnInviteToFriendshipClicked);
    }

    private void StateIsExpected()
    {
        ClearView();
        addToFriendDynamicButton.gameObject.SetActive(true);

        addToFriendDynamicButton.LoadedText = "Заявка отправлена";
        addToFriendDynamicButton.OnDataLoaded(true); //refactor. bad code. (bad method name?)
    }

    private void StateIsWishing()
    {
        ClearView();
        acceptDeclineMenu.gameObject.SetActive(true);

        Button acceptButton = acceptDeclineMenu.FindDeepChild("AcceptButton").GetComponent<Button>();
        Button declineButton = acceptDeclineMenu.FindDeepChild("DeclineButton").GetComponent<Button>();

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(OnFriendshipAccepted);

        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(OnFriendshipDeclined);

        userStatusText.text = "Хочет добавить вас в друзья";
    }

    private void StateIsFriend()
    {
        ClearView();
        viewStatusText.text = "Ваш друг";
    }

    //danger bad code refactor
    public void OnInviteToFriendshipClicked()
    {
        dataController.FriendshipController.InviteFriend(foundUserData.Id, (notUsed) => {
            addToFriendDynamicButton.gameObject.SetActive(false);

            viewStatusText.gameObject.SetActive(true);
            viewStatusText.text = "Отправлено";
        }, () => {
            addToFriendDynamicButton.gameObject.SetActive(false);

            viewStatusText.gameObject.SetActive(true);
            viewStatusText.text = "Ошибка";
        });

        addToFriendDynamicButton.OnClicked();
    }
    public void OnFriendshipAccepted()
    {
        Messenger.Broadcast<string, string, Action<bool>>("OnAcceptFriendshipClicked", foundUserData.Id, "battleofminds", (success) => {
            if (!success)
            {
                //refactor? Может добавить Error message Text ?
                Debug.LogError("Error while accepting friendship");
                ClearView();
                
                viewStatusText.gameObject.SetActive(true);
                viewStatusText.text = "Ошибка";
                return;
            }
            else
            {
                StateIsFriend();
            }
        });
    }
    public void OnFriendshipDeclined()
    {
        Messenger.Broadcast<string, string, Action<bool>>("OnDeclineFriendshipClicked", foundUserData.Id, "battleofminds", (success) => {
            if (!success)
            {
                //refactor? Может добавить Error message Text ?
                Debug.LogError("Error while declining friendship");
                ClearView();

                viewStatusText.text = "Ошибка";
                return;
            }
            else
            {
                ClearView();

                viewStatusText.text = "Отклонено";
            }
        });
    }
    private void ClearView()
    {
        addToFriendDynamicButton.gameObject.SetActive(false);
        acceptDeclineMenu.gameObject.SetActive(false);
        
        viewStatusText.text = "";
        userStatusText.text = "";
    }
}
