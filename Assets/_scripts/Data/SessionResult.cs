using System.Collections.Generic;

public class SessionResult
{
    public string winner;
    public Dictionary<string, int> usersResults;

    public SessionResult(string winner, Dictionary<string, int> usersResults)
    {
        this.winner = winner;
        this.usersResults = usersResults;
    }

    public SessionResult()
    {
        usersResults = new Dictionary<string, int>();
    }

    public int GetUserResultById(string id)
    {
        if (id != null && usersResults.ContainsKey(id))
            return usersResults[id];
        else
            return 0;
    }
    public void setWinner(string winner)
    {
        this.winner = winner;
    }



    public string Winner { get => winner; }
    public Dictionary<string, int> UsersResults { get => usersResults; }


    /* example 
     * "sessionsResult" : {
     *  "winner" : "user1",
     *  "user1" : 7,
     *  "user2" : 5
     * }
     */

}
