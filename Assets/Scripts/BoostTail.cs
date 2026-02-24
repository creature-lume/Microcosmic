#pragma warning disable CS0618 // Type or member is obsolete
using UnityEngine;

public class BoostTail : MonoBehaviour
{
    [SerializeField] private float lifetime;
    [SerializeField] ParticleSystem particle;
    [SerializeField] Transform follow;

    private void Start()
    {
        if (!particle) particle = GetComponent<ParticleSystem>();

        particle.startLifetime = 0;
    }

    private void FixedUpdate()
    {
        transform.position = follow.position;
    }

    public void OnBoostUpdate(bool isBoosting)
    {
        if (isBoosting) particle.startLifetime = lifetime;
        else            particle.startLifetime = 0;
    }
}
#pragma warning restore CS0618 // startLifetime is obsolete and idc cause the other one doesn't work at all