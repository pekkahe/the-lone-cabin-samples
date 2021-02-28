using UnityEngine;
using System.Collections;

/// <summary>
/// Defines conveniance properties and methods for the most used player components
/// and actions.
/// </summary>
public class Player
{
    /// <summary>
    /// Defines all cached <c>GameObjects</c> and script components for the player.
    /// </summary>
    private class PlayerCache
    {
        public GameObject GameObject { get; set; }
        public Transform Eyes { get; set; }
        public PlayerHealth Health { get; set; }
        public PlayerMoveController MoveController { get; set; }
        public PlayerInventory Inventory { get; set; }
        public PlayerAnimatorController Animator { get; set; }
        public EnvironmentMonitor EnvironmentMonitor { get; set; }
        public AudioSource FootstepAudio { get; set; }
    }

    #region Singleton

    private static Player _instance;

    public static Player Get 
    {
        get 
        {
            if (_instance == null)
                _instance = new Player();

            return _instance;
        }
    }

    private Player() 
    {
        _cache = new PlayerCache();
    }

    #endregion

    private PlayerCache _cache;

    #region Properties for player components
   
    /// <summary>
    /// The player <c>GameObject</c>.
    /// </summary>
    /// <remarks>
    /// If the player is dead, calling this each frame will lead to a performance
    /// drop because of <c>GameObject.FindWithTag</c>. Use the cached component
    /// properties instead at first hand.
    /// </remarks>
    public GameObject GameObject
    {
        get
        {
            // GameObject should be refreshed on game launch and checkpoint load,
            // but this acts as a fail-safe in case refresh was forgotten.
            if (_cache.GameObject == null)
            {
                _cache.GameObject = GameObject.FindWithTag("Player");

                if (Debug.isDebugBuild)
                    Debug.LogWarning("Player cache was not refreshed properly!");
            }

            return _cache.GameObject;
        }
    }

    /// <summary>
    /// The <c>Transform</c> component for the player's eyes.
    /// </summary>
    public Transform Eyes
    {
        get
        {
            if (_cache.Eyes == null && _cache.GameObject != null)
                _cache.Eyes = _cache.GameObject.transform.Find("Eyes");

            return _cache.Eyes;
        }
    }

    /// <summary>
    /// The current world position of the player.
    /// </summary>
    public Vector3 Position
    {
        get 
        {
            if (_cache.GameObject == null)
                return Vector3.zero;

            return _cache.GameObject.transform.position; 
        }
    }

    /// <summary>
    /// The health manager for the player.
    /// </summary>
    public PlayerHealth Health
    {
        get
        {
            if (_cache.Health == null && _cache.GameObject != null)
                _cache.Health = _cache.GameObject.GetComponent<PlayerHealth>();

            return _cache.Health;
        }
    }

    /// <summary>
    /// The inventory manager for the player.
    /// </summary>
    public PlayerInventory Inventory
    {
        get
        {
            if (_cache.Inventory == null && _cache.GameObject != null)
                _cache.Inventory = _cache.GameObject.GetComponent<PlayerInventory>();

            return _cache.Inventory;
        }
    }

    /// <summary>
    /// The character animation controller for the player.
    /// </summary>
    public PlayerAnimatorController Animator
    {
        get
        {
            if (_cache.Animator == null && _cache.GameObject != null)
                _cache.Animator = _cache.GameObject.GetComponent<PlayerAnimatorController>();

            return _cache.Animator;
        }
    }

    /// <summary>
    /// The game world environment monitor for the player.
    /// </summary>
    public EnvironmentMonitor Environment
    {
        get
        {
            if (_cache.EnvironmentMonitor == null && _cache.GameObject != null)
                _cache.EnvironmentMonitor = _cache.GameObject.GetComponent<EnvironmentMonitor>();

            return _cache.EnvironmentMonitor;
        }
    }

    /// <summary>
    /// The character movement controller for the player.
    /// </summary>
    public PlayerMoveController MoveController
    {
        get
        {
            if (_cache.MoveController == null && _cache.GameObject != null)
                _cache.MoveController = _cache.GameObject.GetComponent<PlayerMoveController>();

            return _cache.MoveController;
        }
    }

    /// <summary>
    /// The targeting cursor of the player.
    /// </summary>
    public AimCursor Cursor
    {
        get
        {
            if (MoveController == null)
                return null;

            return MoveController.AimCursor;
        }
    }

    /// <summary>
    /// The <see cref="AudioSource"/> component for the player's footsteps.
    /// </summary>
    public AudioSource FootstepAudio
    {
        get
        {
            if (_cache.FootstepAudio == null && _cache.GameObject != null)
                _cache.FootstepAudio = GameObject.FindWithTag("PlayerFootstepAudio").audio;

            return _cache.FootstepAudio;
        }
    }

    #endregion

    #region Helpers

    public static bool IsAlive
    {
        get { return Get._cache.GameObject != null; }
    }

    public static bool IsIndoors
    {
        get { return IsAlive && Get.Environment.IsIndoors; }
    }

    /// <summary>
    /// Refreshes this instance by locating the player <c>GameObject</c>.
    /// </summary>
    /// <remarks>
    /// Should be used after the player has died or the <c>GameObject</c>
    /// has been nullified by other means.
    /// </remarks>
    public static void Refresh()
    {
        Get._cache.GameObject = GameObject.FindWithTag("Player");

        if (Debug.isDebugBuild)
        {
            if (Get._cache.GameObject != null)
            {
                Debug.Log("Player cache refreshed successfully.");    
            }
            else
            { 
                Debug.LogError("Failed to refresh player cache. No GameObject found with tag Player");
            }
        }
    }

    /// <summary>
    /// Enables or disables the player's movement and actions.
    /// </summary>
    public static void EnableInput(bool enabled)
    {
        if (!IsAlive)
            return;

        Get.MoveController.InputDisabled = !enabled;
        Get.Inventory.enabled = enabled;
    }

    public static bool HasItem(Item item)
    {
        if (!IsAlive)
            return false;

        return Get.Inventory.HasItem(item);
    }

    public static bool CanSeePosition(Vector3 position)
    {
        if (!IsAlive)
            return false;

        return !Physics.Linecast(Get.Eyes.position, position, LayerMaskStorage.PlayerLineOfSightMask);
    }

    public static bool CanSeeEnemy(Transform enemy, float maxDistance)
    {
        if (!IsAlive)
            return false;

        var target = enemy.transform.position;
        target.y += 0.5f;

        RaycastHit hit;

        if (Physics.Raycast(Get.Eyes.position, target - Get.Eyes.position,
            out hit, maxDistance, LayerMaskStorage.PlayerCanSeeEnemyMask))
            return hit.transform == enemy;

        return false;
    }

    #endregion
}
