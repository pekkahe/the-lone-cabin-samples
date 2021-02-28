using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// An <c>AStarPathFinder</c> visibility graph for <c>AStarNode</c>s.
/// The graph defines which nodes can see each other and how they are connected.
/// </summary>
public class VisibilityGraph
{
    /// <summary>
    /// Dictionary containing all nodes in the graph and their IDs.
    /// </summary>
    private readonly Dictionary<Guid, AStarNode> _nodes = new Dictionary<Guid, AStarNode>();

    /// <summary>
    /// The visibility graph represented by the node IDs.
    /// </summary>
    private readonly Dictionary<Guid, List<Guid>> _graph = new Dictionary<Guid, List<Guid>>();

    /// <summary>
    /// Instantiates a new <c>VisibilityGraph</c> for the given nodes.
    /// </summary>
    public VisibilityGraph(AStarNode[] nodes)
    {
        foreach (var node in nodes)
        {
            _nodes.Add(Guid.NewGuid(), node);
        }
    }

    /// <summary>
    /// Creates a deep copy of the visibility graph by instantiating new <c>AStarNode</c>
    /// objects for each graph node in the source.
    /// </summary>
    public VisibilityGraph(VisibilityGraph other)
    {
        foreach (var node in other._nodes)
        {
            // Use the same key IDs for the listed nodes, so that we can simply
            // copy the graph IDs from the original without braking the graph.
            _nodes.Add(node.Key, new AStarNode(node.Value.Position));
        }

        foreach (var map in other._graph)
        {
            _graph.Add(map.Key, new List<Guid>(map.Value));
        }
    }

    /// <summary>
    /// Builds a visibility graph for the given nodes, describing which nodes can see each other.
    /// Previously built graph is overwritten.
    /// </summary>
    public void Build()
    {
        // Copy the graph nodes into a two-dimensional array so we can easily cross check them in a for-loop
        var nodes = new AStarNode[_nodes.Count];
        _nodes.Values.CopyTo(nodes, 0);

        _graph.Clear();

        // Check if nodes can see each other, and add entries to the graph if they can.
        for (int i = 0; i < nodes.Length; i++)
        {
            for (int j = i + 1; j < nodes.Length; j++)
            {
                if (CanPointsSeeEachOther(nodes[i].Position, nodes[j].Position))
                {
                    Connect(nodes[i], nodes[j]);
                }
            }
        }
    }

    /// <summary>
    /// Adds the given node into the visibility graph.
    /// </summary>
    /// <remarks>
    /// This function is not thread safe.
    /// </remarks>
    public void Add(AStarNode node)
    {
        // Add new entry to listed nodes in graph
        var nodeId = Guid.NewGuid();
        _nodes.Add(nodeId, node);

        // Copy graph keys to array
        var graphIds = new Guid[_graph.Count];
        _graph.Keys.CopyTo(graphIds, 0);

        // Compare the given node between each existing graph node and add connections
        foreach (var graphId in graphIds)
        {
            var graphNode = _nodes[graphId];

            if (CanPointsSeeEachOther(node.Position, graphNode.Position))
                Connect(nodeId, graphId);
        }
    }

    /// <summary>
    /// Adds a connection between node1 and node2 into the visibility graph.
    /// </summary>
    public void Connect(AStarNode node1, AStarNode node2)
    {
        var node1Id = GetNodeId(node1);
        var node2Id = GetNodeId(node2);

        if (node1Id == null || node2Id == null)
            ThrowException("Failed to connect nodes. Either node {0} or {1} is not listed in graph."
                .Parameters(node1.Position, node2.Position));

        Connect(node1Id.Value, node2Id.Value);
    }

    /// <summary>
    /// Removes the connection between node1 and node2 in the visibility graph.
    /// </summary>
    public void Disconnect(AStarNode node1, AStarNode node2)
    {
        var node1Id = GetNodeId(node1);
        var node2Id = GetNodeId(node2);

        if (node1Id == null || node2Id == null)
            ThrowException("Failed to disconnect nodes. Either node {0} or {1} doesn't exist in graph."
                .Parameters(node1.Position, node2.Position));

        // Remove connection from node1 to node2 and vice versa
        RemoveFromGraph(node1Id.Value, node2Id.Value);
        RemoveFromGraph(node2Id.Value, node1Id.Value);
    }

    private void Connect(Guid node1Id, Guid node2Id)
    {
        // Add connection from node1 to node2
        AddToGraph(node1Id, node2Id);

        // Add connection from node2 to node1
        AddToGraph(node2Id, node1Id);
    }

    private Guid? GetNodeId(AStarNode node)
    {
        if (!_nodes.ContainsValue(node))
            return null;

        foreach (var data in _nodes)
        {
            if (data.Value == node)
                return data.Key;
        }

        return null;
    }

    private void AddToGraph(Guid key, Guid value)
    {
        if (_graph.ContainsKey(key))
        {
            if (_graph[key].Contains(value))
            {
                Debug.LogWarning("Connection between nodes {0} and {1} already exists.".Parameters(key, value));
            }
            else
            {
                _graph[key].Add(value);
            }
        }
        else
        {
            _graph.Add(key, new List<Guid> { value });
        }
    }

    private void RemoveFromGraph(Guid key, Guid value)
    {
        if (_graph.ContainsKey(key))
        {
            if (_graph[key].Contains(value))
            {
                _graph[key].Remove(value);
            }
            else
            {
                // Log as warning because this happens when two doors opposite to each other
                // are both locked (cabin first floor).
                Debug.LogWarning(("Failed to disconnect nodes {0} and {1}, because first " +
                                  "node has no connection to second node.").Parameters(key, value));
            }
        }
        else
        {
            Debug.LogError("Failed to disconnect nodes {0} and {1}, because first node isn't in map."
                .Parameters(key, value));
        }
    }

    /// <summary>
    /// Returns the visible path finding nodes for the given node.
    /// </summary>
    public AStarNode[] Get(AStarNode node)
    {
        var nodeId = GetNodeId(node);
        if (nodeId == null)
            ThrowException("Failed to get node ID. Node {0} doesn't exist in graph.".Parameters(node.Position));

        var mapIds = _graph[nodeId.Value];
        var array = new AStarNode[mapIds.Count];

        for (int i = 0; i < mapIds.Count; i++)
        {
            var mapId = mapIds[i];
            var mapNode = _nodes[mapId];

            array[i] = mapNode;
        }

        return array;
    }

    public static bool CanPointsSeeEachOther(Vector3 p1, Vector3 p2)
    {
        // Create two line casts using both points as origins, and allow either of the casts to succeed.
        // A two-way check is required since we must check p1->p2 and p2->p1 casts separately, to verify that 
        // the points can really see each other. For example, if enemy (p1) is inside a collider (waypoint barrier),
        // p1->p2 cast will succeed, but p2->p1 cast would fail.
        return !Physics.Linecast(p1, p2, LayerMaskStorage.WaypointVisibilityMask) ||
               !Physics.Linecast(p2, p1, LayerMaskStorage.WaypointVisibilityMask);
    }

    public void Draw()
    {
        foreach (var graphId in _graph.Keys)
        {
            foreach (var otherId in _graph[graphId])
            {
                Debug.DrawLine(_nodes[graphId].Position, _nodes[otherId].Position, Color.grey, 1.0f);
            }
        }
    }

    private static void ThrowException(string message)
    {
        var exception = new InvalidOperationException(message);

        // Log before throwing, since if exception is thrown in another thread, it won't
        // automatically show up in Unity Inspector.
        Debug.LogException(exception);

        throw exception;
    }
}
