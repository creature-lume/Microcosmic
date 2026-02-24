using TMPro;
using UnityEngine;

public class LevelLeaderboard : MonoBehaviour
{
    public static LevelLeaderboard instance;
    [Header("References")]
    [SerializeField] private TextMeshProUGUI failedText;
    [SerializeField] private TextMeshProUGUI okText;
    [SerializeField] private TextMeshProUGUI goodText;
    [SerializeField] private TextMeshProUGUI perfectText; [Space]
    [SerializeField] private TextMeshProUGUI inspoText; [Space]
    [SerializeField] private TextMeshProUGUI timeText; [Space]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI letterText;

    public void SetFailedText(string text)  { failedText.text  = text; }
    public void SetOkText(string text)      { okText.text      = text; }
    public void SetGoodText(string text)    { goodText.text    = text; }
    public void SetPerfectText(string text) { perfectText.text = text; }
    public void SetInspoText(string text)   { inspoText.text   = text; }
    public void SetTimeText(string text)    { timeText.text    = text; }
    public void SetScoreText(string text)   { scoreText.text   = text; }
    public void SetLetterText(string text)  { letterText.text  = text; }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("INSTANCE NOT NULL");
            gameObject.SetActive(false);
        }
        instance = this;
        gameObject.SetActive(false);
    }
}
