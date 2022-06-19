using FullSerializer;
using System.Collections.Generic;

public class RoundResult 
{
    [fsProperty] private string id; //example "round3"
    [fsProperty] private Dictionary<string, string> usersResults; //example "userKey" : "101"

    public int RoundIndex { get => System.Convert.ToInt32(id[id.Length - 1]); }    
    public string Id { get => id; }
    public Dictionary<string, string> UsersResult { get => usersResults; set => usersResults = value; }

    public RoundResult()
    {
        usersResults = new Dictionary<string, string>();
    }

    public RoundResult(string id, Dictionary<string, string> usersResults)
    {
        this.id = id;
        this.usersResults = usersResults;
    }

    public void setId(string id)
    {
        this.id = id;
    }

    public string GetResultOfUserByKey(string key)
    {
        if (key == null)
            return null;

        if (usersResults.ContainsKey(key))
            return usersResults[key];
        else
            return null;
    }

    public int GetIntResultOfUserByKey(string key)
    {
        if (key == null)
            return 0;

        string resultString;

        if (usersResults.ContainsKey(key))
            resultString = usersResults[key];
        else
            resultString = null;

        if (resultString == null)
            return 0;

        int score = 0;
        foreach (var nextChar in resultString)
            if (nextChar == '1')
                score++;

        return score;
    }

    public void Add(string userId, string result)
    {
        this.usersResults.Add(userId, result);
    }
}
