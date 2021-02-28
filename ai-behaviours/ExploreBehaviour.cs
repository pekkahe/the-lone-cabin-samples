using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A specific behaviour where the enemy tries reach the position given on initialization.
/// </summary>
public class ExploreBehaviour : BaseBehaviour
{
    /// <summary>
    /// How much the player must move from the last failed explorable position to regain enemy's interest.
    /// </summary>
    public float FailureRadius = 15f;

    /// <summary>
    /// Allow this behaviour to be triggered when the search target is indoors or not.
    /// </summary>
    public bool DontExploreIndoors;

    private Vector3 _lastPathTarget;
    private float _hearingCooldown;
    private float _failureTime;
    private bool _hasFailed;

    private const float _updateSearchLocationInterval = 3f;
    private const float _rememberFailureTime = 10f;

    public override void Begin()
    {
        base.Begin();

        Debug.Log("Begin exploring...");
    }

    public override void End()
    {
        base.End();
    }

    public override void Tick()
    {
        if (PathFinder.IsSearchingForPath)
            return;

        if (PathFinder.HasPath)
        {
            FollowPath();
        }
        else
        {
            Debug.Log("Couldn't find a path. Starting to patrol.");

            AiController.Patrol(2.0f);
        }
    }

    public bool CanExplorePosition(Vector3 position)
    {
        // If we have failed previously, allow exploration if the target position
        // is not inside the failure radius
        if (HasPreviouslyFailed())
            return Vector3.Distance(position, _lastPathTarget) > FailureRadius;

        return true;
    }

    private bool HasPreviouslyFailed()
    {
        return _hasFailed && Time.time < _failureTime + _rememberFailureTime;
    }

    public override void OnPlayerHeard(Vector3 position)
    {
        if (_hearingCooldown > 0)
        {
            _hearingCooldown -= Time.deltaTime;
        }
        else
        {
            PathFinder.FindPathTo(position);

            _hearingCooldown = _updateSearchLocationInterval;
        }
    }

    public override void OnPathFound(Path path)
    {
        _lastPathTarget = path.Target;

        if (!path.IsValid)
        {
            Debug.LogWarning("Failed to find path to explorable position.");

            _hasFailed = true;
            _failureTime = Time.time;
        }
        else
        {
            _hasFailed = false;
        }
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
