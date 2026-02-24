using UnityEngine;
using UnityEngine.UI;

public class BoostGauge : MonoBehaviour
{
    private Slider slider;
    [SerializeField] Image fill;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public int GetMax()
    {
        return (int)slider.maxValue;
    }

    public void SetMinMaxSlider(int min, int max)
    {
        slider.minValue = min;
        slider.maxValue = max;
    }

    public void SetSliderValue(int value)
    {
        value = (int)Mathf.Clamp(value, slider.minValue, slider.maxValue);

        slider.value = value;

        fill.enabled = (slider.value > slider.minValue);
    }

    public void ChangeSliderValue(int amount)
    {
        slider.value = Mathf.Clamp(slider.value + amount, slider.minValue, slider.maxValue);

        fill.enabled = (slider.value > slider.minValue);
    }
}
