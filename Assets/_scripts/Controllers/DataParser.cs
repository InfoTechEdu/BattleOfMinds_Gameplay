using FullSerializer;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DataParser
{
    private static fsSerializer serializer = new fsSerializer();

    public static void ParsePlatformUserData(string jsonData, out UserData userData)
    {
        userData = new UserData();

        JSONNode userDataJsonObj = JSONNode.Parse(jsonData);

        Debug.Log("[debug] parsing user jsonData - " + jsonData);

        //object deserialized = null;
        //serializer.TryDeserialize(fsJsonParser.Parse(userDataJsonObj.Value.ToString()), typeof(UserProgressData), ref deserialized);
        //UserProgressData upd = deserialized as UserProgressData;
        UserProgressData upd = new UserProgressData(userDataJsonObj["name"].Value, userDataJsonObj["surname"].Value, 0);
        userData.updatePublicData(upd, null);

        Debug.LogWarning("[debug] result progress - " + upd);

        //getting class data
        userData.setUserClass(userDataJsonObj["userClass"].Value.ToString());
        Debug.LogWarning("[debug] User class - " + userData.UserClass);
    }
    public static void ParseUserData(string jsonData, out UserData userData)
    {
        userData = new UserData();

        JSONNode userDataJsonObj = JSONNode.Parse(jsonData);

        Debug.Log("[debug] parsing user jsonData - " + jsonData);

        //loading progress data
        UserProgressData upd = JsonUtility.FromJson<UserProgressData>(userDataJsonObj["public"]["progressData"].ToString());
        StatisticsData sd = JsonUtility.FromJson<StatisticsData>(userDataJsonObj["public"]["statistics"].ToString());
        userData.updatePublicData(upd, sd);

        Debug.LogWarning("[debug] result progress - " + upd);
        Debug.LogWarning("[debug] result statistics - " + sd);

        //getting class data
        userData.userClass = userDataJsonObj["public"]["userClass"].Value.ToString();
        userData.status = userDataJsonObj["public"]["status"].Value.ToString();
        Debug.LogWarning("[debug] User class - " + userData.userClass);
        Debug.LogWarning("[debug] User status - " + userData.status);

        //getting profilePhotoUrl (private)
        userData.ProfilePhotoUrl = userDataJsonObj["public"]["profilePhoto"].Value.ToString(); //Так как без Value ставит кавычки в начале и конце
    }
    public static void ParsePublicUserData(string publicUserJsonData, out UserData userData)
    {
        Debug.Log("Passed - " + publicUserJsonData);
        if (string.IsNullOrEmpty(publicUserJsonData))
        {
            Debug.LogWarning("No data for parsing. Return...");
            userData = null;
            return;
        }

        userData = new UserData();

        JSONNode publicUserDataJsonObj = JSONNode.Parse(publicUserJsonData);

        //loading progress data
        UserProgressData upd = JsonUtility.FromJson<UserProgressData>(publicUserDataJsonObj["progressData"].ToString());
        StatisticsData sd = JsonUtility.FromJson<StatisticsData>(publicUserDataJsonObj["statistics"].ToString());
        userData.updatePublicData(upd, sd);

        Debug.LogWarning("[debug] result progress - " + upd);
        Debug.LogWarning("[debug] result statistics - " + sd);

        //getting class data
        userData.userClass = publicUserDataJsonObj["userClass"].Value.ToString();
        userData.status = publicUserDataJsonObj["status"].Value.ToString();
        Debug.LogWarning("[debug] User class - " + userData.userClass);
        Debug.LogWarning("[debug] User status - " + userData.status);

        //getting profilePhotoUrl (private)
        userData.ProfilePhotoUrl = publicUserDataJsonObj["profilePhoto"].Value.ToString(); //Так как без Value ставит кавычки в начале и конце
    }
    public static void ParseLeaderboardData(string jsonData, out LeaderboardData leaderboardData)
    {
        Debug.Log($"Leaderboard data was get - {jsonData}. Starting parsing...");
        JSONNode leaderboardJsonObj = JSONNode.Parse(jsonData);

        List<UserData> users = new List<UserData>();
        foreach (var nextUserNode in leaderboardJsonObj)
        {
            UserData next = new UserData();

            UserProgressData upd = JsonUtility.FromJson<UserProgressData>(nextUserNode.Value["progressData"].ToString());
            StatisticsData sd = JsonUtility.FromJson<StatisticsData>(nextUserNode.Value["statistics"].ToString());
            next.updatePublicData(upd, sd);

            next.ProfilePhotoUrl = nextUserNode.Value["profilePhoto"].Value;

            next.setId(nextUserNode.Key);

            Debug.Log("[debug] Next leaderboard user was parsed. Result - " + next);

            users.Add(next);
        }

        leaderboardData = new LeaderboardData(users.ToArray());
    }
    public static void ParseFriendsList(string jsonData, out List<UserData> activeFriends, out List<UserData> expectedFriends, out List<UserData> wishingToBeFriends)
    {
        activeFriends = new List<UserData>();
        expectedFriends = new List<UserData>();
        wishingToBeFriends = new List<UserData>();

        JSONNode friendsJsonObj = JSONNode.Parse(jsonData);
        if (friendsJsonObj == null)
            return;


        foreach (var active in friendsJsonObj["active"])
            activeFriends.Add(new UserData(active.Key));

        foreach (var expected in friendsJsonObj["expected"])
            expectedFriends.Add(new UserData(expected.Key));

        foreach (var wishing in friendsJsonObj["wishing"])
            wishingToBeFriends.Add(new UserData(wishing.Key));
    }
    public static void ParseSessionsList(string jsonData, out Dictionary<string, List<SessionData>> sessions)
    {
        sessions = new Dictionary<string, List<SessionData>>();
        sessions.Add("active", new List<SessionData>());
        sessions.Add("expected", new List<SessionData>());
        sessions.Add("ended", new List<SessionData>());

        JSONNode sessionsJsonObj = JSONNode.Parse(jsonData);
        if (sessionsJsonObj == null) //no sessions
            return;

        foreach (var session in sessionsJsonObj)
        {   
            string type = session.Value;
            string id = session.Key;

            if (!sessions.ContainsKey(type)) //may come something like "error";
                continue;

            sessions[type].Add(new SessionData(id));
        }
        //Не удаляй пока
        //sessions = new List<SessionData>();

        //JSONNode sessionsJsonObj = JSONNode.Parse(jsonData);
        //if (sessionsJsonObj == null) //no sessions
        //    return;

        //foreach (var session in sessionsJsonObj["active"])
        //    sessions.Add(new SessionData(session.Key, "active"));
        //foreach (var session in sessionsJsonObj["expected"])
        //    sessions.Add(new SessionData(session.Key, "expected"));
        //foreach (var session in sessionsJsonObj["ended"])
        //    sessions.Add(new SessionData(session.Key, "ended"));
    }
    public static void ParseAllSessionsData(string jsonData, out List<SessionData> sessions)
    {
        sessions = new List<SessionData>();

        JSONNode sessionsJsonObj = JSONNode.Parse(jsonData);
        if (sessionsJsonObj == null) //no sessions
            return;


        foreach (var session in sessionsJsonObj)
        {
            ParseSessionData(session.ToString(), out SessionData sd);
            sessions.Add(sd);
        }
        return;

        foreach (var session in sessionsJsonObj["active"])
        {
            ParseSessionData(session.ToString(), out SessionData sd);
            sessions.Add(new SessionData(session.Key, "active"));
        }
            
        foreach (var session in sessionsJsonObj["expected"])
            sessions.Add(new SessionData(session.Key, "expected"));
        foreach (var session in sessionsJsonObj["ended"])
            sessions.Add(new SessionData(session.Key, "ended"));
    }
    public static void ParseSessionData(string jsonData, out SessionData sessionData)
    {
        sessionData = new SessionData();

        JSONNode sessionDataJsonObj = JSONNode.Parse(jsonData);

        //parsing main info
        sessionData.setId(sessionDataJsonObj["gameId"].Value.ToString());
        sessionData.setStatus(sessionDataJsonObj["status"].Value.ToString());

        string movingPlayer = sessionDataJsonObj["movingPlayer"].Value.ToString();
        if (movingPlayer == "null")
            movingPlayer = null;
        sessionData.setMovingPlayer(movingPlayer);

        //parsing rounds data
        List<RoundData> roundsList = new List<RoundData>();
        foreach (var roundNode in sessionDataJsonObj["rounds"])
        {
            //getting questions
            List<QuestionData> roundQuestions = new List<QuestionData>();
            foreach (var questionNode in roundNode.Value["questions"])
            {
                QuestionData qd = JsonUtility.FromJson<QuestionData>(questionNode.Value.ToString());
                roundQuestions.Add(qd);
            }

            roundsList.Add(new RoundData(roundQuestions));
        }
        sessionData.setRounds(roundsList.ToArray());

        Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
        foreach (var roundResultNode in sessionDataJsonObj["results"])
        {
            Dictionary<string, string> roundResults = new Dictionary<string, string>();
            foreach (var userResultNode in roundResultNode.Value)
            {
                roundResults.Add(userResultNode.Key, userResultNode.Value);
            }

            results.Add(roundResultNode.Key, roundResults);
        }
        sessionData.setRoundResultsData(results);

        //Пока не удаляй, может вернемся к этому варианту
        //parsing rounds results
        //List<RoundResult> roundsResultsList = new List<RoundResult>();
        //foreach (var roundResultNode in sessionDataJsonObj["results"])
        //{
        //    RoundResult rr = new RoundResult();
        //    rr.setId(roundResultNode.Key);
        //    foreach (var userResultNode in roundResultNode.Value)
        //    {
        //        rr.Add(userResultNode.Key, userResultNode.Value);
        //    }

        //    roundsResultsList.Add(rr);
        //}
        //sessionData.setRoundResultsData(roundsResultsList.ToArray());

        //parsing session result
        string winnerId = sessionDataJsonObj["sessionResult"]["winner"];
        Dictionary<string, int> userSessionResultDict = new Dictionary<string, int>();
        foreach (var node in sessionDataJsonObj["sessionResult"]["usersResults"])
        {
            //delete. "Winner" node not in the "usersResults" node!
            //if (node.Key == "winner")
            //{
            //    winnerId = node.Value;
            //    continue;
            //}

            userSessionResultDict.Add(node.Key, node.Value);
        }
        SessionResult sr = new SessionResult(winnerId, userSessionResultDict);
        sessionData.setSessionResult(sr);

        //parsing users data
        foreach (var userNode in sessionDataJsonObj["users"])
        {
            sessionData.addUser(userNode.Key);
        }

        //parsting session inviter
        sessionData.setGameInviter(sessionDataJsonObj["gameInviter"].Value);
    }
    public static void ParseSessionDataToExisting(string jsonData, ref SessionData sessionData)
    {
        JSONNode sessionDataJsonObj = JSONNode.Parse(jsonData);

        //parsing main info
        sessionData.setId(sessionDataJsonObj["gameId"].Value.ToString());
        sessionData.setStatus(sessionDataJsonObj["status"].Value.ToString());
        sessionData.setMovingPlayer(sessionDataJsonObj["movingPlayer"].Value.ToString());

        //parsing rounds data
        List<RoundData> roundsList = new List<RoundData>();
        foreach (var roundNode in sessionDataJsonObj["rounds"])
        {
            //getting questions
            List<QuestionData> roundQuestions = new List<QuestionData>();
            foreach (var questionNode in roundNode.Value["questions"])
            {
                QuestionData qd = JsonUtility.FromJson<QuestionData>(questionNode.Value.ToString());
                roundQuestions.Add(qd);
            }

            roundsList.Add(new RoundData(roundQuestions));
        }
        sessionData.setRounds(roundsList.ToArray());

        Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
        foreach (var roundResultNode in sessionDataJsonObj["results"])
        {
            Dictionary<string, string> roundResults = new Dictionary<string, string>();
            foreach (var userResultNode in roundResultNode.Value)
            {
                roundResults.Add(userResultNode.Key, userResultNode.Value);
            }

            results.Add(roundResultNode.Key, roundResults);
        }
        sessionData.setRoundResultsData(results);

        //old? Пока оставь
        //parsing rounds results
        //List<RoundResult> roundsResultsList = new List<RoundResult>();
        //foreach (var roundResultNode in sessionDataJsonObj["results"])
        //{
        //    RoundResult rr = new RoundResult();
        //    rr.setId(roundResultNode.Key);
        //    foreach (var userResultNode in roundResultNode.Value)
        //    {
        //        rr.Add(userResultNode.Key, userResultNode.Value);
        //    }

        //    roundsResultsList.Add(rr);
        //}
        //sessionData.setRoundResultsData(roundsResultsList.ToArray());

        sessionData.Users.Clear();
        //parsing users data
        foreach (var userNode in sessionDataJsonObj["users"])
        {
            sessionData.addUser(userNode.Key);
        }
    }
    public static void ParseRoundData(string jsonData, out RoundData roundData)
    {
        JSONNode roundDataJsonObj = JSONNode.Parse(jsonData);

        //getting questions
        List<QuestionData> questions = new List<QuestionData>();
        foreach (var questionNode in roundDataJsonObj["questions"])
        {
            QuestionData qd = JsonUtility.FromJson<QuestionData>(questionNode.Value.ToString());
            questions.Add(qd);
        }

        roundData = new RoundData(questions);
    }

    public static void ParseNotificationsData(string jsonData, out List<NotificationData> notifications)
    {
        notifications = new List<NotificationData>();

        SimpleJSON.JSONNode notificationsJsonObj = SimpleJSON.JSONNode.Parse(jsonData);
        if (notificationsJsonObj == null)
        {
            Debug.Log("No notification. Return");
            //return notifications; 
        }
        foreach (var notification in notificationsJsonObj)
        {
            //Debug.Log("notificationValue = " + notification.Value.ToString());
            //if (notificationPool == null) Debug.Log("pool is null");

            NotificationData n = ParseNotificationData(notification.Value.ToString());
            n.setKey(notification.Key);
            //NotificationData n = JsonUtility.FromJson<NotificationData>(notification.Value.ToString());
            //n.setKey(notification.Key);
            //n.setDate(new System.DateTime(notification.Value["date"].ToString()));
            

            Debug.Log("[debug] Parsed notification - " + n);
            notifications.Add(n);
            //notificationPool.Add(n);
        }

        //Debug.Log("[debug] notifications parsed: ");
        //Utils.CollectionUtils.ListToString<NotificationData>(notifications);

        //return notifications;
    }
    public static NotificationData ParseNotificationData(string json)
    {
        object deserialized = null;
        serializer.TryDeserialize(fsJsonParser.Parse(json), typeof(NotificationData), ref deserialized);
        return deserialized as NotificationData;
    }
    //public static NotificationData ParseNotificationData(string jsonData)
    //{
    //    SimpleJSON.JSONNode notificationJsonObj = SimpleJSON.JSONNode.Parse(jsonData);
    //    if (notificationJsonObj == null)
    //    {
    //        Debug.Log("No notification. Return");
    //        return null;
    //    }

    //    NotificationData notification = null;
    //    foreach (var notificationNode in notificationJsonObj) //refactor. Может есть другой способ взять ключ и значение
    //    {
    //        notification = JsonUtility.FromJson<NotificationData>(notificationNode.Value.ToString());
    //        notification.setKey(notificationNode.Key);
    //        break;
    //    }

    //    return notification;
    //}

}
