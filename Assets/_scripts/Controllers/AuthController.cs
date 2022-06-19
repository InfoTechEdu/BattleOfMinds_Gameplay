using Proyecto26;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum AuthErrors
{
    //edit
}
public class AuthController : MonoBehaviour
{
    [SerializeField] private string NextSceneToLoad = null;
    [SerializeField] private string PlatformRefgistrationScene = "PlatformRegistration";
    [SerializeField] private GameObject banner;

    [Header("UI panels")]
    public GameObject AuthPanel;
    public GameObject ResetPasswordPanel;
    public GameObject ResetPasswordSuccess;

    [Header("UI Elements")]
    public InputField emailInput;
    public InputField passwordInput;
    public InputField resetPasswordEmailInput;

    private void Start()
    {
        //PlayerPrefs.DeleteAll();

        //If Firebase Emulator is acitve, do nothing, because this object authentificates in system
        if (FirebaseProjectConfigurations.PROJECT_BUILD == ProjectBuildType.Emulator)
            return;

        if (FirebaseProjectConfigurations.PROJECT_BUILD == ProjectBuildType.Release)
        {
            InitView();
        }
            
        DataController dataController = FindObjectOfType<DataController>();
        if (dataController != null)
            Destroy(dataController.gameObject);


        if (FirebaseManager.Instance.Auth.HasActiveSession())
        {
            string refreshToken = FirebaseManager.Instance.Auth.GetRefreshToken();
            FirebaseManager.Instance.Auth.RefreshToken(refreshToken, ()=>
            {

                GoToNextScene();
            }, ()=>
            {
                HideBanner();
            });
        }
        else
        {
            HideBanner();
        }

        //SkipIfUserSignedIn(); //old
        //SkipSceneIfUserSignedInREST(); //old
        //StartCoroutine(DeativateBanner(3f)); //костыль, так как запрос на проверку в базу не отправляется
        //EnableAndClearAuthUI();
    }

    private void InitView()
    {
        AuthPanel.SetActive(true);
        ResetPasswordPanel.SetActive(false);
        ResetPasswordSuccess.SetActive(false);

        ShowBanner();
        EnableAndClearAuthUI();
    }
    private void EnableAndClearAuthUI()
    {
        Debug.Log("Enabling auth ui...");
        AuthPanel.transform.FindDeepChild("ErrorMessage").GetComponent<Text>().text = "";

        AuthPanel.SetActive(true);
        banner.SetActive(false);
    }
    private void ShowBanner()
    {
        banner.SetActive(true);
    }
    private void HideBanner()
    {
        banner.SetActive(false);
    }

    public void OnSignInClicked()
    {
        if (string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text))
            return;

        AuthPanel.transform.FindDeepChild("ErrorMessage").GetComponent<Text>().text = string.Empty;
        DisableMenuButtons(AuthPanel);

        FirebaseManager.Instance.Auth.SignInEmailPassword(emailInput.text, passwordInput.text, (response)=>
        {
            FirebaseManager.Instance.Auth.SetIdToken(response.idToken);
            FirebaseManager.Instance.Auth.SetLocalId(response.localId);
            FirebaseManager.Instance.Auth.SetRefreshToken(response.refreshToken);

            GoToNextScene();
        }, (exception)=>
        {
            Debug.Log($"Failed to sign in. Message: {exception.Message}");

            AuthPanel.transform.FindDeepChild("ErrorMessage").GetComponent<Text>().text = "Ошибка. Проверьте данные";
            EnableMenuButtons(AuthPanel);
        });
    }
    public void OnSignUpClicked()
    {
        GoToPlatformRegisterScene();
    }
    public void OnResetPasswordClicked()
    {
        if (string.IsNullOrEmpty(resetPasswordEmailInput.text))
            return;

        ResetPasswordPanel.transform.FindDeepChild("ErrorMessage").GetComponent<Text>().text = "";

        DisableMenuButtons(ResetPasswordPanel);

        FirebaseManager.Instance.Auth.SendPasswordResetEmail(resetPasswordEmailInput.text, (success) =>
        {
            if (success)
            {
                ResetPasswordPanel.SetActive(false);
                ResetPasswordSuccess.SetActive(true);

                EnableMenuButtons(ResetPasswordPanel);
            }
            else
            {
                EnableMenuButtons(ResetPasswordPanel);

                ResetPasswordPanel.transform.FindDeepChild("ErrorMessage").GetComponent<Text>().text =
                     $"Произошла ошибка. Проверьте правильность email адреса";
            }
        });
    }

    private void GoToNextScene()
    {
        if(NextSceneToLoad == null)
        {
            Debug.Log("Cannot load next scene. Value is null");
            return;
        }

        SceneManager.LoadScene(NextSceneToLoad);
    }
    private void GoToPlatformRegisterScene()
    {
        //DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(PlatformRefgistrationScene);
    }

    private void EnableMenuButtons(GameObject panel)
    {
        Button[] btns = panel.transform.FindDeepChild("Menu").GetComponentsInChildren<Button>();
        foreach (var btn in btns)
        {
            btn.interactable = true;
        }
    }
    private void DisableMenuButtons(GameObject panel)
    {
        Button[] btns = panel.transform.FindDeepChild("Menu").GetComponentsInChildren<Button>();
        foreach (var btn in btns)
        {
            btn.interactable = false;
        }
    }

    //maybe old code
    //private void SkipIfUserSignedIn()
    //{
    //    string cookies = HttpCookie.GetCookie("idToken");
    //    if (!string.IsNullOrEmpty(cookies))
    //    {
    //        string userId = HttpCookie.GetCookie("userId");
    //        FirebaseManager.Instance.Auth.SetLocalId(userId);
    //        GoToNextScene();
    //    }
    //}
    //private void SkipSceneIfUserSignedInREST()
    //{
    //    FirebaseManager.Instance.Auth.CheckIsSignedIn((isSignedIn) =>
    //    {
    //        if (isSignedIn)
    //        {
    //            FirebaseManager.Instance.Auth.SetLocalId(PlayerPrefs.GetString("userId"));
    //            GoToNextScene();
    //        }
    //        else
    //        {
    //            EnableAndClearAuthUI();
    //        }  
    //    });
    //}
}


