using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Utils;
using System.Text;
using System;

public class GameController : MonoBehaviour
{
    public Text questionDisplayText;

    public GameObject questionDisplay;
    //public GameObject categoryPanel; //edit. Uncomment in newest releases
    //public GameObject roundEndDisplay;

    public TimerView questionTimerView;

    public TableResultView tableResultView;

    public GameEndView gameEndDisplay;

    public AnswerButton[] answerButtons;
    private AnswerButton correctAnswerButton;

    private DataController dataController;
    private RoundData currentRoundData;

    private bool isRoundActive;
    private float timeRemaining;
    private int activeQuestionIndex; //1,2 or 3
    private int roundScore; //1,2 or 3 points
    private string roundResult = ""; // "001" or "111"

    float roundEndDelay = 3f;

    //private string selectedCategory; //edit. Uncomment in newest releases

    // Use this for initialization
    void Start()
    {
        dataController = FindObjectOfType<DataController>();

        dataController.GetStatusEndGame();

        currentRoundData = dataController.GetActiveRoundData();

        questionTimerView.Init(GameConstants.QUESTION_TIME_LIMIT);
        tableResultView.Init(OnNextRoundClicked, dataController.GetUserData(), dataController.GetOpponentData(), dataController.GetCurrentSessionData());
        gameEndDisplay.Init();

        InitGameView();
    }
    // Update is called once per frame
    void Update()
    {
        if (isRoundActive)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimeRemainingDisplay();

            if (timeRemaining <= 0f)
            {
                OnQuestionTimeExpired();
            }
        }
    }

    private void InitGameView()
    {
        ShowResultTable();
        gameEndDisplay.Hide();
    }

    
    public void OnAnswered(bool isCorrect)
    {
        isRoundActive = false;
        DisableAnswersButtons();
        //if (!isRoundActive)
        //    isRoundActive = true;

        activeQuestionIndex++;

        if (isCorrect)
            OnAnswerCorrect();
        else
            OnAnswerWrong();

        if (!IsLastQuestionInRound())
        {
            ShowNextQuestionButton();
            return;
        }

        dataController.SaveRoundResult(roundResult);
        dataController.UpdateUserSessionResult();

        if (OpponentPlayedCurrentRound())
        {
            if (IsRoundLast()) //User and opponent played all rounds
            {
                tableResultView.UpdateView();
                tableResultView.DisableBackButton();

                Invoke("HideQuestion", 2f);
                Invoke("ShowResultTable", 2f);
                Invoke("EndGame", 2f);
            }
            else //игра продолжается. добавить раунд
            {
                AddNewRoundAndShowTable();

                roundResult = "";
                activeQuestionIndex = 0;
            }

            return;
        }

        PassTurnToOpponent();
        //PassTurnToOpponent((success)=>
        //{
        //    if (success)
        //    {
        //        HideQuestion();
        //        ShowResultTable();
        //    }
        //});
        tableResultView.UpdateView();

        //old?
        //Invoke("HideQuestion", 2f);
        //Invoke("ShowResultTable", 2f);

        if (dataController.GetOpponentData().Id == null) //refactor
            dataController.UpdateStatusInWishingToPlayNode("played");

        return;

        //old?
        //if (!OpponentPlayedCurrentRound())
        //{
        //    PassTurnToOpponent();
        //    tableResultView.UpdateView();

        //    Invoke("HideQuestion", 2f);
        //    Invoke("ShowResultTable", 2f);

        //    if (dataController.GetOpponentData().Id == null) //refactor
        //        dataController.UpdateStatusInWishingToPlayNode("played");
        //}
        //else
        //{

        //}

        ////if(IsLastQuestionInRound() && OpponentPlayedCurrentRound())
        ////{

        ////}
        ////else if (IsLastQuestionInRound() && OpponentPlayedCurrentRound())
        ////{

        ////}else if(!IsLastQuestionInRound())

        //if(IsLastQuestionInRound())
        //{
        //    dataController.SaveRoundResult(roundResult);
        //    dataController.UpdateUserSessionResult();
            
        //    if (!OpponentPlayedCurrentRound())
        //    {
        //        PassTurnToOpponent();
        //        tableResultView.UpdateView();

        //        Invoke("HideQuestion", 2f);
        //        Invoke("ShowResultTable", 2f);

        //        if (dataController.GetOpponentData().Id == null) //refactor
        //            dataController.UpdateStatusInWishingToPlayNode("played");
        //    }
        //    else //user продолжает играть или конец игры
        //    {
        //        if(IsRoundLast()) //User and opponent played all rounds
        //        {
        //            tableResultView.UpdateView();

        //            Invoke("HideQuestion", 2f);
        //            Invoke("ShowResultTable", 2f);
        //            Invoke("EndGame", 2f);
        //        }
        //        else //игра продолжается. добавить раунд
        //        {
        //            AddNewRound(()=>
        //            {
        //                tableResultView.UpdateView();
        //                HideQuestion();
        //                ShowResultTable();
        //            });
                    
        //        }
        //    }


        //    roundResult = "";
        //    activeQuestionIndex = 0;
        //    return;
        //}

        //ShowNextQuestionButton();
    }
    public void OnBackButtonClicked()
    {
        SceneManager.LoadScene("MenuScreen");
    }
    public void OnNextQuestionButtonClicked()
    {
        UpdateTimeRemaining();
        ShowQuestion();

        isRoundActive = true;
    }
    private void OnNextRoundClicked()
    {
        Debug.LogWarning("On next round clicked");
        HideResultTable();

        ShowCategorySelectionPanel(); //edit. update in newest releases
    }
    private void OnAnswerCorrect()
    {
        roundScore++;
        roundResult += "1";
    }
    private void OnAnswerWrong()
    {
        roundResult += "0";

        ShowCorrectAnswer();
    }
    private bool IsRoundLast()
    {
        return dataController.GetActiveRoundIndex() == (GameConstants.MAX_SESSION_ROUNDS - 1);
    }
    private bool IsLastQuestionInRound()
    {
        return activeQuestionIndex >= 3;
    }
    private bool OpponentPlayedCurrentRound()
    {
        return dataController.GetOpponentsActiveRoundResult() != null;
    }


    float addRoundCalledAt;
    private void AddNewRoundAndShowTable()
    {
        addRoundCalledAt = Time.time;
        AddNewRound(OnNewRoundAdded);
    }
    private void OnNewRoundAdded()
    {
        StartCoroutine(UpdateRoundDataAndShowTable());
    }
    private IEnumerator UpdateRoundDataAndShowTable()
    {
        while (Time.time - addRoundCalledAt < roundEndDelay)
        {
            //Debug.LogError($"Time.time - {Time.time}. Diff - {Time.time - addRoundCalledAt}");
            yield return null;
        }
            

        currentRoundData = dataController.GetActiveRoundData();
        tableResultView.UpdateView();
        HideQuestion();
        ShowResultTable();
    }


    float turnPassStartedAt;
    private void PassTurnToOpponent()
    {
        turnPassStartedAt = Time.time;
        dataController.UpdateCurrentSessionMovingPlayer(dataController.GetOpponentData().Id, OnTurnPassed);

        //check is round is last nad opponent player this round

        //if (currentQuiz.getCurrentRoundIndex() < currentQuiz.getRoundsCount() - 1)
        //{
        //    currentQuiz.nextRound();
        //    currentRoundData = currentQuiz.GetCurrentRoundData();
        //    ShowQuestion();
        //}
        //else
        //{
        //    EndGame(true);
        //}
    }

    private void OnTurnPassed(bool success)
    {
        if(success)
            StartCoroutine(WaitRoundEndDelayAndShowTable());
    }
    private IEnumerator WaitRoundEndDelayAndShowTable()
    {
        while (Time.time - turnPassStartedAt < roundEndDelay)
        {
            yield return null;
        }

        HideQuestion();
        ShowResultTable();
    }
    private void ShowCategorySelectionPanel() //edit. update in newest releases
    {
        //edit. Update in newer versions
        //categoryPanel.SetActive(true);
        //StartCoroutine(AnimateCategorySelectionTemp(()=>
        //{
        //    ShowQuestion();
        //    UpdateTimeRemaining();
        //    UpdateTimeRemainingDisplay();
        //    //categoryPanel.SetActive(false);

        //    isRoundActive = true;
        //}));


        EnableQuestionDisplay();
        DisableAnswersButtons();
        ClearAnswersButtons();
        ResetTimeRemainingSlider();

        //DisableAnswersButtons();

        UpdateRoundText();
        StartCoroutine(AnimateQuestionSelection(()=>
        {
            ShowQuestion();
            ShowAnswersButtons();
            EnableAnswersButtons();
            UpdateTimeRemaining();
            UpdateTimeRemainingDisplay();

            isRoundActive = true;
        }));
    }
    private IEnumerator AnimateQuestionSelection(Action onAnimated)
    {
        float animTime = 2.5f;
        float startedAt = Time.time;
        int pseudoRandomIndex = -1;
        while (Time.time - startedAt < animTime)
        {
            pseudoRandomIndex = UnityEngine.Random.Range(0, questionsForAnimation.Length-1);
            questionDisplayText.text = questionsForAnimation[pseudoRandomIndex];
            yield return new WaitForSeconds(0.3f);
        }

        onAnimated.Invoke();
    }
    //edit. Update in newer versions
    //private IEnumerator AnimateCategorySelectionTemp(Action onAnimated)
    //{
    //    Text cateforyText = categoryPanel.transform.FindDeepChild("CategoryText").GetComponent<Text>();
    //    float animTime = 2.5f;
    //    float startedAt = Time.time;
    //    int pseudoRandomIndex = -1;
    //    while(Time.time - startedAt < animTime)
    //    {
    //        pseudoRandomIndex = UnityEngine.Random.Range(0, GameConstants.CATEGORIES.Length);
    //        cateforyText.text = GameConstants.CATEGORIES[pseudoRandomIndex];
    //        yield return new WaitForSeconds(0.3f);
    //    }

    //    selectedCategory = GameConstants.CATEGORIES[pseudoRandomIndex];
    //    onAnimated.Invoke();
    //}

    private void DisableAnswersButtons()
    {
        foreach (var ab in answerButtons)
        {
            ab.Disable();
        }
    }
    private void EnableAnswersButtons()
    {
        foreach (var ab in answerButtons)
        {
            ab.Enable();
        }
    }
    private void EnableQuestionDisplay()
    {
        if (!questionDisplay.activeInHierarchy)
            questionDisplay.SetActive(true);
    }
    private void ShowAnswersButtons()
    {
        foreach (var ab in answerButtons)
        {
            ab.gameObject.SetActive(true);
        }
    }
    private void HideAnswersButtons()
    {
        foreach (var ab in answerButtons)
        {
            ab.gameObject.SetActive(false);
        }
    }
    private void ClearAnswersButtons()
    {
        foreach (var ab in answerButtons)
        {
            ab.Clear();
        }
    }
    private void ShowQuestion()
    {
        HideNextQuestionButton();

        UpdateRoundText(); //bad code. refactor

        QuestionData questionData = currentRoundData.GetQuestionByIndex(activeQuestionIndex);
        questionDisplayText.text = questionData.questionText;

        for (int i = 0; i < questionData.answers.Length; i++)
        {
            AnswerButton answerButton = answerButtons[i].GetComponent<AnswerButton>();
            answerButton.Setup(questionData.answers[i]);

            if (questionData.answers[i].isCorrect)
                correctAnswerButton = answerButton;
        }

        EnableAnswersButtons();
    }
    private void HideQuestion()
    {
        HideNextQuestionButton();
        questionDisplay.SetActive(false);
    }
    private void ShowNextQuestionButton()
    {
        questionDisplay.transform.Find("NextQuestionButton").gameObject.SetActive(true);
    }
    private void UpdateRoundText()
    {
        questionDisplay.transform.Find("QuestionIndex").GetComponent<Text>().text = "Вопрос " + (activeQuestionIndex + 1);
    }
    private void HideNextQuestionButton()
    {
        questionDisplay.transform.Find("NextQuestionButton").gameObject.SetActive(false);
    }
    private void ShowCorrectAnswer()
    {
        correctAnswerButton.HighlightAsCorrect();
    }
    private void OnQuestionTimeExpired()
    {
        OnAnswered(false);
        //old?
        //OnAnswerWrong();
    }
    public void AddNewRound(Action callback)
    {
        Dictionary<string, object> @params = new Dictionary<string, object>();
        @params.Add("gameId", dataController.GetCurrentSessionData().Id);
        @params.Add("roundIndex", dataController.GetCurrentSessionData().ActiveRoundIndex + 1);
        @params.Add("userClass", dataController.GetUserData().UserClass);
        @params.Add("opponentClass", dataController.GetOpponentData().UserClass);

        FirebaseManager.Instance.Functions.CallCloudFunction("AddNewRoundToSessionBOM", @params, (data) =>
        {
            if(data.body == null || data.body == "null")
            {
                Debug.LogError($"Error while adding new round. Server response data is {data}");
            }
            else
            {
                DataParser.ParseRoundData(data.body, out RoundData newRound);
                dataController.GetCurrentSessionData().addNewRoundData(newRound);
            }

            callback.Invoke();
        }, (exception) =>
        {
            Debug.LogError($"Error while adding new round. Exception - {exception}");
        });
    }
    //private void DisableResultTableBackButton() => tableResultView.DisableBackButton();
    //private void EnableResultTableBackButton() => tableResultView.EnableBackButton();
    public void CallEndGame()
    {
        EndGame();
    }

    private void EndGame()
    {
        //chicking winner
        dataController.WaitUntilWinnerWasSet(() =>
        {
            string winnerId = dataController.GetCurrentSessionData().SessionWinner;
            string userId = dataController.GetUserId();

            if (winnerId == userId) //user winned
            {
                gameEndDisplay.UpdateView(GameResult.WIN, dataController.GetUserSesionResult(userId), dataController.GetUserPositionInLeaderboard(userId));
            }
            else if (winnerId == "noWinner") //draw
            {
                gameEndDisplay.UpdateView(GameResult.DRAW, dataController.GetUserSesionResult(userId), dataController.GetUserPositionInLeaderboard(userId));
            }
            else //user lost
            {
                gameEndDisplay.UpdateView(GameResult.LOSE, 0, dataController.GetUserPositionInLeaderboard(userId));
            }

            gameEndDisplay.Show();
            tableResultView.EnableBackButton();
        });

        

        //if (roundScore != 0)
        //{
        //    Debug.LogWarning("Debug. updatin data...");
        //    //dataController.UpdateUsersCapitalLocal(playerScore); //delete old code
        //    dataController.UpdateUserInLeaderboard();
        //    //dataController.UpdateLocalLeaderboard(); //delete this after adding online
        //}

        //roundEndCapitalLabel.text = "Ваш счет: " + dataController.GetUserCapital();

        //isRoundActive = false;

        //questionDisplay.SetActive(false);
        //roundEndDisplay.SetActive(true);

        //if (isWin)
        //{
        //    roundEndText.text = "Отличная игра! Ваш выигрыш составил: " + roundScore + "$";
        //}
        //else
        //{
        //    roundEndText.text = "К сожалению, вы проиграли... Ваш выигрыш составил: " + roundScore + "$";
        //}
    }


    private void UpdateTimeRemaining()
    {
        timeRemaining = GameConstants.QUESTION_TIME_LIMIT;
    }
    private void ResetTimeRemainingSlider()
    {
        questionTimerView.UpdateView(GameConstants.QUESTION_TIME_LIMIT);
    }
    private void UpdateTimeRemainingDisplay()
    {
        questionTimerView.UpdateView(timeRemaining);
    }
    private void ShowResultTable()
    {
        tableResultView.gameObject.SetActive(true);
    }
    private void HideResultTable()
    {
        tableResultView.gameObject.SetActive(false);
    }
    private void updateRoundResult(int playedRoundIndex, bool isAnswerCorrect)
    {
        
    }



    private string[] questionsForAnimation =
    {
        "Глава правительства Канады",
        "Исландия, Норвегия, Швеция, Финляндия и Дания относятся к субрегиону",
        "Лист какого дерева украшает канадский флаг",
        "Николай Коперник был",
        "Польша это государство",
        "Родиной шахмат является страна",
        "С помощью чего можно легко найти биномиальные коэффициенты",
        "Столица Канады",
        "Столица Польши",
        "Как отделяется придаточное предложение от главного в составе сложноподчинённого предложения",
        "Определите жанры художественного стиля речи",
        "Передача государственной (муниципальной) собственности в частную собственность называется"
    };

}

