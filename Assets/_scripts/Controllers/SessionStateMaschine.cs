using BestHTTP.ServerSentEvents;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

enum SessionState
{
    Start,
    Searching,
    OpponentNotFound,
    WaitingGameCreation,
    WaitingGameRun,
    Exit, //user want to exit from session screen
    End //game created and running
}


public class SessionStateMaschine : MonoBehaviour
{
    //public float opponentSearchingTime = 120f;
    //private float opponentSearchingStartedAt;

    private DataController dataController;
    private SessionViewController sessionViewController;

    private SessionState state = SessionState.Start;

    //Listeners
    EventSource opponentFoundListener;
    EventSource sessionCreatedListener;
    EventSource sessionRunningListener;


    private void Start()
    {
        dataController = FindObjectOfType<DataController>();
        sessionViewController = FindObjectOfType<SessionViewController>();

        //RemoveFromWishingToPlayTemp();

        StateStart();
    }
    //private void Update()
    //{
    //    if (state == SessionState.Searching && Time.time - opponentSearchingStartedAt > opponentSearchingTime)
    //    {
    //        Debug.Log("Searching time expired");
    //        OnOpponentNotFound();
    //    }
    //}

    void StateStart()
    {
        Debug.Log("STATE START ()");
        
        
        if (dataController.GetOpponentData() != null)
        { 
            StateWaitingGameRun();
        }
        else
        {
            StateSearching();
        }
    }

    void StateSearching()
    {
        Debug.Log("STATE SEARCHING ()");
        switch (state)
        {

        }

        
        SetState(SessionState.Searching);

        //opponentSearchingStartedAt = Time.time;

        AddOnOpponentFoundListener(()=>
        {
            dataController.AddUserToWishingToPlayREST(dataController.GetUserId());
            dataController.UpdateUserStatusREST(dataController.GetUserId(), "wishingToPlay");

            //updating view
            sessionViewController.InitSearchingView();
        });

    }

    //edited
    //void StateOpponentNotFound(string generatedGameId)
    //{
    //    Debug.Log("STATE OPPONENT NOT FOUND ()");

    //    switch (state)
    //    {
            
    //    }

    //    opponentFoundListener.Close();
    //    dataController.InitCurrentSession(generatedGameId);

    //    sessionViewController.BlockCancelation();

    //    //dataController.RemoveFromSearchersREST();

    //    SetState(SessionState.OpponentNotFound);

    //    AddOnSessionCreatedListener(null);
    //}

    void StateWaitingGameCreation(string generatedGameId)
    {
        Debug.Log("STATE WAITING GAME CREATION ()");
        
        switch (state)
        {
        }

        opponentFoundListener.Close();
        dataController.InitCurrentSession(generatedGameId);

        sessionViewController.BlockCancelation();

        SetState(SessionState.WaitingGameCreation);

        AddOnSessionCreatedListener(null);
    }

    void StateWaitingGameRun(string gameId = null)
    {
        Debug.Log("STATE WAITING GAME RUN ()");

        switch (state)
        {
            case SessionState.Searching:
                opponentFoundListener.Close();
                dataController.InitCurrentSession(gameId);
                break;
            case SessionState.Start:
                break;
            case SessionState.WaitingGameCreation:
                sessionCreatedListener.Close();
                break;
        }

        SetState(SessionState.WaitingGameRun);

        AddOnSessionRunningListener(()=>
        {
            dataController.DownloadAndUpdateCurrentSessionDataREST((hasError) =>
            {
                if (hasError)
                {
                    Debug.LogError("Error while downloading and updating session data");
                    return;
                }

                dataController.DownloadAndUpdateOpponentData((error) =>
                {
                    if (error)
                    {
                        Debug.LogError("Error while downloading and updating session data");
                        return;
                    }

                    //updating view
                    sessionViewController.InitGameWithOpponentView();
                    sessionViewController.UpdateOpponentFoundView(dataController.GetUserData(), dataController.GetOpponentData());
                    Debug.Log("Opponent found view initialized");
                });
            });
        });

        
    }

    void StateExit()
    {
        Debug.Log("STATE EXIT ()");
        
        switch (state)
        {

        }

        opponentFoundListener.Close();
        dataController.RemoveFromWishingToPlayREST();

        SetState(SessionState.Exit);

        SceneManager.LoadScene("MenuScreen");
    }

    void StateEnd()
    {
        Debug.Log("STATE END ()");
        
        switch (state)
        {

        }

        sessionRunningListener.Close();

        SetState(SessionState.End);


        //bad code. downloading again. refactor
        dataController.DownloadAndUpdateCurrentSessionDataREST((hasError) =>
        {
            if (hasError)
            {
                Debug.LogError("Error while getting opponent's data");
                return;
            }

            sessionViewController.ShowPreparationForTheStart(() => { SceneManager.LoadScene("Game"); });
        });

    }

    void SetState(SessionState value)
    {
        switch (state)
        {
            case SessionState.Start:
                // state Animating exit logic
                break;
                // other states
        }
        state = value;

    }

    public void OnCanceled()
    {
        switch (state)
        {
            case SessionState.Searching:
                StateExit();
                break;
        }
    }

    //убрал, так как нет вызовов извне
    //public void OnOpponentFound()
    //{
    //    switch (state)
    //    {
    //        case SessionState.Searching:
    //            StateWaitingGameCreation();
    //            break;
    //    }
    //}

