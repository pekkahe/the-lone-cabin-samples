using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Base class for all AI behaviours.
/// </summary>
[RequireComponent(typeof(BehaviourController))]
public abstract class BaseBehaviour : MonoBehaviour
{
    private bool _isHandlingDoor;
    private const float _doorHandlerInterval = 0.5f;

    protected float DefaultFrequency;
    protected PathFinder PathFinder;
    protected LineOfSight LineOfSight;
    protected EnemyMovementMotor Motor;
    protected BehaviourController AiController;

    /// <summary>
    /// AI tick frequency in seconds. 
    /// </summary>
    public float Frequency = 0.5f;

    protected virtual void Awake()
    {
        DefaultFrequency = Frequency;

        Motor = transform.parent.GetComponent<EnemyMovementMotor>();
        PathFinder = transform.parent.GetComponentInChildren<PathFinder>();
        LineOfSight = transform.parent.GetComponentInChildren<LineOfSight>();

        AiController = GetComponent<BehaviourController>();
    }

    public virtual void Initialize()
    { }

    public virtual void Begin()
    {
        PathFinder.ClearPath();

        SetFrequency(DefaultFrequency);
    }

    public virtual void End()
    {
        ResetFrequency();
    }

    public void SetFrequency(float frequency)
    {
        Frequency = frequency;
        LineOfSight.CheckInterval = frequency;
    }

    public void ResetFrequency()
    {
        SetFrequency(DefaultFrequency);
    }

    public virtual void Tick()
    { }

    protected abstract void HandleDoor(OpenableDoor door);

    public void Wait(float timeInSeconds)
    {
        AiController.Wait(timeInSeconds);
    }

    public void FollowPath()
    {
        var waypoint = PathFinder.GetCurrentWaypoint();

        Motor.MoveTo(waypoint);

        PathFinder.DrawPath();
        PathFinder.UpdatePath();

        if (Debug.isDebugBuild)
        {
            Debug.DrawLine(new Vector3(
                transform.position.x, transform.position.y + 0.5f, transform.position.z),
                waypoint, Color.green, 0.1f);
        }
    }

    protected bool OpenDoor(OpenableDoor door)
    {
        var success = door.Open();
        if (success)
        {
            Wait(0.5f);

            // NOTE: Tried to disable collisions between layers DoorUnlocked and EnemyAtDoor
            // instead, but this caused the enemies to move through doors to reach the player.

            StartCoroutine(IgnoreCollisionsWith(door.collider, 5.0f));
            //StartCoroutine(IgnoreCollisionsUntilOpen(door)); // Doesn't work as well
        }

        return success;
    }

    private IEnumerator IgnoreCollisionsUntilOpen(OpenableDoor door)
    {
        var enemy = transform.parent;

        if (enemy == null)
            yield break;

        Physics.IgnoreCollision(enemy.collider, door.collider, true);

        yield return new WaitForSeconds(1f);

        if (enemy == null)
            yield break;

        // Restore collisions if the door is open or if the enemy has already moved away from the door
        if (door.IsOpen || enemy.gameObject.layer != LayerMaskStorage.EnemyAtDoorLayer)
        {
            Physics.IgnoreCollision(enemy.collider, door.collider, false);
        }
        else
        {
            // If door is still open or opening, keep ignoring collisions
            StartCoroutine(IgnoreCollisionsUntilOpen(door));
        }
    }

    private IEnumerator IgnoreCollisionsWith(Collider other, float time)
    {
        var enemy = transform.parent;

        Physics.IgnoreCollision(enemy.collider, other, true);

        yield return new WaitForSeconds(time);

        if (enemy == null)
        {
            Debug.LogWarning("Enemy has died while collisions are ignored.");
            yield break;
        }

        Physics.IgnoreCollision(enemy.collider, other, false);
    }

    public void OnDoorStay(OpenableDoor door)
    {
        if (!_isHandlingDoor)
            StartCoroutine(DelayedDoorHandler(door));
    }

    private IEnumerator DelayedDoorHandler(OpenableDoor door)
    {
        _isHandlingDoor = true;

        yield return new WaitForSeconds(_doorHandlerInterval);

        _isHandlingDoor = false;

        HandleDoor(door);
    }

    public virtual void DoWhileWaiting()
    { }

    public virtual void OnPlayerSeen(Vector3 position)
    { }

    public virtual void OnPlayerHeard(Vector3 position)
    { }

    public virtual void OnPathFound(Path path)
    { }

    public virtual void OnPathTraversed()
    { }
}