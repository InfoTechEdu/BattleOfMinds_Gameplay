using BestHTTP.ServerSentEvents;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LobbiesNotifications
{
    public const string OnSessionDataLoaded = "OnSessionDataLoaded";
    public const string OnAllSessionsListsDataLoaded = "OnAllSessionsDataLoaded";
    public const string OnNewActiveSession = "OnNewActiveSession";

    public const string OnNewSessionInvitation = "OnNewSessionInvitation";
    public const string OnRemovedFromExpected = "OnRemovedFromExpected";
    public const string OnRemovedFromActive = "OnRemovedFromActive";
}

public class LobbiesController : MonoBehaviour
{
    private DataController dataController;

    //first - type (active, expected, ended), second - list of sessions data of that type
    private Dictionary<string, List<SessionData>> sessionsDictionary;

    private List<EventSource> activeAndExpectedListeners;
    private EventSource sessionsListener;

    private bool preloaded = false;
    private bool loaded = false;
    public bool Preloaded { get => preloaded; }
    public bool Loaded { get => loaded; }
    private void Start()
    {
        dataController = FindObjectOfType<DataController>();

        sessionsDictionary = new Dictionary<string, List<SessionData>>()
        {
            { "active", new List<SessionData>() },
            { "expected", new List<SessionData>() },
            { "ended", new List<SessionData>() },
        };

        activeAndExpectedListeners = new List<EventSource>();

        Debug.Log(dataController.GetUserId());

        sessionsListener = FirebaseManager.Instance.Database.ListenForChildChanged($"users/{dataController.GetUserId()}/private/sessions", SessionsUpdatesHandler, (exception) =>
        {
            Debug.LogError($"Exception while listening sessions data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading sessions data. Message - {exception.Message}");
        });
    }
    private void OnDestroy()
    {
        foreach (var listener in activeAndExpectedListeners)
            listener.Close();

        sessionsListener.Close();

        FirebaseManager.Instance.Database.StopAllCustomListeners();
    }
    private void OnEnable()
    {
        //problem
        //В общем извне апдейт вызываю
    }

    public void SetDataController(DataController controller)
    {
        dataController = controller;
    }

    //private List<string> sessionUpdatesRequests = new List<string>();
    /// <summary>
    /// Этот метод позволяет сразу запросить обновление данных о сессии, в случае его прихода. Пригождается при обновлении view
    /// </summary>
    /// <param name="sessionId"></param>
    //public void RequestSessionUpdating(string sessionId)
    //{
    //    if (sessionUpdatesRequests.Contains(sessionId))
    //    {
    //        Debug.LogWarning($"Updating session with id {sessionId} requested");
    //        return;
    //    }

    //    StartCoroutine(ExpectAndDownloadSessionData(sessionId, 10f));
    //}
    //private IEnumerator ExpectAndDownloadSessionData(string sessionId, float waitingTime)
    //{
    //    float startedAt = Time.time;

        
    //}

