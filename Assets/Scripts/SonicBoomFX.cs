using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class SonicBoomFX : MonoBehaviour
{
    [SerializeField] LaunchSequence launchSequence;
    [SerializeField] List<ParticleSystem> okParticles;
    [SerializeField] List<ParticleSystem> goodParticles;
    [SerializeField] List<ParticleSystem> perfectParticles;

    private void Start()
    {
        Debug.LogWarning("Script flagged as using the deprecated PlayerController");

        if (launchSequence == null)
        {
            Debug.LogWarning("Launch Sequence Reference not set");
            enabled = false;
            return;
        }

        enabled = true;
        launchSequence.LaunchPlayer.AddListener(OnLaunch);
    }

    public void OnLaunch(Prompt.PromptQuality quality)
    {
        switch (quality)
        {
            case Prompt.PromptQuality.Failed:
                return;
            case Prompt.PromptQuality.Ok:
                foreach (ParticleSystem particle in okParticles) particle.Play();
                break;
            case Prompt.PromptQuality.Good:
                foreach (ParticleSystem particle in goodParticles) particle.Play();
                break;
            case Prompt.PromptQuality.Perfect:
                foreach (ParticleSystem particle in perfectParticles) particle.Play();
                break;
        }

        enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out NewPlayerController nPlayer)) gameObject.SetActive(false);
        else if (collision.TryGetComponent(out PlayerController player)) gameObject.SetActive(false);
    }
}
