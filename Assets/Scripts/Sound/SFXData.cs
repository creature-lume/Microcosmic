using UnityEngine;

public class SFXData : MonoBehaviour
{
    [SerializeField] private SoundManager.SFX sfx;
    private AudioSource source;

    public SoundManager.SFX GetSFX() { return sfx; }

    private void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void SetPitch(float pitch)
    {
        source.pitch = pitch;
    }

    public void PlaySFX()
    {
        source.loop = false;
        source.Play();
    }

    public void PlaySFX(bool doLoop)
    {
        source.loop = doLoop;
        source.Play();
    }
}
