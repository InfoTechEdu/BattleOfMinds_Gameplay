

[System.Serializable]
public class AnswerData
{
    public string answerText;
    public bool isCorrect;

    public AnswerData(string answerText, bool isCorrect)
    {
        this.answerText = answerText;
        this.isCorrect = isCorrect;
    }

    public override string ToString()
    {
        return string.Format("AnswerData : text - {0}, isCorrect - {1}", answerText, isCorrect);
    }
}
