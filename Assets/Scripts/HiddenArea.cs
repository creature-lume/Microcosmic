using UnityEngine;

public class HiddenArea : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        //animator.SetBool("Hide", true);
        //animator.SetBool("Show", false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out PlayerState state)) animator.SetTrigger("Hide");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out PlayerState state)) animator.SetTrigger("Show");
    }
}
