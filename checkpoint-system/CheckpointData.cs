using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Base class for all <c>GameObjects</c> stored by the checkpoint system.
/// </summary>
public class CheckpointData : ICheckpointData
{
    public GameObject GameObject { get; set; }

    public Transform Parent { get; set; }

    public virtual GameObject Load()
    {
        var copy = GameObject.Instantiate(GameObject, GameObject.transform.position,
            GameObject.transform.rotation) as GameObject;
        copy.name = GameObject.name;
        copy.transform.parent = Parent;
        copy.SetActive(true);

        return copy;
    }

    public virtual void Save(GameObject original)
    {
        // Prevent instantiating multiple GameObjects for a single checkpoint data
        if (GameObject != null)
            throw new InvalidOperationException("Checkpoint data has been already saved.");

        // Create duplicate copy of original game object
        GameObject = GameObject.Instantiate(original, original.transform.position,
            original.transform.rotation) as GameObject;
        GameObject.name = original.name;
        Parent = original.transform.parent;
    }
}