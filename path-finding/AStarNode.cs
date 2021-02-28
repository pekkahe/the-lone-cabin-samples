using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A* node for <see cref="AStarPathFinder"/>.
/// </summary>
public class AStarNode : IEquatable<AStarNode>
{
    private readonly Guid _id;

    public float G { get; set; }

    public float H { get; set; }

    public float F { get { return G + H; } }

    public AStarNode Parent { get; set; }

    public Vector3 Position { get; private set; }

    public AStarNode(Vector3 position)
    {
        Position = position;
        _id = Guid.NewGuid();
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as AStarNode);
    }

    public bool Equals(AStarNode obj)
    {
        return obj != null && obj._id == this._id;
    }
}
