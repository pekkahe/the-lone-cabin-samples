using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Aggressive behaviour where the enemy will try to reach the player and attack him.
/// </summary>
public class PursueBehaviour : BaseBehaviour
{
    /// <summary>
    /// How long until this behaviour should be stopped.
    /// </summary>
    public float PursueTime = 10.0f;

    /// <summary>
    /// Whether to pursue the player indoors or not.
    /// </summary>
    public bool DontPursueIndoors;

    private Transform _transform;
    private CapsuleCollider _collider;
    private EnemyAttackExecutor _attackExecutor;
    private Vector3 _lastHeardPlayerPosition;
    private Vector3 _lastSeenPlayerPosition;
    private float _pursueTimer;
    private float _defaultStuckInterval;

    private const float _stuckInspectionInterval = 1.0f;

    protected override void Awake()
    {
        base.Awake();

        _transform = transform;
        _collider = transform.parent.collider as CapsuleCollider;
        _attackExecutor = GetComponent<EnemyAttackExecutor>();
    }

    public override void Begin()
    {
        base.Begin();

        LineOfSight.Extend();

        // If enemy can't see player when beginning pursuit, find a path
        if (!LineOfSight.CanSeePlayer)
            PathFinder.FindPathTo(Player.Get.Position);

        _pursueTimer = PursueTime;

        Debug.Log("Begin pursuing...");
    }

    public override void End()
    {
        base.End();

        LineOfSight.Reset();
    }

    public override void Tick()
    {
        if (HasLostInterest())
        {
            AiController.Patrol(2.0f);
            return;
        }

        if (LineOfSight.CanSeePlayer)
        {
            OnPlayerSeen(Player.Get.Position);

            // Attack player if possible, or continue pursuing
            if (_attackExecutor.CanHitPlayer())
            {
                _attackExecutor.AttackPlayer();
            }
            else
            {
                PursuePlayer();
            }
        }
        else if (PathFinder.HasPath)
        {
            FollowPath();
        }
        else if (!PathFinder.IsSearchingForPath)
        {
            FindPlayer();
        }
    }

    public bool HasLostInterest()
    {
        if (Player.IsIndoors && DontPursueIndoors)
            return true;

        _pursueTimer -= Time.deltaTime;

        return _pursueTimer < 0;
    }

    private void PursuePlayer()
    {
        if (CanMoveToPlayer())
        {
            if (PathFinder.HasPath)
                PathFinder.ClearPath();

            Motor.MoveTo(Player.Get.Position);
        }
        else if (PathFinder.HasPath)
        {
            FollowPath();
        }
        else if (!PathFinder.IsSearchingForPath)
        {
            PathFinder.FindPathTo(Player.Get.Position);
        }
    }

    private bool CanMoveToPlayer()
    {
        // Add small offset to start cast above ground
        var groundOffset = 0.5f;

        var radius = _collider.radius;

        // Create the bottom point of the capsule, taking into account that the collider's
        // pivot point might not necessary be same as transform.position.
        var p1 = _transform.position + _collider.center + Vector3.up * (-_collider.height * 0.5f);

        // Add ground offset and collider radius to the bottom point
        p1.y += groundOffset + radius;

        // Create the top point of the capsule, with radius
        var p2 = p1 + Vector3.up * _collider.height;

        // Substract the offset previously added to p1 and the collider radius from the top point
        p2.y -= groundOffset - radius;

        var direction = Player.Get.Position - _transform.position;

        RaycastHit hit;

        //Debug.DrawRay(p1, direction, Color.blue);
        //Debug.DrawRay(p2, direction, Color.cyan);

        // According to Unity documentation, the points p1 and p2 are the centers of the bottom and top ends
        // of the capsule, and radius extends them. This was taken into account when both points were created.
        if (Physics.CapsuleCast(p1, p2, radius, direction, out hit, LineOfSight.SightDistance, 
            LayerMaskStorage.EnemyCanMoveToPlayerMask))
        {
            //Debug.DrawLine(p1, hit.point, Color.cyan);
            //Debug.Log(hit.transform);

            return hit.transform.CompareTag("Player");
        }

        // If we hit nothing, we must be able to move to the direction.
        // However, this code should never be reached if we already see the player.
        return true;
    }

