using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public enum AiBehaviour
{
    Patrolling,
    Pursuing,
    Exploring
}

/// <summary>
/// The AI controller for the enemy. Manages attached behaviours and AI state.
/// </summary>
public class BehaviourController : MonoBehaviour
{
    /// <summary>
    /// Whether this enemy is passive or not. Passive enemies do not aggro the player.
    /// </summary>
    public bool IsPassive;

    /// <summary>
    /// Whether this enemy is dormant or not. Dormant enemies do not move or have any
    /// active behaviour, but they aggro the player if they see him.
    /// </summary>
    public bool IsDormant;

    public float PatrolTransitionTime = 1.0f;
    public float ExploreTransitionTime = 0.5f;
    public float PursueTransitionTime = 0.5f;

    /// <summary>
    /// How many paths can be invalid (unable to find path to target),
    /// until the enemy is put to a dormant state.
    /// </summary>
    public int InvalidPathsBeforeSleep = 5;

    private EnemyMovementMotor _motor;
    private EnemyAnimatorController _animator;
    private EnemyHealth _health;
    private EnemySoundBank _soundBank;
    private PathFinder _pathFinder;

    private AiBehaviour _activeBehaviour;
    private BaseBehaviour _activeAi;
    private PatrolBehaviour _patrolAi;
    private PursueBehaviour _pursueAi;
    private ExploreBehaviour _exploreAi;

    private float _waitTimer;
    private float _timeSinceLastTick;

    public bool IsPatrolling { get { return _activeBehaviour == AiBehaviour.Patrolling && _activeAi != null; } }
    public bool IsExploring { get { return _activeBehaviour == AiBehaviour.Exploring && _activeAi != null; } }
    public bool IsPursuing { get { return _activeBehaviour == AiBehaviour.Pursuing && _activeAi != null; } }

    /// <summary>
    /// Indicates whether AI has been set to waiting state and all behaviours should be paused.
    /// </summary>
    public bool IsWaiting { get { return _waitTimer > 0; } }

    void Awake()
    {
        _motor = transform.parent.GetComponent<EnemyMovementMotor>();
        _animator = transform.parent.GetComponent<EnemyAnimatorController>();
        _health = transform.parent.GetComponent<EnemyHealth>();

        _soundBank = transform.parent.GetComponentInChildren<EnemySoundBank>();
        _pathFinder = transform.parent.GetComponentInChildren<PathFinder>();

        _patrolAi = GetComponent<PatrolBehaviour>();
        _pursueAi = GetComponent<PursueBehaviour>();
        _exploreAi = GetComponent<ExploreBehaviour>();
    }

    void Start()
    {
        _patrolAi.Initialize();
        _pursueAi.Initialize();
        _exploreAi.Initialize();
    }

    void Update()
    {
        _timeSinceLastTick += Time.deltaTime;

        if (_waitTimer > 0)
        {
            _waitTimer -= Time.deltaTime;

            if (_activeAi != null)
                _activeAi.DoWhileWaiting();
        }
        else if (_activeAi != null && _timeSinceLastTick > _activeAi.Frequency)
        {
            _timeSinceLastTick = 0f;

            if (!IsDormant)
                _activeAi.Tick();
        }
    }

    public void ForceTick()
    {
        if (_activeAi != null)
            _timeSinceLastTick = _activeAi.Frequency;
    }

    #region Behaviour activators

    /// <summary>
    /// Changes behaviour to <see cref="PatrolBehaviour"/>.
    /// </summary>
    public void Patrol()
    {
        Patrol(PatrolTransitionTime);
    }

    /// <summary>
    /// Changes behaviour to <see cref="PursueBehaviour"/>.
    /// </summary>
    /// <param name="playAnimation">Whether to play a pursue animation prior running or not.</param>
    public void Pursue(bool playAnimation = true)
    {
        Pursue(PursueTransitionTime, playAnimation);
    }

    /// <summary>
    /// Changes behaviour to <see cref="ExploreBehaviour"/>.
    /// </summary>
    /// <param name="position">The position to explore.</param>
    public void Explore(Vector3 position)
    {
        Explore(position, ExploreTransitionTime);
    }

    /// <summary>
    /// Changes behaviour to <see cref="PatrolBehaviour"/>.
    /// </summary>
    /// <param name="transitionTime">Time in seconds until behaviour is activated.</param>
    public void Patrol(float transitionTime)
    {
        if (IsPatrolling)
            return;

        if (TryChangeBehaviour(AiBehaviour.Patrolling, transitionTime))
        {
            _motor.Walk();

            _soundBank.PlayBreathingSound();
        }
    }

    /// <summary>
    /// Changes behaviour to <see cref="PursueBehaviour"/>.
    /// </summary>
    /// <param name="transitionTime">Time in seconds until behaviour is activated.</param>
    /// <param name="playAnimation">Whether to play a pursue animation prior running or not.</param>
    public void Pursue(float transitionTime, bool playAnimation)
    {
        if (IsPursuing)
            return;

        if (TryChangeBehaviour(AiBehaviour.Pursuing, transitionTime))
        {
            WakeUp();

            _motor.Run();
            _motor.LookAt(Player.Get.Position);

            _soundBank.StopBreathingSound();

            if (playAnimation)
                PlayPursueAnimation();
        }
    }

