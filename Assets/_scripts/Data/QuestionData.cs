
[System.Serializable]
public class QuestionData
{
    public string questionText;
    public string questionFact;
    public AnswerData[] answers;

    public string QuestionFact { get => questionFact; set => questionFact = value; }

    public QuestionData(string questionText, AnswerData[] answers)
    {
        this.questionText = questionText;
        this.answers = answers;
    }

    public QuestionData()
    {
    }

    public void setQuestionText(string questionText)
    {
        this.questionText = questionText;
    }

    public void setAnswers(AnswerData[] answers)
    {
        this.answers = answers;
    }
    public override string ToString()
    {
        return string.Format("QuestionData : text - {0}, fact - {1}, answers - {2}", 
            questionText, questionFact, Utils.CollectionUtils.ArrayToString(answers));
    }
}
