using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
[DisallowMultipleComponent]
public class IgnoreFirstInDropdown : MonoBehaviour, IPointerClickHandler
{

    //NOT WORKING
    private Dropdown dropdown;

    void Start()
    {
        dropdown = GetComponent<Dropdown>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var toggles = dropdown.GetComponentsInChildren<Toggle>(true);

        Debug.Log("toggles[1] - " + toggles[0].name);
        toggles[0].interactable = false;
        return;
        for (var i = 1; i < toggles.Length; i++)
        {
            Debug.Log("toggle name - " + toggles[i].name);
            toggles[i].interactable = int.TryParse(toggles[i].name, out int result);
            //toggles[i].interactable = !indexesToDisable.Contains(i - 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
