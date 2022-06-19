using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.ServerSentEvents;
using System;
using SimpleJSON;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbieViewController : MonoBehaviour
{
    DataController dataController;

    public Transform activeLobbies;
    public Transform expectedLobbies;
    public Transform endedLobbies;

    public GameObject lobbieViewPrefab;
    public GameObject lobbieWindow;

    private List<LobbieView> allLobbiesViews;

    private void Start()
    {
        dataController = FindObjectOfType<DataController>();

        allLobbiesViews = new List<LobbieView>();

        endedLobbies.gameObject.SetActive(false);

        Messenger.MarkAsPermanent(LobbiesNotifications.OnAllSessionsListsDataLoaded); //без этой строчки, Messenger вызывает Cleanup метод после загрузки сцены и удаляет все слушатели
        Messenger.AddListener(LobbiesNotifications.OnAllSessionsListsDataLoaded, InitLobbiesViews);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnSessionDataLoaded);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnSessionDataLoaded, UpdateLobbieView);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnNewActiveSession);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnNewActiveSession, OnNewActiveSessionAdded);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnRemovedFromExpected); //пока убрал, так как при отказе или принятии приглашения отображается статус
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnRemovedFromExpected, OnSessionInvitationRejected);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnRemovedFromActive); 
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnRemovedFromActive, OnActiveSessionEnded);
        Messenger.MarkAsPermanent(LobbiesNotifications.OnNewSessionInvitation);
        Messenger.AddListener<SessionData>(LobbiesNotifications.OnNewSessionInvitation, OnNewSessionInvitation);
    }
    private void OnDestroy()
    {
        Messenger.RemoveListener(LobbiesNotifications.OnAllSessionsListsDataLoaded, InitLobbiesViews);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnSessionDataLoaded, UpdateLobbieView);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnNewActiveSession, OnNewActiveSessionAdded);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnRemovedFromActive, OnActiveSessionEnded);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnRemovedFromExpected, OnSessionInvitationRejected);
        Messenger.RemoveListener<SessionData>(LobbiesNotifications.OnNewSessionInvitation, OnNewSessionInvitation);
    }

    /* LOBBIES METHODS */
    //private void InitLobbiesPanels()
    //{
    //    ClearLobbiesPanelContent(activeLobbies.transform.FindDeepChild("Content"));
    //    ClearLobbiesPanelContent(expectedLobbies.transform.FindDeepChild("Content"));
    //    ClearLobbiesPanelContent(endedLobbies.transform.FindDeepChild("Content"));
    //}
    public void UpdateLobbiesInfo()
    {
        foreach (var lobbieView in allLobbiesViews)
        {
            lobbieView.UpdateView();
        }
    }

    public void InitLobbiesViews()
    {
        ClearSessionsContent();
        allLobbiesViews.Clear();

        List<SessionData> allSessions = dataController.LobbiesController.GetAllSessionsList();
        foreach (var sData in allSessions)
        {
            LobbieView lobbieView = CreateLobbieView(sData);
            AddLobbieToLobbiePanel(lobbieView);

            allLobbiesViews.Add(lobbieView);
        }

        if (dataController.LobbiesController.Loaded) //в случае если мы пригласили в игру и затем вышли на главный экран
        {
            dataController.LobbiesController.UpdateEmptySessionWithType("active");
            dataController.LobbiesController.UpdateEmptySessionWithType("expected");
        }
            

        //Решил, что грузим сразу
        //dataController.LobbiesController.UpdateSessionsDataByType("active");
        //dataController.LobbiesController.UpdateSessionsDataByType("expected");
    }
    
    private void UpdateLobbieView(SessionData session)
    {
        LobbieView view = allLobbiesViews.Find(lobbie => lobbie.SessionData.Id == session.Id);
        if (view != null)
            view.UpdateView();

        //Возможно это не нужно
        //if (dataController.LobbiesController.IsActiveSession(sessionId))
        //{
        //    activeLobbies.FindDeepChild(sessionId).GetComponent<LobbieView>().UpdateView();
        //}
        //else if (dataController.FriendshipController.IsActiveFriend(id))
        //{
        //    friendsPanel.FindDeepChild(id).GetComponent<FriendView>().UpdateView();
        //}
    }
    //refactor. Оставь только один метод "OnNewSession"
    private void OnNewActiveSessionAdded(SessionData newSession)
    {
        LobbieView view = allLobbiesViews.Find(lobbie => lobbie.SessionData.Id == newSession.Id);
        if (view != null)
        {
            allLobbiesViews.Remove(view);
            Destroy(view.gameObject);
        }
            
        view = CreateLobbieView(newSession);
        AddLobbieToLobbiePanel(view);
        allLobbiesViews.Add(view);

        dataController.LobbiesController.UpdateSessionDataById(newSession.Id); //refactor? bad code?
    }
    //refactor. Duplicated code
    private void OnActiveSessionEnded(SessionData ended)
    {
        LobbieView view = allLobbiesViews.Find(lobbie => lobbie.SessionData.Id == ended.Id);
        if (view != null)
        {
            allLobbiesViews.Remove(view);
            Destroy(view.gameObject);
        }

        view = CreateLobbieView(ended);
        AddLobbieToLobbiePanel(view);
        allLobbiesViews.Add(view);

        dataController.LobbiesController.UpdateSessionDataById(ended.Id); //refactor? bad code?
    }
    //refactor. Оставь только один метод "OnNewSession"
    private void OnNewSessionInvitation(SessionData newExpected)
    {
        LobbieView view = allLobbiesViews.Find(lobbie => lobbie.SessionData.Id == newExpected.Id);
        if (view != null)
        {
            allLobbiesViews.Remove(view);
            Destroy(view.gameObject);
        }

        view = CreateLobbieView(newExpected);
        AddLobbieToLobbiePanel(view);
        allLobbiesViews.Add(view);

        dataController.LobbiesController.UpdateSessionDataById(newExpected.Id);//refactor? bad code?
    }
    private void OnSessionInvitationRejected(SessionData rejected)
    {
        LobbieView view = allLobbiesViews.Find(lobbie => lobbie.SessionData.Id == rejected.Id);
        if (view != null)
        {
            allLobbiesViews.Remove(view);
            Destroy(view.gameObject);
        }
    }
    
    private void ClearSessionsContent()
    {
        foreach (var lobbieView in allLobbiesViews)
        {
            Destroy(lobbieView.gameObject);
        }
    }

    private void AddLobbieToLobbiePanel(LobbieView lobbieView)
    {
        Transform parent = GetLobbieViewContentParent(lobbieView.SessionData);
        if (parent == null)
        {
            //danger. Не понял зачем это
            //Destroy(lobbieView.gameObject);//edit. Add expected lobbies to waiting player
            return;
        }

        lobbieView.transform.parent = parent;
        lobbieView.transform.localPosition = lobbieViewPrefab.transform.localPosition; //maybe need not
        lobbieView.transform.localScale = lobbieViewPrefab.transform.localScale;

    }
    private Transform GetLobbieViewContentParent(SessionData session)
    {
        string type = dataController.LobbiesController.GetSessionTypeById(session.Id);
        if (type == "ended")
        {
            return endedLobbies.transform.FindDeepChild("Content");
        }
        else if (type == "active")
        {
            return activeLobbies.transform.FindDeepChild("Content");
            //old
            //if (session.MovingPlayer == dataController.GetUserId())
            //    return activeLobbies.transform.FindDeepChild("Content");
            //else
            //    return expectedLobbies.transform.FindDeepChild("Content");
        }
        else //status is expected. No moving player
        {
            return expectedLobbies.transform.FindDeepChild("Content"); //test
            //edit. add code ?
        }
    }
    
    public void OnLobbieClicked(LobbieView lobbie)
    {
        //session data not loaded
        if (lobbie.SessionData.Status == null)
            return;
        else if (lobbie.SessionData.Status == "expected")
            OnExpectedLobbieClickHandler(lobbie);
        else if (lobbie.SessionData.Status == "active" || lobbie.SessionData.Status == "ended")
            OnActiveLobbieClickHandler(lobbie);

       
        //else //acitve or ended session
        //{
        //    //danger. edit. refactor. Если FriendshipController не загрузит данные об оппоненте до того, как пользователь кликнет по сессии,
        //    //то в текущего оппонента загрузится пустой UserData. Поэтому лучше пока оставлю повторную загрузку
        //    //dataController.UpdateCurrentOnlineSession(session);
        //    //string opponentId = session.GetOpponentId(dataController.GetUserId());
        //    //dataController.UpdateCurrentOpponentData(dataController.FriendshipController.GetActiveFriendById(opponentId));

           

        //    //old
        //    //dataController.DownloadSessionDataREST(session.Id, (loaded) =>
        //    //{
        //    //    dataController.UpdateCurrentOnlineSession(loaded);

        //    //    string opponentId = loaded.Users.FirstOrDefault(u => u.Key != dataController.GetUserId()).Key;

        //    //    dataController.DownloadUserDataREST(opponentId, (loadedOpponent) =>
        //    //    {
        //    //        dataController.UpdateCurrentOpponentData(loadedOpponent);
        //    //        SceneManager.LoadScene("Game");
        //    //    });

        //    //    //dataController.UpdateOpponentData(new UserData(opponentId));
        //    //    //SceneManager.LoadScene("Game");
        //    //});
        //}


    }
    private void OnExpectedLobbieClickHandler(LobbieView lobbie)
    {
        SessionData session = lobbie.SessionData;

        if (session.GameInviter == dataController.GetUserId()) //user пригласил в игру другого и ждет ответа
        {
            ShowPopupMessage("Соперник еще не одобрил игру. Ждем ответа");
            return;
        }
        else if (session.GameInviter != dataController.GetUserId()) ////user получил приглашение в игру от другого
        {
            lobbie.Block(); //to prevent clicking

            UserData opponent = dataController.FriendshipController.GetActiveFriendById(session.GameInviter);
            ShowAskPopupMessage($"{opponent.FullName} пригласил вас в игру. Принять?",
                () =>
                {
                    HideAskPopupMessage();
                    GameObject.Find("Canvas").transform.Find("SessionLoadingPanel").gameObject.SetActive(true);

                    dataController.AcceptGameInvitation(session.Id, session.GameInviter, "battleofminds", (success) =>
                    {
                        if (!success)
                        {
                            Debug.LogError("Error while accepting friendly game");
                            ShowPopupMessage("Извините, произошла ошибка");
                            GameObject.Find("Canvas").transform.Find("SessionLoadingPanel").gameObject.SetActive(false);
                            lobbie.Unblock();
                            return;
                        }
                        else
                        {
                            dataController.UpdateCurrentOpponentData(opponent);
                            Messenger.Broadcast(NotificationEvents.OnGameInvitationResponseGet, session.GameInviter);
                            SceneManager.LoadScene("Game");
                        }
                    });


                }, () =>
                {
                    HideAskPopupMessage();

                    dataController.DeclineGameInvitation(session.Id, session.GameInviter, "battleofminds", (success) =>
                    {
                        if (!success)
                        {
                            Debug.LogError("Error while decline friendly game");
                            ShowPopupMessage("Извините, произошла ошибка");
                            lobbie.Unblock();
                            return;
                        }
                        else
                        {
                            Messenger.Broadcast(NotificationEvents.OnGameInvitationResponseGet, session.GameInviter);
                            //возможно пригодиться, если слишком долго будет удаляться lobbieView. Так как пока слушатель ополучит обновление...
                            //allLobbiesViews.Remove(lobbie);
                            //Destroy(lobbie.gameObject);
                        }
                    });
                }, ()=>
                {
                    lobbie.Unblock();
                });
        }
    }
    private float acitveLobbieClickedAt;
    private float loadingLobbieMinAnimTime = 2f;
    private void OnActiveLobbieClickHandler(LobbieView lobbie)
    {

        //refactor. bad code. downloading again
        //refactor. bad code
        GameObject.Find("Canvas").transform.FindDeepChild("SessionLoadingPanel").gameObject.SetActive(true);
        //dataController.LobbiesController.UpdateSessionDataById(session.Id)

        dataController.UpdateCurrentOnlineSession(lobbie.SessionData);
        string opponentId = lobbie.SessionData.Users.FirstOrDefault(u => u.Key != dataController.GetUserId()).Key;

        acitveLobbieClickedAt = Time.time;
        dataController.DownloadUserDataREST(opponentId, (loadedOpponent) =>
        {
            dataController.UpdateCurrentOpponentData(loadedOpponent);
            StartSessionWithDelay();
        }, true);
    }
    private void StartSessionWithDelay()
    {
        StartCoroutine(StartSessionWithDelayCoroutine());
    }
    private IEnumerator StartSessionWithDelayCoroutine()
    {
        while (Time.time - acitveLobbieClickedAt < loadingLobbieMinAnimTime)
            yield return null;

        SceneManager.LoadScene("Game");
    }


    private LobbieView CreateLobbieView(SessionData data)
    {
        LobbieView lobbieView = Instantiate(lobbieViewPrefab).GetComponent<LobbieView>();
        lobbieView.LoadAndUpdateViewData(data, OnLobbieClicked);

        return lobbieView;
    }

    
    private void ClearLobbiesPanelContent(Transform content)
    {
        //edit. Remove from lobbies old
        //allLobbies = new List<LobbieView>();

        foreach (Transform lobbieViewTransform in content)
        {
            Destroy(lobbieViewTransform.gameObject);
            allLobbiesViews.Remove(lobbieViewTransform.GetComponent<LobbieView>());
        }
    }

    private void ShowPopupMessage(string message)
    {
        Transform mainCanvas = GameObject.Find("Canvas").transform;
        GameObject popup = Instantiate(Resources.Load("Prefabs/PopupMessage") as GameObject, mainCanvas);
        popup.transform.Find("InfoText").GetComponent<Text>().text = message;

        //popup.transform.parent = GameObject.Find("Canvas").transform;
    }
    private void ShowAskPopupMessage(string message, Action onAccepted, Action onDeclined, Action onClosed = null)
    {
        //Instantiate
        Transform mainCanvas = GameObject.Find("Canvas").transform;
        GameObject popup = Instantiate(Resources.Load("Prefabs/AskPopup") as GameObject, mainCanvas);
        popup.name = "AskPopup";
        popup.transform.Find("InfoText").GetComponent<Text>().text = message;

        //Assign handlers
        Button acceptButton = popup.transform.FindDeepChild("AcceptButton").GetComponent<Button>();
        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(() =>  onAccepted() ); //refactor?

        Button declineButton = popup.transform.FindDeepChild("DeclineButton").GetComponent<Button>();
        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(() => onDeclined()); //refactor?

        if(onClosed != null)
        {
            Button closeButton = popup.transform.FindDeepChild("CloseButton").GetComponent<Button>();
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { popup.gameObject.SetActive(false); onClosed(); }); //refactor?
        }
    }
    private void HideAskPopupMessage()
    {
        GameObject.Find("AskPopup").gameObject.SetActive(false);
    }
}
