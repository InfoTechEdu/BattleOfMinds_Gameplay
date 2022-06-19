using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppScenesNames
{
    public const string AuthScene = "Auth";
    public const string FirebaseConfigurationsScene = "FirebaseConfigurations";
    public const string DataLoadingScene = "DataLoadingScreen";
    public const string MenuScene = "MenuScreen";
    public const string SessionScene = "SessionScreen";
    public const string GameScene = "Game";
    public const string FriendsSessionScene = "FriendsSessionScreen";
    public const string PlatformRegistrationScene = "PlatformRegistration";
}
public class ControllersManager : MonoBehaviour
{
    private string currentSceneName;
    private string previousSceneName;

    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        //Destroy(null);
        

        SceneManager.activeSceneChanged += OnActiveSceneChanged;


        Debug.Log("ControllersManager");
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        //always null?
        //Debug.Log("Current scene: " + current.name);

        currentSceneName = next.name;

        switch (currentSceneName)
        {
            case AppScenesNames.AuthScene:
                Destroy(FindObjectOfType<DataController>()?.gameObject);
                Destroy(FindObjectOfType<NotificationsController>()?.gameObject);
                //old code
                //if(previousSceneName == AppScenesNames.MenuScene)
                //    Destroy(FindObjectOfType<FirebaseManager>()?.gameObject); //refactor. bad code. Как не удалять данный объект? И какой из двух он будет удалять?
                break;
            case AppScenesNames.MenuScene:
                if(previousSceneName == AppScenesNames.FriendsSessionScene)
                {
                    break;
                }
                else
                {
                    FindObjectOfType<DataController>().OnMenuScreenLoaded();
                    if (FindObjectOfType<NotificationsController>() == null)
                        Instantiate(new GameObject()).AddComponent<NotificationsController>().gameObject.name = "NotificationsController";
                }
                break;
            case AppScenesNames.GameScene:
                FindObjectOfType<DataController>().OnGameSessionScreenLoaded();

                Destroy(FindObjectOfType<NotificationsController>()?.gameObject);
                break;
            default:
                break;
        }

        previousSceneName = currentSceneName;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
