using UnityEngine;

public class Bumper : MonoBehaviour
{
    // *-* BUMPERS WILL NEED TO BE REFACTORED WITH THE LAUNCH DIR

    [SerializeField] OldPlayerController.LaunchDir bumpDir;
    [SerializeField] float bumpPower = 52f;

    private void Start()
    {
        //Debug.LogWarning("Script flagged as using the deprecated PlayerController");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerController playerController))
        {
            //playerController.Bump(bumpDir, bumpPower);s
        }
    }
}
