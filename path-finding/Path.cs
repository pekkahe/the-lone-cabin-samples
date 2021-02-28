using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A route between two points. The end result of <see cref="AStarPathFinder"/>.
/// </summary>
public class Path
{
    private int _currentIndex = 0;
    private List<PathWaypoint> _waypoints = new List<PathWaypoint>();

    /// <summary>
    /// The target position to which path finding was used for.
    /// </summary>
    /// <remarks>
    /// If path finding failed this will differ from <c>Path</c>'s last waypoint.
    /// </remarks>
    public Vector3 Target { get; private set; }

    public bool IsValid { get; set; }
    public bool IsEmpty { get { return _waypoints.Count == 0; } }
    public bool IsTraversed { get { return _currentIndex >= _waypoints.Count - 1; } }
    public int CurrentIndex { get { return _currentIndex; } }

    /// <summary>
    /// Instantiate a <c>Path</c> with the given positions and target.
    /// </summary>
    /// <param name="waypoints">The path's position vectors.</param>
    /// <param name="target">The path's target position.</param>
    public Path(List<Vector3> waypoints, Vector3 target)
    {
        foreach (var waypoint in waypoints)
            _waypoints.Add(new PathWaypoint(waypoint));

        IsValid = true;
        Target = target;
    }

    public bool IsOnPath(Transform target)
    {
        var startIndex = _currentIndex - 1;

        if (startIndex < 0)
            startIndex = 0;

        for (int i = startIndex; i < _waypoints.Count; i++)
        {
            // If this is not the last point, raycast to next point
            if (i + 1 < _waypoints.Count)
            {
                var layerMask = 1 << target.gameObject.layer;
                RaycastHit hit;

                if (Physics.Linecast(_waypoints[i].WorldPosition, _waypoints[i + 1].WorldPosition,
                    out hit, layerMask))
                {
                    // If we hit the target, it's on our path
                    if (hit.transform == target)
                        return true;
                }
            }
        }

        // Our linecasts didn't hit the target, so it can't be on our path
        return false;
    }

    public Vector3 GetCurrentWaypoint()
    {
        return _waypoints[_currentIndex].GroundPosition;
    }

    public List<Vector3> GetWaypoints()
    {
        var waypoints = new List<Vector3>();

        foreach (var waypoint in _waypoints)
            waypoints.Add(waypoint.GroundPosition);

        return waypoints;
    }

    public void MoveToNextWaypoint()
    {
        if (_currentIndex < _waypoints.Count - 1)
            _currentIndex++;
    }

    /// <summary>
    /// Draws the path using <see cref="Debug.DrawLine"/>.
    /// </summary>
    public void Draw()
    {
        var startIndex = _currentIndex - 1;
        if (startIndex < 0)
            startIndex = 0;

        for (int i = startIndex; i < _waypoints.Count; i++)
        {
            // If this is not the last point, draw line to next point
            if (i + 1 < _waypoints.Count)
            {
                Debug.DrawLine(_waypoints[i].WorldPosition, _waypoints[i + 1].WorldPosition, Color.white);
            }
        }
    }

    /// <summary>
    /// Convenience class for <c>Path</c> waypoints storing the actual world position and
    /// the closest ground position of the waypoint.
    /// </summary>
    public class PathWaypoint
    {
        private Vector3 _groundPosition = Vector3.zero;

        public Vector3 WorldPosition { get; private set; }

        public Vector3 GroundPosition
        {
            get
            {
                if (_groundPosition == Vector3.zero)
                    _groundPosition = Common.GetGroundPoint(WorldPosition);

                return _groundPosition;
            }
        }

        public PathWaypoint(Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
        }
    }
}
