using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public enum VertexVisibility
{
    Visible,
    Partial,
    Hidden
}

[Serializable]
public class Offset
{
    public float X;
    public float Y;
    public float Z;
}

/// <summary>
/// Mesh based fog of war implementation. Each mesh vertex has a trigger point which is calculated on
/// <c>Start</c> using the main camera's view angle. If the player can see this point, the vertex is
/// made transparent and all vertices adjacent to it are made partially transparent.
/// </summary>
/// <remarks>
/// Works adequately on smaller meshes, but will need a better algorithm to replace the brute force
/// approach on <c>UpdateVertexVisibilities</c> and <c>UpdateMeshTransparency</c> on larger meshes
/// with more vertices.
/// </remarks>
[RequireComponent(typeof(MeshFilter))]
public class FogOfWar : MonoBehaviour
{
    /// <summary>
    /// Utility class for <see cref="FogOfWar"/>. Represents a single vertex in the fog of war mesh.
    /// </summary>
    private class FogMeshVertex
    {
        /// <summary>
        /// Adjacent vertices to this vertex, which are set partially transparent
        /// when this vertex is set transparent.
        /// </summary>
        public List<FogMeshVertex> AdjacentVertices { get; set; }

        /// <summary>
        /// The world position which if seen by the player, will trigger this vertex's
        /// transparency.
        /// </summary>
        public Vector3 TriggerPoint { get; set; }

        /// <summary>
        /// Enumeration defining whether this vertex is opaque, partially transparent
        /// or fully transparent.
        /// </summary>
        public VertexVisibility Visibility { get; set; }
    }

    public Camera MainCamera;

    /// <summary>
    /// Alpha value given to partially transparent vertices. Value in the range of 0 to 255.
    /// </summary>
    public int PartialAlpha = 130;

    /// <summary>
    /// How much vertex alpha is increased or decreased during transition.
    /// </summary>
    public int AlphaIncrement = 7;

    /// <summary>
    /// How close each mesh vertex must be from each other to be considered adjacent.
    /// </summary>
    public float AdjacentVertexRange = 1.1f;

    /// <summary>
    /// Offset applied to negative y coordinate for each vertex during adjacency checking.
    /// </summary>
    /// <remarks>
    /// Used to prevent close enough adjacent vertices from seeing each other if they are behind
    /// separating colliders, such as walls.
    /// </remarks>
    public float AdjacentVertexOffset;

    /// <summary>
    /// Offset applied to the mesh after initialization to fine tune its placement in the scene.
    /// </summary>
    public Offset MeshOffset;

    /// <summary>
    /// How far a vertex's transparency trigger point should be from the vertex's position.
    /// </summary>
    public float VertexTriggerPointOffset = 3.0f;

    /// <summary>
    /// Debug helper to draw vertices visible to the player.
    /// </summary>
    public bool DrawVisibleVertices;

    /// <summary>
    /// Debug helper to draw vertices hidden to the player.
    /// </summary>
    public bool DrawHiddenVertices;

    private Mesh _mesh;
    private bool _isFreezed;

    /// <summary>
    /// Vertex cache for the fog of war mesh vertices. Stores adjacent vertices and trigger
    /// points for each vertex.
    /// </summary>
    /// <remarks>
    /// The array indexes between cache and mesh vertices match together.
    /// </remarks>
    private FogMeshVertex[] _vertexCache;

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        UpdateVertexVisibilities();

