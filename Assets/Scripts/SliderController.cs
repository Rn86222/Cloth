using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{

    private Slider slider;

    [SerializeField] private float maxValue;
    [SerializeField] private float minValue;
    [SerializeField] private float initialValue;

    void Start()
    {
        slider = GetComponent<Slider>();

        slider.maxValue = maxValue;
        slider.minValue = minValue;

        slider.value = initialValue;
    }

    public float GetSliderValue()
    {
        return slider.value;
    }
}
