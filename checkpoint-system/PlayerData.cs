using UnityEngine;
using System.Collections;

/// <summary>
/// Checkpoint data stored for the player <c>GameObject</c>.
/// </summary>
public class PlayerData : CheckpointData
{
    public Vector3 MainCameraPosition { get; set; }

    public Quaternion MainCameraRotation { get; set; }

    public override void Save(GameObject original)
    {
        base.Save(original);

        // Remove the cursor of the instantiated copy, because we don't want to store it
        var moveController = GameObject.GetComponentInChildren<PlayerMoveController>();
        moveController.DeleteCursor();

        MainCameraPosition = Camera.main.transform.position;
        MainCameraRotation = Camera.main.transform.rotation;
    }
}