        UpdateMeshTransparency();
    }

    private void Initialize()
    {
        // Get the mesh to be used as the fog of war
        _mesh = GetComponent<MeshFilter>().mesh;

        // Initialize mesh vertices colors to default white with full alpha
        var colors = new Color32[_mesh.vertices.Length];
        for (var i = 0; i < _mesh.vertices.Length; i++)
        {
            colors[i] = new Color32(0, 0, 0, 255);
        }
        _mesh.colors32 = colors;

        // Populate vertex cache and disable fog of war until triggered by player
        PopulateVertexCache();
        Enable(false);

        // Fine tune the fog of war position now that vertex cache has been built
        var offsetPosition = transform.position;
        offsetPosition.x += MeshOffset.X;
        offsetPosition.y += MeshOffset.Y;
        offsetPosition.z += MeshOffset.Z;
        transform.position = offsetPosition;
    }

    /// <summary>
    /// Populates a vertex cache which can be used runtime to query <see cref="FogMeshVertex"/>
    /// data for vertices. Calculates adjacent vertices and trigger points for each mesh vertex.
    /// </summary>
    private void PopulateVertexCache()
    {
        var vertices = _mesh.vertices;

        // Initialize vertex cache
        _vertexCache = new FogMeshVertex[vertices.Length];
        for (int i = 0; i < _vertexCache.Length; i++)
        {
            _vertexCache[i] = new FogMeshVertex
            {
                AdjacentVertices = new List<FogMeshVertex>(),
                TriggerPoint = Vector3.zero,
                Visibility = VertexVisibility.Hidden
            };
        }

        // Build a connection map between adjacent mesh vertices, by raycasting to determine if
        // a vertex can see another vertex.
        for (int i = 0; i < vertices.Length; i++)
        {
            var p1 = vertices[i];
            p1 = transform.TransformPoint(p1);

            // Build connection map between this vertex and all other vertices on mesh
            for (int j = i + 1; j < vertices.Length; j++)
            {
                var p2 = vertices[j];
                p2 = transform.TransformPoint(p2);

                if (AreVerticesAdjacent(p1, p2))
                {
                    _vertexCache[i].AdjacentVertices.Add(_vertexCache[j]);
                    _vertexCache[j].AdjacentVertices.Add(_vertexCache[i]);
                }
            }

            // Calculate a trigger point for the vertex at startup. This increases FPS about 10 to 20,
            // as opposed to runtime calculation. If the trigger point is seen by player, vertex will
            // be given full transparency.
            _vertexCache[i].TriggerPoint = GetTriggerPoint(p1);
        }
    }

    /// <summary>
    /// Calculates a point for the vertex in world space, which if seen by the player,
    /// will trigger the vertex's transparency.
    /// </summary>
    private Vector3 GetTriggerPoint(Vector3 vertexWorldPosition)
    {
        // If possible, we want the trigger point to be a short distance away from the vertex
        // position, and in the same direction than the main camera is facing the player. 
        var playerScreenPoint = MainCamera.WorldToScreenPoint(Player.Get.Position);
        var cameraProjection = MainCamera.ScreenPointToRay(playerScreenPoint);

        // Let's create a vertex projection that can be raycasted to check if this point is available.
        var vertexProjection = new Ray(vertexWorldPosition, cameraProjection.direction);

        RaycastHit hit;

        if (Physics.Raycast(vertexProjection, out hit, VertexTriggerPointOffset))
        {
            // There is an object between the vertex and the desired trigger point. To prevent excessive
            // fog of war revealing, e.g. from across a closed door, we don't use the desired point, but
            // the raycast's hit point instead.
            return hit.point;
        }
        else
        {
            // The desired trigger point is available, so let's use it.
            return vertexProjection.origin + (vertexProjection.direction * VertexTriggerPointOffset);
        }
    }

    private bool AreVerticesAdjacent(Vector3 p1, Vector3 p2)
    {
        // If the fog of war mesh is positioned on top of collider(s), e.g. walls, we don't want vertices
        // from separate rooms to be considered adjacent, triggering each others transparency. To prevent
        // this we apply a small offset to the vertices and move the raycast origins downward.
        p1 += Vector3.down * AdjacentVertexOffset;
        p2 += Vector3.down * AdjacentVertexOffset;

        // If vertices are not close to each other, they are not adjacent
        var sqrMagnitude = (p1 - p2).sqrMagnitude;
        if (sqrMagnitude >= AdjacentVertexRange * AdjacentVertexRange)
            return false;

        // If vertices are close to each other and can see each other, they are adjacent. Both linecasts
        // from p1-to-p2 and from p2-to-p1 must pass, because either vertex can be within a collider.
        return !Physics.Linecast(p1, p2, LayerMaskStorage.FogOfWarVertexInsideColliderMask) &&
               !Physics.Linecast(p2, p1, LayerMaskStorage.FogOfWarVertexInsideColliderMask);
    }

    /// <summary>
    /// Updates the mesh vertices' visibility information by line casting from the player's position
    /// towards each vertex.
    /// </summary>
    private void UpdateVertexVisibilities()
    {
        // Clear previous visibilities by setting all vertices hidden
        foreach (var vertex in _vertexCache)
            vertex.Visibility = VertexVisibility.Hidden;

        foreach (var vertex in _vertexCache)
        {
            // If player can see the vertex on the fog of war mesh, set it visible
            if (Player.CanSeePosition(vertex.TriggerPoint))
            {
                if (DrawVisibleVertices)
                    Debug.DrawLine(Player.Get.Position, vertex.TriggerPoint, Color.white);

                vertex.Visibility = VertexVisibility.Visible;

                // Go through each adjacent vertex and set them partially visible
                foreach (var adjacentVertex in vertex.AdjacentVertices)
                {
                    // If an adjacent vertex is already set fully visible, leave it at that
                    if (adjacentVertex.Visibility != VertexVisibility.Visible)
                        adjacentVertex.Visibility = VertexVisibility.Partial;
                }
            }
            else if (DrawHiddenVertices)
            {
                Debug.DrawLine(Player.Get.Position, vertex.TriggerPoint, Color.grey);
            }
        }
    }

    /// <summary>
    /// Updates the mesh vertices' alpha values based on their visibilities to the player.
    /// </summary>
    private void UpdateMeshTransparency()
    {
        var colors = _mesh.colors32;

        for (var i = 0; i < _vertexCache.Length; i++)
        {
            var vertex = _vertexCache[i];

            // Gradually increase or decrease current alpha value
            var alpha = (int)colors[i].a;

            if (vertex.Visibility == VertexVisibility.Visible)
            {
                alpha -= AlphaIncrement;
            }
            else if (vertex.Visibility == VertexVisibility.Partial)
            {
                // Try to reach the specified partial alpha value
                if (alpha > PartialAlpha)
                {
                    alpha -= AlphaIncrement;
                    if (alpha < PartialAlpha)
                        alpha = PartialAlpha;
                }
                else if (alpha < PartialAlpha)
                {
                    alpha += AlphaIncrement;
                    if (alpha > PartialAlpha)
                        alpha = PartialAlpha;
                }
            }
            else if (vertex.Visibility == VertexVisibility.Hidden)
            {
                alpha += AlphaIncrement * 2;
            }

            // Clamp alpha between 0 and 255
            colors[i].a = (byte)Mathf.Clamp(alpha, 0, 255);
        }

        _mesh.colors32 = colors;
    }

    /// <summary>
    /// Freeze or unfreeze the fog of war from its current state. Frozen fog of war
    /// will still be visible, but remain unaffected by player movement.
    /// </summary>
    public void Freeze(bool freezed)
    {
        this.enabled = !freezed;
        _isFreezed = freezed;
    }

    /// <summary>
    /// Enable or disable fog of war calculation. Disabled fog of wars are not
    /// rendered.
    /// </summary>
    public void Enable(bool enabled)
    {
        this.enabled = enabled;
        renderer.enabled = enabled;
    }

    void OnTriggerEnter()
    {
        if (!_isFreezed)
            Enable(true);
    }

    void OnTriggerExit()
    {
        if (!_isFreezed)
            Enable(false);
    }
}