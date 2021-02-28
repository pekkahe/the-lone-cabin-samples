using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameProgress
{
    Intro = 0,
    Beginning = 1,
    FirstEnemyFound = 2,
    FirstEnemyKilled = 3,
    OutdoorAvailable = 4,
    CarFound = 5,
    AllCarPartsFound = 6
}

/// <summary>
/// Game progress manager responsible for spawning enemies, triggering narrations,
/// showing items, etc. based on the current story progress.
/// </summary>
public class ProgressManager : MonoBehaviour
{
    /// <summary>
    /// The animated intro shown on game start.
    /// </summary>
    public Intro Intro;

    private const string _beginning = 
        "Hmmm... strange. This cabin seemed inhabited from the outside.\n\n" +
        "Well, maybe the owner has left to conduct some business.\n" +
        "I'll just have to wait until he comes back. There's no point in-\n\n" +
        "What was that noise?";

    private const string _firstEnemyFound = 
        "What the hell!?";
    
    private const string _firstEnemyKilled = 
        "Was that a ...werewolf!?\n\n" +
        "I need to get out of here! I saw a car on the yard when I arrived.\n" +
        "Let's hope it's still there!";

    private const string _flashlightFound = 
        "All right, it's time to get out here.\n\n" +
        "Whoever the owner of this cabin is, I don't think he'll be coming back.";

    private const string _carFound = 
        "Damn! This car won't go anywhere in its current condition.\n" +
        "One tire is flat, the battery is worn out and the gas tank is empty.\n\n" +
        "I should look around, maybe I can find spare parts somewhere around here.";

    private const string _allCarPartsFound = 
        "Phew! That should be all. Now I just need some time to fix this.";

    private NarrationCallback _narrationCallback;
    private GameProgress _highestProgress;

    /// <summary>
    /// Current game progress.
    /// </summary>
    public GameProgress CurrentProgress { get; private set; }

    /// <summary>
    /// Begins the game progress from the given state.
    /// </summary>
    public void Begin(GameProgress startProgress)
    {
        CurrentProgress = startProgress;
        _highestProgress = CurrentProgress;

        ActOnProgress();
    }

    /// <summary>
    /// Advances the game progress to the next state.
    /// </summary>
    public void Advance()
    {
        CurrentProgress++;
        _highestProgress = CurrentProgress;

        ActOnProgress();
    }

    /// <summary>
    /// Loads the game progress to the specified state.
    /// </summary>
    public void LoadProgress(GameProgress progress)
    {
        CurrentProgress = progress;

        ActOnProgress(true);
    }

    /// <summary>
    /// Overrides internal game progress state without acting on it.
    /// </summary>
    /// <remarks>
    /// Provided for debugging purposes.
    /// </remarks>
    public void Override(GameProgress gameProgress)
    {
        CurrentProgress = gameProgress;
    }

    /// <summary>
    /// Handles the current game progress by e.g. spawning enemies, triggering narrations
    /// or showing items.
    /// </summary>
    /// <param name="checkpointLoaded">Whether acting was triggered by a checkpoint load or not.</param>
    private void ActOnProgress(bool checkpointLoaded = false)
    {
        switch (CurrentProgress)
        {
            case GameProgress.Intro:
                GameManager.Instance.InGamePause(true);
                AudioManager.Instance.PlayEerieMusic();

                Intro.Play();
                break;

            case GameProgress.Beginning:
                AudioManager.Instance.PlayForestAmbient();

                if (checkpointLoaded)
                    AudioManager.Instance.PlayEerieMusic();

                _narrationCallback += PlayBeginningAudio;
                _narrationCallback += ClearCallback;

                Narrator.Instance.Narrate(_beginning, KeyCode.Space, _narrationCallback);
                break;

            case GameProgress.FirstEnemyFound:
                AudioManager.Instance.StopBackgroundMusic(0.3f);

                if (checkpointLoaded)
                    OpenCutsceneDoor();

                _narrationCallback += AudioManager.Instance.PlayChaseMusic;
                _narrationCallback += ClearCallback;

                Narrator.Instance.Narrate(_firstEnemyFound, KeyCode.Space, _narrationCallback);

                DisableOutDoors(true);
                break;

            case GameProgress.FirstEnemyKilled:
                AudioManager.Instance.StopBackgroundMusic();

                Invoke("EndFirstEnemyKilledProgress", 4.0f);
                break;

            case GameProgress.OutdoorAvailable:
                AudioManager.Instance.PlayAmbientLoop();

                Narrator.Instance.Narrate(_flashlightFound, KeyCode.Space);

                ResetDropZone();

                if (checkpointLoaded)
                    EnemyManager.Instance.ChangeAi(AiBehaviour.Patrolling);
                else
                    SpawnEnemies();
                break;

            case GameProgress.CarFound:
                Narrator.Instance.Narrate(_carFound, KeyCode.Space);
                break;

            case GameProgress.AllCarPartsFound:
                AudioManager.Instance.PlayAmbientLoop();

                Narrator.Instance.Narrate(_allCarPartsFound, KeyCode.Space);

                ResetFixer();
                break;
        }

        SetItemAvailability();

        Debug.Log("Acted on progress: " + CurrentProgress);
    }

