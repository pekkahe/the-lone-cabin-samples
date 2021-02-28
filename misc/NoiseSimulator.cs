using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simulates noise heard by enemies. Uses <see cref="Physics.OverlapSphere"/>
/// to receive all enemy colliders within a radius based on movement speed.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class NoiseSimulator : MonoBehaviour
{
    /// <summary>
    /// How often should this simulation run.
    /// </summary>
    public float TickInterval = 0.5f;

    /// <summary>
    /// Maximum range of noise.
    /// </summary>
    public float MaxNoiseRadius = 15.0f;

    private Transform _transform;
    private Rigidbody _rigidbody;
    private float _velocityLimit;
    private float _tickTimer;
    private const float _velocityThreshold = 1.0f;

    void Awake()
    {
        _transform = transform;
        _rigidbody = rigidbody;
        _velocityLimit = _velocityThreshold;
    }

    void Start()
    {
        var motor = GetComponent<FreeMovementMotor>();
        if (motor != null)
            _velocityLimit = motor.RunSpeed;

        _tickTimer = TickInterval;
    }

    void Update()
    {
        _tickTimer -= Time.deltaTime;

        if (_tickTimer < 0f)
        {
            Tick();

            _tickTimer = TickInterval;
        }
    }

    private void Tick()
    {
        var enemiesWhoHeardPlayer = SimulateNoise();

        foreach (var enemy in enemiesWhoHeardPlayer)
        {
            var enemyAi = enemy.GetComponentInChildren<BehaviourController>();
            if (enemyAi == null)
                continue;

            enemyAi.OnPlayerHeard();
        }
    }

    void OnDrawGizmos()
    {
        if (_transform == null)
            return;

        Gizmos.color = _tickTimer < 0.1f ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(_transform.position, GetNoiseRadius());
    }

    /// <summary>
    /// Simulate noise by firing a <see cref="Physics.OverlapSphere"/> with a radius based on movement speed.
    /// Returns the colliders of nearby enemies caught in the sphere.
    /// </summary>
    public Collider[] SimulateNoise()
    {
        return Physics.OverlapSphere(_transform.position, GetNoiseRadius(), LayerMaskStorage.PlayerNoiseMask);
    }

    public float GetNoiseRadius()
    {
        var noiseMultiplier = Mathf.InverseLerp(_velocityThreshold, _velocityLimit, _rigidbody.velocity.magnitude);

        return noiseMultiplier * MaxNoiseRadius;
    }
}
