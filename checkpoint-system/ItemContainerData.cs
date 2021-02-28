using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Checkpoint data stored for item containers.
/// </summary>
public class ItemContainerData : CheckpointData
{
    public Guid? RequiredItemId { get; set; }

    public override GameObject Load()
    {
        var copy = base.Load();

        // If the container required an item to be opened, restore it
        if (RequiredItemId != null)
        {
            var container = copy.GetComponent<ItemContainer>();
            container.RequiredItem = GameManager.FindItem(RequiredItemId.Value);
        }

        return copy;
    }

    public override void Save(GameObject original)
    {
        base.Save(original);

        var container = original.GetComponent<ItemContainer>();

        if (container.RequiredItem != null)
        {
            var identifier = container.RequiredItem.GetComponent<UniqueIdentifier>();
            RequiredItemId = identifier.Id;
        }
    }

    public static ItemContainerData Create(GameObject from)
    {
        var data = new ItemContainerData();

        data.Save(from);

        return data;
    }
}