using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendSessionViewController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private FriendSessionController sessionController;
    [SerializeField] private NotificationsViewController notificationsViewController;

    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject searchFriendScreen;
    [SerializeField] private GameObject friendSearchResultViewPrefab;

    [SerializeField] private Text searchText;
    private string _searchText;
    private float delaySearch = 0.2f;

    private InputField searchFriendInput;
    private void Awake()
    {
        //lobbies listeners
        Messenger.MarkAsPermanent(LobbiesNotifications.OnAllSessionsListsDataLoaded);
        Messenger.AddListener(LobbiesNotifications.OnAllSessionsListsDataLoaded, OnSessionsListsLoaded);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnSessionDataLoaded);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnSessionDataLoaded, OnSessionDataLoaded);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnNewActiveSession);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnNewActiveSession, OnNewActiveSession);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnNewSessionInvitation);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnNewSessionInvitation, OnNewSessionInvitation);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnRemovedFromExpected);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnRemovedFromExpected, OnSessionRejected);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnRemovedFromActive);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnRemovedFromActive, OnSessionRejected);

        //friendship listeners
        Messenger.MarkAsPermanent(FriendshipNotifications.OnFriendDataLoaded);
        Messenger.AddListener<string>(FriendshipNotifications.OnFriendDataLoaded, UpdateFriendView);
        Messenger.MarkAsPermanent(FriendshipNotifications.OnNewFriendship);
        Messenger.AddListener<UserData>(FriendshipNotifications.OnNewFriendship, OnNewFriendAdded);

        //empty listeners
        Messenger.MarkAsPermanent(FriendshipNotifications.OnAllFriendsListsLoaded);
        Messenger.AddListener(FriendshipNotifications.OnAllFriendsListsLoaded, Empty);
        Messenger.MarkAsPermanent(FriendshipNotifications.OnRemovedFromWishers);
        Messenger.AddListener<string>(FriendshipNotifications.OnRemovedFromWishers, Empty);
        Messenger.MarkAsPermanent(FriendshipNotifications.OnNewFriendshipInvitation);
        Messenger.AddListener<UserData>(FriendshipNotifications.OnNewFriendshipInvitation, Empty);


        Debug.Log("End Awake");
    }

    // Start is called before the first frame update
    void Start()
    {
        searchFriendInput = searchFriendScreen.transform.FindDeepChild("SearchInput").GetComponent<InputField>();

        _searchText = searchText.text;

        InitView();
        Debug.Log("End Start");
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Return) && searchFriendInput.gameObject.activeInHierarchy)
        {
            Debug.Log("Enter clicked");
            SearchFriendClicked(searchFriendInput);
        }

        //delaySearch -= Time.deltaTime;

        if(_searchText != searchText.text)
        {
            _searchText = searchText.text;
            SearchFriendClicked(_searchText);
        }
    }
    private void OnDestroy()
    {
        //lobbies listeners
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnSessionDataLoaded, OnSessionDataLoaded);
        Messenger.RemoveListener(LobbiesNotifications.OnAllSessionsListsDataLoaded, OnSessionsListsLoaded);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnNewActiveSession, OnNewActiveSession);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnNewSessionInvitation, OnNewSessionInvitation);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnRemovedFromExpected, OnSessionRejected);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnRemovedFromActive, OnActiveSessionEnded);

        //friendship listeners
        Messenger.RemoveListener<string>(FriendshipNotifications.OnFriendDataLoaded, UpdateFriendView);
        Messenger.RemoveListener<UserData>(FriendshipNotifications.OnNewFriendship, OnNewFriendAdded);

        //empty listeners
        Messenger.RemoveListener(FriendshipNotifications.OnAllFriendsListsLoaded, Empty);
        Messenger.RemoveListener<string>(FriendshipNotifications.OnRemovedFromWishers, Empty);
        Messenger.RemoveListener<UserData>(FriendshipNotifications.OnNewFriendshipInvitation, Empty);
    }
    //private void LateUpdate()
    //{
    //    if (friendViewToUpdating.Count == 0)
    //        return;

    //    foreach (var friendView in friendViewToUpdating)
    //    {
    //        friendView.Key.GetComponent<FriendSearchResultInfoView>().LoadAndUpdateViewData(friendView.Value);
    //    }
    //    friendViewToUpdating.Clear();
    //}

    private void InitView()
    {
        mainScreen.SetActive(true);
        searchFriendScreen.SetActive(false);

        UserData ud = sessionController.GetUserData();
        mainScreen.transform.FindDeepChild("UserInfoPanel").GetComponent<UserProfileInfoView>().LoadInfo(ud.ProfilePhoto, ud.Name, ud.Surname, ud.Statistics.WinCount);
    }

    public void SearchFriendClicked(string search)
    {
        if (search == string.Empty)
            return;

        ClearSearchResult();
        GameObject pleaseWaitPanel = searchFriendScreen.transform.FindDeepChild("PleaseWaitPanel").gameObject;
        pleaseWaitPanel.SetActive(true);

        //UserData found = sessionController.SearchFriend(searchInput.text);

        List<UserData> found = sessionController.SearchFriends(search);
        pleaseWaitPanel.SetActive(false);
        if (found == null)
        {
            Debug.LogWarning($"User {search} was not found!");
            return;
        }

        StartCoroutine(UpdateSearchFriendScreen(found));
    }

    public void SearchFriendClicked(InputField searchInput)
    {
        if (searchInput.text == string.Empty)
            return;

        ClearSearchResult();
        GameObject pleaseWaitPanel = searchFriendScreen.transform.FindDeepChild("PleaseWaitPanel").gameObject;
        pleaseWaitPanel.SetActive(true);

        //UserData found = sessionController.SearchFriend(searchInput.text);

        List<UserData> found = sessionController.SearchFriends(searchInput.text);
        pleaseWaitPanel.SetActive(false);
        if (found == null)
        {
            Debug.LogWarning($"User {searchInput.text} was not found!");
            return;
        }

        StartCoroutine(UpdateSearchFriendScreen(found));

        //Transform resultContent = searchFriendScreen.transform.FindDeepChild("Content");
        //FriendSearchResultInfoView userView = Instantiate(friendSearchResultViewPrefab, resultContent).GetComponent<FriendSearchResultInfoView>();
        //foreach (UserData friend in found)
        //{
        //    userView.LoadAndUpdateViewData(friend);
        //}
    }
    
    /* Lobbies controller broadcast handlers */
    private void OnSessionsListsLoaded()
    {
        Transform resultContent = searchFriendScreen.transform.FindDeepChild("Content");
        foreach (Transform view in resultContent)
        {
            view.GetComponent<FriendSearchResultInfoView>().UpdateView();
        }
    }
    private void OnSessionDataLoaded(SessionData updated)
    {
        //searching friend view with session
        string opponentId = sessionController.GetOpponentIdFromSession(updated);
        Transform opponent = searchFriendScreen.transform.FindDeepChild(opponentId);
        if (opponent != null)
            opponent.GetComponent<FriendSearchResultInfoView>().UpdateView();
        
        //searchFriendScreen.transform.FindDeepChild(updated.Id)?.GetComponent<FriendSearchResultInfoView>().UpdateView();
    }
    private void OnNewActiveSession(SessionData session)
    {
        //if (searchFriendScreen == null)
        //    return;

        //if (searchFriendScreen.activeInHierarchy)
            searchFriendScreen.transform.FindDeepChild(session.Id)?.GetComponent<FriendSearchResultInfoView>().UpdateView();
    }
    //refactor. Duplicate code
    private void OnActiveSessionEnded(SessionData session)
    {
        searchFriendScreen.transform.FindDeepChild(session.Id)?.GetComponent<FriendSearchResultInfoView>().UpdateView();
    }
    private void OnNewSessionInvitation(SessionData session)
    {

        //if (searchFriendScreen == null)
        //    return;

        //if (searchFriendScreen.activeInHierarchy)
        sessionController.UpdateSessionData(session.Id);
            //searchFriendScreen.transform.FindDeepChild(session.Id)?.GetComponent<FriendSearchResultInfoView>().UpdateView();
    }
    private void OnSessionRejected(SessionData session)
    {
        //if (searchFriendScreen == null)
        //    return;

        //if (searchFriendScreen.activeInHierarchy)
        //searching friend view with session
        string opponentId = sessionController.GetOpponentIdFromSession(session);
        Transform opponent = searchFriendScreen.transform.FindDeepChild(opponentId);
        if (opponent != null)
        {
            //Destroy(opponent.gameObject); test
            opponent.GetComponent<FriendSearchResultInfoView>().UpdateView();

        }

        //searchFriendScreen.transform.FindDeepChild(session.Id)?.GetComponent<FriendSearchResultInfoView>().UpdateView();
    }
    

    /* Friendship controller broadcast handlers */
    private void Empty(/*edit?*/) { Debug.Log("Empty handler"); } //Nothing. Messenger "нуждаетс€" в обработчике, и чтобы не вызывало ошибки, добавл€ю их. ’от€ можно просто в классе убрать необходимость обработки
    private void Empty(string empty) { } //Nothing. Messenger "нуждаетс€" в обработчике, и чтобы не вызывало ошибки, добавл€ю их. ’от€ можно просто в классе убрать необходимость обработки
    private void Empty(UserData empty) { } //Nothing. Messenger "нуждаетс€" в обработчике, и чтобы не вызывало ошибки, добавл€ю их. ’от€ можно просто в классе убрать необходимость обработки
    private void UpdateFriendView(string friendId)
    {
        searchFriendScreen.transform.FindDeepChild(friendId)?.GetComponent<FriendSearchResultInfoView>().UpdateView();//not tested
    }
    private void OnNewFriendAdded(UserData friendData)
    {
        Transform resultContent = searchFriendScreen.transform.FindDeepChild("Content");
        FriendSearchResultInfoView userView = Instantiate(friendSearchResultViewPrefab, resultContent).GetComponent<FriendSearchResultInfoView>();
        userView.LoadAndUpdateViewData(friendData);
        sessionController.UpdateFriendData(friendData);
        //sessionController.UpdateSessionDataWithFriend(friendData); //ѕока не знаю, как обновл€ть данные о сессии таким способом
    }

    /* Notifications handlers */
    public void ShowGameEndNotification(UserData opponentData, NotificationData notification, Action onSubmitted) =>
        notificationsViewController.ShowGameEndNotification(opponentData, notification, onSubmitted);
    public void ShowGameInvititationNotification(UserData opponentData, NotificationData notification, Action<string, UserData> onAccept, Action<string, UserData> onDecline) =>
        notificationsViewController.ShowGameInviteNotification(opponentData, notification, onAccept, onDecline);
    public void ShowFriendInviteNotification(UserData friend, NotificationData notification, Action<object, string, bool, Action<bool>> onResponseGet) =>
        notificationsViewController.ShowFriendInviteNotification(friend, notification, onResponseGet);
    public void ShowFriendshipStatusNotification(UserData friend, NotificationData notification, Action onSubmitted = null) =>
        notificationsViewController.ShowFriendshipStatusNotification(friend, notification, onSubmitted);
    public void ShowGameInvitationStatusNotification(UserData friend, NotificationData notification, Action onSubmitted = null) =>
        notificationsViewController.ShowGameInvitationStatusNotification(friend, notification, onSubmitted);

    /* BUTTONS HANDLERS */
    public void HomeButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScreen");
    }
    public void InviteFriendClicked()
    {
        OpenSearchInviteScreen();
    }

    /* UI HANDLERS */
    public void OnSearchInputValueChanged(InputField input)
    {
        Debug.Log("OnSearchInputValueChanged. Value - " + input.text);
        if (input.text == string.Empty)
            StartCoroutine(UpdateSearchFriendScreen());
    }
    public void OnSearchInputEndEdit(InputField input)
    {
        SearchFriendClicked(input); //edit. can be bug when user typing text, and clicking mouse in another place (not pressing Enter)
        Debug.Log("OnSearchInputEndEdit. Value - " + input.text);
    }

    /* SEARCH FRIEND METHODS */
    public void OpenSearchInviteScreen()
    {
        searchFriendScreen.SetActive(true);
        StartCoroutine(UpdateSearchFriendScreen());
    }
    private IEnumerator UpdateSearchFriendScreen()
    {
        ShowPleaseWaitText("»дет загрузка");
        while (!sessionController.FriendsDataWasLoaded) yield return null;
        HidePleaseWaitText();

        ClearSearchResult();

        Transform resultContent = searchFriendScreen.transform.FindDeepChild("Content");

        List<UserData> userFriends = sessionController.GetFriendsList();
        for (int i = 0; i < userFriends.Count; i++)
        {
            Debug.Log("SearchResultView i = " + i + "Instatiated");
            GameObject friendView = Instantiate(friendSearchResultViewPrefab, resultContent);
            friendView.GetComponent<FriendSearchResultInfoView>().LoadAndUpdateViewData(userFriends[i]);
            friendView.name = userFriends[i].Id;
        }

        //нет необходимости, так как они груз€тс€ в menuscreen
        //sessionController.UpdateFriendsData();
    }

    private IEnumerator UpdateSearchFriendScreen(List<UserData> foundFriends)
    {
        ShowPleaseWaitText("»дет загрузка");
        while (!sessionController.FriendsDataWasLoaded) yield return null;
        HidePleaseWaitText();

        ClearSearchResult();

        Transform resultContent = searchFriendScreen.transform.FindDeepChild("Content");

        foreach (UserData friends in foundFriends)
        {
            GameObject friendView = Instantiate(friendSearchResultViewPrefab, resultContent);
            friendView.GetComponent<FriendSearchResultInfoView>().LoadAndUpdateViewData(friends);
            friendView.name = friends.Id;
        }
    }

    private void ClearSearchResult()
    {
        //searchFriendScreen.transform.FindDeepChild("SearchInput").GetComponent<InputField>().text = string.Empty;

        Transform content = searchFriendScreen.transform.FindDeepChild("Content");
        foreach (Transform item in content)
        {
            Destroy(item.gameObject);
        }
    }
    private void ShowPleaseWaitText(string message)
    {
        GameObject pleaseWaitPanel = searchFriendScreen.transform.FindDeepChild("PleaseWaitPanel").gameObject;
        pleaseWaitPanel.SetActive(true);

        pleaseWaitPanel.GetComponentInChildren<LoadingText>().textBase = message;
    }
    private void HidePleaseWaitText()
    {
        GameObject pleaseWaitPanel = searchFriendScreen.transform.FindDeepChild("PleaseWaitPanel").gameObject;
        pleaseWaitPanel.SetActive(false);
    }
}
