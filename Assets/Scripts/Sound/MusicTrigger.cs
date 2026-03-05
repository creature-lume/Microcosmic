using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [SerializeField] private SoundManager.OST toPlay;

    private void Start()
    {
        //Debug.LogWarning("Script flagged as using the deprecated PlayerController");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent(out PlayerController player) && !collision.TryGetComponent(out NewPlayerController nPlayer)) return;

        if (SoundManager.instance.CheckIfPlaying(toPlay)) return;

        SoundManager.instance.PlaySong(toPlay, true);
    }
}