    //Prefer to not use this
    public void UpdateSessionsData()
    {
        if (sessionsListener == null)
        {
            Debug.LogWarning("Sessions list is null. Data download denied");
            return;
        }


        foreach (var kvp in sessionsDictionary)
        {
            List<SessionData> sessionsList = kvp.Value;

            foreach (var session in sessionsList)
            {
                dataController.DownloadSessionDataREST(session.Id, (sessionData)=>
                {
                    SessionData updating = sessionsList.FirstOrDefault(s => s.Id == sessionData.Id);
                    updating.CopyFrom(sessionData);

                    Messenger.Broadcast(LobbiesNotifications.OnSessionDataLoaded, sessionData);
                });
            }
        }

        //old
        //StartCoroutine(dataController.DownloadAndUpdateAllSessionsDataREST(() =>
        //{
        //    onReady.Invoke();
        //}));
    }
    public void UpdateEmptySessionWithType(string type)
    {
        if (sessionsDictionary == null || sessionsDictionary[type] == null)
        {
            Debug.LogWarning($"Sessions dictionary or {type} sessions list is null. Data download denied");
            return;
        }

        foreach (var session in sessionsDictionary[type])
        {
            if(session.Status == null)
                dataController.DownloadSessionDataREST(session.Id, (sessionData) =>
                {
                    SessionData updating = sessionsDictionary[type].FirstOrDefault(s => s.Id == sessionData.Id);
                    updating.CopyFrom(sessionData);

                    Messenger.Broadcast(LobbiesNotifications.OnSessionDataLoaded, sessionData);
                });
        }
    }
    public void UpdateSessionsDataWithType(string type)
    {
        if(sessionsDictionary == null || sessionsDictionary[type] == null)
        {
            Debug.LogWarning($"Sessions dictionary or {type} sessions list is null. Data download denied");
            return;
        }

        if(sessionsDictionary[type].Count == 0)
        {
            loaded = true;
            return;
        }

        int updatedCount = 0;
        int updatingCount = sessionsDictionary[type].Count; //не удаляй. Если добавится еще друг, то activeFriendsList.Count увеличится. А так, мы сохраняем в буффер
        foreach (var session in sessionsDictionary[type])
        {
            dataController.DownloadSessionDataREST(session.Id, (sessionData) =>
            {
                SessionData updating = sessionsDictionary[type].FirstOrDefault(s => s.Id == sessionData.Id);
                updating.CopyFrom(sessionData);

                Messenger.Broadcast(LobbiesNotifications.OnSessionDataLoaded, sessionData);

                updatedCount++;
                if (updatedCount >= updatingCount)
                {
                    Debug.Log("FriendshipController. Data loaded");
                    loaded = true;
                }
            });
        }
    }
    public void UpdateSessionDataById(string id)
    {
        SessionData updating = GetSessionById(id);
        if (updating == null)
        {
            Debug.LogWarning($"Declined updating session by id {id}. Reason - not found");
        }
        dataController.DownloadSessionDataREST(id, (loaded) =>
        {
            updating.CopyFrom(loaded);
            Messenger.Broadcast(LobbiesNotifications.OnSessionDataLoaded, updating);
        });
    }
    public void UpdateSessionData(SessionData sd, Action onUpdated = null)
    {
        dataController.DownloadSessionDataREST(sd.Id, (loaded) =>
        {
            sd.CopyFrom(loaded);
            onUpdated?.Invoke();
        });
    }
    private void OnSessionsListsLoaded(string json)
    {
        if (json == "null" || json == null)
        {
            preloaded = true;
            loaded = true;
            return;
        }
            
        //Debug.Log("Sessions dictionary data was loaded");

        DataParser.ParseSessionsList(json, out sessionsDictionary);

        foreach (var active in sessionsDictionary["active"])
            AddActiveSessionListener(active);
        //foreach (var expected in sessionsDictionary["expected"])
        //    AddSessionListener(expected);

        Debug.LogWarning($"[debug] Sessions data parsed. Active - {Utils.CollectionUtils.DictionaryToString(sessionsDictionary)}" +
            $"Expected - {Utils.CollectionUtils.ListToString(sessionsDictionary["expected"])}");

        preloaded = true;
        Debug.Log("Lobbies controller. Data (dictionary) preloaded ");
        Messenger.Broadcast(LobbiesNotifications.OnAllSessionsListsDataLoaded);

        UpdateSessionsDataWithType("active");
        UpdateSessionsDataWithType("expected");
    }
    private void SessionsUpdatesHandler(EventSource eSource, Message message)
    {
        Debug.LogWarning("SESSIONS UPDATES HANDLER. MESSAGE - " + message.Data);

        //In first call after starting we get PUT event. Then we get the PATCH
        if (message.ToString().Contains("put"))
        {
            Debug.Log("Temp Temp SessionsUpdatesHandler called");
            OnSessionsListsLoaded(JSONNode.Parse(message.Data)["data"].ToString());
            return;
        }

        if (message.Data == null || message.Data == "null")
            return;

        JSONNode messageJsonObj = JSONNode.Parse(message.Data);
        string dataJson = messageJsonObj["data"].ToString();

        if (dataJson == null || dataJson == "null")
            return;

        // parsting json like {"SomeSessionId" : "expected"} and getting data
        // bad code refactor. can not get key
        JSONNode dataJsonObj = JSON.Parse(dataJson);
        string sessionId = "";
        string sessionType = "";
        foreach (var item in dataJsonObj)
        {
            sessionId = item.Key;
            sessionType = item.Value;
        }

        if (sessionType == null || sessionType == "null")
            sessionType = GetSessionTypeById(sessionId);

        //not working
        //dataJsonObj.Keys.MoveNext();

        //string sessionId = words[1];
        //string sessionType = words[0];

        //old?
        //string sessionId = path.Replace("active/", "").Replace("wishing/", ""); //refactor? Убирает либо active/ либо wishing/
        //string sessionType = path.Substring(0, path.IndexOf("/"));

        switch (sessionType)
        {
            case "active":
                if (dataJsonObj[sessionId].ToString() == "null") RemoveSession(sessionType, sessionId); 
                else if (dataJsonObj[sessionId].AsBool == true) OnNewActiveSession(sessionId); 
                break;

            case "expected":
                if (dataJsonObj[sessionId].ToString() == "null") { SessionData removed = RemoveSession(sessionType, sessionId); Messenger.Broadcast<SessionData>(LobbiesNotifications.OnRemovedFromExpected, removed); }
                else if (dataJsonObj[sessionId].AsBool == true) OnNewExpected(sessionId); 
                break;
                 
            case "ended":
                if (dataJsonObj[sessionId].ToString() == "null") { RemoveSession(sessionType, sessionId); }
                else if (dataJsonObj[sessionId].AsBool == true) OnNewEndedSession(sessionId); 
                break;

            default:
                Debug.LogWarning("Unexpected session type. Type - " + sessionType);
                break;
        }
    }
    
