using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//Notes
//1. Don't instantiates new roundView. Just fills with content panels created in editor
public class TableResultView : MonoBehaviour
{
    public Sprite correctAnswerSprite;
    public Sprite wrongAnswerSprite;

    private Transform roundsContent;

    private Action onPlayNextRoundClicked;

    private GameObject userView;
    private GameObject opponentView;

    private UserData userData;
    private UserData opponentData;

    private SessionData sessionData;


    private void Start()
    {
        userView = transform.FindDeepChild("User").gameObject;
        opponentView = transform.FindDeepChild("Opponent").gameObject;

        roundsContent = transform.FindDeepChild("RoundsPanel");

        updateUsersView();
        updateScore();
        updateResultsView();
    }

    public void Init(Action onPlayNextRoundCallback, UserData user, UserData opponent, SessionData sessionData)
    {
        this.onPlayNextRoundClicked = onPlayNextRoundCallback;

        this.userData = user;
        this.opponentData = opponent;

        this.sessionData = sessionData;
    }
    public void UpdateView()
    {
        updateScore();
        updateResultsView();
        //updateUsersView();
        //updateActiveRoundView();
    }
    public void OnPlayNextRoundClicked()
    {
        Debug.Log("[debug] Table Result View. OnPlayNextRoundClicked()");
        onPlayNextRoundClicked.Invoke();
    }
    public void DisableBackButton()
    {
        Transform backButton = transform.FindDeepChild("BackButton");
        backButton.GetComponent<Button>().interactable = false;
        backButton.GetComponentInChildren<Text>().text = "Идет загрузка";
    }
    public void EnableBackButton() 
    {
        Transform backButton = transform.FindDeepChild("BackButton");
        backButton.GetComponent<Button>().interactable = true;
        backButton.GetComponentInChildren<Text>().text = "Назад";
    }

    private void updateUsersView()
    {
        userView.transform.FindDeepChild("Name").GetComponent<Text>().text = userData.FullName;
        opponentView.transform.FindDeepChild("Name").GetComponent<Text>().text = opponentData.FullName;

        userView.transform.FindDeepChild("ProfilePhoto").GetComponent<Image>().sprite = userData.ProfilePhoto;
        opponentView.transform.FindDeepChild("ProfilePhoto").GetComponent<Image>().sprite = opponentData.ProfilePhoto;
    }
    private void updateScore()
    {
        Text sessionScoreText = transform.FindDeepChild("SessionScore").GetComponentInChildren<Text>();
        sessionScoreText.text = sessionData.GetUserPoints(userData.Id) + "-" + sessionData.GetUserPoints(opponentData.Id);
    }
    private void updateResultsView()
    {
        Debug.Log("Updating result view...");
        //Dictionary<string, Dictionary<string, string>> results = sessionData.Results; //не нужно?
        
        //if (results == null)
        //    results = new RoundResult[0];

        int index = 0;
        foreach (Transform roundTransform in roundsContent)
        {
            initRoundView(roundTransform);
            string userRoundResult = sessionData.GetUserRoundResultById(index, userData.Id);
            string opponentRoundResult = sessionData.GetUserRoundResultById(index, opponentData.Id);

            //Game has active rounds and this round (round{index}) was played
            if (index < sessionData.ActiveRoundIndex)
            {
                UpdatePlayedRound(roundTransform, userRoundResult, opponentRoundResult);
            }
            //this is active round
            else if (index == sessionData.ActiveRoundIndex)
            {
                UpdateActiveRound(roundTransform, userRoundResult, opponentRoundResult);
            }
            //this is not played rounds
            else if (index > sessionData.ActiveRoundIndex)
            {
                UpdateUnplayedRound(roundTransform);
            }


            index++;
        }
    }
    private void UpdatePlayedRound(Transform round, string userResult, string opponentResult)
    {
        updateUserRoundResult(round.FindDeepChild("UserResult"), userResult);
        updateUserRoundResult(round.FindDeepChild("OpponentResult"), opponentResult);
    }
    //refactor. bad method name?
    private void UpdateUnplayedRound(Transform round)
    {
        round.Find("Result").gameObject.SetActive(false);
    }
    private void UpdateActiveRound(Transform round, string userResult, string opponentResult)
    {
        round.Find("Result").gameObject.SetActive(true);

        if(userResult == null && opponentResult == null && userData.Id == sessionData.MovingPlayer) //Никто не сделал ход. Ход user1 (возникает в начале раунда, {Ваш ход : ожидает/скрыто})
        {
            //Кнопка "Ваш ход"
            GameObject userTurnButton = round.FindDeepChild("UserTurn").gameObject;
            userTurnButton.SetActive(true);
            userTurnButton.GetComponent<Button>().onClick.AddListener(OnPlayNextRoundClicked);

            //Текст "Ожидает" или "скрыто"
            Transform opponentStatus = round.FindDeepChild("OpponentStatus");
            opponentStatus.GetComponentInChildren<Text>().text = "Ожидает";
            opponentStatus.gameObject.SetActive(true);
        }
        else if(userResult == null && opponentResult == null && opponentData.Id == sessionData.MovingPlayer) //Никто не сделал ход. Ход user2 (возникает в начале раунда или при игре оппонента в следующий раунд) (пусто : играет)
        {
            //Пусто. Отключить кнопку "Ваш ход"
            round.FindDeepChild("UserTurn").gameObject.SetActive(false);
            round.FindDeepChild("UserResult").FindDeepChild("Content").gameObject.SetActive(false);

            //Текст "играет"
            Transform opponentStatus = round.FindDeepChild("OpponentStatus");
            opponentStatus.GetComponentInChildren<Text>().text = "ИГРАЕТ";
            opponentStatus.gameObject.SetActive(true);
        }
        else if (userResult != null && opponentResult == null && opponentData.Id == sessionData.MovingPlayer) //User 1 сделал ход. Ход User 2
        {
            //Пусто. Отключить кнопку "Ваш ход"
            round.FindDeepChild("UserTurn").gameObject.SetActive(false);
            updateUserRoundResult(round.FindDeepChild("UserResult"), userResult);

            //Текст "играет"
            Transform opponentStatus = round.FindDeepChild("OpponentStatus");
            opponentStatus.GetComponentInChildren<Text>().text = "ИГРАЕТ";
            opponentStatus.gameObject.SetActive(true);
        }
        else if (userResult == null && opponentResult != null && userData.Id == sessionData.MovingPlayer) //User 2 сделал ход. Ход User 1
        {
            //Кнопка "Ваш ход"
            GameObject userTurnButton = round.FindDeepChild("UserTurn").gameObject;
            userTurnButton.SetActive(true);
            userTurnButton.GetComponent<Button>().onClick.AddListener(OnPlayNextRoundClicked);

            //Текст "ожидает"
            Transform opponentStatus = round.FindDeepChild("OpponentStatus");
            opponentStatus.GetComponentInChildren<Text>().text = "Ожидает";
            opponentStatus.gameObject.SetActive(true);
        }
    }
    //private void updateResultsView()
    //{
    //    Debug.Log("Updating result view...");
    //    RoundResult[] results = sessionData.Results;
    //    int index = 0;
    //    foreach (Transform round in roundsContent)
    //    {
    //        initRoundView(round);
    //        string userRoundResult = null; 
    //        string opponentRoundResult = null;  

