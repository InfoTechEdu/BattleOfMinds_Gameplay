using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerView : MonoBehaviour
{
    private float maxValue;

    private Slider slider;

    private void Awake()
    {
        slider = transform.GetComponent<Slider>();
    }

    public void Init(float maxValue)
    {
        this.maxValue = maxValue;
    }

    public void UpdateView(float timerValue)
    {
        slider.value = 1 - Mathf.Clamp01(timerValue / maxValue);

        timerValue += 1;
        if (timerValue >= maxValue)
            timerValue = maxValue;
    }
}
