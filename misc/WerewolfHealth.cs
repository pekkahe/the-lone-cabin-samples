using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// Defines Werewolf specific health management. Werewolves have a special protective
/// mode which is triggered by a critical hit. For a short duration all received damage
/// is reduced with the cost of movement speed.
/// </summary>
public class WerewolfHealth : EnemyHealth
{
    private bool _protectiveMode = false;

    /// <summary>
    /// How much damage can be sustained from a hit until it's considered critical.
    /// </summary>
    public float CriticalHitThreshold = 20.0f;

    /// <summary>
    /// How much damage is reduced on protective mode. Value is multiplied with damage.
    /// </summary>
    public float ProtectiveModifier = 0.5f;

    public WerewolfSoundBank WerewolfSoundBank { get { return SoundBank as WerewolfSoundBank; } }
    public WerewolfAnimatorController WerewolfAnimator { get { return Animator as WerewolfAnimatorController; } }

    protected override void Awake()
    {
        base.Awake();
    }

    public override void ReceiveHit(Damage damage)
    {
        if (_protectiveMode)
            damage.Reduce(ProtectiveModifier);

        base.ReceiveHit(damage);

        if (HitPoints > 0)
        {
            if (damage.Total < CriticalHitThreshold)
            {
                WerewolfAnimator.ReceiveHitLight();
                WerewolfSoundBank.PlayHitSound();
            }
            else
            {
                StartCoroutine(CriticalHitRoutine());
            }
        }
    }

    private IEnumerator CriticalHitRoutine()
    {
        WerewolfAnimator.ReceiveHitMedium();
        WerewolfSoundBank.PlayCriticalHitSound(Random.Range(0.8f, 1.0f));

        AiController.Wait(1.0f);

        yield return new WaitForSeconds(0.5f);

        _protectiveMode = true;
        WerewolfAnimator.StartProtecting();

        var wasRunning = Motor.IsRunning;

        if (wasRunning)
            Motor.Walk();

        yield return new WaitForSeconds(4.0f);

        WerewolfAnimator.StopProtecting();
        _protectiveMode = false;

        if (wasRunning)
            Motor.Run();
    }
}
