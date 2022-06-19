using Proyecto26;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RegistrationController : MonoBehaviour
{
    [Header("Styles")]
    public Color errorInputColor;
    public Color errorMessageColor;

    [Header("UI")]
    public GameObject SuccessRegistrationPanel;
    public GameObject DataPanel;
    public InputField emailInput;
    public InputField passwordInput;
    public InputField nameInput;
    public InputField surnameInput;
    public Dropdown classDropdown;
    public Transform menu;
    public Text errorMessage;

    [Header("Firebase Data")]
    [SerializeField] private string domznaniyProjectAuthKey = FirebaseProjectConfigurations.PROJECT_API_KEY;


    public int tempValue;

    /// <summary>
    /// Regular expression, which is used to validate an E-Mail address.
    /// </summary>
    public const string MatchEmailPattern =
        @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
        + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
          + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
        + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,25})$";

    private void Start()
    {
        HideErrorMessage();
        InitInputValidators();

        //test
        //RegisterUserREST("test", "test", "test6@mail.ru", "123123", 0);
        //RegisterUserRest("unitytest@mail.ru", "123123");
        //StartCoroutine(RegisterUserRestUnityWebRequest("unitytestwebrequest@mail.ru", "123123"));
    }

    private void Update()
    {
        tempValue = classDropdown.value;
    }

    public void OnRegisterClicked()
    {
        HideErrorMessage();
        DisableMenuButtons();

        if (!ValidateUserData())
        {
            EnableMenuButtons();
            return;
        }

        FirebaseManager.Instance.Auth.SignUpEmailPassword(emailInput.text, passwordInput.text, (response) =>
        {
            ShowSuccesMessagePanel();

            PlatformUserData platformUserData = new PlatformUserData(response.localId, nameInput.text, surnameInput.text, "class" + classDropdown.value, null);
            FirebaseManager.Instance.Functions.CallCloudFunctionPostObject("CreateNewUserInPlatform", platformUserData, null, (statusCode) =>
            {
                Debug.Log("Data for new user in platform created");
            }, (exception) =>
            {
                Debug.LogError("Error while creating new user");
            });
        }, (exception) =>
        {
            ShowErrorMessage("Произошла ошибка. Повторите попытку позже");
            EnableMenuButtons();
            Debug.LogError("Failed to sign up. Error message - " + exception.Message);
        });
    }
    public void OnBackButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Auth");
    }
    public void RegisterUser()
    {
        HideErrorMessage();
        DisableMenuButtons();

        if (!ValidateUserData())
        {
            EnableMenuButtons();
            return;
        }

        RegisterUserREST(nameInput.text, surnameInput.text, classDropdown.value, emailInput.text, passwordInput.text);
    }


    private void RegisterUserREST(string name, string surname, int userClass, string email, string password)
    {
        string userData = "{\"email\":\"" + email + "\",\"password\":\"" + password + "\",\"returnSecureToken\":true}";
        string url = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + FirebaseProjectConfigurations.PROJECT_API_KEY;
        RestClient.Post<RegisterResponse>(url, userData).Then(
            response =>
            {
                //PlayerPrefs.SetString("userId", response.localId);
                //PlayerPrefs.SetString("idToken", response.idToken);
                Debug.Log("User registered. Local id - " + response.localId);

                string localId = response.localId;
                StartCoroutine(CallCloudFunctionGet("createNewUserInPlatform", $"userId={localId}&name={name}&surname={surname}&userClass=class{userClass}", (success) =>
                {
                    if (!success)
                    {
                        Debug.LogError("Error while creating new user");
                        return;
                    }

                    Debug.Log("Data for new user created");
                }));

                ShowSuccesMessagePanel();

            }).Catch(error =>
            {
                ShowErrorMessage("Произошла ошибка. Повторите попытку позже");
                EnableMenuButtons();
                Debug.LogError("Failed to sign up. Error message - " + error.Message);
            });
    }

    private void InitInputValidators()
    {
        //edit. add code in update
        //read https://docs.unity3d.com/2018.1/Documentation/ScriptReference/UI.InputField-onValidateInput.html
    }



    private bool ValidateUserData()
    {
        bool inputError = false;
        //validating inputs
        foreach (var input in DataPanel.GetComponentsInChildren<InputField>())
        {
            if (input.text == null || input.text.Trim() == string.Empty)
            {
                input.text = string.Empty;
                input.placeholder.color = errorInputColor; //Color.red;
                inputError = true;
            }
        }
        if (inputError)
        {
            ShowErrorMessage("Введите данные");
            return false;
        }

        //validating dropdown
        if (classDropdown.value <= 0)
        {
            ShowErrorMessage("Пожалуйста, выберите класс");
            return false;
        }
        //string dropdownText = classDropdown.options[classDropdown.value].text;
        //if (!int.TryParse(dropdownText, out int result))
        //{
        //    ShowErrorMessage("Пожалуйста, выберите класс");

        //    return false;
        //}

        if (!IsEmail(emailInput.text))
        {
            ShowErrorMessage("Некорректный email адрес");
            return false;
        }

        if (IsWeakPassword(passwordInput.text))
        {
            ShowErrorMessage("Пароль должен содержать мин. 6 символов");
            return false;
        }

        return true;
    }
    private bool IsEmail(string email)
    {
        if (email != null) return Regex.IsMatch(email, MatchEmailPattern);
        else return false;
    }
    private bool IsWeakPassword(string password)
    {
        if (password.Length < 6)
            return true;

        return false;
    }

    private void ShowErrorMessage(string message)
    {
        errorMessage.gameObject.SetActive(true);
        errorMessage.text = message;

        Debug.LogError("Error in registration. Message - " + message);
    }
    private void HideErrorMessage()
    {
        errorMessage.gameObject.SetActive(false);
        errorMessage.text = string.Empty;
    }
    private void EnableMenuButtons()
    {
        Button[] btns = menu.GetComponentsInChildren<Button>();
        foreach (var btn in btns)
        {
            btn.interactable = true;
        }
    }
    private void DisableMenuButtons()
    {
        Button[] btns = menu.GetComponentsInChildren<Button>();
        foreach (var btn in btns)
        {
            btn.interactable = false;
        }
    }

    private void ShowSuccesMessagePanel()
    {
        string message = "Успешно зарегистрированы!";
        Text messageText = SuccessRegistrationPanel.transform.FindDeepChild("Message").GetComponent<Text>();
        messageText.text = message;

        SuccessRegistrationPanel.SetActive(true);
    }

    public IEnumerator CallCloudFunctionGet(string functionName, string GetParams, Action<bool> callback = null)
    {
        UnityEngine.Debug.Log($"Starting CallCloudFunction {functionName}");

        var projectId = FirebaseProjectConfigurations.PROJECT_ID;
        UnityEngine.Debug.Log("projectId - " + projectId);

        //string url = "https://us-central1-dzgames-12ad8.cloudfunctions.net/GenerateSingleGameQuiz";
        //UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        UnityWebRequest request = new UnityWebRequest($"https://us-central1-{projectId}.cloudfunctions.net/{functionName}?{GetParams}",
            UnityWebRequest.kHttpVerbGET);
        request.SetRequestHeader("Access-Control-Allow-Origin", "*");

        UnityEngine.Debug.Log("request was send");
        yield return request.SendWebRequest();

        Debug.Log("end request");
        UnityEngine.Debug.Log($"End of CallCloudFunction - {functionName}. Request status code - {request.responseCode}");

        if (request.responseCode == 200)
        {
            Debug.Log($"Succes calling {functionName}");
            callback?.Invoke(true);
        }
        else
        {
            Debug.Log("Error while calling cloud function");
            callback?.Invoke(false);
        }
        //commented
        //StartCoroutine(OnNewSingleGameQuizCreated());



        //exception
        //UnityEngine.Debug.Log("request.downloadHandler.text - " + request.downloadHandler.text);

    }
}

[System.Serializable]
public class RegisterResponse
{
    public string localId;
    public string idToken;
    public string expriresIn;
}
