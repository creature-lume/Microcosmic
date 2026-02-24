using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GUIManager : MonoBehaviour
{
    public static GUIManager instance;

    [Header("References")]
    [SerializeField] GameObject pauseScreen;
    [SerializeField] LevelLeaderboard leaderboard;
    [SerializeField] GameObject debugMode;
    [SerializeField] GameObject promptUIs;
    [SerializeField] UnityEngine.UI.Image promptImage;
    [SerializeField] Animator promptImageAnimator;
    [SerializeField] TextMeshProUGUI qualityText;
    [SerializeField] TextMeshProUGUI lifeCountText;
    [SerializeField] Animator qualityTextAnimator;
    [SerializeField] GUITimer timer;

    [Header("Prompt Quality Feedbacks")]
    [SerializeField] float hideFeedbacksDelay = 1f;
    [SerializeField] string failedFeedbackText;
    [SerializeField] string okFeedbackText;
    [SerializeField] string goodFeedbackText;
    [SerializeField] string perfectFeedbackText;
    [SerializeField] Color PromptImageFail;
    [SerializeField] Color PromptImageSuccess;

    [Header("Start Quality")]
    [SerializeField] GameObject roughStart;
    [SerializeField] GameObject okStart;
    [SerializeField] GameObject goodStart;
    [SerializeField] GameObject headStart;

    private bool isInDebugMode;

    public void ShowPrompt(Sprite newSprite)
    {
        promptImage.color = Color.white;

        if (newSprite == null)
        {
            Debug.LogWarning("PROMPT SPRITE NULL");
            return;
        }

        promptImageAnimator.SetTrigger("Show");
        promptImage.sprite = newSprite;
    }

    public void ShowQuality(Prompt.PromptQuality quality)
    {
        switch (quality)
        {
            case Prompt.PromptQuality.Failed:
                qualityText.SetText(failedFeedbackText);
                qualityTextAnimator.SetTrigger("Failed");
                break;
            case Prompt.PromptQuality.Ok:
                qualityText.SetText(okFeedbackText);
                qualityTextAnimator.SetTrigger("Ok");
                break;
            case Prompt.PromptQuality.Good:
                qualityText.SetText(goodFeedbackText);
                qualityTextAnimator.SetTrigger("Good");
                break;
            case Prompt.PromptQuality.Perfect:
                qualityText.SetText(perfectFeedbackText);
                qualityTextAnimator.SetTrigger("Perfect");
                break;
        }
    }

    public void UpdateLifeCount(string value)
    {
        lifeCountText.SetText(value);
    }

    public void InputPrompt(bool isCorrect)
    {
        promptImage.color = new Color(PromptImageFail.r, PromptImageFail.g, PromptImageFail.b, promptImage.color.a);
    }

    public void HideQuality()
    {
        qualityTextAnimator.SetTrigger("Hide");
    }

    public void HidePrompt()
    {
        promptImageAnimator.SetTrigger("Hide");
    }

    public void FadeOutPrompt()
    {
        promptImageAnimator.SetTrigger("FadeOut");
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            pauseScreen.SetActive(true);
            Time.timeScale = 0f;
            UnityEngine.Cursor.visible = true;
            return;
        }
        pauseScreen.SetActive(false);
        UnityEngine.Cursor.visible = isInDebugMode;
        Time.timeScale = 1f;
    }

    public void OnRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ToggleDebugMode()
    {
        isInDebugMode = !isInDebugMode;
        debugMode.SetActive(isInDebugMode);
        UnityEngine.Cursor.visible = isInDebugMode;
        SetPaused(false);
    }

    public void TogglePause()
    {
        if (pauseScreen.activeSelf)
        {
            pauseScreen.SetActive(false);
            Time.timeScale = 1f;
            UnityEngine.Cursor.visible = false;
            return;
        }
        pauseScreen.SetActive(true);
        UnityEngine.Cursor.visible = isInDebugMode;
        Time.timeScale = 0f;
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void Awake()
    {
        Application.runInBackground = true;
        if (instance != null)
        {
            Debug.LogWarning("GUIMANAGER INSTANCE NOT NULL");
            gameObject.SetActive(false);
        }

        instance = this;

        roughStart.SetActive(false);
        okStart.SetActive(false);
        goodStart.SetActive(false);
        headStart.SetActive(false);
    }

    private void Start()
    {
        isInDebugMode = false;
        debugMode.SetActive(false);
        SetPaused(false);
    }

    public void OnLevelStart(Prompt.PromptQuality quality)
    {
        timer.SetStopTimer(false);

        switch (quality)
        {
            case Prompt.PromptQuality.Failed:
                roughStart.SetActive(true); break;
            case Prompt.PromptQuality.Ok:
                okStart.SetActive(true); break;
            case Prompt.PromptQuality.Good:
                goodStart.SetActive(true); break;
            case Prompt.PromptQuality.Perfect:
                headStart.SetActive(true); break;
        }

        StartCoroutine(HideFeedbacksDelay());
    }

    private IEnumerator HideFeedbacksDelay()
    {
        yield return new WaitForSeconds(hideFeedbacksDelay);
        HideQuality();
        HidePrompt();

        promptUIs.SetActive(false);
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            SetPaused(true);
        }
    }
}
