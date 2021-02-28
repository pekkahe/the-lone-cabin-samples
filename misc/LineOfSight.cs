using UnityEngine;
using System.Collections;

/// <summary>
/// Simulates the line-of-sight of an enemy. 
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class LineOfSight : MonoBehaviour
{
    /// <summary>
    /// The angle in degrees of the enemy's vision. Can be extended with <see cref="Extend"/>.
    /// </summary>
    public float SightAngle = 180.0f;

    /// <summary>
    /// The enemy mesh's eye height calculated from its feet.
    /// </summary>
    public float EyeHeight = 3.5f;

    /// <summary>
    /// The distance of the enemy's vision. Can be extended with <see cref="Extend"/>.
    /// </summary>
    public float SightDistance = 15f;

    /// <summary>
    /// Time in seconds of how often line-of-sight properties <see cref="CanSeePlayer"/>
    /// and <see cref="IsVisibleToPlayer"/> are updated.
    /// </summary>
    public float CheckInterval = 0.3f;

    private Transform _transform;
    private BehaviourController _aiController;
    private bool _isPlayerInsideSightDistance;
    private float _originalSightAngle;
    private float _originalSightDistance;
    private float _lastCheckTime;
    private SphereCollider _collider;

    /// <summary>
    /// Returns true if the enemy has a clear line of sight to the player,
    /// and the player is inside sight angle.
    /// </summary>
    public bool CanSeePlayer { get; private set; }

    /// <summary>
    /// Returns true if the player can see the enemy, but the enemy cannot see
    /// the player because he is outside sight angle.
    /// </summary>
    public bool IsVisibleToPlayer { get; private set; }

    void Awake()
    {
        _transform = transform;
        _aiController = transform.parent.GetComponentInChildren<BehaviourController>();
        _collider = collider as SphereCollider;
        _originalSightAngle = SightAngle;
        _originalSightDistance = SightDistance;

        gameObject.layer = LayerMaskStorage.PlayerOnlyLayer;
    }

    void Start()
    {
        _collider.radius = SightDistance;
        _lastCheckTime = Time.time;
    }

    void Update()
    {
        if (Time.time > _lastCheckTime + CheckInterval)
        {
            CheckLineOfSight();

            if (CanSeePlayer)
                _aiController.OnPlayerSeen();

            _lastCheckTime = Time.time;
        }
    }

    /// <summary>
    /// Extends the enemy's line-of-sight by removing sight angle restrictions
    /// and increasing sight distance.
    /// </summary>
    public void Extend()
    {
        SightAngle = 360f;
        SightDistance = 30f;
        _collider.radius = SightDistance;
    }

    /// <summary>
    /// Resets the enemy's line-of-sight to default values.
    /// </summary>
    public void Reset()
    {
        SightAngle = _originalSightAngle;
        SightDistance = _originalSightDistance;
        _collider.radius = SightDistance;
    }

    /// <summary>
    /// Checks line-of-sight by raycasting. Sets <c>CanSeePlayer</c> flag to true, if there
    /// are no obstructing objects between the enemy and the player, the distance to player
    /// is below <c>SightDistance</c>, and the angle between the enemy's transform and player
    /// position is inside <c>SightAngle</c>.
    /// </summary>
    private void CheckLineOfSight()
    {
        CanSeePlayer = false;
        IsVisibleToPlayer = false;

        if (!Player.IsAlive || !_isPlayerInsideSightDistance)
            return;

        var eyePosition = _transform.position;
        eyePosition.y += EyeHeight;

        var lookAtDirection = Player.Get.GameObject.collider.bounds.center - eyePosition;

        RaycastHit hit;

        if (Physics.Raycast(eyePosition, lookAtDirection, out hit, SightDistance,
            LayerMaskStorage.EnemyCanSeePlayerMask))
        {
            IsVisibleToPlayer = hit.transform.CompareTag("Player");
        }
        else
        {
            IsVisibleToPlayer = false;
        }

        CanSeePlayer = IsVisibleToPlayer && IsInsideSightAngle(lookAtDirection);
    }

    private bool IsInsideSightAngle(Vector3 direction)
    {
        return Vector3.Angle(direction, _transform.forward) <= SightAngle / 2;
    }

    void OnTriggerEnter()
    {
        _isPlayerInsideSightDistance = true;
    }

    void OnTriggerExit()
    {
        _isPlayerInsideSightDistance = false;
    }
}