using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The game's checkpoint system. Manages state by copying <c>GameObjects</c> into
/// a container and disabling them. To load a saved checkpoint, the inplay
/// game objects are deleted and the copies are restored from the container.
/// </summary>
public class CheckpointStorage
{
    private GameObject _storageContainer;
    private PlayerData _playerData;
    private Dictionary<string, List<ICheckpointData>> _dataDictionary =
        new Dictionary<string, List<ICheckpointData>>();

    public GameObject StorageContainer
    {
        get
        {
            if (_storageContainer == null)
                _storageContainer = new GameObject("Checkpoint Storage");

            return _storageContainer;
        }
    }

    /// <summary>
    /// Saves checkpoint by cloning a specified set of game objects.
    /// </summary>
    public void Save()
    {
        SavePlayer();
        SaveData("Enemy");
        SaveData("Corpse");
        SaveData("PickableItem");
        SaveData("ItemContainer");

        // Items outside of checkpoint storage:
        // - Drop zone items
        // - Door states (whether was open or closed)
    }

    /// <summary>
    /// Loads the previously saved checkpoint by removing all
    /// specified game objects and restoring the cloned ones.
    /// </summary>
    public void Load()
    {
        LoadPlayer();
        LoadData("Enemy");
        LoadData("Corpse");
        LoadData("PickableItem");
        LoadData("ItemContainer");
    }

    private void SavePlayer()
    {
        ClearStorage(_playerData);

        _playerData = new PlayerData();
        _playerData.Save(Player.Get.GameObject);

        MoveToStorage(_playerData);
    }

    private void SaveData(string tag)
    {
        // Try to get the appropriate data container for given tag,
        // or create one if it doesn't exist
        if (!_dataDictionary.ContainsKey(tag))
            _dataDictionary.Add(tag, new List<ICheckpointData>());

        // Clear any previously stored data
        ClearStorage(_dataDictionary[tag]);

        // Store data to storage
        foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
        {
            var data = CreateData(tag);
            data.Save(obj);

            MoveToStorage(data);

            _dataDictionary[tag].Add(data);
        }
    }

    private void LoadPlayer()
    {
        if (_playerData == null)
        {
            Debug.Log("No player data saved. Nothing to load.");
            return;
        }

        // Remove existing player object and cursor
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            Delete(player);

        // Restore camera position _before_ instantiating the new player object,
        // because player move controller's Awake function rely on camera position
        Camera.main.transform.position = _playerData.MainCameraPosition;
        Camera.main.transform.rotation = _playerData.MainCameraRotation;

        // Instantiate copy of player object from storage
        _playerData.Load();

        // Refresh player cache
        Player.Refresh();

        // Setup inventory and animation
        Player.Get.Inventory.SetActiveItem();
        Player.Get.Inventory.HoldItems();

        // Refresh main camera audio sources
        AudioManager.Refresh();
    }

    private void LoadData(string tag)
    {
        // Try to get the appropriate data container for given tag
        if (!_dataDictionary.ContainsKey(tag))
        {
            Debug.Log("No checkpoint data saved for game objects tagged '{0}'. Nothing to load."
                .Parameters(tag));
            return;
        }

        // Delete any existing scene objects with the given tag
        foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
            Delete(obj);

        // Create copies of the given checkpoint data back to scene
        foreach (var data in _dataDictionary[tag])
            data.Load();
    }

    private ICheckpointData CreateData(string tag)
    {
        // Note: Use reflection instead?
        switch (tag)
        {
            case "Player":
                return new PlayerData();
            case "Enemy":
            case "Corpse":
            case "PickableItem":
                return new UniqueData();
            case "ItemContainer":
                return new ItemContainerData();
            default:
                throw new InvalidOperationException("Invalid tag " + tag);
        }
    }

    private void Delete(GameObject gameObj)
    {
        if (gameObj == null)
            return;

        // Disable first before deleting, so if GameObject.Find is used during this frame,
        // it will not return the disabled object waiting to be destroyed.
        gameObj.SetActive(false);

        GameObject.Destroy(gameObj);
    }

    private void ClearStorage<T>(List<T> data) where T : CheckpointData
    {
        foreach (var item in data)
            ClearStorage(item);

        data.Clear();
    }

    private void ClearStorage<T>(T data) where T : CheckpointData
    {
        if (data != null && data.GameObject != null)
        {
            GameObject.Destroy(data.GameObject);
        }
    }

    private void ClearStorage(List<ICheckpointData> data)
    {
        foreach (var item in data)
            ClearStorage(item);

        data.Clear();
    }

    private void ClearStorage(ICheckpointData data)
    {
        if (data != null && data.GameObject != null)
            GameObject.Destroy(data.GameObject);
    }

    private void MoveToStorage(ICheckpointData data)
    {
        // Move object into storage and disable it
        data.GameObject.transform.parent = StorageContainer.transform;
        data.GameObject.SetActive(false);
    }
}
