using UnityEngine;

public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out PlayerState playerState))
        {
            Debug.Log("OUT OF BOUNDS");
            playerState.OHKO();
        }
    }
}