    private void SetItemAvailability()
    {
        HidePlayerHealthBar(_highestProgress < GameProgress.FirstEnemyFound);

        DisablePlayerRunning(_highestProgress < GameProgress.FirstEnemyFound);

        DisableItemContainer("GunCabinet", _highestProgress < GameProgress.FirstEnemyFound);

        DisablePickableItem("GunCabinetKey", _highestProgress < GameProgress.FirstEnemyFound);
        DisablePickableItem("HealthDose", _highestProgress < GameProgress.FirstEnemyFound);
        DisablePickableItem("Flashlight", _highestProgress < GameProgress.FirstEnemyKilled);
        DisablePickableItem("GasCan", _highestProgress < GameProgress.CarFound ||
                                      _highestProgress >= GameProgress.AllCarPartsFound);
        DisablePickableItem("Tire", _highestProgress < GameProgress.CarFound ||
                                    _highestProgress >= GameProgress.AllCarPartsFound);
        DisablePickableItem("Toolbox", _highestProgress < GameProgress.CarFound ||
                                       _highestProgress >= GameProgress.AllCarPartsFound);
        DisablePickableItem("CarBattery", _highestProgress < GameProgress.CarFound ||
                                          _highestProgress >= GameProgress.AllCarPartsFound);
        DisablePickableItem("FenceGateKey", _highestProgress < GameProgress.CarFound ||
                                            _highestProgress >= GameProgress.AllCarPartsFound);
    }

    private void SpawnEnemies()
    {
        Spawner.Instance.SpawnEnemies(EnemyType.Insectoid, GameAreaEnum.UpperHighlands, 3);
        Spawner.Instance.SpawnEnemies(EnemyType.Muktar, GameAreaEnum.LowerHighlands, 3);
        Spawner.Instance.SpawnEnemies(EnemyType.Insectoid, GameAreaEnum.UpperLowlands, 3);
        Spawner.Instance.SpawnEnemies(EnemyType.Muktar, GameAreaEnum.LowerLowlands, 3);
        Spawner.Instance.SpawnEnemies(EnemyType.Werewolf, GameAreaEnum.FencedPowerLine, 1);
        Spawner.Instance.SpawnEnemies(EnemyType.Werewolf, GameAreaEnum.DecayedConcreteHouse, 1);
    }

    private void HidePlayerHealthBar(bool hide)
    {
        Player.Get.Health.enabled = !hide;
    }

    private void PlayBeginningAudio()
    {
        var doorObject = GameObject.FindWithTag("FirstEnemyCutsceneDoor");
        var doorAudio = doorObject.GetComponentInChildren<OpenableDoorAudio>();
        doorAudio.PlayOpenSound();

        AudioManager.Instance.PlayBoomAmbient(0.4f, 2.0f);
    }

    private void EndFirstEnemyKilledProgress()
    {
        // Ensure the cabin outdoor is closed until further game progress is made
        CloseCutsceneDoor();

        var cutscene = GameObject.FindWithTag("FirstEnemyCutscene");
        GameObject.Destroy(cutscene);

        Narrator.Instance.Narrate(_firstEnemyKilled, KeyCode.Space);

        AudioManager.Instance.PlayAmbientLoop();

        DisableOutDoors(false);
    }

    private void DisableOutDoors(bool disable)
    {
        foreach (var doorTrigger in GameObject.FindGameObjectsWithTag("MainDoorTrigger"))
        {
            var trigger = doorTrigger.GetComponent<DoorTrigger>();
            trigger.enabled = !disable;

            // Lock the doors also, so the enemy can't open them
            trigger.Door.IsLocked = disable;
        }
    }

    private void DisablePlayerRunning(bool disable)
    {
        Player.Get.MoveController.ForceWalk = disable;
    }

    private void ClearCallback()
    {
        _narrationCallback = null;
    }

    private void DisableItemContainer(string name, bool disable)
    {
        foreach (var item in GameObject.FindGameObjectsWithTag("ItemContainer"))
        {
            if (item.gameObject.name == name)
            {
                var script = item.GetComponent<ItemContainer>();
                script.enabled = !disable;
            }
        }
    }

    private void DisablePickableItem(string name, bool disable)
    {
        foreach (var item in GameObject.FindGameObjectsWithTag("PickableItem"))
        {
            if (item.gameObject.name == name)
            {
                var script = item.GetComponent<PickableItem>();
                if (disable)
                    script.MakeStatic();
                else
                    script.MakeDynamic();
            }
        }
    }

    private void OpenCutsceneDoor()
    {
        var gameObj = GameObject.FindWithTag("FirstEnemyCutsceneDoor");

        var door = gameObj.GetComponentInChildren<OpenableDoor>();
        door.ForceOpen();
    }

    private void CloseCutsceneDoor()
    {
        var gameObj = GameObject.FindWithTag("FirstEnemyCutsceneDoor");

        var door = gameObj.GetComponentInChildren<OpenableDoor>();
        door.ForceClose();
    }

    private void ResetDropZone()
    {
        var gameObj = GameObject.FindWithTag("DropZone");

        var dropZone = gameObj.GetComponent<DropZone>();
        dropZone.Reset();
    }

    private void ResetFixer()
    {
        var gameObj = GameObject.FindWithTag("DropZone");

        var fixer = gameObj.GetComponent<DropZoneFixer>();
        fixer.Reset();
    }
}