    //public void OnOpponentNotFound()
    //{
    //    switch (state)
    //    {
    //        case SessionState.Searching:
    //            StateOpponentNotFound();
    //            break;
    //    }
    //}

    public void OnExit()
    {
        switch (state)
        {
            case SessionState.OpponentNotFound:
                StateExit();
                break;
        }
    }

    public void OnSearchAgain()
    {
        switch (state)
        {
            case SessionState.OpponentNotFound:
                StateSearching();
                break;
        }
    }

    

    ////////////////////////////
    private void AddOnOpponentFoundListener(Action onListenerOpened)
    {
        string url = $"{dataController.DatabaseUrl}/users/wishingToPlay/{dataController.GetUserId()}.json?auth={dataController.IdToken}";
        Debug.Log("Listening wishingToPlay node at path - " + url);

        opponentFoundListener = new EventSource(new Uri(url), 1);

#if UNITY_WEBGL && !UNITY_EDITOR
        opponentFoundListener.On("put", OnOpponentFoundMessage);
#elif UNITY_EDITOR || UNITY_STANDALONE
        opponentFoundListener.OnMessage += OnOpponentFoundMessage;
#endif

        opponentFoundListener.OnError += (eventSource, message) =>
        {
            Debug.LogError($"Error while listening searching node. Message - {message}");
        };

        opponentFoundListener.OnOpen += (eventSource) => { Debug.Log($"Listener {eventSource.ConnectionKey} opened!"); onListenerOpened?.Invoke(); };

        opponentFoundListener.Open();
    }
    private void OnOpponentFoundMessage(EventSource eventSource, Message message)
    {
        Debug.Log("[debig] On new message. Data - " + message.Data);

        if (message.Data == null || message.Data == "null")
            return;

        JSONNode responseObj = JSONNode.Parse(message.Data);
        if (responseObj == null || responseObj == "null" ||
            responseObj["data"] == null || responseObj["data"] == "null")
            return;

        string status = responseObj["data"]["status"].Value;
        string gameId = responseObj["data"]["gameId"].Value;
        Debug.Log("Status updated. Status = " + gameId);
        Debug.Log("GameId updated. Id = " + gameId);

        if (status == "waiting")
            return;
        else if (status == "playing")
            StateWaitingGameCreation(gameId);
        else if (status == "preparingToPlay")
            StateWaitingGameRun(gameId);
    }
    private void AddOnSessionCreatedListener(Action onListenerOpened)
    {
        string url = $"{dataController.DatabaseUrl}/games/{dataController.GetCurrentSessionData().Id}/status.json?auth={dataController.IdToken}";
        Debug.Log("Starting listening node at path - " + url);

        sessionCreatedListener = new EventSource(new Uri(url), 1);
#if UNITY_WEBGL && !UNITY_EDITOR
        sessionCreatedListener.On("put", OnSessionCreatedMessage);
#elif UNITY_EDITOR || UNITY_STANDALONE
        sessionCreatedListener.OnMessage += OnSessionCreatedMessage;
#endif

        sessionCreatedListener.OnError += (eventSource, message) =>
        {
            Debug.LogError($"Error while listening session status node(checking is created). Message - {message}");
        };

        sessionCreatedListener.OnOpen += (eventSource) => { Debug.Log($"Listener {eventSource.ConnectionKey} opened!"); onListenerOpened?.Invoke(); };

        sessionCreatedListener.Open();

        Debug.Log("[debug] Added on session created listener");
    }
    private void OnSessionCreatedMessage(EventSource eventSource, Message message)
    {
        Debug.Log("[debug] On new message. Data - " + message.Data);

        if (message.Data == null || message.Data == "null")
            return;

        string sessionStatus = JSONNode.Parse(message.Data)["data"].Value;

        if (sessionStatus == null || sessionStatus == "null")
            return;

        Debug.Log("OnSessionCreated(). sessionStatus = " + sessionStatus);
        if (sessionStatus == "active")
        {
            StateWaitingGameRun();
        }
    }
    private void AddOnSessionRunningListener(Action onListenerOpened)
    {
        string url = $"{dataController.DatabaseUrl}/games/{dataController.GetCurrentSessionData().Id}/movingPlayer.json?auth={dataController.IdToken}";
        Debug.Log("Starting listening node at path - " + url);

        sessionRunningListener = new EventSource(new Uri(url), 1);
#if UNITY_WEBGL && !UNITY_EDITOR
        sessionRunningListener.On("put", OnSessionRunningMessage);
#elif UNITY_EDITOR || UNITY_STANDALONE
        sessionRunningListener.OnMessage += OnSessionRunningMessage;
#endif

        sessionRunningListener.OnOpen += (eventSource) => { Debug.Log($"Listener {eventSource.ConnectionKey} opened!"); onListenerOpened?.Invoke(); };

        sessionRunningListener.Open();

        Debug.Log("[debug] Added on session running listener");
    }
    private void OnSessionRunningMessage(EventSource eventSource, Message message)
    {
        Debug.Log("[debig] On new message. Data - " + message.Data);

        if (message.Data == null || message.Data == "null")
            return;

        string movingPlayer = JSONNode.Parse(message.Data)["data"].Value;

        if (movingPlayer == null || movingPlayer == "null")
            return;

        Debug.Log("OnSessionRunning(). SessionStatus = " + movingPlayer);

        if (movingPlayer == dataController.GetUserId())
        {
            StateEnd();
        }
    }
}
