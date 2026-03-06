using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    private bool isInLaunchingSequence;

    [Header("Score Parameters")]
    [SerializeField] private float bestTime;
    [SerializeField] private float worstTime;
    public static int score;

    [Header("Prefabs")]
    [SerializeField] List<Sprite> UpPromptSprites;
    [SerializeField] List<Sprite> DownPromptsSprites;
    [SerializeField] List<Sprite> LeftPromptsSprites;
    [SerializeField] List<Sprite> RightPromptsSprites;
    [SerializeField] List<Sprite> SpacePromptsSprites;
    [SerializeField] List<Sprite> ShiftPromptsSprites;

    public UnityEvent<Prompt.PromptQuality> LevelStartEvent;
    public UnityEvent LevelEndEvent;

    private Prompt.PromptQuality startingQuality;

    public enum Ranks
    {
        W,
        Sp,
        S,
        Sm,
        Ap,
        A,
        Am,
        Bp,
        B,
        Bm,
        Cp,
        C,
        Cm,
        Dp,
        D,
        Dm,
        Ep,
        E,
        Em,
        Fp,
        F,
        Fm,
        L,
    }

    public List<Sprite> GetUpPrompts()     { return UpPromptSprites;       }
    public List<Sprite> GetDownPrompts()   { return DownPromptsSprites;    }
    public List<Sprite> GetLeftPrompts()   { return LeftPromptsSprites;    }
    public List<Sprite> GetRightPrompts()  { return RightPromptsSprites;   }
    public List<Sprite> GetSpacePrompts()  { return SpacePromptsSprites;   }
    public List<Sprite> GetShiftPrompts()  { return ShiftPromptsSprites;   }

    public bool GetIsInLaunchingSequence() { return isInLaunchingSequence; }

    public void EndLaunchingSequence(Prompt.PromptQuality quality)
    {
        isInLaunchingSequence = false;
        LevelStartEvent.Invoke(quality);
        startingQuality = quality;

        if (quality == Prompt.PromptQuality.Perfect) 
             SoundManager.instance.PlayIntro(SoundManager.OST.HeadStart, SoundManager.OST.SpiralNotebook, true, 0f);

        else SoundManager.instance.PlayIntro(SoundManager.OST.JustGetStarted, SoundManager.OST.SpiralNotebook, true, 0f);
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }

        instance = this;
        isInLaunchingSequence = true;
    }

    private void Start()
    {
        LevelStartEvent.AddListener(GUIManager.instance.OnLevelStart);
        score = 0;

        EndLaunchingSequence(Prompt.PromptQuality.Perfect);
    }

    public void EndLevel()
    {
        LevelEndEvent.Invoke();

        GUITimer.instance.SetStopTimer(true);
        LevelLeaderboard.instance.SetInspoText("Inspiration Picked Up: " + PickupInspiration.pickupCount.ToString());
        LevelLeaderboard.instance.SetTimeText("Time: " + GUITimer.instance.GetTimerDisplayText());

        CalculateScore();
    }

    private void CalculateScore()
    {
        SoundManager.instance.PlaySong(SoundManager.OST.Leaderboard, true);

        score += PickupInspiration.pickupCount * 100;
        int timeScore = 300 - (int)GUITimer.instance.GetTimer();
        if (timeScore > 0) score += timeScore * 100;

        LevelLeaderboard.instance.SetScoreText("Score: " + score);
        LevelLeaderboard.instance.gameObject.SetActive(true);
        UnityEngine.Cursor.visible = true;

        int lowestScore  = 30000;
        int highestScore = 150000;
        int amountPerScoreCap = (highestScore - lowestScore) / (((int)Ranks.L + 1));
        int rankNumber = 0;

        for (int i = 23; i >= 1; i--)
        {
            if (i == 1)
            {
                if (score <= lowestScore)
                {
                    rankNumber = 22; break;
                }
                else continue;
            }

            if (i == 23)
            {
                if (score >= highestScore)
                {
                    rankNumber = 0; break;
                }
                else continue;
            }

            if (lowestScore + amountPerScoreCap * (i - 1) <= score && score <= lowestScore +  amountPerScoreCap * i)
            {
                rankNumber = 22 - i; break;
            }
        }

        switch (rankNumber) //oooooooooh this is the worst code known to man but it is 02:23am on sumbmission day
        {
            case 0: 
                LevelLeaderboard.instance.SetLetterText("W");
                break;
            case 1:
                LevelLeaderboard.instance.SetLetterText("S+");
                break;
            case 2:
                LevelLeaderboard.instance.SetLetterText("S");
                break;
            case 3:
                LevelLeaderboard.instance.SetLetterText("S-");
                break;
            case 4:
                LevelLeaderboard.instance.SetLetterText("A+");
                break;
            case 5:
                LevelLeaderboard.instance.SetLetterText("A");
                break;
            case 6:
                LevelLeaderboard.instance.SetLetterText("A-");
                break;
            case 7:
                LevelLeaderboard.instance.SetLetterText("B+");
                break;
            case 8:
                LevelLeaderboard.instance.SetLetterText("B");
                break;
            case 9:
                LevelLeaderboard.instance.SetLetterText("B-");
                break;
            case 10:
                LevelLeaderboard.instance.SetLetterText("C+");
                break;
            case 11:
                LevelLeaderboard.instance.SetLetterText("C");
                break;
             case 12:
                LevelLeaderboard.instance.SetLetterText("C-");
                break;
            case 13:
                LevelLeaderboard.instance.SetLetterText("D+");
                break;
             case 14:
                LevelLeaderboard.instance.SetLetterText("D");
                break;
            case 15:
                LevelLeaderboard.instance.SetLetterText("D-");
                break;
            case 16:
                LevelLeaderboard.instance.SetLetterText("E+");
                break;
            case 17:
                LevelLeaderboard.instance.SetLetterText("E");
                break;
            case 18:
                LevelLeaderboard.instance.SetLetterText("E-");
                break;
             case 19:
                LevelLeaderboard.instance.SetLetterText("F+");
                break;
            case 20:
                LevelLeaderboard.instance.SetLetterText("F");
                break;
            case 21:
                LevelLeaderboard.instance.SetLetterText("F-");
                break;
            case 22:
                LevelLeaderboard.instance.SetLetterText("L");
                break;
        }
    }
}
