using FullSerializer;

[System.Serializable]
public class UserProgressData
{
    public string name;
    public string surname;
    public int points;

    public UserProgressData(string name, string surname, int score)
    {
        this.name = name;
        this.surname = surname;
        this.points = score;
    }

    public UserProgressData(int points)
    {
        this.points = points;
    }

    [fsIgnore] public string Name { get => name; set => name = value; }
    [fsIgnore] public string Surname { get => surname; set => surname = value; }
    [fsIgnore] public int Points { get => points; set => points = value; }
    

    public override string ToString()
    {
        return $"[name - {name}, points - {points}]";
    }
}
