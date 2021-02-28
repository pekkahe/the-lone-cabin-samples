using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Checkpoint data stored for unique <c>GameObjects</c>, such as enemies, items, etc. 
/// </summary>
public class UniqueData : CheckpointData
{
    public Guid Id { get; set; }

    public override GameObject Load()
    {
        var copy = base.Load();

        // Restore same ID to created object than the stored object has
        var identifier = copy.GetComponent<UniqueIdentifier>();
        identifier.Id = Id;

        return copy;
    }

    public override void Save(GameObject original)
    {
        base.Save(original);

        var identifier = original.GetComponent<UniqueIdentifier>();
        Id = identifier.Id;
    }
}