using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LaunchSequence : MonoBehaviour
{
    [Header("Game Parameters")]
    [SerializeField] float baseInputTime;
    [Space]

    [Header("References")]
    [SerializeField] List<Prompt> prompts;
    [Space]

    public UnityEvent<Prompt.PromptQuality> LaunchPlayer;

    private int activePromptIndex;
    private int failedPrompts  = 0;
    private int okPrompts      = 0;
    private int goodPrompts    = 0;
    private int perfectPrompts = 0;

    private bool  isDone = false;

    public enum LaunchInputs
    {
        Up,
        Down,
        Left,
        Right,
        Space,
        Shift
    }

    private void Start()
    {
        //Debug.LogWarning("Script flagged as using the deprecated PlayerController");

        foreach (var prompt in prompts)
        {
            prompt.CompletedPromptEvent.AddListener(ReceiveComplete);
            prompt.SetInputTime(baseInputTime);
            prompt.NewPromptEvent.AddListener(OnNewPrompt);
        }

        LaunchPlayer.AddListener(LevelManager.instance.EndLaunchingSequence);
        BeginSequence();
    }

    public void BeginSequence()
    {
        activePromptIndex = -1;
        isDone = false;
    }

    private void ReceiveComplete(Prompt.PromptQuality quality)
    {
        if (isDone) return;

        switch (quality)
        {
            case Prompt.PromptQuality.Failed:
                failedPrompts++;
                break;
            case Prompt.PromptQuality.Ok:
                LevelManager.score += 100;
                okPrompts++;
                break;
            case Prompt.PromptQuality.Good:
                LevelManager.score += 1000;
                goodPrompts++;
                break;
            case Prompt.PromptQuality.Perfect:
                LevelManager.score += 10000;
                perfectPrompts++;
                break;
        }
    }

    public void OnNewPrompt()
    {
        activePromptIndex++;

        if (activePromptIndex == 0) SoundManager.instance.PlaySong(SoundManager.OST.LaunchingSequence, true);

        if (activePromptIndex != 0 && !prompts[activePromptIndex - 1].WasCompleted())
            prompts[activePromptIndex - 1].CompleteFail(false);

        prompts[activePromptIndex].SetInputTime(baseInputTime);
    }

    public void FinishSequence()
    {
        isDone = true;

        if (failedPrompts + okPrompts + goodPrompts + perfectPrompts != prompts.Count) Debug.LogWarning($"All prompts aren't accounted for. activePromptIndex: {activePromptIndex}, prompts.Count: {prompts.Count}, failedPrompts: {failedPrompts}, okPrompts: {okPrompts}, goodPrompts: {goodPrompts}, perfectPrompts: {perfectPrompts}");
        
        LevelLeaderboard.instance?.SetFailedText("Failed: " + failedPrompts.ToString() + "/" + prompts.Count);
        LevelLeaderboard.instance?.SetOkText("Ok: " + okPrompts.ToString() + "/" + prompts.Count);
        LevelLeaderboard.instance?.SetGoodText("Good: " + goodPrompts.ToString() + "/" + prompts.Count);
        LevelLeaderboard.instance?.SetPerfectText("Perfect: " + perfectPrompts.ToString() + "/" + prompts.Count);

        if (perfectPrompts == prompts.Count && failedPrompts == 0)
        {
            LaunchPlayer.Invoke(Prompt.PromptQuality.Perfect);
            return;
        }

        if (failedPrompts == prompts.Count)
        {
            LaunchPlayer.Invoke(Prompt.PromptQuality.Failed);
            return;
        }

        if (failedPrompts > (prompts.Count) * 0.5f)
        {
            LaunchPlayer.Invoke(Prompt.PromptQuality.Failed);
            return;
        }

        if ((okPrompts != prompts.Count) && (goodPrompts == prompts.Count || failedPrompts <= 0 || (perfectPrompts * 2 + goodPrompts > okPrompts + failedPrompts * 2)))
        {
            LaunchPlayer.Invoke(Prompt.PromptQuality.Good);
            return;
        }

        else
        {
            LaunchPlayer.Invoke(Prompt.PromptQuality.Ok);
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDone) return;

        if (collision.TryGetComponent(out NewPlayerController nPlayer))
        {
            if (!prompts[prompts.Count - 1].WasCompleted()) prompts[prompts.Count - 1].CompleteFail(true);
            FinishSequence();
        }
        else if (collision.TryGetComponent(out PlayerController player))
        {
            if (!prompts[prompts.Count - 1].WasCompleted()) prompts[prompts.Count - 1].CompleteFail(true);
            FinishSequence();
        }
    }
}
