
using Proyecto26;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SessionViewController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject OpponentSearchPanel;
    [SerializeField] private GameObject OpponentFoundPanel;
    //[SerializeField] private GameObject OpponentNotFoundPanel;

    [SerializeField] private UserProfileInfoView userInfoView;
    [SerializeField] private UserProfileInfoView opponentInfoView;
    [SerializeField] private Text infoText;

    private Button cancelButton;

    private void Start()
    {
        cancelButton = OpponentSearchPanel.transform.FindDeepChild("CancelButton").GetComponent<Button>();
    }

    public void InitSearchingView()
    {
        OpponentSearchPanel.SetActive(true);
        OpponentFoundPanel.SetActive(false);
        //OpponentNotFoundPanel.SetActive(false);
        hideInfoMessage();
    }
    public void InitGameWithOpponentView()
    {
        OpponentSearchPanel.SetActive(false);
        OpponentFoundPanel.SetActive(true);
        //OpponentNotFoundPanel.SetActive(false);
        showInfoMessage("ИДЕТ СОЗДАНИЕ ИГРЫ...");
    }
    public void ShowPseudoRandomOpponentPanel()
    {
        //OpponentNotFoundPanel.SetActive(true);
    }
    public void UpdateOpponentFoundView(UserData user, UserData opponent)
    {
        Debug.Log("[debug] Updating profiles view...");

        userInfoView.LoadInfo(user.ProfilePhoto, user.ProgressData.Name, user.ProgressData.Surname, user.Statistics.WinCount);

        if (opponent != null)
            opponentInfoView.LoadInfo(opponent.ProfilePhoto, opponent.ProgressData.Name, opponent.ProgressData.Surname, opponent.Statistics.WinCount);
        //else
        //    initPseudoRandomOpponentInfo();
    }
    //private void initPseudoRandomOpponentInfo()
    //{
    //    Sprite profilePhoto = Resources.Load<Sprite>("PseudoRandomOpponent");
    //    string name = "Неизвестный";
    //    string surname = "Соперник";
    //    int winCount = 0;

    //    opponentInfoView.LoadInfo(profilePhoto, name, surname, winCount);
    //}
    public void ShowPreparationForTheStart(Action onAnimationShown)
    {
        hideInfoMessage();
        StartCoroutine(animatePreparationForTheStart(onAnimationShown));
    }
    public void BlockCancelation()
    {
        cancelButton.interactable = false;
    }

    private void showInfoMessage(string message)
    {
        infoText.text = message;
        infoText.gameObject.SetActive(true);
    }
    private void hideInfoMessage()
    {
        infoText.gameObject.SetActive(false);
    }

    private IEnumerator animatePreparationForTheStart(Action onAnimationShown)
    {
        Debug.Log("[debug] starting round at 3...2...1...");

        GameObject roundStartingInTextGO = OpponentFoundPanel.transform.Find("RoundStartingIn").gameObject;
        roundStartingInTextGO.SetActive(true);

        Text roundStartingInText = roundStartingInTextGO.GetComponent<Text>();
        roundStartingInText.text = "3";
        yield return new WaitForSeconds(1f);
        roundStartingInText.text = "2";
        yield return new WaitForSeconds(1f);
        roundStartingInText.text = "1";
        yield return new WaitForSeconds(1f);

        onAnimationShown.Invoke();
    }
}
