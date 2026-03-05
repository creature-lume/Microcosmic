using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using static LaunchSequence;

public class Prompt : MonoBehaviour
{
    [Header("Prompt Parameters")]
    [SerializeField] int randomizeChance;
    //private LaunchSequence launchSequence;
    [SerializeField] private LaunchInputs requiredInput;

    [Header("Game Parameters")]
    private float inputTime;
    private float timeTracker  = 0f;
    [Space]

    private List<Sprite> promptSprites;
    private Sprite promptSprite;
    private bool wasCompleted;
    [HideInInspector] public UnityEvent<PromptQuality> CompletedPromptEvent;
    [HideInInspector] public UnityEvent NewPromptEvent;
    private bool isWaitingForInput = false;
    public bool WasCompleted() { return wasCompleted;  }
    public void Complete()     { wasCompleted = true;  }
    public void SetInputTime(float value)
    {
        if (value >= 2f) inputTime = value;
        else inputTime = 2f;
    }
    public LaunchInputs GetRequiredInput()   { return requiredInput; }

    public enum PromptQuality
    {
        Failed,
        Ok,
        Good,
        Perfect
    }

    private void Start()
    {
        //Debug.LogWarning("Script flagged as using the deprecated PlayerController");

        wasCompleted = false;
        inputTime = 2f;
        //launchSequence = transform.parent.GetComponent<LaunchSequence>();

        CompletedPromptEvent.AddListener(GUIManager.instance.ShowQuality);
        
        switch (Random.Range(0, 6)){
            case 0:
                requiredInput = LaunchInputs.Up;
                promptSprites = LevelManager.instance.GetUpPrompts();
                break;
            case 1:
                requiredInput = LaunchInputs.Down;
                promptSprites = LevelManager.instance.GetDownPrompts();
                break;
            case 2:
                requiredInput = LaunchInputs.Left;
                promptSprites = LevelManager.instance.GetLeftPrompts();
                break;
            case 3:
                requiredInput = LaunchInputs.Right;
                promptSprites = LevelManager.instance.GetRightPrompts();
                break;
            case 4:
                requiredInput = LaunchInputs.Space;
                promptSprites = LevelManager.instance.GetSpacePrompts();
                break;
            case 5:
                requiredInput = LaunchInputs.Shift;
                promptSprites = LevelManager.instance.GetShiftPrompts();
                break;
        }

        if (Random.Range(0, 100) < randomizeChance && promptSprites.Count > 1) //Failed roll
        {
            promptSprite = promptSprites[Random.Range(1, promptSprites.Count)];
        }
        else
            promptSprite = promptSprites[0];

    }

    private void Update()
    {
        if (!isWaitingForInput) return;
        timeTracker += Time.deltaTime;
    }

    public void Show()
    {
        timeTracker = 0f;
        if (inputTime < 2f) inputTime = 2f; //below 2f it's either impossible to get a perfect OR I'm shit at it. Yes ik I'm using an arbitrary value, 
        isWaitingForInput = true;           //I don't wanna risk forgetting to set it up in the inspector

        GUIManager.instance.ShowPrompt(promptSprite);
        NewPromptEvent.Invoke();
        SoundManager.instance.PlaySFX(SoundManager.SFX.PromptUp);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out NewPlayerController nPlayer))
        {
            if (!wasCompleted) Show();
            nPlayer.SetCurrentPrompt(this);
        }

        else if (collision.gameObject.TryGetComponent(out PlayerController player))
        {
            if (!wasCompleted) Show();
            player.SetCurrentPrompt(this);
        }
    }

    public void ReceiveInput(LaunchInputs playerInput)
    {
        if (wasCompleted || !isWaitingForInput) return;

        if      (playerInput != requiredInput) CompleteFail(true);
        else if (timeTracker < inputTime / 4)  CompletePerfect();
        else if (timeTracker < inputTime / 3)  CompleteGood();
        else                                   CompleteOk();
    }

    #region Completes
    public void CompleteFail(bool greyOut)
    {
        Complete();
        CompletedPromptEvent.Invoke(PromptQuality.Failed);
        if (greyOut) GUIManager.instance.InputPrompt(false);
        SoundManager.instance.PlaySFX(SoundManager.SFX.PromptFail);
    }

    public void CompleteOk()
    {
        Complete();
        CompletedPromptEvent.Invoke(PromptQuality.Ok);
        GUIManager.instance.InputPrompt(true);
        SoundManager.instance.PlaySFX(SoundManager.SFX.PromptOk);
    }

    public void CompleteGood()
    {
        Complete();
        CompletedPromptEvent.Invoke(PromptQuality.Good);
        GUIManager.instance.InputPrompt(true);
        SoundManager.instance.PlaySFX(SoundManager.SFX.PromptGood);
    }

    public void CompletePerfect()
    {
        Complete();
        CompletedPromptEvent.Invoke(PromptQuality.Perfect);
        GUIManager.instance.InputPrompt(true);
        SoundManager.instance.PlaySFX(SoundManager.SFX.PromptPerfect);
    }
    #endregion
}
