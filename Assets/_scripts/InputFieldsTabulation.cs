using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// Алгоритм работы:
/// 1. Находит все активные объекты с компонентом InputField на сцене и сохраняет их в массив
/// 2. При нажатии на кнопку Tab ищет элемент (inputField) ниже активного и делает его активным
/// 3. После последнего input переходит к самому первому
/// Примечание: протестировано только на сцене, где inputs расположены сверху вниз
/// Примечание 2: поведение при наличии inputFields с одинаковыми именами неизвестно
/// </summary>
public class InputFieldsTabulation : MonoBehaviour
{
    EventSystem system;
    Selectable[] selectablesInputs;

    // Start is called before the first frame update
    void Start()
    {
        system = FindObjectOfType<EventSystem>();

        RefreshInputs();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GameObject currentSelected = system.currentSelectedGameObject;
            if (currentSelected == null)
                return;

            Selectable next;
            int currentSelectedIndex = Array.FindIndex(selectablesInputs, si => si.gameObject.name == currentSelected.name);
            if (currentSelectedIndex == selectablesInputs.Length - 1) //if element is last
            {
                next = selectablesInputs[0];
            }
            else
            {
                next = currentSelected.GetComponent<Selectable>().FindSelectableOnDown();
                while (next != null && next.GetComponent<InputField>() == null)
                {
                    next = next.FindSelectableOnDown();
                }
            }


            if (next == null) //Это происходит, когда пользователь раскрывает dropdown (кликом мыши), а затем нажимает Tab
                return;

            Debug.Log("Next selectable: " + next.gameObject.name);

            InputField inputField = next.GetComponent<InputField>();
            if (inputField != null) inputField.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret
            system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
        }
    }

    /// <summary>
    /// Call this, if your scene dynamically updates UI
    /// </summary>
    public void RefreshInputs()
    {
        selectablesInputs = FindObjectsOfType<Selectable>();
        selectablesInputs = Array.FindAll(selectablesInputs, s => s.GetComponent<InputField>() != null && s.gameObject.activeInHierarchy);
        SortInputs();
        //Array.Reverse(selectablesInputs); //возвращаем порядок

        foreach (var si in selectablesInputs)
            Debug.Log("Next selectable input: " + si.gameObject.name);
    }
    private void SortInputs()
    {
        selectablesInputs = selectablesInputs.OrderBy(si => si.transform.GetSiblingIndex()).ToArray();
    }
}
