using UnityEngine;
using System.Collections;

/// <summary>
/// Defines a <c>GameObject</c> stored by the checkpoint system.
/// </summary>
public interface ICheckpointData
{
    /// <summary>
    /// The stored <c>GameObject</c>.
    /// </summary>
    GameObject GameObject { get; set; }

    /// <summary>
    /// Loads the previously saved game object data by instantiating a copy of it, and returning it.
    /// </summary>
    GameObject Load();

    /// <summary>
    /// Saves the given game object by instantiating a copy of it, and storing a reference to it.
    /// </summary>
    void Save(GameObject original);
}