using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A world position in the scene. Used in visibility graph building as the source
/// for <see cref="AStarNode"/> positions.
/// </summary>
public class Waypoint : MonoBehaviour
{
    private static bool _drawConnections = true;

    public Guid Id;
    public float GizmoRadius = 0.8f;
    public Color GizmoColor = Color.white;

    void Awake()
    {
        Id = Guid.NewGuid();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireSphere(transform.position, GizmoRadius);
        Gizmos.DrawLine(transform.position, Common.GetGroundPoint(transform.position));
    }

    void OnDrawGizmosSelected()
    {
        if (!_drawConnections)
            return;

        var drawablePoints = new List<GameObject>();

        foreach (var obj in GameObject.FindGameObjectsWithTag("Waypoint"))
        {
            if (obj.activeInHierarchy)
                drawablePoints.Add(obj);
        }
        foreach (var obj in GameObject.FindGameObjectsWithTag("PatrolPoint"))
        {
            if (obj.activeInHierarchy)
                drawablePoints.Add(obj);
        }

        foreach (var obj in drawablePoints)
        {
            if (obj.CompareTag("Waypoint"))
            {
                Gizmos.color = Color.white;
            }
            else if (obj.CompareTag("PatrolPoint"))
            {
                Gizmos.color = Color.yellow;
            }

            if (VisibilityGraph.CanPointsSeeEachOther(transform.position, obj.transform.position))
            {
                Gizmos.DrawLine(transform.position, obj.transform.position);
            }
        }
    }
}
