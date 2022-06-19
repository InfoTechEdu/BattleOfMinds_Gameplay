
using FullSerializer;
using System.Collections.Generic;
using System.Linq;

//Refactor
//1. Мне кажется есть проблемы с отслеживанием максимального количества раундов. И сложно поменять динамически

[System.Serializable]
public class SessionData
{
    public string gameId;
    public string status; //active, waiting, ended (need running and created? we can check this with checking "rounds" info)
    [fsProperty] private string movingPlayer;

    [fsProperty] private Dictionary<string, bool> users;
    [fsProperty] private string gameInviter;
    [fsProperty] private SessionResult sessionResult;
    [fsProperty] private Dictionary<string, RoundData> rounds;
    //[fsProperty] private Dictionary<string, RoundResult> results; //refactor. make sorted dictionary?
    [fsProperty] private Dictionary<string, Dictionary<string, string>> results; //refactor. bad code
    [fsProperty] public Dictionary<string, bool> test;

    private int maxRoundsCount = 6;

    public string Id { get => gameId;}
    public string Status { get => status; }
    public Dictionary<string, bool> Users { get => users;}
    public int ActiveRoundIndex { get => rounds.Count - 1; }
    //public RoundData ActiveRoundData { get => rounds[rounds.Count - 1]; }
    public RoundData ActiveRoundData { get => GetRoundData(rounds.Count - 1); }
    public string MovingPlayer { get => movingPlayer;}
    //public RoundResult[] Results { get => results?.Values.ToArray(); }

    public SessionResult SessionResult { get => sessionResult;  }
    public string SessionWinner { get => sessionResult.Winner; }
    public RoundData[] Rounds { get => rounds.Values.ToArray(); }
    public string GameInviter { get => gameInviter; set => gameInviter = value; }
    public Dictionary<string, Dictionary<string, string>> Results { get => results; set => results = value; }

    public SessionData(string id, string status)
    {
        this.gameId = id;
        this.status = status;

        users = new Dictionary<string, bool>();
        rounds = new Dictionary<string, RoundData>();
        //rounds = new RoundData[0];
    }

    public SessionData()
    {
        users = new Dictionary<string, bool>();
        rounds = new Dictionary<string, RoundData>();
        //rounds = new RoundData[0];
    }

    public SessionData(string id)
    {
        this.gameId = id;

        users = new Dictionary<string, bool>();
        rounds = new Dictionary<string, RoundData>();
        //rounds = new RoundData[0];
    }

    //public string[] users; //delete. old code
    //public string invitingUser; //delete. old code
    //public Dictionary<string, int> result; //delete. old code

    //public string winner; //delete. but maybe will be saved
    public string GetOpponentId(string userId)
    {
        foreach (var id in Users.Keys)
        {
            if (userId != id)
            {
                return id;
            }
        }

        return null;
    }
    public int GetUserPoints(string id)
    {
        if (sessionResult == null)
            return 0;

        return sessionResult.GetUserResultById(id);
    }
    public string GetUserRoundResultById(int roundIndex, string id)
    {
        if (results == null)
            return null;

        if (results.TryGetValue("round" + roundIndex, out Dictionary<string, string> roundResults))
            if(roundResults.TryGetValue(id, out string userResult))
                return userResult;

        //if (results.TryGetValue("round" + roundIndex, out RoundResult result))
        //    return result.GetResultOfUserByKey(id);

        return null;
    }
    public Dictionary<string, string> GetActiveRoundResults() //danger. returns copy 
    {
        string key = "round" + ActiveRoundIndex;
        results.TryGetValue(key, out Dictionary<string, string> roundResults);
        return roundResults;
    }

    public void CopyFrom(SessionData data)
    {
        setId(data.gameId);
        setStatus(data.status);
        setMovingPlayer(data.movingPlayer);
        setRounds(data.Rounds);
        setRoundResultsData(data.Results);
        setUsers(data.users);
        setGameInviter(data.GameInviter);
        setSessionResult(data.sessionResult);
    }

    public void setId(string id)
    {
        this.gameId = id;
    }

    public void setStatus(string status)
    {
        this.status = status;
    }

    public void setMovingPlayer(string playerId)
    {
        movingPlayer = playerId;
    }

