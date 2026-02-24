using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerBoost player))
        {
            if (!player.IsBoosting()) return;

            SoundManager.instance.PlaySFX(SoundManager.SFX.Break);
            gameObject.SetActive(false);
        }
    }
}
