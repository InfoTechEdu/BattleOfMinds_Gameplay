using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FriendSearchResultInfoView : MonoBehaviour
{
    DataController dataController;

    UserData friendData;

    private Image profilePhoto;
    private Text nameText;
    private Text userStatusText;
    private Text viewStatusText;

    //private Button addToFriendButton;
    private DynamicButton inviteToGameDynamicButton;
    private Transform acceptDeclineMenu;

    private void Awake()
    {
        dataController = FindObjectOfType<DataController>();

        nameText = transform.FindDeepChild("Name").GetComponent<Text>();
        userStatusText = transform.FindDeepChild("UserStatusText").GetComponent<Text>();
        viewStatusText = transform.FindDeepChild("ViewStatusText").GetComponent<Text>();

        profilePhoto = transform.FindDeepChild("ProfilePhoto").GetComponent<Image>();

        acceptDeclineMenu = transform.FindDeepChild("AcceptDeclineMenu");

        nameText.text = "Идет загрузка";
        userStatusText.text = "Идет загрузка";

        viewStatusText.gameObject.SetActive(true);
        viewStatusText.text = "";

        inviteToGameDynamicButton = transform.FindDeepChild("InviteToGameButton").GetComponent<DynamicButton>();
        inviteToGameDynamicButton.Configure("#5D9DF2", "Пригласить в игру");
        inviteToGameDynamicButton.gameObject.SetActive(false);
        acceptDeclineMenu = transform.FindDeepChild("AcceptDeclineMenu");
        acceptDeclineMenu.gameObject.SetActive(false);
    }

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

        //Может возникнуть, когда мы инстанциируем пустой view 
        if (!gameObject.activeInHierarchy)
            return;

        nameText.text = friendData.FullName;
        profilePhoto.sprite = friendData.ProfilePhoto;


        //danger. Потенциально плохо может работать в случае, если новый друг добавился во время поиска игры. Тогда Loaded = true, но данные конкретной сессии, связанных
        //с данным другом могут не успеть прийти. И ниже мы получим null в friendly session. Хотя сессия может существовать на сервере.
        //В результате игрок попытается пригласить другого в игру, в то время как сессия уже есть. Так-то сервер отклоняет такие запросы. Но все же пользователь может увидеть слово "Ошибка"
        if (!dataController.LobbiesController.Loaded)
        {
            StateSeessionDataLoading();
            return;
        }
        SessionData friendlySession = dataController.LobbiesController.GetSessionByOpponentId(friendData.Id);
        if (friendlySession == null)
        {
            StateNoInfo();
            return;
        }

        if (friendlySession.Status == "expected" && friendlySession.GameInviter == friendData.Id)
        {
            StateIsInviting();
        }
        else if (friendlySession.Status == "expected" && friendlySession.GameInviter == dataController.GetUserId()) //edit
        {
            StateIsAlreadyInvited();
        }
        else if (friendlySession.Status == "active") //edit
        {
            StateAlreadyHasActiveGame();
        }
        else
        {
            StateNoInfo();
        }
    }
    //refactor? is bad code?
    public void OnProfilePhotoLoaded()
    {
        profilePhoto.sprite = friendData.ProfilePhoto;
    }

    private void StateNoInfo()
    {
        ClearView();
        inviteToGameDynamicButton.gameObject.SetActive(true);
        inviteToGameDynamicButton.Reload();
        //inviteToGameDynamicButton.GetComponentInChildren<Text>().text = "Пригласить в игру"; //old

        inviteToGameDynamicButton.LoadedText = "Отправлено";
        inviteToGameDynamicButton.LoadingText = "Отправка...";
        inviteToGameDynamicButton.ErrorText = "Ошибка";
        inviteToGameDynamicButton.GetComponent<Button>().onClick.RemoveAllListeners();
        inviteToGameDynamicButton.GetComponent<Button>().onClick.AddListener(OnInviteToGameClicked);
    }
    private void StateSeessionDataLoading()
    {
        ClearView();
        inviteToGameDynamicButton.gameObject.SetActive(true);

        inviteToGameDynamicButton.LoadedText = "Идет загрузка";
        inviteToGameDynamicButton.OnDataLoaded(true); //refactor. bad code. (bad method name?)
    }

    private void StateIsAlreadyInvited()
    {
        ClearView();
        inviteToGameDynamicButton.gameObject.SetActive(true);

        inviteToGameDynamicButton.LoadedText = "Заявка отправлена";
        inviteToGameDynamicButton.OnDataLoaded(true); //refactor. bad code. (bad method name?)
    }

    //*friend invites users
    private void StateIsInviting()
    {
        ClearView();
        acceptDeclineMenu.gameObject.SetActive(true);

        Button acceptButton = acceptDeclineMenu.FindDeepChild("AcceptButton").GetComponent<Button>();
        Button declineButton = acceptDeclineMenu.FindDeepChild("DeclineButton").GetComponent<Button>();

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(OnGameAccepted);

        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(OnGameDeclined);

        userStatusText.text = "Хочет сыграть с вами в игру";
    }

    private void StateAlreadyHasActiveGame()
    {
        ClearView();
        viewStatusText.text = "Есть игра";
    }

    //danger bad code refactor
    public void OnInviteToGameClicked()
    {
        FindObjectOfType<FriendSessionController>().InviteToGame(friendData.Id, (createdGameId) =>
        {
            if (createdGameId != null)
            {
                Debug.Log("Game invitation was send. Created game id - " + createdGameId);
                inviteToGameDynamicButton.gameObject.SetActive(false);

                viewStatusText.gameObject.SetActive(true);
                viewStatusText.text = "Отправлено";
            }
            else
            {
                inviteToGameDynamicButton.gameObject.SetActive(false);

                viewStatusText.gameObject.SetActive(true);
                viewStatusText.text = "Ошибка";
            }
        });

        inviteToGameDynamicButton.OnClicked();
    }
    //refactor bad code. dublicated method
    public void OnGameAccepted()
    {
        FindObjectOfType<FriendSessionController>().AcceptGameInvitation(friendData.Id, (createdGameId) =>
        {
            if (createdGameId != null)
            {
                Debug.Log("Game invitation was send. Created game id - " + createdGameId);
                inviteToGameDynamicButton.gameObject.SetActive(false);

                viewStatusText.gameObject.SetActive(true);
                viewStatusText.text = "Принято";

                StateAlreadyHasActiveGame();
            }
            else
            {
                inviteToGameDynamicButton.gameObject.SetActive(false);

                viewStatusText.gameObject.SetActive(true);
                viewStatusText.text = "Ошибка";
            }
        });
    }
    public void OnGameDeclined()
    {
        FindObjectOfType<FriendSessionController>().DeclineGameInvitation(friendData.Id, (response) =>
        {
            if (response != null)
            {
                ClearView();

                viewStatusText.text = "Отклонено";
            }
            else
            {
                inviteToGameDynamicButton.gameObject.SetActive(false);

                viewStatusText.gameObject.SetActive(true);
                viewStatusText.text = "Ошибка";
            }
        });
    }
    private void ClearView()
    {
        inviteToGameDynamicButton.gameObject.SetActive(false);
        acceptDeclineMenu.gameObject.SetActive(false);

        viewStatusText.text = "";
        userStatusText.text = "";
    }
}
