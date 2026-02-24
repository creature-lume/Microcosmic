using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static SoundManager;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    [SerializeField] private AudioClip c_spiralNotebook;
    [SerializeField] private AudioClip c_leaderboard;
    [SerializeField] private AudioClip c_launchingSequence;
    [SerializeField] private AudioClip c_justGetStarted;
    [SerializeField] private AudioClip c_headStart;

    [SerializeField] List<SFXData> sfxList;

    private AudioSource source;

    private bool waitForIntro;
    private AudioClip introClip;
    private AudioClip afterIntroClip;
    private bool afterIntroLoop;
    private float afterIntroStartTime;
    
    public enum OST
    {
        SpiralNotebook,
        Leaderboard,
        LaunchingSequence,
        JustGetStarted,
        HeadStart
    }

    public enum SFX
    {
        Whoosh,
        UIClickHigh,
        UIClickLow,
        Death,
        Damage,
        Inspo,
        PromptUp,
        PromptFail,
        PromptOk,
        PromptGood,
        PromptPerfect,
        Jump1, 
        Jump2,
        Jump3,
        Break,
        Gameover,
        Bumper,
        Checkpoint,
        EndGoal,
        Boost,
    }

    public bool CheckIfPlaying(OST toCheck)
    {
        if (source.clip == GetAudioclipFromEnum(toCheck) && source.isPlaying) return true;
        return false;
    }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("SOUNDMANAGER INSTANCE NOT NULL. DEACTIVATING");
            gameObject.SetActive(false);
            return;
        }

        instance = this;
        source = GetComponent<AudioSource>();
    }

    public void PlaySong(OST ost, bool doLoop)
    {
        waitForIntro    = false;
        source.resource = GetAudioclipFromEnum(ost);
        source.loop     = doLoop;
        source.Play();
    }

    public void PlaySong(OST ost, bool doLoop, float startTime)
    {
        waitForIntro    = false;
        source.resource = GetAudioclipFromEnum(ost);
        source.loop     = doLoop;

        if (startTime == 0f) source.Play();
        else                 source.PlayDelayed(startTime);
    }

    public void PlayIntro(OST intro, OST mainSong, bool loopMain, float songStartTime)
    {
        waitForIntro        = true;
        source.loop         = false;
        afterIntroStartTime = songStartTime;
        afterIntroLoop      = loopMain;

        introClip       = GetAudioclipFromEnum(intro);
        afterIntroClip  = GetAudioclipFromEnum(mainSong);
        source.resource = introClip;

        source.Play();
    }

    private void Update()
    {
        if (!waitForIntro) return;

        if (source.time >= introClip.length)
        {
            source.resource = afterIntroClip;
            source.loop     = afterIntroLoop;

            if (afterIntroStartTime == 0f) source.Play();
            else                           source.PlayDelayed(afterIntroStartTime);

            waitForIntro = false;
        }
    }

    private AudioClip GetAudioclipFromEnum(OST ost)
    {
        switch (ost)
        {
            case OST.SpiralNotebook:
                return c_spiralNotebook;
            case OST.JustGetStarted:
                return c_justGetStarted;
            case OST.HeadStart:
                return c_headStart;
            case OST.LaunchingSequence:
                return c_launchingSequence;
            case OST.Leaderboard:
                return c_leaderboard;
        }

        Debug.LogWarning($"AUDIOCLIP NOT FOUND");
        return null;
    }

    public void PlaySFX(SFX sfx)
    {
        SFXData data = GetSFXFromEnum(sfx);
        data?.PlaySFX();
        data?.SetPitch(1f);
    }

    public void PlaySFX(SFX sfx, bool doLoop, bool randomisePitch)
    {
        SFXData data = GetSFXFromEnum(sfx);

        if (randomisePitch) data?.SetPitch(Random.Range(0.75f, 1.25f));
        else                data?.SetPitch(1f);

        if (doLoop) data?.PlaySFX(true);
        else        data?.PlaySFX();
    }

    public void PlayJumpSFX()
    {
        switch (Random.Range(1, 3))
        {
            case 1:
                PlaySFX(SFX.Jump1); break;
            case 2:
                PlaySFX(SFX.Jump2); break;
            case 3:
                PlaySFX(SFX.Jump3); break;
        }
    }

    private SFXData GetSFXFromEnum(SFX sfx)
    {
        foreach (SFXData data in sfxList)
            if (data.GetSFX() == sfx) return data;

        return null;
    }
}
