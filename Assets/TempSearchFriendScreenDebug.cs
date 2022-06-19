using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempSearchFriendScreenDebug : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.LogWarning("SEARCH SCREEN START");

        if (gameObject.activeInHierarchy)
            Debug.Log("SEARCH SCREEN IS ACTIVE");
    }

    private void OnDisable()
    {
        Debug.LogWarning("SEARCH SCREEN IS DISABLE");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
