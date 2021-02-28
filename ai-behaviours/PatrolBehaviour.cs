using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Basic behaviour where the enemy moves between the waypoints given to him.
/// </summary>
public class PatrolBehaviour : BaseBehaviour
{
    /// <summary>
    /// Positions which the enemy patrols.
    /// </summary>
    public List<Transform> PatrolPoints;

    /// <summary>
    /// Game area to patrol. Defines how <c>PatrolPoints</c> will be populated.
    /// </summary>
    public GameAreaEnum PatrollingArea;

    private Transform _lastPatrolPoint;
    private Vector3 _destination;

    public override void Initialize()
    {
        var enemyId = transform.parent.GetComponent<UniqueIdentifier>();

        // If no patrol points have been preset, read them from the GameManager
        if (PatrolPoints.Count == 0)
        {
            var gameArea = GameManager.Instance.GetGameArea(PatrollingArea);
            PatrolPoints = gameArea.PatrolPoints;
        }
        // If the patrol point collection contains invalid waypoints, reset them
        else if (PatrolPoints.Contains(null))
        {
            PatrolPoints = EnemyManager.Instance.GetPatrolPointsForEnemy(enemyId.Id);
        }

        EnemyManager.Instance.StorePatrolPointsForEnemy(enemyId.Id, PatrolPoints);
    }

    public override void Begin()
    {
        base.Begin();

        Debug.Log("Begin patrolling...");
    }

    public override void End()
    {
        base.End();
    }

    public override void Tick()
    {
        if (Debug.isDebugBuild)
            DrawDestination();

        if (PathFinder.IsSearchingForPath)
            return;

        if (PathFinder.HasPath)
        {
            FollowPath();
        }
        else
        {
            var point = GetPatrolPoint();

            PathFinder.FindPathTo(point.position);

            _destination = point.position;
        }
    }

    private void DrawDestination()
    {
        var origin = transform.position;
        origin.y += 3f;

        Debug.DrawLine(origin, _destination, Color.red);
    }

    private Transform GetRandomPatrolPoint()
    {
        // Random.Range is [inclusive, exclusive] so we have to use _patrolPoints.Count as max
        var index = UnityEngine.Random.Range(0, PatrolPoints.Count);

        return PatrolPoints[index];
    }

    private Transform GetPatrolPoint()
    {
        // If there's only one patrol point, return it
        if (PatrolPoints.Count == 1)
            return PatrolPoints[0];

        // Get a patrol point which wasn't recently used
        var patrolPoint = GetRandomPatrolPoint();
        while (patrolPoint == _lastPatrolPoint)
            patrolPoint = GetRandomPatrolPoint();

        _lastPatrolPoint = patrolPoint;

        return patrolPoint;
    }

    public override void OnPathTraversed()
    {
        Wait(1.0f);
    }

    protected override void HandleDoor(OpenableDoor door)
    {
        // Only open the door if we are is facing it. This somewhat simulates if the door is in our path.
        if (door.IsClosed && PathFinder.IsOnPath(door.transform))
        {
            var success = OpenDoor(door);
            if (!success)
                Debug.LogWarning("Failed to open door.");
        }
    }
}