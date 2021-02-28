using UnityEngine;
using System.Collections;

/// <summary>
/// If this component is attached to an enemy, once the enemy dies its GameObject is not
/// replaced with a corpse prefab, but instead kept until it's no longer in the viewport.
/// </summary>
/// <remarks>
/// This is used for Insectoids, since changing their corpses on the fly changes the material
/// shading, which looks distracting.
/// </remarks>
public class CleanupCorpse : MonoBehaviour
{
    public GameObject Corpse;
    public Transform CorpseContainer;

    private Renderer _renderer;

    void Start()
    {
        _renderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();

        PrepareForCleanup();
    }

    void Update()
    {
        // Run cleanup once "corpse" is no longer in viewport
        if (!_renderer.isVisible)
        {
            Cleanup();
        }
    }

    public void PrepareForCleanup()
    {
        // Remove enemy tag and layer
        gameObject.tag = "Corpse"; // Gets removed after checkpoint load
        gameObject.layer = 0;

        // Move enemy already to corpse container, waiting for cleanup
        gameObject.transform.parent = CorpseContainer;

        // Disable all scripts on root object, except this one
        foreach (var script in gameObject.GetComponents<MonoBehaviour>())
        {
            if (!(script is CleanupCorpse))
                script.enabled = false;
        }

        // Disable all child game objects, except the ones with renderers
        foreach (Transform child in gameObject.transform)
        {
            if (!child.gameObject.HasComponent<SkinnedMeshRenderer>())
                child.gameObject.SetActive(false);
        }
    }

    public void Cleanup()
    {
        CreateCorpse();

        // Destroy this game object finally after cleanup
        GameObject.Destroy(gameObject);
    }

    private void CreateCorpse()
    {
        var corpse = Instantiate(Corpse, gameObject.transform.position,
            gameObject.transform.rotation) as GameObject;
        corpse.transform.parent = gameObject.transform.parent;

        // Make the corpse to use the same material than the enemy used. 
        //
        // This part is the reason why this cleanup script was created in the first place. 
        // Changing material on the fly seems to affect lighting in a way,
        // that the corpse had a slighty more lighter shading.
        //
        // For the player, this seemed obvious and (slightly) distracting.
        var material = gameObject.GetComponentInChildren<RandomMaterial>();
        if (material != null)
        {
            var renderer = corpse.GetComponentInChildren<Renderer>();
            renderer.material = material.UsedMaterial;
        }
    }
}