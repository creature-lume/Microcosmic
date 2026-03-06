using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayerState : MonoBehaviour
{
    [SerializeField] PlayerBoost boost;
    [SerializeField] int baseLives;
    private Transform activeCheckpoint;
    public UnityEvent PlayerHurtEvent;
    public UnityEvent<Vector3> LifeLossEvent;
    public UnityEvent GameoverEvent;
    private int lives;
    public static GameState currentState;
    private float savedTime;

    public enum GameState
    {
        Alive,
        Invincible,
        Dead
    }

    public int GetLives() { return lives; }

    public bool GetHurt(bool isBoostProof)
    {
        if (!isBoostProof && boost.IsBoosting()) return false;

        if (currentState != GameState.Alive) return true;

        //If the player should lose Gauge
        /*
        if (!boost.IsGaugeDepleted()) 
        {
            PlayerHurtEvent.Invoke();
            return true;
        }
        */

        //If the player should lose a Life
        SetLives(lives - 1);
        /*
        if (lives <= 0)
        {
            SoundManager.instance.PlaySFX(SoundManager.SFX.Gameover);
            GameoverEvent.Invoke();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return true;
        }
        */
        currentState = GameState.Dead;
        LifeLossEvent.Invoke(activeCheckpoint.position);
        return true;
    }

    public void OHKO()
    {
        currentState = GameState.Dead;
        SetLives(lives - 1);
        if (lives <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            GameoverEvent.Invoke();
        }
        if (activeCheckpoint) LifeLossEvent.Invoke(activeCheckpoint.position);
        else Debug.LogWarning("NO ACTIVE CHECKPOINTS SET");
    }

    private void Start()
    {
        currentState = GameState.Alive; //TEMPORARY
        SetLives(baseLives);

        if (!boost) boost = GetComponent<PlayerBoost>();
    }

    public void OnStartLevel(Prompt.PromptQuality quality)
    {
        if (quality == Prompt.PromptQuality.Perfect) SetLives(lives + 1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Checkpoint checkpoint))
        {
            activeCheckpoint = checkpoint.transform;
            savedTime = GUITimer.instance.GetTimer();
        }
    }

    private void SetLives(int value)
    {
        lives = value;
        GUIManager.instance.UpdateLifeCount(lives.ToString());
    }

    public void OnRespawn()
    {
        currentState = GameState.Alive;
        GUITimer.instance.SetTime(savedTime);
    }
}
