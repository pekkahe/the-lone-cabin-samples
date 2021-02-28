using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Path finder component for AI characters. Uses <see cref="AStarPathFinder"/> internally.
/// </summary>
public class PathFinder : MonoBehaviour
{
    private BehaviourController _aiController;
    private Path _path;
    private Path[] _pathHistory;
    private AStarPathFinder _pathFinder;

    /// <summary>
    /// Vertical offset added to character's position for path finding to negate
    /// any ground unevenness.
    /// </summary>
    public float GroundOffset = 1f;

    /// <summary>
    /// How close can a character be to a waypoint until it's considered reached.
    /// </summary>
    public float WaypointReachedThreshold = 0.5f;

    /// <summary>
    /// Maximum number of consecutive paths stored.
    /// </summary>
    public int MaxPathHistoryEntries = 3;

    public int InvalidPathCount { get; private set; }
    public bool HasPath { get { return _path != null && _path.IsValid && !_path.IsEmpty; } }
    public bool IsSearchingForPath { get { return _pathFinder != null && _pathFinder.IsRunning; } }

    void Awake()
    {
        _aiController = transform.parent.GetComponentInChildren<BehaviourController>();
        _pathHistory = new Path[MaxPathHistoryEntries];
    }

    void Update()
    {
        if (_pathFinder != null && _pathFinder.IsDone)
        {
            OnPathFound(_pathFinder.Path);
            _pathFinder = null;
        }
    }

    /// <summary>
    /// Find the shortest path from the actor's current position to the given position.
    /// </summary>
    /// <remarks>
    /// <c>Update</c> calls <see cref="OnPathFound"/> when path finding is completed.
    /// </remarks>
    public void FindPathTo(Vector3 position)
    {
        if (position == Vector3.zero)
        {
            Debug.LogError("Cannot find path to a zero vector.");
            return;
        }

        ClearPath();

        var start = transform.position;
        var end = position;

        // Increase the vertical positions for the start and end points,
        // to ensure they are above ground when the vibisility graph is built
        start.y += GroundOffset;
        end.y += GroundOffset;

        // Initialize the path finder with a copy of the visibility graph
        var visibilityGraph = SharedVisibilityGraph.Instance.GetCopy();

        _pathFinder = new AStarPathFinder(visibilityGraph);
        _pathFinder.Start(start, end);
    }

    /// <summary>
    /// Aborts and removes any existing path finder worker threads and paths.
    /// </summary>
    public void ClearPath()
    {
        if (_pathFinder != null)
        {
            _pathFinder.Abort();
            _pathFinder = null;
        }

        _path = null;
    }

    public void SkipWaypoint()
    {
        if (_path != null)
        {
            _path.MoveToNextWaypoint();
        }
    }

    public Path GetPath()
    {
        return _path;
    }

    /// <summary>
    /// Updates path parameters based on character's current position. Should be called periodically.
    /// </summary>
    public void UpdatePath()
    {
        if (HasReachedWaypoint())
        {
            if (_path.IsTraversed)
            {
                ClearPath();
                _aiController.OnPathTraversed();
            }
            else
            {
                _path.MoveToNextWaypoint();
            }
        }
    }

    /// <summary>
    /// Returns true if the given position is the current path's target position, false otherwise.
    /// </summary>
    public bool IsCurrentTarget(Vector3 position)
    {
        // Add ground offset to position, since it's in the Path's target position too
        position.y += GroundOffset;

        return _path != null && _path.Target == position;
    }

    /// <summary>
    /// Returns true if the given position has already been targeted on any of the
    /// latest path findings, false otherwise.
    /// </summary>
    public bool HasSearchedTarget(Vector3 position)
    {
        // Add ground offset to position, since it's in the Path's target position too
        position.y += GroundOffset;

        for (int i = 0; i < _pathHistory.Length; i++)
        {
            var path = _pathHistory[i];

            if (path == null || !path.IsValid || !path.IsTraversed)
                continue;

            if (path.Target == position)
                return true;
        }

        return false;
    }

    public Vector3 GetCurrentWaypoint()
    {
        return _path.GetCurrentWaypoint();
    }

    public List<Vector3> GetWaypoints()
    {
        return _path.GetWaypoints();
    }

    /// <summary>
    /// Returns true if the given <c>Transform</c> is on the current path, false otherwise.
    /// </summary>
    public bool IsOnPath(Transform target)
    {
        if (!HasPath)
            return false;

        return _path.IsOnPath(target);
    }

    /// <summary>
    /// Debug helper. Draws the current path on debug builds.
    /// </summary>
    public void DrawPath()
    {
        if (_path != null)
            _path.Draw();
    }

    /// <summary>
    /// Returns true if the character has reached its current waypoint, false otherwise.
    /// </summary>
    private bool HasReachedWaypoint()
    {
        var waypoint = GetCurrentWaypoint();

        var distance = waypoint - transform.position;

        return distance.magnitude < WaypointReachedThreshold;
    }

    /// <summary>
    /// Called by <c>Update</c> when path finding is completed.
    /// </summary>
    private void OnPathFound(Path path)
    {
        if (path == null)
        {
            Debug.LogError("No path found.");
            return;
        }

        if (!path.IsValid)
        {
            InvalidPathCount++;
            Debug.LogError("Path is invalid.");
        }
        else
        {
            InvalidPathCount = 0;
        }

        _path = path;

        AddToHistory(path);

        _aiController.OnPathFound(path);
    }

    private void AddToHistory(Path path)
    {
        // Push existing paths in history array one index forward,
        // and prepend new path to the start of the array.
        for (int i = _pathHistory.Length - 1; i > 0; i--)
        {
            if (i - 1 >= 0)
                _pathHistory[i] = _pathHistory[i - 1];
        }

        _pathHistory[0] = path;
    }
}