    //        //there is played rounds
    //        if(index < sessionData.ActiveRoundIndex)
    //        {
    //            userRoundResult = results[index].GetResultOfUserByKey(userData.Id);
    //            opponentRoundResult = results[index].GetResultOfUserByKey(opponentData.Id);

    //            updateRoundAnswersSprites(round, userRoundResult, opponentRoundResult);
    //        }
    //        //this is active round
    //        if(index == sessionData.ActiveRoundIndex)
    //        {
    //            round.Find("Result").gameObject.SetActive(true);

    //            if (sessionData.ActiveRoundIndex == results.Length - 1)
    //            {
    //                userRoundResult = results[index].GetResultOfUserByKey(userData.Id);
    //                opponentRoundResult = results[index].GetResultOfUserByKey(opponentData.Id);
    //            }

    //            if (userRoundResult == null && sessionData.MovingPlayer == userData.Id) //Игрок еще не сделал ход. Его очередь (ваш ход : ожидает/скрыто)
    //            {
    //                //Кнопка "Ваш ход"
    //                GameObject userTurnButton = round.FindDeepChild("UserTurn").gameObject;
    //                userTurnButton.SetActive(true);
    //                userTurnButton.GetComponent<Button>().onClick.AddListener(OnPlayNextRoundClicked);

    //                //Текст "Ожидает" или "скрыто"
    //                Transform opponentStatus = round.FindDeepChild("OpponentStatus");
    //                string statusText = opponentRoundResult == null ? "ОЖИДАЕТ" : "СКРЫТО";
    //                opponentStatus.GetComponentInChildren<Text>().text = statusText;
    //                opponentStatus.gameObject.SetActive(true);
    //            }
    //            if (userRoundResult == null && sessionData.MovingPlayer == opponentData.Id) //Игрок еще не сделал ход. Очередь соперника (пусто : играет)
    //            {
    //                //Пусто. Отключить кнопку "Ваш ход"
    //                round.FindDeepChild("UserTurn").gameObject.SetActive(false);

    //                //Текст "играет"
    //                Transform opponentStatus = round.FindDeepChild("OpponentStatus");
    //                opponentStatus.GetComponentInChildren<Text>().text = "ИГРАЕТ";
    //                opponentStatus.gameObject.SetActive(true);
    //            }
    //            if(userRoundResult != null && sessionData.MovingPlayer == opponentData.Id) //Игрок сделал ход. Очередь соперника (результат : играет)
    //            {
    //                updateRoundAnswersSprites(round, userRoundResult, null);

    //                //Текст "играет"
    //                Transform opponentStatus = round.FindDeepChild("OpponentStatus");
    //                opponentStatus.GetComponentInChildren<Text>().text = "ИГРАЕТ";
    //                opponentStatus.gameObject.SetActive(true);
    //            }
    //            if(sessionData.Status == "ended") //игра завершена
    //            {
    //                Transform opponentStatus = round.FindDeepChild("OpponentStatus");
    //                opponentStatus.gameObject.SetActive(false);
                    
