using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Path finding visibility graph component which builds itself on <c>Start</c>.
/// Ment to be shared by individual <see cref="VisibilityGraph"/>s instantiated
/// in path finding, so that the graph doesn't have to be computed on each run.
/// </summary>
public class SharedVisibilityGraph : MonoBehaviour
{
    #region Singleton (Unity)

    private static SharedVisibilityGraph _instance;

    void Awake()
    {
        _instance = this;
    }

    public static SharedVisibilityGraph Instance
    {
        get { return _instance; }
    }

    #endregion

    private VisibilityGraph _visibilityGraph;
    private Dictionary<Guid, AStarNode> _waypointNodes;

    void Start()
    {
        BuildGraph();
    }

    /// <summary>
    /// Builds the graph by collecting all <see cref="Waypoint"/> game objects in the scene,
    /// and using their positions as the source data for <see cref="AStarNode"/>s.
    /// </summary>
    private void BuildGraph()
    {
        _waypointNodes = new Dictionary<Guid, AStarNode>();

        var sceneWaypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        var nodes = new AStarNode[sceneWaypoints.Length];

        // Create AStarNode instances for each Waypoint in the scene
        for (int i = 0; i < sceneWaypoints.Length; i++)
        {
            var waypoint = sceneWaypoints[i].GetComponent<Waypoint>();

            var node = new AStarNode(waypoint.transform.position);

            nodes[i] = node;
            _waypointNodes.Add(waypoint.Id, node);
        }

        // Build the visibility map according to the nodes in scene
        _visibilityGraph = new VisibilityGraph(nodes);
        _visibilityGraph.Build();
    }

    public VisibilityGraph GetCopy()
    {
        return new VisibilityGraph(_visibilityGraph);
    }

    /// <summary>
    /// Disconnects the waypoints in the first list from the waypoint in the second list.
    /// Disconnected waypoints cannot form paths between each other.
    /// </summary>
    public void DisconnectWaypoints(List<Waypoint> waypoints1, List<Waypoint> waypoints2)
    {
        for (int i = 0; i < waypoints1.Count; i++)
        {
            for (int j = 0; j < waypoints2.Count; j++)
            {
                DisconnectWaypoints(waypoints1[i], waypoints2[j]);
            }
        }
    }

    /// <summary>
    /// Connects the waypoints in the first list to the waypoint in the second list.
    /// Connected waypoints can form paths between each other.
    /// </summary>
    public void ConnectWaypoints(List<Waypoint> waypoints1, List<Waypoint> waypoints2)
    {
        for (int i = 0; i < waypoints1.Count; i++)
        {
            for (int j = 0; j < waypoints2.Count; j++)
            {
                ConnectWaypoints(waypoints1[i], waypoints2[j]);
            }
        }
    }

    private void DisconnectWaypoints(Waypoint waypoint1, Waypoint waypoint2)
    {
        try
        {
            var node1 = GetCachedNode(waypoint1.Id);
            var node2 = GetCachedNode(waypoint2.Id);

            _visibilityGraph.Disconnect(node1, node2);
        }
        catch (InvalidOperationException e)
        {
            Debug.LogException(e);
        }
    }

    private void ConnectWaypoints(Waypoint waypoint1, Waypoint waypoint2)
    {
        try
        {
            var node1 = GetCachedNode(waypoint1.Id);
            var node2 = GetCachedNode(waypoint2.Id);

            _visibilityGraph.Connect(node1, node2);
        }
        catch (InvalidOperationException e)
        {
            Debug.LogException(e);
        }
    }

    private AStarNode GetCachedNode(Guid id)
    {
        try
        {
            return _waypointNodes[id];
        }
        catch
        {
            throw new InvalidOperationException("No such cached node with the id of '{0}' exists."
                .Parameters(id));
        }
    }
}