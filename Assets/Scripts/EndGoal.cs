using UnityEngine;

public class EndGoal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out PlayerState playerState))
        {
            LevelManager.instance.EndLevel();
        }
    }
}