    private void AddActiveSessionListener(SessionData session)
    {
        FirebaseManager.Instance.Database.CustomListenForChildChanged($"games/{session.Id}", SessionUpdatesHandler, 7, (exception) =>
        {
            Debug.LogError($"Exception while listening session data with id {session.Id} data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading session data with id {session.Id} data. Message - {exception.Message}");
        });

        /* 
         * Working on Editor
        //var sse = FirebaseManager.Instance.Database.ListenForChildChanged($"games/{session.Id}", SessionUpdatesHandler, (exception) =>
        //{
        //    Debug.LogError($"Exception while listening session data with id {session.Id} data. Message - {exception.Message}");
        //    GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading session data with id {session.Id} data. Message - {exception.Message}");
        //});

        //activeAndExpectedListeners.Add(sse);
        */
    }
    private void RemoveActiveSessionListener(SessionData session)
    {
        FirebaseManager.Instance.Database.StopCustomListener($"games/{session.Id}");
    }
    private void SessionUpdatesHandler(EventSource eSource, Message message)
    {
        //In first call after starting we get PUT event. Then we get the PATCH
        if (message.ToString().Contains("put"))
        {
            return;
        }

        if (message.Data == null || message.Data == "null")
            return;

        JSONNode dataJsonObj = JSONNode.Parse(message.Data);

        //refactor. bad code Downloading again. Edit
        if(dataJsonObj["path"] == "/movingPlayer")
        {
            //edit. how to get session key?
            //
            //
            //
            //
            //
            //UpdateSessionDataById(updatedSessionId);
            
            
            //dataController.DownloadSessionDataREST(updatedSessionId, (loaded) =>
            //{
            //    UpdateSessionsData()
            //});
        }

        //if (dataJsonObj["path"] == "/movingPlayer")
        //{
        //    string currentMovingPlayer = dataJsonObj["data"].Value;


        //    dataController.DownloadSessionDataREST(updatedSessionId, (loaded) =>
        //    {
        //        dataController.UpdateSessionData(loaded);

        //        LobbieView updatingLobbieView = allLobbiesViews.Find(lobbie => lobbie.SessionData.Id == loaded.Id);
        //        updatingLobbieView.UpdateSessionData(loaded);

        //        Transform correctParent = GetLobbieViewContentParent(updatingLobbieView.SessionData);
        //        updatingLobbieView.transform.SetParent(correctParent);
        //    });
        //}
    }
    private void SessionUpdatesHandler(string data)
    {
        DataParser.ParseSessionData(data, out SessionData sd);

        //refactor. bad code. Временный костыль. Удалить, если в данном методе будет единый обработчик для всех типов сессий
        //Вставлено для того, чтобы пока активная сессия не перешла в статус "ended", метод не пытался обработать его как активный (происходят ошибки)
        if (sd != null && sd.Status == "ended")
            return;

        if (sd != null)
        {
            Debug.Log($"Session data with id {sd.Id} was loaded. Data - " + data);

            SessionData updating = sessionsDictionary[sd.Status].FirstOrDefault(s => s.Id == sd.Id);
            updating.CopyFrom(sd);

            Messenger.Broadcast(LobbiesNotifications.OnSessionDataLoaded, sd);
        }
    }

