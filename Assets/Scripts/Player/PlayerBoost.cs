using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

public class PlayerBoost : MonoBehaviour
{
    private int gauge    = 0;
    private int maxGauge = 300;
    private bool isBoosting;
    [Header("Boost Variables")]
    [SerializeField] int   requiredStartAmount;
    [SerializeField] int   gaugeDepletionAmount;
    [SerializeField] float gaugeDepletionRate;
    [Space]
    [SerializeField] int amountLostOnHurt;
    [SerializeField] int amountLostOnHurtWithFullGauge;
    [Space]

    [Header("Gauge GUI")]
    [SerializeField] float gaugeDisplayDelay;
    [SerializeField] BoostGauge firstGauge;
    [SerializeField] BoostGauge secondGauge;
    [SerializeField] BoostGauge thirdGauge;

    public UnityEvent OnGaugeDepleted;

    #region Gauge
    public int GetGauge() { return gauge; }
    public void SetGauge(int value) 
    {
        if (value < 100)
        {
            firstGauge.SetSliderValue(value);
            secondGauge.SetSliderValue(0);
            thirdGauge.SetSliderValue(0);
        }
        else if (value < 200)
        {
            firstGauge.SetSliderValue(100);
            secondGauge.SetSliderValue(value - 100);
            thirdGauge.SetSliderValue(0);
        }
        else
        {
            firstGauge.SetSliderValue(100);
            secondGauge.SetSliderValue(100);
            thirdGauge.SetSliderValue(value - 200);
        }
    }
    public void AddGauge(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("CANNOT ADD NEGATIVE AMOUNT TO GAUGE");
            return;
        }
        gauge = Mathf.Clamp(gauge + amount, 0, maxGauge);

        SetGauge(gauge);
    }
    public void RemoveGauge(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("CANNOT REMOVE NEGATIVE AMOUNT TO GAUGE");
            return;
        }
        gauge = Mathf.Clamp(gauge - amount, 0, maxGauge);

        SetGauge(gauge);

        if (gauge <= 0)
        {
            isBoosting = false;
            OnGaugeDepleted?.Invoke();
        }
    }
    #endregion

    #region Bools
    public bool CanBoost()
    {
        return gauge != 0;
    }

    public bool IsBoosting() { return isBoosting; }

    public bool IsGaugeDepleted() { return gauge <= 0; }
    #endregion

    private void Start()
    {
        maxGauge = 300;
        firstGauge.SetMinMaxSlider(0, 100);
        secondGauge.SetMinMaxSlider(0, 100);
        thirdGauge.SetMinMaxSlider(0, 100);
    }
    private void Update()
    {
        //DEBUG ONLY
        gauge = 300;
    }

    public void StartBoost()
    {
        if (gauge < requiredStartAmount)
        {
            Debug.Log("Not enough Gauge to boost!");
            RemoveGauge(requiredStartAmount);
            return;
        }

        RemoveGauge(requiredStartAmount);
        isBoosting = true;
        SoundManager.instance.PlaySFX(SoundManager.SFX.Boost);
        StartCoroutine(GaugeDepletionRoutine());
    }

    public void OnBoostUpdate(bool _isBoosting)
    {
        isBoosting = _isBoosting;
    }

    IEnumerator GaugeDepletionRoutine()
    {
        yield return new WaitForSeconds(gaugeDepletionRate);

        RemoveGauge(gaugeDepletionAmount);

        if (isBoosting) StartCoroutine(GaugeDepletionRoutine());
    }

    public void OnGetHurt()
    {
        RemoveGauge(100);
    }
}