    private void FindPlayer()
    {
        if (!PathFinder.HasSearchedTarget(_lastSeenPlayerPosition))
        {
            PathFinder.FindPathTo(_lastSeenPlayerPosition);

            Wait(0.5f);
        }
        else if (!PathFinder.HasSearchedTarget(_lastHeardPlayerPosition))
        {
            PathFinder.FindPathTo(_lastHeardPlayerPosition);

            // Small delay to simulate hearing, but mainly to prevent ping ponging
            Wait(0.5f);
        }
        else
        {
            Debug.Log("Found no trace of player. Waiting...");

            Wait(1.0f);
        }
    }

    public override void OnPlayerSeen(Vector3 position)
    {
        // Keep pursue timer maxed as long as enemy sees player
        _pursueTimer = PursueTime;

        _lastSeenPlayerPosition = position;
    }

    public override void OnPlayerHeard(Vector3 position)
    {
        _lastHeardPlayerPosition = position;
    }

    public override void DoWhileWaiting()
    {
        if (LineOfSight.CanSeePlayer)
            OnPlayerSeen(Player.Get.Position);
    }

    public override void OnPathFound(Path path)
    {
        // If no path was found to target, start patrolling
        if (!path.IsValid)
        {
            Debug.Log("No path found to pursued player. Starting to patrol.");
            AiController.Patrol(2.0f);
        }
        else
        {
            SkipWaypointsBehind(path, 1);
        }
    }

    private void SkipWaypointsBehind(Path path, int maxSkip)
    {
        var waypoints = PathFinder.GetWaypoints();
        var skipCount = waypoints.Count < maxSkip ? waypoints.Count : maxSkip;

        // Check the first three waypoints
        for (int i = 0; i < skipCount; i++)
        {
            var waypoint = waypoints[i];

            // Calculate dot product to check if enemy has moved passed the waypoint, and skip it he has
            var waypointDirection = waypoint - _transform.position;
            waypointDirection.Normalize();

            // If dot is below 0, the waypoint is over 90 degrees behind the enemy.
            // If dot is below -0.5, the waypoint is over 120 degrees behind the enemy.
            // If dot is below -0.866, the waypoint is over 150 degrees behind the enemy.
            var dot = Vector3.Dot(_transform.forward, waypointDirection);
            if (dot < -0.866f)
            {
                PathFinder.SkipWaypoint();
                Debug.Log("Waypoint " + i + " is behind. Skipping it.");
            }
        }
    }

    protected override void HandleDoor(OpenableDoor door)
    {
        // If enemy can see player, do nothing since most probably this door is not in the enemy's path
        if (LineOfSight.CanSeePlayer)
            return;

        // If door is being locked, do nothing
        if (door.IsBeingLocked)
            return;

        // TODO: Handle player locking himself in a room; how to detect and what actions to take?
        //       What if the player manages to lock an enemy inside a room? (More of a path manager problem)
        // NOTE: Above do not necessary only happen when pursuing! (Do not restrict to this behaviour)

        if (door.IsLocked)
        {
            Debug.Log("Door is locked.");

            if (!PathFinder.HasPath || (!PathFinder.IsCurrentTarget(_lastHeardPlayerPosition) &&
                                        !PathFinder.HasSearchedTarget(_lastHeardPlayerPosition)))
            {
                PathFinder.FindPathTo(_lastHeardPlayerPosition);

                Debug.Log("Trying to get around by finding path to last heard position " + _lastHeardPlayerPosition);
            }
        }
        // If door is closing or closed, attempt to open it
        else if (door.IsClosing || door.IsClosed)
        {
            var success = OpenDoor(door);

            if (!success)
            {
                Debug.LogWarning("Failed to open door.");
            }
        }
    }
}
