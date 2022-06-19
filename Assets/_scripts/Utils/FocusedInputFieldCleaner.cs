using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FocusedInputFieldCleaner : MonoBehaviour
{

    private InputField inputField;

    private void Start()
    {
        inputField = GetComponent<InputField>();
    }

    void Update()
    {
        if (!inputField.isFocused || inputField.text != string.Empty)
            return;

        inputField.placeholder.GetComponent<Text>().text = string.Empty;
    }
}
