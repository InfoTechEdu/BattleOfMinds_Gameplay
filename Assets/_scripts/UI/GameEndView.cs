using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum GameResult
{
    LOSE,
    DRAW,
    WIN
}
public class GameEndView : MonoBehaviour
{
    public Text pointsText;
    public Text raitingText;

    public Sprite winSprite;
    public Sprite drawSprite;
    public Sprite loseSprite;

    private Text headerText;
    private Text messageText;

    public void Init()
    {
        headerText = transform.FindDeepChild("Header").GetComponent<Text>();
        messageText = transform.FindDeepChild("Message").GetComponent<Text>();

        Hide();
    }
    public void UpdateView(GameResult result, int points, int positionInLeaderboard)
    {
        switch (result)
        {
            case GameResult.LOSE:
                headerText.text = "Ой! Вы проиграли!";
                messageText.text = "Заработано";

                transform.FindDeepChild("LoseWinImage").GetComponent<Image>().sprite = loseSprite;
                break;
            case GameResult.DRAW:
                headerText.text = "У вас ничья!";
                messageText.text = "Заработано";

                transform.FindDeepChild("LoseWinImage").GetComponent<Image>().sprite = drawSprite;
                break;
            case GameResult.WIN:
                headerText.text = "Поздравляем! Вы победили!";
                messageText.text = "Заработано";

                transform.FindDeepChild("LoseWinImage").GetComponent<Image>().sprite = winSprite;
                break;
            default:
                break;
        }

        pointsText.text = points.ToString();
        //raitingText.text = positionInLeaderboard.ToString(); //edit update in next release
    }
    public void Show()
    {
        transform.SetAsLastSibling();
    }
    public void Hide()
    {
        transform.SetAsFirstSibling();
    }
}
