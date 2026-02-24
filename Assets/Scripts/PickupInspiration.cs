using System.Collections;
using UnityEngine;

public class PickupInspiration : MonoBehaviour
{
    [HideInInspector] public static int pickupCount = 0;
    [HideInInspector] public static int totalCount  = 0;

    [SerializeField] protected int gaugeAmount;
    [SerializeField] protected ParticleSystem fx;
    [SerializeField] protected float deactivationDelay = 1.0f;
    [SerializeField] protected GameObject branch1;
    [SerializeField] protected GameObject branch2;
    protected bool canBePickedUp;
    protected bool wasPickedUp;
    private AudioSource source;

    private void Awake()
    {
        totalCount++;
    }

    virtual protected void Start()
    {
        canBePickedUp = true;
        wasPickedUp   = false;
        source = GetComponent<AudioSource>();
    }

    virtual protected void OnTriggerEnter2D(Collider2D other)
    {
        if (!canBePickedUp || wasPickedUp) return;

        if (other.TryGetComponent(out PlayerBoost player))
        {
            source.Play();

            player.AddGauge(gaugeAmount);
            fx.Play();
            branch1.SetActive(false); 
            branch2.SetActive(false);
            wasPickedUp = true;
            pickupCount++;
        }
    }
}
