

[System.Serializable]
public class StatisticsData
{
    public int gamesPlayed;
    public int winCount;

    public StatisticsData(int gamesPlayed, int winCount)
    {
        this.gamesPlayed = gamesPlayed;
        this.winCount = winCount;
    }
    public StatisticsData()
    {

    }

    public int GamesPlayed { get => gamesPlayed; private set => gamesPlayed = value; }
    public int WinCount { get => winCount; set => winCount = value; }

    public override string ToString()
    {
        return string.Format("Statistics Data : [gamesPlayed - {0}]", gamesPlayed);
    }
}