    public void setRounds(RoundData[] rounds)
    {
        this.rounds.Clear();
        foreach (var round in rounds)
        {
            addNewRoundData(round);
        }

        //this.rounds = rounds; //danger
    }
    public void setRoundResultsData(Dictionary<string, Dictionary<string, string>> results)
    {
        this.results = results;

        //if (this.results == null)
        //    this.results = new Dictionary<string, Dictionary<string, string>>();

        //for (int i = 0; i < results.Length; i++)
        //{
        //    this.results.Add(results[i].Id, results[i].UsersResult);

        //    //if(this.results.TryGetValue(results[i].Id, out Dictionary<string, string> users))
        //    //{
        //    //    this.results[results[i].Id] = users;
        //    //}
        //    //this.results.Add("round" + i, results[i]);
        //}

        //this.results = results;
    }

    public void setUsers(Dictionary<string, bool> users)
    {
        this.users = users; //danger
    }
    public void setGameInviter(string gameInviter)
    {
        this.gameInviter = gameInviter;
    }
    public void setSessionResult(SessionResult result)
    {
        this.sessionResult = result;
    }
    public void setSessionWinner(string winnerId)
    {
        sessionResult.winner = winnerId;
    }

    public void addUser(string userId)
    {
        users.Add(userId, true);
    }
    public void addUserResultToActiveRound(string userId, string userResult)
    {
        if (ActiveRoundIndex + 1 > maxRoundsCount)
            return;

        if (results == null)
            results = new Dictionary<string, Dictionary<string, string>>();

        results.TryGetValue("round" + ActiveRoundIndex, out Dictionary<string, string> roundResults);
        if (roundResults != null)
            roundResults.Add(userId, userResult);
        else
            results.Add("round" + ActiveRoundIndex, new Dictionary<string, string>() { {userId, userResult} });
            
        //RoundResult rr = new RoundResult();
        //rr.Add(userId, userResult);

        //results.Add("round" + ActiveRoundIndex, );

        //KeyValuePair<string, string> opponentResult = results[ActiveRoundIndex].UsersResult.FirstOrDefault();
        //if (ActiveRoundIndex + 1 > this.results.Length)
        //{
        //    var resultsList = new List<RoundResult>();
        //    resultsList.AddRange(this.results);

        //    RoundResult rr = new RoundResult();
        //    rr.Add(userId, userResult);
        //    resultsList.Add(rr);

        //    this.results = resultsList.ToArray();
        //}
        //else
        //{
        //    results.Add
        //    results[ActiveRoundIndex].Add(userId, userResult);
        //}        
    }
    public void addNewRoundData(RoundData newRound)
    {
        string key = "round" + rounds.Count;
        rounds.Add(key, newRound);

        //var roundsList = new List<RoundData>();
        //roundsList.AddRange(rounds);
        //roundsList.Add(newRound);

        //rounds = roundsList.ToArray();
    }


    public void recalculateUserSessionResult(string userId)
    {
        if (sessionResult == null)
            sessionResult = new SessionResult();

        int score = 0;
        foreach (var roundResult in results)
        {
            if(roundResult.Value.TryGetValue(userId, out string value))
            {
                foreach (var nextChar in value) if (nextChar == '1') score++;
            }
        }

            //if (roundResult.Value.GetResultOfUserByKey(userId) != null)
            //    score += roundResult.Value.GetIntResultOfUserByKey(userId);

        sessionResult.UsersResults[userId] = score;
    }
    public void updateUserSessionResult(string userId, int points)
    {
        sessionResult.UsersResults[userId] = points;
    }
    public void updateSessionWinner(string winnerId)
    {
        sessionResult.setWinner(winnerId);
        UnityEngine.Debug.Log("Session winner updated. Id - " + winnerId);
    }
    //public UserSessionData getUserSessionData(int index)
    //{
    //    return users[index];
    //}

    private RoundData GetRoundData(int index)
    {
        string key = "round" + index;
        if (rounds.TryGetValue(key, out RoundData data))
            return data;

        return null;
    }
    public override string ToString()
    {
        //delete. old variant
        //return string.Format("Sessiond Data : [id = {0}, status = {1}, users = {2}, winner = {3}]", 
        //    gameId, status, Utils.CollectionUtils.ArrayToString(users), winner);

        return $"SessionData : [id = {gameId}, movingPlayer = {movingPlayer}, activeRound = {ActiveRoundIndex}]";
    }
}



