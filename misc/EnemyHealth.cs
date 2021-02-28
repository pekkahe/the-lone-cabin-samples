using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BloodColor
{
    Red,
    Green
}

/// <summary>
/// Base class for enemy health management. Handles receiving damage and dying.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyHealth : MonoBehaviour
{
    /// <summary>
    /// How much health the enemy has left.
    /// </summary>
    public float HitPoints;

    /// <summary>
    /// Color for blood splatter.
    /// </summary>
    public BloodColor BloodColor;

    /// <summary>
    /// Mesh renderer used to detect position of blood splatter.
    /// </summary>
    public SkinnedMeshRenderer MeshRenderer;

    /// <summary>
    /// Prefab used as the enemy's corpse <c>GameObject</c>. Kept in the scene.
    /// </summary>
    public GameObject CorpsePrefab;

    /// <summary>
    /// Time in seconds of how long to wait until enemy <c>GameObject</c> is 
    /// replaced with <c>CorpsePrefab</c> after death.
    /// </summary>
    public float DeathAnimationTime = 3.0f;

    protected BehaviourController AiController;
    protected EnemyMovementMotor Motor;
    protected EnemySoundBank SoundBank;
    protected EnemyAnimatorController Animator;

    protected virtual void Awake()
    {
        Motor = GetComponent<EnemyMovementMotor>();
        Animator = GetComponent<EnemyAnimatorController>();
        AiController = GetComponentInChildren<BehaviourController>();
        SoundBank = GetComponentInChildren<EnemySoundBank>();
    }

    public virtual void ReceiveHit(Damage damage)
    {
        if (Debug.isDebugBuild)
            Debug.Log(gameObject.name + " was hit with " + damage.Hits.Count + 
                " hits. Received " + damage.Total + " damage.");

        HitPoints -= damage.Total;

        Knockback(damage);

        SoundBank.PlayImpactSound(Random.Range(0.9f, 1.0f));

        EmitBloodSplatter(damage);

        if (HitPoints < 0)
            Die();
    }

    private void Knockback(Damage damage)
    {
        var hitDirection = damage.GetAverageDirection();

        hitDirection.Normalize();

        var knockbackForce = damage.Total * damage.KnockbackForce * -hitDirection;

        // Prevent excessive vertical force, expecially upwards
        knockbackForce.y = Mathf.Clamp(knockbackForce.y, -300f, 100f);

        rigidbody.AddForce(knockbackForce, ForceMode.Impulse);
    }

    public void Die()
    {
        StartCoroutine(DieRoutine());

        // Trigger any death triggers this enemy might have
        foreach (var trigger in GetComponents<GameTrigger>())
            trigger.Trigger();
    }

    public virtual void EmitBloodSplatter(Damage damage)
    {
        foreach (var hit in damage.Hits)
        {
            // Get the point on mesh which received the hit
            var meshPoint = GetClosestPointOnMesh(hit.Point);

            if (BloodColor == BloodColor.Red)
                ParticleManager.Instance.EmitRedBloodSplatter(meshPoint, hit.Direction);
            else
                ParticleManager.Instance.EmitGreenBloodSplatter(meshPoint, hit.Direction);
        }
    }

    public Vector3 GetClosestPointOnMesh(Vector3 point)
    {
        // Convert point to local space
        var localPoint = transform.InverseTransformPoint(point);

        var mesh = MeshRenderer.sharedMesh;
        var minDistanceSqr = Mathf.Infinity;
        var closestPoint = Vector3.zero;

        // Scan all vertices to find the closest
        foreach (var vertex in mesh.vertices)
        {
            var distance = localPoint - vertex;
            var distanceSqr = distance.sqrMagnitude;

            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestPoint = vertex;
            }
        }

        // Convert closest point back to world space
        return transform.TransformPoint(closestPoint);
    }

    private IEnumerator DieRoutine()
    {
        Animator.Die();

        SoundBank.PlayDeathSound();

        Disable();

        // Wait for the die animation to finish
        yield return new WaitForSeconds(DeathAnimationTime);

        Cleanup(GetCorpseContainer());
    }

    private Transform GetCorpseContainer()
    {
        // Add small offset incase corpse transform position is just below the cabin floor
        var offsetPosition = transform.position;
        offsetPosition.y += 0.1f;

        var ground = Common.GetGroundTransform(offsetPosition);
        if (ground != null)
        {
            var floor = ground.FindParent("FirstFloor", LayerMaskStorage.CabinLayer);
            if (floor != null)
                return floor;
        }

        return EnemyManager.Instance.CorpseContainer.transform;
    }

    protected virtual void Disable()
    {
        AiController.Disable(true);

        Motor.enabled = false;

        rigidbody.isKinematic = true;

        collider.enabled = false;
    }

    protected virtual void Cleanup(Transform corpseContainer)
    {
        // Instantiate a corpse prefab on place of this enemy
        var corpse = Instantiate(CorpsePrefab, transform.position, transform.rotation) as GameObject;

        // Move corpse to container
        corpse.transform.parent = corpseContainer;

        // Destroy original enemy object
        Destroy(gameObject);
    }
}