    //                updateRoundAnswersSprites(round, userRoundResult, opponentRoundResult);
    //            }
    //            //if(userRoundResult != null && opponentRoundResult != null)
    //            //{
    //            //    //Оба игрока сыграли
    //            //    updateRoundAnswersSprites(round, userRoundResult, opponentRoundResult);
    //            //}
    //        }
    //        //this is not played rounds
    //        if(index > sessionData.ActiveRoundIndex)
    //        {
    //            round.Find("Result").gameObject.SetActive(false);
    //        }


    //        index++;
    //    }
    //}
    //private void updateResultsView()
    //{
    //    RoundResult[] results = sessionData.Results;
    //    int index = 0;
    //    foreach (Transform round in roundsContent)
    //    {
    //        if(index == results.Length - 1) //active round
    //        {
    //            if(sessionData.MovingPlayer == userData.Id)
    //            {
    //                GameObject userTurnButton = round.FindDeepChild("UserTurn").gameObject;
    //                userTurnButton.SetActive(true);
    //                userTurnButton.GetComponent<Button>().onClick.AddListener(OnPlayNextRoundClicked);

    //                Transform opponentStatus = round.FindDeepChild("OpponentStatus");
    //                string statusText = sessionData.ActiveRoundResult.GetResultOfUserByKey(opponentData.Id) == null ? "СКРЫТО" : "ОЖИДАЕТ";
    //                opponentStatus.GetComponentInChildren<Text>().text =  statusText;
    //                opponentStatus.gameObject.SetActive(true);
    //            }
    //            else
    //            {
    //                round.FindDeepChild("UserTurn").gameObject.SetActive(false);

    //                string userResult = results[index].GetResultOfUserByKey(userData.Id);
    //                updateRoundAnswersSprites(round, userResult);

    //                Transform opponentStatus = round.FindDeepChild("OpponentStatus");
    //                opponentStatus.GetComponentInChildren<Text>().text = "ИГРАЕТ";
    //                opponentStatus.gameObject.SetActive(true);
    //            }
    //            //edit. Здесь кнопка Ваш ход или "Ожидайте"
    //            index++;
    //            continue;
    //        }

    //        if(index > results.Length)
    //        {
    //            round.Find("Result").gameObject.SetActive(false);
    //            continue;
    //        }

    //        string userRoundResult = results[index].GetResultOfUserByKey(userData.Id);
    //        string opponentRoundResult = results[index].GetResultOfUserByKey(opponentData.Id);
    //        updateRoundAnswersSprites(round, userRoundResult, opponentRoundResult);
    //        index++;
    //    }
    //}

    //private void updateUserRoundResult(Transform roundView, string resultValue)
    //{
    //    Transform resultView = roundView.FindDeepChild("")
    //}

    private void updateUserRoundResult(Transform resultView, string result)
    {
        resultView.gameObject.SetActive(true);

        if (result == null)
            return;

        for (int i = 0; i < resultView.GetChild(0).childCount; i++)
        {
            char questionResult = result[i];
            Image roundResultImage = resultView.GetChild(0).GetChild(i).GetComponent<Image>();
            roundResultImage.sprite = (questionResult == '1' ? correctAnswerSprite : wrongAnswerSprite);
        }
    }
    //private void updateRoundAnswersSprites(Transform roundView, string userRoundResult, string opponentRoundResult)
    //{
    //    //Transform roundView = roundsContent.FindDeepChild("Round" + roundViewIndex);

    //    Transform userRoundResultView = roundView.FindDeepChild("UserResult");
    //    Transform opponentRoundResultView = roundView.FindDeepChild("OpponentResult");

    //    userRoundResultView.gameObject.SetActive(true);
    //    opponentRoundResultView.gameObject.SetActive(true);

    //    int index = 0;
    //    if (userRoundResult != null)
    //    {
    //        foreach (Transform roundResultImage in userRoundResultView.GetChild(0))
    //        {
    //            char questionResult = userRoundResult[index];
    //            roundResultImage.GetComponent<Image>().sprite = (questionResult == '1' ? correctAnswerSprite : wrongAnswerSprite);
    //            index++;
    //        }
    //    }

    //    index = 0;
    //    if (opponentRoundResult != null)
    //    {
    //        foreach (Transform roundResultImage in opponentRoundResultView.GetChild(0))
    //        {
    //            char questionResult = opponentRoundResult[index];
    //            roundResultImage.GetComponent<Image>().sprite = (questionResult == '1' ? correctAnswerSprite : wrongAnswerSprite);
    //            index++;
    //        }
    //    }
            
        
    //}
    private void initRoundView(Transform roundView)
    {
        roundView.Find("Result").gameObject.SetActive(true);

        roundView.FindDeepChild("UserResult").gameObject.SetActive(true);
        roundView.FindDeepChild("UserTurn").gameObject.SetActive(false);        

        roundView.FindDeepChild("OpponentResult").gameObject.SetActive(true);
        roundView.FindDeepChild("OpponentStatus").gameObject.SetActive(false); 
    }

    
}
