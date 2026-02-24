using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [SerializeField] private SoundManager.OST toPlay;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent(out PlayerController player)) return;

        if (SoundManager.instance.CheckIfPlaying(toPlay)) return;

        SoundManager.instance.PlaySong(toPlay, true);
    }
}
