using BestHTTP.ServerSentEvents;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuScreenController : MonoBehaviour
{
    [Header("Dashboard")]
    public Text gamesPlayedText;
    public Text onlinePlayersText;
    public Text raitingValueText;
    public Text pointsValueText;

    [Header("Leaderboard")]
    public GameObject userProgressViewPrefab;
    public GameObject leaderboardScreen;
    public GameObject leaderboardContent;

    [Header("Settings")]
    public GameObject settingsPanel;

    [Header("Friendship")]
    public GameObject userSearchResultViewPrefab;
    public GameObject searchUserScreen;
    public GameObject friendViewPrefab;
    public Transform friendsPanel;
    public Transform friendshipWishersPanel;

    [Header("Lobbies")]
    public GameObject lobbiesPanel;

    [Header("Other view")]
    public GameObject sessionLoadingPanel;
    

    DataController dataController;

    LobbieViewController lobbieViewController;
    NotificationsViewController notificationsViewController;

    private InputField searchUserInput;
    
    private void Awake()
    {
        dataController = FindObjectOfType<DataController>();

        lobbieViewController = FindObjectOfType<LobbieViewController>();
        notificationsViewController = FindObjectOfType<NotificationsViewController>();

        Messenger.MarkAsPermanent("OnDashboardDataUpdated"); //без этой строчки, Messenger вызывает Cleanup метод после загрузки сцены и удаляет все слушатели
        Messenger.AddListener("OnDashboardDataUpdated", UpdateDashboardView);

        Messenger.MarkAsPermanent(FriendshipNotifications.OnAllFriendsListsLoaded); //без этой строчки, Messenger вызывает Cleanup метод после загрузки сцены и удаляет все слушатели
        Messenger.AddListener(FriendshipNotifications.OnAllFriendsListsLoaded, UpdateFriendsView);
        Messenger.MarkAsPermanent(FriendshipNotifications.OnFriendDataLoaded);
        Messenger.AddListener<string>(FriendshipNotifications.OnFriendDataLoaded, UpdateFriendView);
        Messenger.AddListener<string>(FriendshipNotifications.OnFriendDataLoaded, UpdateFriendsOnlineCounter);
        Messenger.MarkAsPermanent(FriendshipNotifications.OnNewFriendship);
        Messenger.AddListener<UserData>(FriendshipNotifications.OnNewFriendship, OnNewFriendAdded);
        Messenger.MarkAsPermanent(FriendshipNotifications.OnRemovedFromWishers); //пока убрал, так как при отказе или принятии приглашения отображается статус
        Messenger.AddListener<string>(FriendshipNotifications.OnRemovedFromWishers, RemoveFromFriendshipWishers);
        Messenger.MarkAsPermanent(FriendshipNotifications.OnNewFriendshipInvitation);
        Messenger.AddListener<UserData>(FriendshipNotifications.OnNewFriendshipInvitation, OnNewWishingAdded);

        Messenger.MarkAsPermanent(NotificationEvents.OnNewNotification); //без этой строчки, Messenger вызывает Cleanup метод после загрузки сцены и удаляет все слушатели
        Messenger.AddListener<NotificationData>(NotificationEvents.OnNewNotification, ShowNotification);

        //Messenger.AddListener<object, string, bool, Action<bool>>("OnFriendshipResponseReceived", OnFriendshipResponseReceived);

        Messenger.MarkAsPermanent("OnAcceptFriendshipClicked"); //без этой строчки, Messenger вызывает Cleanup метод после загрузки сцены и удаляет все слушатели
        Messenger.AddListener<string, string, Action<bool>>("OnAcceptFriendshipClicked", AcceptFriendInvitation); //bad code
        Messenger.MarkAsPermanent("OnDeclineFriendshipClicked");
        Messenger.AddListener<string, string, Action<bool>>("OnDeclineFriendshipClicked", DeclineFriendInvitation); //bad code

        //in lobbie view controller
        //Messenger.MarkAsPermanent(LobbiesNotifications.OnRemovedFromExpected);
        //Messenger.AddListener<SessionData>(LobbiesNotifications.OnRemovedFromExpected, OnGameInvitationRejected);

        //Messenger.AddListener<UserData>("OnNewFriend", OnNewFriendAdded);
        //Messenger.AddListener<UserData>("OnFriendshipDeclined", OnFriendshipDeclined);
        //Пока думаю действовать по принципу live. То есть обрабатываем новое поступившее уведомление

        //NotificationData newNotification = dataController.GetNextNotification();
        //if(newNotification != null)
        //{
        //    onNewNotification(newNotification);
        //}

        //dataController.OnMenuScreenLoaded();
        //Temp edit
        //dataController.LoadUserSessionsList();
         
        UpdateDashboardView();
        //InitFriendsContent();
    }
    private IEnumerator Start()
    {
        searchUserInput = searchUserScreen.transform.FindDeepChild("SearchInput").GetComponent<InputField>();

        yield return dataController.FriendshipController; //while null

        if (dataController.FriendshipController.Preloaded)
        {
            UpdateFriendsContent();//Чтобы если вернулись с окна сессий с другом (а не при первом запуске игры) вновь инстанциировались friendview. refactor?
            UpdateWishersScreen();
        }

        yield return dataController.LobbiesController; //while null

        if (dataController.LobbiesController.Preloaded)
            lobbieViewController.InitLobbiesViews();//Чтобы если вернулись с окна сессий с другом (а не при первом запуске игры) вновь инстанциировались lobbieView. refactor?
    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Return) && searchUserInput.gameObject.activeInHierarchy)
        {
            Debug.Log("Enter clicked");
            SearchUserClicked(searchUserInput);
        }
    }
    private void OnDestroy()
    {
        //dataController.OnMenuScreenLeft();
        
        Messenger.RemoveListener("OnDashboardDataUpdated", UpdateDashboardView);

        Messenger.RemoveListener<NotificationData>(NotificationEvents.OnNewNotification, ShowNotification);

        Messenger.RemoveListener(FriendshipNotifications.OnAllFriendsListsLoaded, UpdateFriendsView);
        Messenger.RemoveListener<string>(FriendshipNotifications.OnFriendDataLoaded, UpdateFriendView);
        Messenger.RemoveListener<string>(FriendshipNotifications.OnFriendDataLoaded, UpdateFriendsOnlineCounter);
        Messenger.RemoveListener<string, string, Action<bool>>("OnAcceptFriendshipClicked", AcceptFriendInvitation); //bad code
        Messenger.RemoveListener<string, string, Action<bool>>("OnDeclineFriendshipClicked", DeclineFriendInvitation); //bad code
        Messenger.RemoveListener<UserData>(FriendshipNotifications.OnNewFriendship, OnNewFriendAdded);
        Messenger.RemoveListener<string>(FriendshipNotifications.OnRemovedFromWishers, RemoveFromFriendshipWishers);
        Messenger.RemoveListener<UserData>(FriendshipNotifications.OnNewFriendshipInvitation, OnNewWishingAdded);
    }

    #region SEARCH FRIEND METHODS 
    public void OpenSearchFriendScreen()
    {
        UpdateSearchFriendScreen();
        searchUserScreen.SetActive(true);
    }
    private void UpdateSearchFriendScreen()
    {
        ClearSearchPanel();

        //Transform resultContent = searchFriendScreen.transform.FindDeepChild("Content");

        //List<UserData> wantToBeFriensUsers = dataController.FriendshipController.GetWishingToBeFriends();
        //for (int i = 0; i < wantToBeFriensUsers.Count; i++)
        //{
        //    Debug.Log("SearchResultView i = " + i + "Instatiated");
        //    GameObject userView = Instantiate(userSearchResultViewPrefab, resultContent);
        //    dataController.DownloadUserDataREST(wantToBeFriensUsers[i].Id, (user) =>
        //    {
        //        userView.GetComponent<UserSearchResultInfoView>().LoadAndUpdateViewData(user, true);
        //        //userView.GetComponent<UserSearchResultInfoView>().LoadAndUpdateViewData(user, true);
        //    }, true);
        //}
    }
    private void ClearSearchPanel()
    {
        searchUserScreen.transform.FindDeepChild("SearchInput").GetComponent<InputField>().text = string.Empty;
        searchUserScreen.transform.FindDeepChild("PleaseWaitPanel").gameObject?.SetActive(false);

        Transform content = searchUserScreen.transform.FindDeepChild("Content");
        foreach (Transform item in content)
        {
            Destroy(item.gameObject);
        }
    }
    public void SearchUserClicked(InputField searchInput)
    {
        if (string.IsNullOrEmpty(searchInput.text))
            return;

        string userName = searchInput.text;

        GameObject pleaseWaitPanel = searchUserScreen.transform.FindDeepChild("PleaseWaitPanel").gameObject;
        pleaseWaitPanel.SetActive(true);

        dataController.FriendshipController.SearchFriend(userName, ShowSearchFriendResult);
        return;
        //dataController.FriendshipController.SearchFriend(userName, (foundUserData)=>
        //{
        //    pleaseWaitPanel.SetActive(false);
        //    if(foundUserData == null || foundUserData.Id == dataController.GetUserId())
        //    {
        //        return;
        //    }

        //    ClearSearchPanel();
            
        //});
    }
    public void ShowSearchFriendResult(List<UserData> foundUsers)
    {
        GameObject pleaseWaitPanel = searchUserScreen.transform.FindDeepChild("PleaseWaitPanel").gameObject;
        pleaseWaitPanel.SetActive(true);

        ClearSearchPanel();
        if (foundUsers == null || foundUsers.Count == 0)
        {
            return;
        }
            
        Transform resultContent = searchUserScreen.transform.FindDeepChild("Content");
        foreach (var user in foundUsers)
        {
            if (user.id == dataController.GetUserData().id)
                continue;


            UserSearchResultInfoView userView = Instantiate(userSearchResultViewPrefab, resultContent).GetComponent<UserSearchResultInfoView>();
            userView.LoadAndUpdateViewData(user, dataController.FriendshipController.IsWishingToBeFriend(user.Id));

            StartCoroutine(dataController.GetSpriteFromURL(user.ProfilePhotoUrl, (sprite) =>
            {
                user.ProfilePhoto = sprite;
                userView.OnProfilePhotoLoaded();
            }));
        }
    }
    public void OnSearchInputValueChanged(InputField input)
    {
        
    }
    public void OnSearchInputEndEdit(InputField input)
    {
        if (string.IsNullOrEmpty(input.text))
            return;

        SearchUserClicked(input); //edit. can be bug when user typing text, and clicking mouse in another place (not pressing Enter)
        Debug.Log("[debug] OnSearchInputEndEdit. Value - " + input.text);
    }
    #endregion

    #region FRIENDSHIP
    /* FRIENDS PANEL METHODS */
    public void OnNewFriendAdded(UserData friend)
    {
        CreateAndAddFriendView(friendsPanel.FindDeepChild("Content"), friend);
        dataController.FriendshipController.UpdateActiveFriendData(friend);

        //Testing
        //RemoveFromFriendshipWishers(friend.Id);
        UpdateFriendshipRequestsCountText();

        //old
        //Transform friendsContent = friendsPanel.FindDeepChild("Content");
        //CreateAndAddFriendView(friendsContent, friend);

        //old delete
        //GameObject friendView = Instantiate(friendViewPrefab, friendsContent);
        //if (friend.ProgressData != null)
        //    friendView.GetComponent<FriendView>().LoadAndUpdateViewData(friend);
        //else //download user data

        //    dataController.DownloadUserDataREST(friend.Id, (data) =>
        //    {
        //        friendView.GetComponent<FriendView>().LoadAndUpdateViewData(data);

        //        //update friend data in dataController
        //        dataController.FriendshipController.OnNewFriendship(data);
        //        //dataController.FriendshipController.UpdateFriendData(data);


        //        //friend = data; //save in dataController (not tested. will be saved?) not saves
        //    });

        //dataController.FriendshipController.RemoveFromWishingById(friend.Id);
        //dataController.NotificationsController.RemoveFriendInvitationNotification(friend.Id); //bad code?
    }
    public void OnFriendshipDeclined(UserData declinedFriend)
    {
        //old code
        //dataController.FriendshipController.OnWishingToBeFriendRejected(declinedFriend.Id);

        //dataController.NotificationsController.RemoveFriendInvitationNotification(declinedFriend.Id);

        UpdateFriendshipRequestsCountText();
        if (searchUserScreen.activeInHierarchy)
            UpdateSearchFriendScreen();
    }
    //private void InitFriendsContent()
    //{
    //    dataController.FriendshipController.GetActiveFriendsCount((count) =>
    //    {
    //        if (count == -1) 
    //            return;

    //        Transform friendsContent = friendsPanel.FindDeepChild("Content");
    //        ClearFriendsContent(friendsContent);

    //        for (int i = 0; i < count; i++)
    //        {
    //            FriendView friendView = Instantiate(friendViewPrefab, friendsContent) as FriendView;
    //            if (friendView.ProgressData != null)
    //                friendView.GetComponent<FriendView>().LoadAndUpdateViewData(friend);
    //            else //download user data
    //                dataController.DownloadUserDataREST(friend.Id, (data) =>
    //                {
    //                    friendView.GetComponent<FriendView>().LoadAndUpdateViewData(data);

    //                    //update friend data in dataController
    //                    dataController.FriendshipController.UpdateFriendData(data);
    //                    //friend = data; //save in dataController (not tested. will be saved?) not saves
    //                });
    //        }
    //    });
    //}
    private void UpdateFriendsContent()
    {
        Transform friendsContent = friendsPanel.FindDeepChild("Content");
        ClearFriendsContent(friendsContent);

        List<UserData> friends = dataController.FriendshipController.GetActiveFriends();
        foreach (var fData in friends)
        {
            CreateAndAddFriendView(friendsContent, fData);
            //old code
            //if (fData.ProgressData != null)
            //    friendView.GetComponent<FriendView>().LoadAndUpdateViewData(fData);
            //else //download user data
            //    dataController.DownloadUserDataREST(fData.Id, (data) =>
            //    {
            //        friendView.GetComponent<FriendView>().LoadAndUpdateViewData(data);
                     
            //        //update friend data in dataController
            //        dataController.FriendshipController.UpdateFriendData(data);
            //        //friend = data; //save in dataController (not tested. will be saved?) not saves
            //    });
        }

        //old test. danger. Так как данные о друзьях нужны по игре, все-таки гружу их по умолчанию
        //dataController.FriendshipController.UpdateActiveFriendsData();

        //for (int i = 0; i < friends.Count; i++)
        //{
        //    GameObject friendView = Instantiate(friendViewPrefab, friendsContent);
        //    if(friends[i].ProgressData != null)
        //        friendView.GetComponent<FriendView>().LoadAndUpdateViewData(friends[i]);
        //    else //download user data
        //        dataController.DownloadUserDataREST(friends[i].Id, (data) =>
        //        {
        //            friendView.GetComponent<FriendView>().LoadAndUpdateViewData(data);
        //            friends[i] = data; //save in dataController (not tested. will be saved?)
        //        });
        //}
    }
    private void CreateAndAddFriendView(Transform content, UserData fData)
    {
        FriendView friendView = Instantiate(friendViewPrefab, content).GetComponent<FriendView>();
        friendView.LoadAndUpdateViewData(fData);
        friendView.transform.name = fData.Id;
    }
    private void UpdateFriendView(string id)
    {
        if (lobbiesPanel.activeInHierarchy)
            return;

        if (dataController.FriendshipController.IsWishingToBeFriend(id))
        {
            UserSearchResultInfoView view = friendshipWishersPanel.FindDeepChild(id).GetComponent<UserSearchResultInfoView>();
            view.UpdateView();
        }
        else if (dataController.FriendshipController.IsActiveFriend(id))
        {
            friendsPanel.FindDeepChild(id).GetComponent<FriendView>().UpdateView();
        }
    }
    private void UpdateFriendsOnlineCounter(string id)
    {
        if (dataController.FriendshipController.IsActiveFriend(id))
        {
            UpdateOnlineFriendsCountText();
        }
    }


    private void UpdateOnlineFriendsCountText()
    {
        Text friendsCountText = friendsPanel.FindDeepChild("FriendsOnline").GetComponent<Text>();
        friendsCountText.text = dataController.FriendshipController.GetOnlineFriendsCount() + " онлайн";
    }
    private void UpdateFriendshipRequestsCountText()
    {
        Text friendshipRequestsText = friendsPanel.FindDeepChild("FriendshipRequestsValue").GetComponent<Text>();
        friendshipRequestsText.text = dataController.FriendshipController.GetFriendshipRequestsCount() + " заявок в друзья";
    }
    private void ClearFriendsContent(Transform content)
    {
        foreach (Transform item in content)
        {
            Destroy(item.gameObject);
        }
    }

    /* Friendship wishers */
    public void OpenWishersScreen()
    {
        UpdateWishersScreen();
        friendshipWishersPanel.gameObject.SetActive(true);

        //Request updating wishers data
        dataController.FriendshipController.UpdateWishingFriendsData();
    }
    private void UpdateWishersScreen()
    {
        //clear panel
        Transform noInvitationsText = friendshipWishersPanel.transform.FindDeepChild("NoInvitationsText");
        Transform content = friendshipWishersPanel.transform.FindDeepChild("Content");
        foreach (Transform item in content)
            Destroy(item.gameObject);

        //Getting wishers
        List<UserData> wishers = dataController.FriendshipController.GetWishingToBeFriends();
        if(wishers == null || wishers.Count == 0)
        {
            noInvitationsText.gameObject.SetActive(true);
            return;
        }
        noInvitationsText.gameObject.SetActive(false);

        //Instantiating empty (or not) wishers view
        foreach (var wisher in wishers)
        {
            UserSearchResultInfoView wisherView = Instantiate(userSearchResultViewPrefab, content).GetComponent<UserSearchResultInfoView>();
            wisherView.LoadAndUpdateViewData(wisher, true);
            wisherView.transform.name = wisher.Id;
        }
    }
    private void OnNewWishingAdded(UserData newWishing)
    {
        UpdateFriendshipRequestsCountText();

        //tested. Ok
        //Do nothing, cause wishing screen is not open
        if (!friendshipWishersPanel.gameObject.activeInHierarchy)
            return;

        //tested. Ok
        //Else clear or initialize screen
        if (friendshipWishersPanel.transform.FindDeepChild("Content").childCount == 0)
        {
            UpdateWishersScreen();
        }
        else
        {
            //testing
            //Else create add new wishing view to content
            Transform content = friendshipWishersPanel.transform.FindDeepChild("Content");
            UserSearchResultInfoView wisherView = Instantiate(userSearchResultViewPrefab, content).GetComponent<UserSearchResultInfoView>();
            wisherView.LoadAndUpdateViewData(newWishing, true);
            wisherView.transform.name = newWishing.Id;
        }

        dataController.FriendshipController.UpdateWishingFriendsData();
    }
    //refactor. Может удалить метод и оставить только UpdateFriendshipRequestCountText()? Временно оставляю на случай изменений
    private void RemoveFromFriendshipWishers(string wisherId)
    {
        UpdateFriendshipRequestsCountText();

        
        //refactor? Вызывается с двух мест, может ограничиться только через Messenger Broadcast?
        //if (friendshipWishersPanel.gameObject.activeInHierarchy)
        //{
        //    Transform wisherViewTransform = friendshipWishersPanel.transform.FindDeepChild(wisherId);
        //    if(wisherViewTransform != null) 
        //        Destroy(wisherViewTransform.gameObject);
        //}
    }
    #endregion

    #region SETTINGS METHODS
    public void OpenSettings()
    {
        UserData currentUser = dataController.GetUserData();

        UserProfileInfoView userProfileInfoView = settingsPanel.transform.FindDeepChild("UserProfileInfoView").GetComponent<UserProfileInfoView>();
        userProfileInfoView.LoadInfo(currentUser.ProfilePhoto, currentUser.Name, currentUser.Surname, 0);
    }
    #endregion

    #region LEADERBOARD METHODS
    public void OpenLeaderboard()
    {
        //delete. old code
        //dataController.LoadLeaderboardDataLocal();

        UpdateLeaderboardView();
        leaderboardScreen.SetActive(true);
    }
    private void UpdateLeaderboardView()
    {
        ClearLeaders();

        leaderboardScreen.transform.FindDeepChild("LoadingText").gameObject.SetActive(true);
        dataController.DownloadTop10((leaderboard) =>
        {
            leaderboardScreen.transform.FindDeepChild("LoadingText").gameObject.SetActive(false);
            for (int i = 0; i < leaderboard.AllUsers.Length; i++)
            {
                Debug.Log("i = " + i + "Instatiated");
                GameObject userProgressView = Instantiate(userProgressViewPrefab, leaderboardContent.transform);
                userProgressView.GetComponent<UserLeaderboardView>().loadViewData(i + 1, leaderboard.AllUsers[i]);
            }
        });

        //not working correct
        //UserData[] leaders = dataController.SortUsersByPoints();
        //for (int i = 0; i < leaders.Length; i++)
        //{

        //    Debug.Log("i = "+ i+"Instatiated");
        //    GameObject userProgressView = Instantiate(userProgressViewPrefab, leaderboardContent.transform);
        //    userProgressView.GetComponent<UserLeaderboardView>().loadViewData(i + 1, leaders[i]);
        //    //userProgressView.GetComponent<UserLeaderboardView>().updateView();
        //}
    }
    private void ClearLeaders()
    {
        foreach (Transform leaderItem in leaderboardContent.transform)
        {
            Destroy(leaderItem.gameObject);
        }
    }
    #endregion

    #region UI HANDLERS
    public void OnPlayWithPseudoRandom()
    {
        SceneManager.LoadScene("SessionScreen");
    }
    public void OnPlayWithFriend()
    {
        SceneManager.LoadScene("FriendsSessionScreen");
    }
    public void OnEndedLobbiesClicked()
    {
        dataController.LobbiesController.UpdateSessionsDataWithType("ended");
    }
    public void OnFindOpponentClicked()
    {
        //refactor? Поместил сюда, так как до активации обновление игнорируется
        lobbiesPanel.SetActive(true);
        lobbieViewController.UpdateLobbiesInfo();
    }
    public void OnLogOutClicked()
    {
        //dataController.DestroySelf();


        //HttpCookie.RemoveCookie("idToken");
        //HttpCookie.RemoveCookie("userId");
        //old
        //PlayerPrefs.DeleteAll();

        dataController.OnLoggedOut();
        SceneManager.LoadScene("Auth");
    }
    #endregion
    public void UpdateUserProgress() //?
    {

    }

    /* GAME INFO VALUE CHANGED HANDLERS */
    public void AddMainMenuHandlers() //?
    {

    }

    /* UPDATE VIEW METHODS */
    private void UpdateDashboardView()
    {
        //load online players info
        int onlinePlayers = (int) Mathf.Clamp(dataController.GetOnlinePlayersCount(), 0, float.MaxValue); //exclusive user
        onlinePlayersText.text = onlinePlayers.ToString();

        //load leaderboard position data
        int leaderboardPosition = dataController.GetLeaderboardPosition();
        raitingValueText.text = leaderboardPosition.ToString();

        StatisticsData statistics = dataController.GetUserStatisticsData();
        //load game stat info
        gamesPlayedText.text = statistics.GamesPlayed.ToString();
        
        //load score data
        UserProgressData userProgress = dataController.GetUserProgressData();
        if (userProgress != null)
        {
            pointsValueText.text = userProgress.Points.ToString();
        }
        else
        {
            Debug.Log("User progress is null");
        }
    }
    private void UpdateFriendsView()
    {
        UpdateFriendsContent();

        UpdateFriendshipRequestsCountText();
        UpdateOnlineFriendsCountText();
        if(friendshipWishersPanel.gameObject.activeInHierarchy)
            UpdateWishersScreen();
    }
    private void ShowLoadingNewSessionPanel()
    {
        sessionLoadingPanel.SetActive(true);
    }
    private void HideLoadingNewSessionPanel()
    {
        sessionLoadingPanel.SetActive(false);
    }

    /* NOTIFICATION */
    NotificationData activeNotification;
    public void ShowNotification(NotificationData notification) 
    {
        //activeNotification = (NotificationData) dataController.NotificationsController.GetLastNotification();

        //refactor. Как сделать, чтобы Messenger отправлял INotification<DateTime> но слушатель получал NotificationData.
        //Смотреть метод Broadcast<T>(string eventType, T arg1)
        //notification = (NotificationData)notification; 
        
        activeNotification =  notification;

        switch (notification.Type)
        {
            case "GameEnd":
                dataController.DownloadUserDataREST(activeNotification.sender, (loadedOpponent) =>
                {
                    notificationsViewController.ShowGameEndNotification(loadedOpponent, notification, null);
                }, true);
                
                break;
            case "FriendInvitation":
                dataController.DownloadUserDataREST(activeNotification.sender, (loadedFriend) =>
                {
                    notificationsViewController.ShowFriendInviteNotification(loadedFriend, notification, OnFriendshipResponseReceived);
                }, true);
                //edit;
                break;
            case "FriendshipStatus":
                dataController.DownloadUserDataREST(activeNotification.sender, (loadedFriend) =>
                {
                    notificationsViewController.ShowFriendshipStatusNotification(loadedFriend, notification, null);
                }, true);
                //dataController.NotificationsController.onNotificationWasShown(activeNotification); //old
                break;
            case "GameInvitation":
                dataController.DownloadUserDataREST(activeNotification.sender, (loadedOpponent) =>
                {
                    notificationsViewController.ShowGameInviteNotification(loadedOpponent, notification, onGameAccepted, onGameDeclined);
                    dataController.UpdateCurrentOpponentData(loadedOpponent);
                }, true);
                //edit
                break;
            case "GameInvitationStatus":
                dataController.DownloadUserDataREST(activeNotification.sender, (loadedOpponent) =>
                {
                    notificationsViewController.ShowGameInvitationStatusNotification(loadedOpponent, notification, null);
                }, true);
                //edit
                break;
            default:
                break;
        }
    }
    private void OnFriendshipResponseReceived(object sender, string friendId, bool isAccepted, Action<bool> onReady = null)
    {
        //Удалил, так как о новом друге теперь оповещает FriendshipController
        //if (isAccepted)
        //{
        //    UserData newFriend = new UserData(friendId);
        //    CreateAndAddFriendView(friendsPanel.FindDeepChild("Content"), newFriend);
        //    dataController.FriendshipController.OnNewFriendship(newFriend);
        //}

        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("userId", dataController.GetUserId());
        @params.Add("wishingId", friendId);
        @params.Add("game", "battleofminds");
        @params.Add("isAccepted", isAccepted);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineFriendInvitation", @params, (data) =>
        {
            if (data.statusCode != 400)
            {
                Debug.Log($"Success calling function AcceptDeclineFriendInvitation");
                onReady?.Invoke(true);

                //OnNewFriendAdded(new UserData(newFriendId));

            }
        }, (exception) =>
        {
            Debug.LogError($"Error while calling AcceptDeclineFriendInvitation. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling AcceptDeclineFriendInvitation data. Message - {exception.Message}");
            //onReady?.Invoke(false);
        });

        //OnAcceptFriendshipClicked(friendId, "battleofminds", null);
        
        
        //dataController.NotificationsController.onNotificationWasShown(activeNotification); //old
    }
    //private void onFriendshipDeclined(string friendId)
    //{
    //    DeclineFriendInvitation(friendId, "battleofminds", null);
    //    dataController.NotificationsController.RemoveNotification(activeNotification);
    //    //dataController.NotificationsController.onNotificationWasShown(activeNotification); //old
    //}
    private void onGameAccepted(string sessionId, UserData inviter)
    {
        ShowLoadingNewSessionPanel();

        dataController.AcceptGameInvitation(sessionId, inviter.Id, "battleofminds", (success) =>
        {
            if (success)
            {
                Debug.Log($"Accepting game success status - {success}");

                dataController.UpdateCurrentOpponentData(inviter);
                SceneManager.LoadScene("Game");
            }
            else
            {
                HideLoadingNewSessionPanel();
                //edit. Show error message
            }
        });

        activeNotification.OnReacted();


        //refactor? Обычно не загружаю через этот метод, но ждать пока сессия появится в списке и только потом начнет грузить, тоже не очень
        //dataController.DownloadSessionDataREST(sessionId, (loaded) =>
        //{
        //    dataController.UpdateCurrentOnlineSession(loaded);
        //    SceneManager.LoadScene("Game");
        //});



        //dataController.NotificationsController.onNotificationWasShown(activeNotification);
        //dataController.NotificationsController.RemoveNotification(activeNotification);

        //test edit (remove if working)
        //dataController.WaitUntilGameInitialized(sessionId, () =>
        //{
        //    OpenLobbie(sessionId);
        //});

        //SceneManager.LoadScene("SessionScreen");
    }
    private void onGameDeclined(string sessionId, UserData inviter)
    {
        dataController.DeclineGameInvitation(sessionId, inviter.Id, "battleofminds", (success) => {Debug.Log("Game decined status = " +success); });
        activeNotification.OnReacted();
        //dataController.NotificationsController.RemoveNotification(activeNotification);
        //dataController.NotificationsController.onNotificationWasShown(activeNotification); //old
    }
    //refactor. Метод аналогичен OnFriendshipResponseReceived. Пока не избавился от данного метода
    public void AcceptFriendInvitation(string newFriendId, string gameName, Action<bool> onReady = null) //bad code
    {
        //dataController.FriendshipController.OnNewFriendship(new UserData(newFriendId));

        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("userId", dataController.GetUserId());
        @params.Add("wishingId", newFriendId);
        @params.Add("game", "battleofminds");
        @params.Add("isAccepted", true);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineFriendInvitation", @params, (data) =>
        {
            if (data.statusCode != 400)
            {
                Debug.Log($"Success calling function AcceptDeclineFriendInvitation");
                Messenger.Broadcast("OnFriendInvitationResponseGet", newFriendId);
                
                onReady?.Invoke(true);
                //OnNewFriendAdded(new UserData(newFriendId));
            }
        }, (exception) =>
        {
            Debug.LogError($"Error while calling AcceptDeclineFriendInvitation. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling AcceptDeclineFriendInvitation data. Message - {exception.Message}");
            onReady?.Invoke(false);
        });

        //StartCoroutine(dataController.CallCloudFunctionGET("AcceptDeclineFriendInvitation",
        //    $"playerId={dataController.GetUserId()}&sender={newFriendId}&game={gameName}&isAccepted=true",
        //    (success) =>
        //    {
        //        UserData newFriend = new UserData(newFriendId);
        //        OnNewFriendAdded(newFriend);
        //        //Messenger.Broadcast<UserData>("OnNewFriend", newFriend); //refactor

        //        onReady?.Invoke(success);
        //    }));
    }
    public void DeclineFriendInvitation(string rejectedFriendId, string gameName, Action<bool> onReady = null)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("userId", dataController.GetUserId());
        @params.Add("wishingId", rejectedFriendId);
        @params.Add("game", "battleofminds");
        @params.Add("isAccepted", false);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineFriendInvitation", @params, (data) =>
        {
            if (data.statusCode != 400)
            {
                Debug.Log($"Success calling function AcceptDeclineFriendInvitation");
                Messenger.Broadcast("OnFriendInvitationResponseGet", rejectedFriendId);
                onReady?.Invoke(true);
                //OnNewFriendAdded(new UserData(newFriendId));
            }
        }, (exception) =>
        {
            Debug.LogError($"Error while calling AcceptDeclineFriendInvitation. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling AcceptDeclineFriendInvitation data. Message - {exception.Message}");
            onReady?.Invoke(false);
        });
    }
    //public void OnAcceptFriendshipClicked(string newFriendId, string gameName, Action<bool> onReady)
    //{



    //    //StartCoroutine(dataController.CallCloudFunctionGET("AcceptDeclineFriendInvitation",
    //    //    $"playerId={dataController.GetUserId()}&sender={newFriendId}&game={gameName}&isAccepted=true",
    //    //    (success) =>
    //    //    {
    //    //        UserData newFriend = new UserData(newFriendId);
    //    //        OnNewFriendAdded(newFriend);
    //    //        //Messenger.Broadcast<UserData>("OnNewFriend", newFriend); //refactor

    //    //        onReady?.Invoke(success);
    //    //    }));
    //}
    //public void DeclineFriendInvitation(string rejectedFriendId, string gameName, Action<bool> onReady)
    //{
    //    Dictionary<string, object> @params = new Dictionary<string, object>();
    //    @params.Add("userId", dataController.GetUserId());
    //    @params.Add("senderId", rejectedFriendId);
    //    @params.Add("game", gameName);
    //    @params.Add("isAccepted", true);

    //    FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineFriendInvitation", @params, (data) =>
    //    {
    //        if (data.statusCode != 400)
    //        {
    //            Debug.Log($"Success calling function AcceptDeclineFriendInvitation");
    //            OnNewFriendAdded(new UserData(rejectedFriendId));
    //            onReady?.Invoke(true);
    //        }
    //    }, (exception) =>
    //    {
    //        Debug.LogError($"Error while calling AcceptDeclineFriendInvitation");
    //        GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while calling AcceptDeclineFriendInvitation data. Message - {exception.Message}");
    //        onReady?.Invoke(false);
    //    });

    //    //StartCoroutine(dataController.CallCloudFunctionGET("AcceptDeclineFriendInvitation",
    //    //    $"playerId={dataController.GetUserId()}&sender={rejectedFriendId}&game={gameName}&isAccepted=false",
    //    //    (success) =>
    //    //    {
    //    //        UserData rejectedFriend = new UserData(rejectedFriendId);
    //    //        OnFriendshipDeclined(rejectedFriend);
    //    //        //Messenger.Broadcast<string>("OnFriendshipDeclined", rejectedFriendId);


    //    //        onReady?.Invoke(success);
    //    //    }));
    //}
}
