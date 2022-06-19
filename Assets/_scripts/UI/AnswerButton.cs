using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AnswerButton : MonoBehaviour
{
    public bool showAnswerCorrectAnim = true;

    public Text answerText;

    private Image mainImage;
    public Color rightSpriteColor; //#80C43B
    public Color wrongSpriteColor; //#FA5051
    public Color defaultColor;

    private AnswerData answerData;
    private GameController gameController;
    private Button button;

    private void Awake()
    {
        mainImage = GetComponent<Image>();
        button = GetComponent<Button>();
        gameController = FindObjectOfType<GameController>();

    }
    // Use this for initialization
    private void Start()
    {
        defaultColor = mainImage.color;
        //onlineGameController = FindObjectOfType<OnlineGameController>();
    }

    public void Setup(AnswerData data)
    {
        answerData = data;
        answerText.text = answerData.answerText;
        mainImage.color = defaultColor;
        //InitColors(); //test
        
        
        //Enable();
        //ActivateIfNotInteractable();
    }
    public void Clear()
    {
        answerText.text = string.Empty;
        mainImage.color = defaultColor;

        Debug.Log("[debug] Cleared button. Color = " + defaultColor);
    }
    
    //public void ActivateIfNotInteractable()
    //{
    //    if (button != null && !button.interactable)
    //        button.interactable = true;
    //}
    public void HighlightAsCorrect()
    {
        mainImage.color = rightSpriteColor;
    }
    public void Disable()
    {
        //answerText.text = string.Empty;
        button.interactable = false;
    }
    public void Enable()
    {
        //answerText.text = string.Empty;
        button.interactable = true;
    }

    bool answerIsCorrectAnimShown;
    public void HandleClick()
    {
        StartCoroutine(HandleClickCoroutine());
    }
    public IEnumerator HandleClickCoroutine()
    {
        //onlineGameController.PauseTimer();

        StartCoroutine(ShowAnswerSelectedAnim());
        yield return new WaitUntil(() => answerIsCorrectAnimShown);
        answerIsCorrectAnimShown = false;

        gameController.OnAnswered(answerData.isCorrect);

        //mainImage.sprite = defaultSprite;
    }
    private void InitColors()
    {
        mainImage.color = defaultColor;

        Color disabledColor = answerData.isCorrect ? rightSpriteColor : defaultColor;
        ColorBlock disabledColorBlock = button.colors;
        disabledColorBlock.disabledColor = disabledColor;
        button.colors = disabledColorBlock;
    }
    private IEnumerator ShowAnswerSelectedAnim()
    {
        yield return null;

        mainImage.color = answerData.isCorrect ? rightSpriteColor : wrongSpriteColor;

        answerIsCorrectAnimShown = true;
    }
    private IEnumerator flashAnim(Image img, Sprite flashing, float animTime)
    {
        Sprite boof = img.sprite;

        for (int i = 0; i < 2; i++)
        {
            img.sprite = flashing;
            yield return new WaitForSeconds(animTime / 4);           
            img.sprite = boof;
            yield return new WaitForSeconds(animTime / 4);
        }
    }

    private const float INTRIGUE_ANIM_TIME = 1f;
    private const float SHOW_IS_ANSWER_CORRECT_ANIM_TIME = 1f;
    private const float ANSWER_SHOW_DELAY = 2f;

}