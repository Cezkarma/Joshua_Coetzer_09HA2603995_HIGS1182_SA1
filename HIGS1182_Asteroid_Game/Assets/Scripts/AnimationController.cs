using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    private static readonly int IsThrustingId = Animator.StringToHash("IsThrusting");
    private static readonly int ShootId = Animator.StringToHash("Shoot");
    private static readonly int DieId = Animator.StringToHash("Die");

    public event Action DeathAnimationFinished;

    private Animator animator;
    private bool isThrusting;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("AnimationController: the Animator has no controller assigned, so the player will not animate.", this);
        }
    }

    public void SetThrusting(bool thrusting)
    {
        // Only push changes, so the Animator and console aren't updated every frame.
        if (thrusting == isThrusting)
        {
            return;
        }

        isThrusting = thrusting;
        animator.SetBool(IsThrustingId, thrusting);
        Debug.Log(thrusting ? "Animation: thrust." : "Animation: idle.");
    }

    public void PlayShoot()
    {
        animator.SetTrigger(ShootId);
    }

    public void PlayDeath()
    {
        animator.SetTrigger(DieId);
        Debug.Log("Animation: death.");
    }

    // Called by an Animation Event placed on the last frame of the Death clip.
    public void OnDeathAnimationFinished()
    {
        Debug.Log("Death animation finished.");
        DeathAnimationFinished?.Invoke();
    }
}
