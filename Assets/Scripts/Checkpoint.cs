using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] bool hideCheckpoints = true;

    private void Start()
    {
        sprite.color = Color.black;
        if (hideCheckpoints) sprite.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hideCheckpoints) return;
        if (collision.TryGetComponent(out PlayerBoost playerBoost))
        {
            sprite.color = Color.blue;
            playerBoost.AddGauge(100);
            SoundManager.instance.PlaySFX(SoundManager.SFX.Checkpoint);
        }
    }
}
