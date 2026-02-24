using UnityEngine;

public class HurtCollider : MonoBehaviour
{
    [SerializeField] bool isBoostProof = true;
    [SerializeField] bool shouldOHKO   = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerState player))
        {
            if (shouldOHKO)
            {
                player.OHKO();
                return;
            }

            if (!player.GetHurt(isBoostProof))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