    /// <summary>
    /// Changes behaviour to <see cref="ExploreBehaviour"/>.
    /// </summary>
    /// <param name="position">The position to explore.</param>
    /// <param name="transitionTime">Time in seconds until behaviour is activated.</param>
    public void Explore(Vector3 position, float transitionTime)
    {
        if (IsExploring)
            return;

        if (TryChangeBehaviour(AiBehaviour.Exploring, transitionTime))
        {
            _motor.Walk();

            _soundBank.PlayBreathingSound();

            _pathFinder.FindPathTo(position);
        }
    }

    /// <summary>
    /// Plays the enemy's pursue animation and sound if he has them.
    /// </summary>
    public void PlayPursueAnimation()
    {
        _animator.NoticePlayer();
        _soundBank.PlayPursueSound();
    }

    /// <summary>
    /// Returns true if behaviour was changed successfully, false otherwise.
    /// </summary>
    private bool TryChangeBehaviour(AiBehaviour behaviour, float transitionTime)
    {
        try
        {
            ChangeBehaviour(behaviour, transitionTime);

            return true;
        }
        catch (InvalidOperationException e)
        {
            Debug.LogError("Failed to change behaviour. " + e.Message);

            return false;
        }
    }

    private void ChangeBehaviour(AiBehaviour behaviour, float transitionTime)
    {
        if (_activeAi != null)
            _activeAi.End();

        // Stop movement prior to behaviour change, or else enemy will move towards its
        // old movement direction during the wait period.
        _motor.StopUntilResumed();

        // Set new behaviour
        _activeAi = GetBehaviour(behaviour);
        _activeAi.Begin();

        _activeBehaviour = behaviour;

        // Simulate transition to new behaviour by waiting for a short time
        Wait(transitionTime);
    }

    #endregion

    /// <summary>
    /// Disables enemy AI for the given amount of time.
    /// </summary>
    public void Wait(float timeInSeconds)
    {
        _waitTimer = timeInSeconds;

        _motor.Pause(timeInSeconds);
    }

    /// <summary>
    /// Disables enemy AI completely until explicitly resumed.
    /// </summary>
    public void Disable(bool disabled)
    {
        enabled = !disabled;

        if (disabled)
        {
            _motor.StopUntilResumed();
        }
        else
        {
            _motor.Resume();
        }
    }

    /// <summary>
    /// Changes enemy AI into a dormant state where all behaviours are disabled,
    /// but reaction to player is kept normal.
    /// </summary>
    public void Sleep()
    {
        _motor.StopUntilResumed();

        _soundBank.PlaySleepingSound();

        IsDormant = true;

        Debug.Log("AI " + transform.name + " is now dormant.");
    }

    /// <summary>
    /// Wakes the enemy up from a dormant state.
    /// </summary>
    public void WakeUp()
    {
        if (IsDormant)
            Debug.Log("AI " + transform.name + " is no longer dormant.");

        IsDormant = false;

        _soundBank.StopSleepingSound();
    }

    private BaseBehaviour GetBehaviour(AiBehaviour behaviour)
    {
        if (behaviour == AiBehaviour.Patrolling)
            return _patrolAi;
        if (behaviour == AiBehaviour.Pursuing)
            return _pursueAi;
        if (behaviour == AiBehaviour.Exploring)
            return _exploreAi;

        throw new InvalidOperationException("Enemy " + transform.name + " does not have behaviour " + behaviour);
    }

    private bool HasBehaviour(AiBehaviour behaviour)
    {
        if (behaviour == AiBehaviour.Patrolling)
            return _patrolAi != null;
        if (behaviour == AiBehaviour.Pursuing)
            return _pursueAi != null;
        if (behaviour == AiBehaviour.Exploring)
            return _exploreAi != null;

        return false;
    }

    public bool CanExplorePlayerPosition(Vector3 position)
    {
        if (!HasBehaviour(AiBehaviour.Exploring))
            return false;

        if (Player.IsIndoors && _exploreAi.DontExploreIndoors)
            return false;

        return _exploreAi.CanExplorePosition(position);
    }

    #region Event handlers

    public void OnPlayerSeen()
    {
        if (!enabled || IsPassive)
            return;

        if (_activeAi != null)
            _activeAi.OnPlayerSeen(Player.Get.Position);

        if (Player.IsIndoors && _pursueAi != null && _pursueAi.DontPursueIndoors)
            return;

        if (!IsPursuing)
            Pursue();
    }

    public void OnPlayerHeard()
    {
        if (!enabled || IsPassive)
            return;

        if (_activeAi != null)
            _activeAi.OnPlayerHeard(Player.Get.Position);

        if (IsPursuing || !CanExplorePlayerPosition(Player.Get.Position))
            return;

        if (!IsExploring)
            Explore(Player.Get.Position);
    }

    public void OnPathFound(Path path)
    {
        if (!enabled)
            return;

        // If enemy has reached the maximum amount of subsequent failed paths, go into dormant state.
        // We don't know what has caused this, but the current state is no longer preferred.
        if (_pathFinder.InvalidPathCount >= InvalidPathsBeforeSleep)
            Sleep();
        else if (_activeAi != null)
            _activeAi.OnPathFound(path);
    }

    public void OnPathTraversed()
    {
        if (!enabled)
            return;

        if (_activeAi != null)
            _activeAi.OnPathTraversed();
    }

    public void OnDoorStay(OpenableDoor door)
    {
        if (!enabled)
            return;

        if (_activeAi != null)
            _activeAi.OnDoorStay(door);
    }

    public void OnHit(Damage damage)
    {
        _health.ReceiveHit(damage);

        if (_activeBehaviour != AiBehaviour.Pursuing && !IsPassive)
        {
            _motor.LookAt(Player.Get.Position);
            Pursue();
        }
    }

    #endregion
}