    public SessionData GetActiveSessionById(string id)
    {
        return sessionsDictionary["active"].Find(s => s.Id == id);
    }
    public List<SessionData> GetInvitedSessions()
    {
        return sessionsDictionary["expected"].FindAll(s => s.GameInviter != null && s.GameInviter != dataController.GetUserId());
    }
    public List<SessionData> GetActiveSessions()
    {
        return sessionsDictionary["active"];
    }

    public SessionData GetSessionByOpponentId(string opponentId)
    {
        return dataController.LobbiesController.GetAllSessionsList().Find(session => session.Users.ContainsKey(opponentId));
    }
    public string GetSessionTypeById(string id)
    {
        if(sessionsDictionary == null)
        {
            Debug.LogWarning($"Denied searching session with id {id}. Sessions dictionary is null");
        }

        SessionData data = null;
            data = sessionsDictionary["active"].Find(s => s.gameId == id);
            if (data != null)
                return "active";

            data = sessionsDictionary["expected"].Find(s => s.gameId == id);
            if (data != null)
                return "expected";

            data = sessionsDictionary["ended"].Find(s => s.gameId == id);
            if (data != null)
                return "ended";

        return null;
    }
    public List<SessionData> GetAllSessionsList()
    {
        List<SessionData> result = new List<SessionData>();

        foreach (var kvp in sessionsDictionary)
        {
            result.AddRange(kvp.Value);
        }

        return result;
    }
    private SessionData GetSessionById(string id)
    {
        SessionData foundSession = null;

        foreach (var sessionKVP in sessionsDictionary)
        {
            SessionData found = sessionKVP.Value.Find(s => s.Id == id);
            if (found != null)
            {
                foundSession = found;
                break;
            }
        }

        return foundSession;
    }
    public bool HasSessionWithId(string id)
    {
        return GetSessionById(id) != null;

        //SessionData foundSession = null;

        //foreach (var sessionKVP in sessionsDictionary)
        //{
        //    SessionData found = sessionKVP.Value.Find(s => s.Id == id);
        //    if (found != null)
        //        return true;
        //}

        //return false;
    }
    public bool IsExpectedSession(string id)
    {
        SessionData expectedSession = sessionsDictionary["expected"].Find(s => s.Id == id);
        return expectedSession != null;
    }
    public bool IsInvitingToBeSession(string id)
    {
        SessionData wishingToBeSession = sessionsDictionary["expected"].Find(s => s.Id == id);
        return wishingToBeSession != null;
    }
    public bool IsActiveSession(string id)
    {
        SessionData activeSession = sessionsDictionary["active"].Find(s => s.Id == id);
        return activeSession != null;
    }

    public void OnNewActiveSession(string sessionId)
    {
        SessionData newActive = AddSession("active", sessionId);

        //refactor. bad code
        RemoveSession("expected", sessionId);

        Messenger.Broadcast<SessionData>(LobbiesNotifications.OnNewActiveSession, newActive);
    }
    public void OnNewExpected(string sessionId)
    {

        SessionData newExpected = AddSession("expected", sessionId);

        Messenger.Broadcast<SessionData>(LobbiesNotifications.OnNewSessionInvitation, newExpected);
    }
    public void OnNewEndedSession(string sessionId)
    {
        //Переносим из active в ended
        SessionData newEnded = sessionsDictionary["active"].Find((s) => s.Id == sessionId); //AddSession("ended", sessionId);
        sessionsDictionary["ended"].Add(newEnded);
        RemoveSession("active", sessionId);

        //Останавливаем слушатель активной сессии
        RemoveActiveSessionListener(newEnded); //refactor. bad code

        //Обновляем данные сессии и оповещаем другие контроллеры об изменениях
        UpdateSessionData(newEnded, () =>
        {
            Messenger.Broadcast<SessionData>(LobbiesNotifications.OnRemovedFromActive, newEnded);
        });
    }

    private SessionData AddSession(string type, string id)
    {
        SessionData sd = new SessionData(id);
        sessionsDictionary[type].Add(sd);
        return sd;
    }
    private SessionData RemoveSession(string type, string id)
    {
        SessionData removingCopy = new SessionData();
        removingCopy.CopyFrom(GetSessionById(id));
        //sessionsDictionary[type].Remove(removing);
        sessionsDictionary[type].RemoveAll((session) => session.Id == id);

        return removingCopy;
    }
    private void RemoveSessionById(string id)
    {
        //find, 
    }

   

}
