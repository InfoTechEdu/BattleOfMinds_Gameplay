
using BestHTTP.ServerSentEvents;
using Proyecto26;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class DataController : MonoBehaviour
{
    public UnityEngine.UI.Slider progressBar;
    [Header("SubControllersPrefabs")]
    public NotificationsController notificationsControllerPrefab;
    public FriendshipController friendshipControllerPrefab;
    public LobbiesController lobbiesControllerPrefab;

    [HideInInspector] public FriendshipController FriendshipController;
    [HideInInspector] public LobbiesController LobbiesController;

    private string databaseUrl = FirebaseProjectConfigurations.REALTIME_DATABASE_ROOT_PATH;
    private string userId;
    private string idToken;

    private int progress;

    UserData userData;
    UserData currentOpponent;
    LeaderboardData leaderboardData;

    //List<SessionData> sessionsList;
    //List<EventSource> sessionsListeners;

    EventSource menuDashboardListener;

    SessionData currentOnlineSession;

    private int onlinePlayersCount = 0;
    private int leaderboardPosition = 0;


    //MONO BEHAVIOUR
    private void Start()
    {
        Debug.Log("Starting download data...");
        DontDestroyOnLoad(gameObject);

        userId = FirebaseManager.Instance.Auth.UserId;
        idToken = FirebaseManager.Instance.Auth.IdToken;

        //notificationPool = new List<NotificationData>();
        //wishingToBeFriendsUsers = new List<UserData>();
        //activeFriendsList = new List<UserData>();

        //sessionsList = new List<SessionData>();
        //sessionsListeners = new List<EventSource>();

        GetPlatformUserData(OnPlatformUserDataWasGet);

        GameAnalyticsSDK.GameAnalytics.Initialize();
    }
    private void OnDestroy()
    {
        menuDashboardListener.Close();
    }
    private void OnApplicationQuit()
    {
        UpdateUserStatusREST(userData.Id, "offline");
        RemoveFromWishingToPlayREST();

        //old
        //PushUserToOfflineREST();
    }

    //Platform user data
    private void GetPlatformUserData(Action<PlatformUserData> onGet)
    {
        FirebaseManager.Instance.Database.GetObject<PlatformUserData>(FirebaseProjectConfigurations.PLATFORM_DATABASE_ROOT_PATH, $"allUsers/{userId}", onGet, (exception) =>
        {
            Debug.LogError("Error getting platform user data. Exception - " + exception.Message);
        });
    }
    private void OnPlatformUserDataWasGet(PlatformUserData pud)
    {
        if (!IsUserExistInGame(pud))
        {
            Dictionary<string, string> args = new Dictionary<string, string>() { { "game", "battleofminds" } };
            FirebaseManager.Instance.Functions.CallCloudFunctionPostObject<PlatformUserData>("CreateNewUserGameData", pud, args, (statusCode) =>
            {
                Debug.Log("User successfully created in game BattleOfMinds");

                PushUserToOnlineREST();
                //RemoveFromSearchers();

                StartCoroutine(LoadAllDataREST());
            }, (exception) =>
            {
                Debug.LogError("Error while calling CreateNewUser in game BattleOfMinds cloud function. Message - " + exception.Message);
            });
        }
        else
        {
            PushUserToOnlineREST();
            //RemoveFromSearchers();

            StartCoroutine(LoadAllDataREST());
        }
    }
    private bool IsUserExistInGame(PlatformUserData pud)
    {
        return pud.Games != null && pud.Games.ContainsKey("battleofminds");
    }

    public void OnGameSessionScreenLoaded()
    {
        if (FriendshipController != null)
            Destroy(FriendshipController.gameObject);
        if (LobbiesController != null)
            Destroy(LobbiesController.gameObject);

        if (menuDashboardListener != null)
            menuDashboardListener.Close();
    }
    public void OnMenuScreenLoaded()
    {
        FriendshipController = Instantiate(friendshipControllerPrefab, transform);
        FriendshipController.SetDataController(this);

        LobbiesController = Instantiate(lobbiesControllerPrefab, transform);
        LobbiesController.SetDataController(this);

        InitAndStartMenuDashboardListener();
    }
    //old?
    //public void OnMenuScreenLeft()
    //{
    //    if (FriendshipController != null)
    //        Destroy(FriendshipController.gameObject);

    //    if (menuDashboardListener != null)
    //        menuDashboardListener.Close();
    //}
    public void InitAndStartMenuDashboardListener()
    {
        menuDashboardListener = FirebaseManager.Instance.Database.ListenForValueChanged($"users/{userData.Id}/public", (message) =>
      {
          Debug.Log("OnMenuDashboardDataUpdated(). Data = " + message);

          DownloadUserDataREST(userData.Id, (loaded) =>
          {
              userData.updatePublicData(loaded.ProgressData, loaded.Statistics);
              Messenger.Broadcast("OnDashboardDataUpdated", true);
          });
      }, (exception) =>
      {
          Debug.LogError($"Exception while listening session status node data. Message - {exception.Message}");
          GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading session status node data. Message - {exception.Message}");
      });

        menuDashboardListener.OnOpen += (eventSource) => { Debug.Log($"Menu dashboard listener opened!"); };
    }

    //USERS DATA
    public UserProgressData GetUserProgressData()
    {
        return userData.ProgressData;
    }
    public StatisticsData GetUserStatisticsData()
    {
        return userData.Statistics;
    }
    public string GetUserId()
    {
        return userData.Id;
    }
    public UserData GetUserData()
    {
        return userData;
    }
    public UserData GetOpponentData()
    {
        return currentOpponent;
    }
    public SessionData GetCurrentSessionData()
    {
        return currentOnlineSession;
    }
    public RoundData GetActiveRoundData()
    {
        return currentOnlineSession.ActiveRoundData;
    }
    public int GetActiveRoundIndex()
    {
        return currentOnlineSession.ActiveRoundIndex;
    }

    //LEADERBOARD DATA
    public LeaderboardData GetLeaderboardData()
    {
        return leaderboardData;
    }
    public int GetUserPositionInLeaderboard(string userId)
    {
        //temp. edit
        return 100;
    }
    public UserData[] SortUsersByPoints()
    {
        List<UserData> usersList = new List<UserData>();
        usersList.AddRange(leaderboardData.allUsers);
        List<UserData> sortedUsersList = usersList.OrderBy(o => o.ProgressData.Points).ToList();


        Debug.Log("[temp] sorted list of users - " + Utils.CollectionUtils.ListToString(sortedUsersList));

        return sortedUsersList.ToArray();
    }
    //public void DownloadTop10(Action<LeaderboardData> onLoaded)
    //{
    //    RestClient.GetArray<LeaderboardData>($"{databaseUrl}/leaderboard/allUsers.json?auth={idToken}&orderBy=\"progressData/points\"&startAt=0&print=pretty",
    //    (exception, responseHelper, notUsed) =>
    //    {
    //        if (exception != null)
    //        {
    //            Debug.LogError("Exception while downloading leaderboard data. Message - " + exception.Message + ", response - " + exception.Response);
    //            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error,
    //                "Exception while downloading leaderboard data. Message - " + exception.Message);
    //            return;
    //        }

    //        Debug.Log("Top 10 leaderboard data was loaded. Response - " + responseHelper.Text);
    //        DataParser.ParseLeaderboardData(responseHelper.Text, out leaderboardData);
    //        leaderboardData.SortDescending();
    //        if (leaderboardData.Count > 10)
    //        {
    //            leaderboardData = new LeaderboardData(leaderboardData.AllUsers.Take(10).ToArray());
    //        }

    //        //if (leaderboardData.Count < 10)
    //        //{
    //        //    leaderboardData.SortDescending(); //fixing not sorting bug in firebase? Read notes in trello
    //        //}
    //        Debug.LogWarning("[debug] Top 10 Leaderboard data parsed. Result - " + leaderboardData);

    //        onLoaded.Invoke(leaderboardData);
    //    });
    //}
    public void DownloadTop10(Action<LeaderboardData> onLoaded)
    {
        Dictionary<string, object> args = new Dictionary<string, object>() { { "game", "battleofminds" } };
        FirebaseManager.Instance.Functions.CallCloudFunction("DownloadTop10Leaderboard", args, (data) =>
        {
            DataParser.ParseLeaderboardData(data.body, out leaderboardData);
            leaderboardData.SortDescending();
            onLoaded(leaderboardData);
        }, (exception) =>
        {
            Debug.LogError("Error while downloading leaderboard data");
        });

    }
    public void UpdateUserInLeaderboard()
    {
        leaderboardData.UpdateUserProgress(userData);
    }

    //SESSIONS DATA
    //old?
    //public List<SessionData> GetAllUserSessions()
    //{
    //    return sessionsList;
    //}
    //public List<SessionData> GetSessionListByStatus(string status)
    //{
    //    return sessionsList.Where(s => s.Status == status).ToList();
    //}
    public int GetUserSesionResult(string userId)
    {
        return currentOnlineSession.SessionResult.UsersResults[userId];
    }
    //old?
    //public void UpdateSessionData(SessionData updated)
    //{
    //    Debug.LogWarning("Updating session data. Updated - " + updated);
    //    SessionData old = sessionsList.Find(s => s.Id == updated.Id);
    //    old.CopyFrom(updated);
    //    //old = updated; //Думаю просто копирует ссылку
    //}
    //public void UpdateCurrentOnlineSession(string id)
    //{
    //    currentOnlineSession = sessionsList.Find(s => s.Id == id);
    //}
    public void UpdateCurrentOnlineSession(SessionData sd)
    {
        currentOnlineSession = sd;
    }
    public void UpdateCurrentOpponentData(UserData co)
    {
        if (co == null)
        {
            currentOpponent = new UserData(null);
            currentOpponent.updatePublicData(new UserProgressData("Неизвестный", "Соперник", 0), new StatisticsData(0, 0));
            currentOpponent.setProfilePhotoSprite(Resources.Load<Sprite>("PseudoRandomOpponent"));
            return;
        }

        currentOpponent = co;
    }
    public void UpdateOpponentData()
    {
        string opponentId = currentOnlineSession.Users.FirstOrDefault(u => u.Key != userData.Id).Key;
        currentOpponent = new UserData(opponentId);

        DownloadUserDataREST(opponentId, (loadedOpponentData) =>
        {
            currentOpponent.CopyFrom(loadedOpponentData);
        });
    }
    public void InitCurrentSession(string sessionId)
    {
        currentOnlineSession = new SessionData(sessionId);
    }
    public void UpdateCurrentSessionUsers()
    {
        Dictionary<string, bool> sessionUsers = new Dictionary<string, bool> { { userData.Id, true }, { currentOpponent.Id, true } };
        currentOnlineSession.setUsers(sessionUsers);
    }

    //OTHER DATA
    public int GetOnlinePlayersCount()
    {
        return onlinePlayersCount;
    }
    public int GetLeaderboardPosition()
    {
        return leaderboardPosition;
    }




    private void UpdateProgressBar()
    {
        if (progressBar == null)
            return;
        progress += UnityEngine.Random.Range(0, 100 - progress);
        progressBar.value = progress / 100f;
    }
    public void OnGameSessionEnd(string gameStatus)
    {
        UpdateGameStatusREST(gameStatus);
    }

    #region REST FIREBASE 

    private bool pushUserToOnlineTransactionSuccess = false;
    private bool pushUserToOnlineTransactionWasSend = false;
    private void PushUserToOnlineREST()
    {
        //edit update transaction method
        return;

        
        string onlineCounterUrl = $"{databaseUrl}//users/online/count.json?auth={idToken}";
        pushUserToOnlineTransactionSuccess = false; //not used
        pushUserToOnlineTransactionWasSend = false; //not used

        RestClient.Get(new RequestHelper
        {
            Uri = onlineCounterUrl,
            Headers = new Dictionary<string, string> { { "X-Firebase-ETag", "true" } }
        }).Then(response =>
        {
            string Etag = response.Request.GetResponseHeader("ETag");
            //refactor. bad code. Не понял как сделать PUT запрос и положить туда int
            int onlineCounterValue = 0;

            if (response.Request.downloadHandler.text != "null")
                onlineCounterValue = int.Parse(response.Request.downloadHandler.text);

            onlineCounterValue++;
            byte[] rawData = System.Text.Encoding.UTF8.GetBytes(onlineCounterValue.ToString());

            RequestHelper requestHelper = new RequestHelper
            {
                Uri = onlineCounterUrl,
                Headers = new Dictionary<string, string> { { "if-Match", Etag } },
                Method = "PUT",
                ContentType = "text/plain",
                BodyRaw = rawData
            };

            return RestClient.Request(requestHelper);
        }).Then(response =>
        {
            pushUserToOnlineTransactionSuccess = true;
            Debug.Log("Transaction write to online was success. Response - " + response.ToString());
        }).Catch(exception =>
        {
            Debug.LogError("Error while writing transaction. Message - " + exception.Message);
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Critical,
                "Error while writing (to online) transaction. Message - " + exception.Message);
        });

        pushUserToOnlineTransactionWasSend = true;
    }
    private void PushUserToOfflineREST()
    {
        //edit update transaction method
        return;
        string onlineCounterUrl = $"{databaseUrl}//users/online/count.json?auth={idToken}";
        pushUserToOnlineTransactionSuccess = false; //not used
        pushUserToOnlineTransactionWasSend = false; //not used

        RestClient.Get(new RequestHelper
        {
            Uri = onlineCounterUrl,
            Headers = new Dictionary<string, string> { { "X-Firebase-ETag", "true" } }
        }).Then(response =>
        {
            string Etag = response.Request.GetResponseHeader("ETag");
            //refactor. bad code. Не понял как сделать PUT запрос и положить туда int
            int onlineCounterValue = 0;

            if (response.Request.downloadHandler.text != "null")
                onlineCounterValue = int.Parse(response.Request.downloadHandler.text);

            onlineCounterValue--;
            byte[] rawData = System.Text.Encoding.UTF8.GetBytes(onlineCounterValue.ToString());

            RequestHelper requestHelper = new RequestHelper
            {
                Uri = onlineCounterUrl,
                Headers = new Dictionary<string, string> { { "if-Match", Etag } },
                Method = "PUT",
                ContentType = "text/plain",
                BodyRaw = rawData
            };

            return RestClient.Request(requestHelper);
        }).Then(response =>
        {
            pushUserToOnlineTransactionSuccess = true;
            Debug.Log("Transaction write to online was success. Response - " + response.ToString());
        }).Catch(exception =>
        {
            Debug.LogError("Error while writing transaction. Message - " + exception.Message);
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Critical,
                "Error while writing (to offline) transaction. Message - " + exception.Message);
        });

        pushUserToOnlineTransactionWasSend = true;
    }


    FirebaseCustomYield firebaseCustomYield;

    public string DatabaseUrl { get => databaseUrl; set => databaseUrl = value; }
    public string IdToken { get => idToken; set => idToken = value; }


    //FIREBASE LOAD MAIN DATA
    private IEnumerator LoadAllDataREST()
    {
        firebaseCustomYield = new FirebaseCustomYield();

        //Getting Leaderboard Data
        firebaseCustomYield.onRequestStarted();
        LoadUserData();
        yield return firebaseCustomYield;
        UpdateProgressBar();

        firebaseCustomYield.onRequestStarted();
        LoadLeaderboardData();
        yield return firebaseCustomYield;
        UpdateProgressBar();

        firebaseCustomYield.onRequestStarted();
        LoadOnlinePlayersCount();
        yield return firebaseCustomYield;
        UpdateProgressBar();

        firebaseCustomYield.onRequestStarted();
        LoadLeaderboardPosition();
        yield return firebaseCustomYield;
        UpdateProgressBar();

        //firebaseCustomYield.onRequestStarted();
        //LoadNotificationsREST();
        //yield return firebaseCustomYield;
        //UpdateProgressBar();

        //firebaseCustomYield.onRequestStarted();
        //LoadFriendsListREST();
        //yield return firebaseCustomYield;
        //UpdateProgressBar();

        //old?
        //firebaseCustomYield.onRequestStarted();
        //LoadUserSessionsList();
        //yield return firebaseCustomYield;
        //UpdateProgressBar();

        Debug.LogWarning("All UserData was loaded. Result - " + userData);

        OnAllDataLoadedREST();
        //edit. add loading settings
    }
    public void LoadUserData()
    {
        FirebaseManager.Instance.Database.GetObject<UserData>($"users/{userId}/public", (data) =>
        {
            userData = data;
            userData.setId(userId);

            StartCoroutine(GetSpriteFromURL(userData.ProfilePhotoUrl, (sprite) =>
            {
                userData.setProfilePhotoSprite(sprite);
            }));

            firebaseCustomYield.onRequestEnd();
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading user data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error,
                $"Exception while downloading user data. Message - {exception.Message}");
        });
    } 
    public void LoadLeaderboardData()
    {
        FirebaseManager.Instance.Database.GetObject<LeaderboardData>($"leaderboard/allUsers", (data) =>
        {
            leaderboardData = data;

            firebaseCustomYield.onRequestEnd();
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading leaderboard data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error,
                "Exception while downloading leaderboard data. Message - " + exception.Message);
        });
    }

    //old?
    //public void LoadUserSessionsList()
    //{
    //    FirebaseManager.Instance.Database.GetJson($"users/{userId}/private/sessions", (data) =>
    //    {
    //        Debug.Log("Sessions data was loaded");
    //        DataParser.ParseSessionsList(data, out sessionsList);
    //        Debug.LogWarning("[debug] Session data parsed. Result - " + Utils.CollectionUtils.ListToString(sessionsList));

    //        firebaseCustomYield.onRequestEnd();
    //    }, (exception) =>
    //    {
    //        Debug.LogError($"Exception while downloading sessions data. Message - {exception.Message}");
    //        GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading sessions data. Message - {exception.Message}");
    //    });
    //}
    private void DownloadAndUpdateSessionDataREST(SessionData sessionData, Action onLoaded)
    {

    }
    //old?
    //public IEnumerator DownloadAndUpdateAllSessionsDataREST(Action onLoaded)
    //{
    //    int counter = 0;

    //    foreach (var session in sessionsList)
    //    {
    //        DownloadSessionDataREST(session.Id, (data) =>
    //        {
    //            int index = sessionsList.IndexOf(session);
    //            sessionsList[index] = data;

    //            counter++;
    //        });
    //    }

    //    while (counter < sessionsList.Count)
    //    {
    //        yield return null;
    //    }

    //    onLoaded?.Invoke();
    //}
    private void LoadOnlinePlayersCount()
    {

        FirebaseManager.Instance.Database.GetValue($"users/online/count", (data) =>
        {
            if (data != "null")
                onlinePlayersCount = int.Parse(data);

            Debug.Log("Online players count was get. Value - " + onlinePlayersCount);
            firebaseCustomYield.onRequestEnd();
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading users count data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading users count data. Message - {exception.Message}");
        });
    }
    private void LoadLeaderboardPosition()
    {
        FirebaseManager.Instance.Database.GetValue($"leaderboard/allUsers/{userData.Id}/position", (value) =>
        {
            Debug.Log(value);
            if (value == "\"null\"")
            {
                leaderboardPosition = 0;
            }
            else
            {
                leaderboardPosition = int.Parse(value);
            }
            firebaseCustomYield.onRequestEnd();
        }, (exception) =>
        {
            firebaseCustomYield.onRequestEnd();
            Debug.Log("Cannot get leaderboard position! Message - " + exception.Message);
        });
    }
    private void OnAllDataLoadedREST()
    {
        Debug.Log("OnAllDataLoaded()");

        UpdateUserStatusREST(userData.Id, "online");
        RemoveFromWishingToPlayREST(); //clearing data

        Scene scene = SceneManager.GetSceneByName("DataLoadingScreen");
        SceneManager.SetActiveScene(scene);

        SceneManager.LoadScene("MenuScreen");
    }


    //FIREBASE REST LISTENERS

    private void StopAllListenersREST()
    {
        //notificationsListener.Close();
    }
    //Отложил данный метод, пока гружу уведомления в Load All data

    private void UpdateOpponentDataListenersREST()
    {
        //edit. Add listeners
    }



    //UPDATE USER SESSION DATA
    public void RemoveFromWishingToPlayREST()
    {
        FirebaseManager.Instance.Database.Delete($"users/wishingToPlay/{userData.Id}", () =>
        {
            Debug.Log("Success removing from wishingToPlay");
        }, (exception) =>
        {
            Debug.LogError($"Exception while deleting from wishingToPlay. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while deleting from wishingToPlay. Message - {exception.Message}");
        });
    }
    private void UpdateGameStatusREST(string status)
    {
        FirebaseManager.Instance.Database.PutValue($"games/{currentOnlineSession.Id}/status", status, () =>
        {
            Debug.Log("Success updating user status");
        }, (exception) =>
        {
            Debug.LogError($"Exception while putting data to status. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while putting data to status. Message - {exception.Message}");
        });
    }

    public void SaveRoundResult(string roundResult)
    {
        //locally
        currentOnlineSession.addUserResultToActiveRound(userId, roundResult);

        //server
        string roundKey = "round" + currentOnlineSession.ActiveRoundIndex;
        FirebaseManager.Instance.Database.PutValue($"games/{currentOnlineSession.Id}/results/{roundKey}/{userId}", roundResult, () =>
        {
            Debug.Log("Success updating roundResult of player");
        }, (exception) =>
        {
            Debug.LogError($"Exception while updating roundResult of player. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while updating roundResult of player. Message - {exception.Message}");
        });

    }
    public void UpdateUserSessionResult()
    {
        currentOnlineSession.recalculateUserSessionResult(userId);

        //currentOnlineSession.updateUserSessionResult(userId);

        //int points = currentOnlineSession.GetUserPoints(userId);

        //FirebaseManager.Instance.Database.PutValue($"games/{currentOnlineSession.Id}/sessionResult/usersResults/{userId}", points, () =>
        //{
        //    Debug.Log("Success updating sessionResult of player");
        //}, (exception) =>
        //{
        //    Debug.LogError("Exception while updating sessionResult of player. Message - " + exception.Message);
        //    GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Critical,
        //        "Error while updating sessionResult of player. Message - " + exception.Message);
        //});
    }
    
    //old
    //public void UpdateSessions(Action<SessionData> onSessionUpdated)
    //{
    //    foreach (var currentSession in sessionsList)
    //    {
    //        CheckMovingPlayerUpdated(currentSession.Id, currentSession.MovingPlayer, (sessionId, isUpdated) =>
    //        {
    //            if (isUpdated)
    //            {
    //                Debug.Log($"Moving player of session {sessionId} updated. Downloading new data...");
    //                DownloadSessionDataREST(sessionId, UpdateSessionData, onSessionUpdated);
    //            }
    //        }, ()=>
    //        {
    //            //edit
    //        });
    //    }
    //}

    
    public void UpdateCurrentSessionMovingPlayer(string currentMoving, Action<bool> onPassed)
    {
        //locally
        currentOnlineSession.setMovingPlayer(currentMoving);
        
        //server
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("gameId", currentOnlineSession.Id);
        @params.Add("opponentId", currentMoving);

        FirebaseManager.Instance.Functions.CallCloudFunction("PassTurnToOpponentBOM", @params, (data) =>
        {
            Debug.Log("Moving player updated in firebase");
            onPassed(true);
        }, (exception) =>
        {
            Debug.Log("Error while passing turn to opponent. Message: " + exception.Message);
            onPassed(false);
        });

        /*
         * byte[] rawData = System.Text.Encoding.UTF8.GetBytes(currentMoving);

        string url = $"{databaseUrl}/games/{currentOnlineSession.Id}/movingPlayer/{userId}.json?auth={idToken}";

        RequestHelper requestHelper = new RequestHelper
        {
            Uri = url,
            Method = "PUT",
            ContentType = "text/plain",
            BodyRaw = rawData
        };

        Debug.Log("Saving moving player to - " + url);

        RestClient.Request(requestHelper).Then(response =>
        {
            Debug.Log("Success updating moving player . Response - " + response);
        }).Catch(exception =>
        {
            Debug.LogError("Exception while updating moving player. Message - " + exception.Message);
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Critical,
                "Error while updating moving player. Message - " + exception.Message);
        });
        SessionData updatingSession = sessionsList.Find(s => s.Id == sessionId);
        updatingSession.setMovingPlayer(currentMoving);
        */
    }
    public void UpdateStatusInWishingToPlayNode(string status)
    {
        FirebaseManager.Instance.Database.PutValue($"users/wishingToPlay/{userData.Id}/status", status, () =>
        {
            Debug.Log("Success updating status in wishingToPlay");
        }, (exception) =>
        {
            Debug.LogError($"Exception while updating status in wishingToPlay. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while updating status in wishingToPlay. Message - {exception.Message}");
        });
    }
    public void WaitUntilWinnerWasSet(Action onWinnerWasSet)
    {
      var listener = FirebaseManager.Instance.Database.ListenForValueChanged($"games/{currentOnlineSession.Id}/sessionResult/winner", (winnerId) =>
      {
          currentOnlineSession.updateSessionWinner(winnerId);
          onWinnerWasSet.Invoke();
          FirebaseManager.Instance.Database.StopListen($"games/{currentOnlineSession.Id}/sessionResult/winner");
      }, (exception) =>
      {
          Debug.LogError($"Exception while listening winner node. Message - {exception.Message}");
          GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading winner node data. Message - {exception.Message}");
      });

        Debug.Log("Kurva!");
    }

    public void GetStatusEndGame()
    {
        FirebaseManager.Instance.Database.GetJson("games/" + currentOnlineSession.Id.ToString(), (status) =>  
        {
            JSONNode statusJsonObj = JSONNode.Parse(status);
            if (statusJsonObj["status"] == "ended")
            {
                //Костыль да, но работает )
                GameController gameController = FindObjectOfType<GameController>();
                gameController.CallEndGame();
            }
            
            Debug.LogError("Status: " + statusJsonObj["status"]);

        }, (exception) =>
        {
            Debug.LogError("Exception check game status - " + exception.Message);
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error,
                "Exception check game status - " + exception.Message);
        });
    }

    public void WaitUntilGameInitialized(string sessionId, Action onInitialized)
    {
        var listener = FirebaseManager.Instance.Database.ListenForValueChanged($"games/{sessionId}", (data) =>
        {
            string pathValue = JSONNode.Parse(data);
            if (pathValue != "/movingPlayer")
                return;


            onInitialized();
            FirebaseManager.Instance.Database.StopListen($"games/{sessionId}");
        }, (exception) =>
        {
            Debug.LogError($"Exception while listening winner node. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading winner node data. Message - {exception.Message}");
        });
    }
    public void AcceptGameInvitation(string sessionId, string inviterId, string gameName, Action<bool> onReady)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("gameId", sessionId);
        @params.Add("userId", userData.Id);
        @params.Add("inviterId", inviterId);
        @params.Add("game", gameName);
        @params.Add("isAccepted", true);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineGameInvitation", @params, (data) =>
        {
            if(data.body == null)
            {
                Debug.LogError($"Exception while accepting and initizaling game. Game - {gameName}. Inviter - {inviterId}");
                onReady.Invoke(false);
            }
            else
            {
                Debug.Log("New friendly game accepted and intialized. Saved to currentOnlineSession");
                DataParser.ParseSessionData(data.body, out currentOnlineSession);
                onReady.Invoke(true);
            }
        }, (exception) =>
        {
            Debug.LogError($"Exception while accepting game invitation. Game - {gameName}. Inviter - {inviterId}. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while accepting game invitation. Game - {gameName}. Inviter - {inviterId}. Message - {exception.Message}");
            onReady(false);
        });
    }
    public void DeclineGameInvitation(string sessionId, string inviterId, string gameName, Action<bool> onReady)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("gameId", sessionId);
        @params.Add("userId", userData.Id);
        @params.Add("inviterId", inviterId);
        @params.Add("game", gameName);
        @params.Add("isAccepted", false);

        FirebaseManager.Instance.Functions.CallCloudFunction("AcceptDeclineGameInvitation", @params, (data) =>
        {
            onReady.Invoke(true);
        }, (exception) =>
        {
            Debug.LogError($"Exception while accepting game invitation. Game - {gameName}. Inviter - {inviterId}. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while accepting game invitation. Game - {gameName}. Inviter - {inviterId}. Message - {exception.Message}");
            onReady(false);
        });
    }
    public void AddUserToWishingToPlayREST(string userId, Action<bool> onAdded = null)
    {
        JSONNode jsonObj = new JSONObject();

        JSONNode child = new JSONObject();
        child["gameId"] = "empty";
        child["className"] = userData.UserClass;
        child["status"] = "waiting";

        jsonObj[userId] = child;

        string jsonData = jsonObj.ToString();


        FirebaseManager.Instance.Database.PatchJson($"users/wishingToPlay", jsonData, () =>
        {
            Debug.Log("Success adding user to wishingToPlay");
            onAdded(true);
        }, (exception) =>
        {
            Debug.LogError($"Exception while patch wishingToPlay data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while patch wishingToPlay data. Message - {exception.Message}");
            onAdded(false);
        });
    }
    public void UpdateUserStatusREST(string user, string status)
    {
        FirebaseManager.Instance.Database.PutValue($"users/{user}/public/status", status, () =>
        {
            Debug.Log("Success updating user data");
        }, (exception) =>
        {
            Debug.LogError($"Exception while put status data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while put status data. Message - {exception.Message}");
        });
    }

    public string GetOpponentsActiveRoundResult()
    {
        Dictionary<string, string> activeRoundResults = currentOnlineSession.GetActiveRoundResults();
        if (activeRoundResults == null)
            return null;

        if (activeRoundResults.TryGetValue(currentOpponent.Id, out string result))
            return result;

        return null;
        //return currentOnlineSession.GetActiveRoundResult().GetResultOfUserByKey(currentOpponent.Id);
    }
    
    

    //OTHER DATA DOWNLOAD
    public void DownloadUserDataREST(string id, Action<UserData> onLoaded, bool waitPhotoLoading = false)
    {
        FirebaseManager.Instance.Database.GetObject<UserData>($"users/{id}/public", (data) =>
        {
            data.setId(id);
            if (!waitPhotoLoading)
                onLoaded(data);
            else
                StartCoroutine(GetSpriteFromURL(data.ProfilePhotoUrl, (sprite) =>
                {
                    data.ProfilePhoto = sprite; //sprite may be null
                    onLoaded.Invoke(data);
                }));
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading user data with id {id} data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading user data with id {id} data. Message - {exception.Message}");
            onLoaded(null);
        });
    }
   
    private void DownloadOpponentDataREST(string opponentId)
    {
        currentOpponent = null;
        FirebaseManager.Instance.Database.GetObject<UserData>($"users/{opponentId}/public", (data) =>
        {
            //danger edit refactor. Мне кажется лучше отдельно грузить картинку
            StartCoroutine(GetSpriteFromURL(data.ProfilePhotoUrl, (sprite) =>
            {
                currentOpponent = data;
                currentOpponent.ProfilePhoto = sprite;
            }));
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading opponent data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error,  $"Exception while downloading opponent data. Message - {exception.Message}");
        });

        Debug.LogWarning("Downloading opponent data at url - " + $"{databaseUrl}/users/{opponentId}/public.json?auth={idToken}");
        currentOpponent = null;
    }



    public IEnumerator DownloadOpponentDataUpdated(string opponentId, Action<UserData> onOpponentDataLoaded)
    {
        DownloadOpponentDataREST(opponentId);
        yield return new WaitUntil(() => currentOpponent != null);


        Debug.Log("[debug Data Controller] Opponent data was loaded. Data - " + currentOpponent);
        onOpponentDataLoaded.Invoke(currentOpponent);
    }
    public void DownloadSessionDataREST(string sessionId, Action<SessionData> onLoaded, Action<SessionData> other = null)
    {
        FirebaseManager.Instance.Database.GetJson($"games/{sessionId}", (json) =>
        {
            DataParser.ParseSessionData(json, out SessionData sd);
            if(sd != null)
            {
                Debug.Log($"Session data with id {sessionId} was loaded. Data - " + json);
                onLoaded.Invoke(sd);
                other?.Invoke(sd);
            }
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading session data with id {sessionId} data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading session data with id {sessionId} data. Message - {exception.Message}");
            onLoaded.Invoke(null);
            other?.Invoke(null);
        });


        //refactor Не работает. Не парсит данные сессии
        //FirebaseManager.Instance.Database.GetObject<SessionData>($"games/{sessionId}", (data) =>
        //{
        //    Debug.Log($"Session data with id {sessionId} was loaded. Data - " + data);
        //    onLoaded.Invoke(data);
        //    other?.Invoke(data);
        //}, (exception) =>
        //{
        //    Debug.LogError($"Exception while downloading session data with id {sessionId} data. Message - {exception.Message}");
        //    GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading session data with id {sessionId} data. Message - {exception.Message}");
        //    onLoaded.Invoke(null);
        //    other?.Invoke(null);
        //});
    }
    public void CheckMovingPlayerUpdated(string sessionId, string lastMoving, Action<string, bool> movingPlayerUpdated, Action onFailed)
    {
        FirebaseManager.Instance.Database.GetValue($"games/{sessionId}/movingPlayer", (value) =>
        {
            Debug.Log($"Session data with id {sessionId} was loaded");
            string movingPlayer = value.Replace("\"", "");
            if (movingPlayer == null || movingPlayer == lastMoving)
                movingPlayerUpdated.Invoke(sessionId, false);
            else
                movingPlayerUpdated.Invoke(sessionId, true);

        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading checking moving player in session with id {sessionId} data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading checking moving player in session with id {sessionId} data. Message - {exception.Message}");
            onFailed();
        });
    }
    public void DownloadRoundDataRest(string sessionId, string roundId, Action<RoundData> onLoaded)
    {
        FirebaseManager.Instance.Database.GetObject<RoundData>($"games/{sessionId}/rounds/{roundId}", (data) =>
        {
            Debug.Log($"Round data with id {roundId} was loaded. Data - " + data);
            onLoaded.Invoke(data);
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading round data. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading round data. Message - {exception.Message}");
            onLoaded.Invoke(null);
        });
    }
    public void DownloadAndUpdateCurrentSessionDataREST(Action<bool> onLoaded)
    {

        FirebaseManager.Instance.Database.GetObject<SessionData>($"games/{currentOnlineSession.Id}", (data) =>
        {
            onLoaded.Invoke(false);
        }, (exception) =>
        {
            Debug.LogError($"Exception while downloading current session data with id {currentOnlineSession.Id}. Message - {exception.Message}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Error, $"Exception while downloading current session data with id {currentOnlineSession.Id} data. Message - {exception.Message}");
            onLoaded.Invoke(true);
        });
    }
    public void DownloadAndUpdateOpponentData(Action<bool> onLoaded)
    {
        if (currentOnlineSession.Users.Count < 2) //There is 1 user in the game
        {
            currentOpponent = new UserData(null);
            currentOpponent.updatePublicData(new UserProgressData("Неизвестный", "Соперник", 0), new StatisticsData(0, 0));
            currentOpponent.setProfilePhotoSprite(Resources.Load<Sprite>("PseudoRandomOpponent"));
            onLoaded(false);
            return;
        }

        string opponentId = currentOnlineSession.Users.FirstOrDefault(user => user.Key != userData.Id).Key;
        DownloadUserDataREST(opponentId, (loadedOpponent) =>
        {
            if (loadedOpponent == null)
            {
                Debug.LogError("Opponent's data was not get");
                onLoaded.Invoke(true);
                return;
            }

            Debug.Log("Opponent data was get. Data - " + loadedOpponent);

            currentOpponent = loadedOpponent;
            onLoaded.Invoke(false);
        });
    }

    public void OnLoggedOut()
    {
        UpdateUserStatusREST(userData.Id, "offline");
        RemoveFromWishingToPlayREST(); //clearing data

        FirebaseManager.Instance.Auth.LogOut();
    }
    #endregion

    public IEnumerator CallCloudFunctionGet(string functionName, string GetParams, Action<bool> callback = null)
    {
        UnityEngine.Debug.Log($"Starting CallCloudFunction {functionName}");

        var projectId = FirebaseProjectConfigurations.PROJECT_ID;
        UnityEngine.Debug.Log("projectId - " + projectId);

        //string url = "https://us-central1-dzgames-12ad8.cloudfunctions.net/GenerateSingleGameQuiz";
        //UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        UnityWebRequest request = new UnityWebRequest($"https://us-central1-{projectId}.cloudfunctions.net/{functionName}?{GetParams}",
            UnityWebRequest.kHttpVerbGET);
        request.SetRequestHeader("Access-Control-Allow-Origin", "*");

        UnityEngine.Debug.Log("request was send");
        yield return request.SendWebRequest();

        Debug.Log("end request");
        UnityEngine.Debug.Log($"End of CallCloudFunction - {functionName}. Request status code - {request.responseCode}");

        if (request.responseCode == 200)
        {
            Debug.Log($"Succes calling {functionName}");
            callback?.Invoke(true);
        }
        else
        {
            Debug.Log("Error while calling cloud function");
            callback?.Invoke(false);
        }
        //commented
        //StartCoroutine(OnNewSingleGameQuizCreated());



        //exception
        //UnityEngine.Debug.Log("request.downloadHandler.text - " + request.downloadHandler.text);

    }

    #region Other network methods
    public IEnumerator GetSpriteFromURL(string url, Action<Sprite> callback)
    {
        Debug.Log("Downloading texture with url - " + url);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError($"Error downloading texture from url {url}. Error - {www.error}");
            GameAnalyticsSDK.GameAnalytics.NewErrorEvent(GameAnalyticsSDK.GAErrorSeverity.Warning, "Error downloading texture. Error - " + www.error);
            callback.Invoke(null);
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite downloadedSprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            callback.Invoke(downloadedSprite);
        }

    }
    public IEnumerator CallCloudFunctionGET(string functionName, string GetParams, Action<bool> callback = null)
    {
        UnityEngine.Debug.Log($"Starting CallCloudFunction {functionName}");

        var projectId = FirebaseProjectConfigurations.PROJECT_ID;
        UnityEngine.Debug.Log("projectId - " + projectId);

        //string url = "https://us-central1-dzgames-12ad8.cloudfunctions.net/GenerateSingleGameQuiz";
        //UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        UnityWebRequest request = new UnityWebRequest($"https://us-central1-{projectId}.cloudfunctions.net/{functionName}?{GetParams}",
            UnityWebRequest.kHttpVerbGET);
        request.SetRequestHeader("Access-Control-Allow-Origin", "*");

        UnityEngine.Debug.Log("request was send");
        yield return request.SendWebRequest();

        Debug.Log("end request");
        UnityEngine.Debug.Log($"End of CallCloudFunction - {functionName}. Request status code - {request.responseCode}");

        if (request.responseCode == 200)
        {
            Debug.Log($"Succes calling {functionName}");
            callback?.Invoke(true);
        }
        else
        {
            Debug.Log("Error while calling cloud function");
            callback?.Invoke(false);
        }
        //commented
        //StartCoroutine(OnNewSingleGameQuizCreated());



        //exception
        //UnityEngine.Debug.Log("request.downloadHandler.text - " + request.downloadHandler.text);

    }
    #endregion
}

