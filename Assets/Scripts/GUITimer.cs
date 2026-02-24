using System;
using TMPro;
using UnityEngine;

public class GUITimer : MonoBehaviour
{
    public static GUITimer instance;

    bool isStopped;
    private float timer;
    private string displayText;
    [SerializeField] TextMeshProUGUI tmp;

    public void SetStopTimer(bool doStop)
    {
        isStopped = doStop;
        if (isStopped) tmp.enabled = false;
        else           tmp.enabled = true;
    }

    public void SetTime(float time)
    {
        timer = time;
    }

    public void ClearTimer() { timer = 0f;   }
    public float GetTimer()  { return timer; }
    public string GetTimerDisplayText() { return displayText; }

    void Awake()
    {
        if (!tmp) tmp = GetComponent<TextMeshProUGUI>();
        ClearTimer();

        if (instance != null)
        {
            Debug.LogWarning("OTHER GUI TIMER DETECTED. DESTROYING GAMEOBJECT.");
            Destroy(gameObject);
            return;
        }
        instance = this;
        SetStopTimer(true);
    }

    void Update()
    {
        if (isStopped) return;

        timer += Time.deltaTime;

        displayText = (TimeSpan.FromSeconds(timer).ToString("mm")) + ":" + 
                      (TimeSpan.FromSeconds(timer).ToString("ss")) + ":" + 
                      (TimeSpan.FromSeconds(timer).ToString("ff"));
        tmp.SetText(displayText);
    }
}
