using UnityEngine;

public class Bumper : MonoBehaviour
{
    [SerializeField] PlayerController.LaunchDir bumpDir;
    [SerializeField] float bumpPower = 52f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerController playerController))
        {
            playerController.Bump(bumpDir, bumpPower);
        }
    }
}
