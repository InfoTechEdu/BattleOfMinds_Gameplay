using FullSerializer;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundData
{
    [fsProperty] private string id;
    [fsProperty] private Dictionary<string, QuestionData> questions;

    [fsIgnore] public int RoundIndex { get => System.Convert.ToInt32(id[id.Length - 1]); }
    [fsIgnore] public string Id { get => id; set => id = value; }

    public RoundData()
    {
        questions = new Dictionary<string, QuestionData>();
    }

    
    public RoundData(List<QuestionData> roundQuestions)
    {
        questions = new Dictionary<string, QuestionData>();
        foreach (var q in roundQuestions)
            AddQuestion(q);

        //old
        //this.questions = roundQuestions;
    }

    public QuestionData GetQuestionByIndex(int index)
    {
        string key = "question" + index;
        if (questions.TryGetValue(key, out QuestionData data))
            return data;

        return null;
        //return questions[index];
    }

    private void AddQuestion(QuestionData question)
    {
        string key = "question" + questions.Count;
        questions.Add(key, question);
    }

    public override string ToString()
    {
        return $"Round data : [ { Utils.CollectionUtils.DictionaryToString(questions) } ]";
    }
}