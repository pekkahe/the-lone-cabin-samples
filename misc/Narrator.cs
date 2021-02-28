using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public delegate void NarrationCallback();

/// <summary>
/// Defines a simple text-based story narration.
/// </summary>
[Serializable]
public class Narration
{
    public string Text;
    public KeyCode ExitKey;

    public string Footer
    {
        get { return "<Press " + ExitKey.ToString() + " to Continue>"; }
    }
}

/// <summary>
/// Defines a story narration with text and image.
/// </summary>
[Serializable]
public class ItemReveal : Narration
{
    public Texture2D Image;
}

/// <summary>
/// Pauses the game and draws the GUI with the specified story narration
/// when requested.
/// </summary>
public class Narrator : MonoBehaviour
{
    #region Singleton (Unity)

    private static Narrator _instance;

    void Awake()
    {
        _instance = this;
        _narrationQueue = new List<Narration>();
        enabled = false;
    }

    public static Narrator Instance
    {
        get { return _instance; }
    }

    #endregion

    /// <summary>
    /// The texture drawn for the narration's background.
    /// </summary>
    public Texture2D Background;

    /// <summary>
    /// Alpha applied for the narration background texture.
    /// </summary>
    public float BackgroundAlpha = 0.9f;

    /// <summary>
    /// Alpha applied for specific <see cref="ItemReveal"/> narration backgrounds.
    /// </summary>
    public float ItemRevealBackgroundAlpha = 0.5f;

    /// <summary>
    /// Amount of padding in pixels between the border of the narration and content.
    /// </summary>
    public float BackgroundPadding = 50;

    /// <summary>
    /// The guaranteed height of a narration background regardless of content.
    /// </summary>
    public float MinBackgroundHeight = 150;

    private List<Narration> _narrationQueue;
    private Rect _itemWindow;
    private NarrationCallback _callback;

    void Update()
    {
        if (_narrationQueue.Count == 0)
            return;

        var narration = _narrationQueue[0];

        if (Input.GetKeyDown(narration.ExitKey))
        {
            // Continue to next narration, or normal gameplay
            _narrationQueue.RemoveAt(0);

            // Return to normal gameplay if there are no more narrations
            if (_narrationQueue.Count == 0)
                Exit();
        }
    }

    void OnGUI()
    {
        if (_narrationQueue.Count == 0)
            return;

        GUI.skin = GUIManager.Instance.GUISkin;

        GUIManager.Instance.SetupScaling();

        var narration = _narrationQueue[0];
        if (narration is ItemReveal)
        {
            DrawItemReveal(narration as ItemReveal);
        }
        else
        {
            DrawNarration(narration);
        }

        GUIManager.Instance.ResetScaling();
    }

    /// <summary>
    /// Hides any ongoing narration as well as clears the queue,
    /// and resumes normal gameplay.
    /// </summary>
    public void Break()
    {
        if (_narrationQueue.Count > 0)
        {
            enabled = false;

            _narrationQueue.Clear();

            GameManager.Instance.InGamePause(false);
        }
    }

    /// <summary>
    /// Plays the given <see cref="Narration"/>.
    /// </summary>
    public void Narrate(Narration narration)
    {
        // Prevent duplicate entries, as this can cause infinite narration looping
        if (IsInQueue(narration.Text))
            return;

        GameManager.Instance.InGamePause(true);

        AddToQueue(narration);
        enabled = true;
    }

    /// <summary>
    /// Plays the given <see cref="ItemReveal"/> narration.
    /// </summary>
    public void RevealItem(ItemReveal itemReveal)
    {
        Narrate(itemReveal);
    }

    /// <summary>
    /// Plays a <see cref="Narration"/> with the given text and exit key.
    /// </summary>
    public void Narrate(string text, KeyCode exitKey)
    {
        var narration = new Narration
        {
            Text = text,
            ExitKey = exitKey
        };

        Narrate(narration);
    }

    /// <summary>
    /// Plays the given <see cref="Narration"/> and invokes a callback
    /// delegate once the narration exits.
    /// </summary>
    public void Narrate(Narration narration, NarrationCallback callback)
    {
        _callback = callback;

        Narrate(narration);
    }

    /// <summary>
    /// Plays a <see cref="Narration"/> with the given text and exit key
    /// and invokes a callback delegate once the narration exits.
    /// </summary>
    public void Narrate(string text, KeyCode exitKey, NarrationCallback callback)
    {
        var narration = new Narration
        {
            Text = text,
            ExitKey = exitKey
        };

        Narrate(narration, callback);
    }

    private void Exit()
    {
        GameManager.Instance.InGamePause(false);

        if (_callback != null)
        {
            _callback();
            _callback = null;
        }

        enabled = false;
    }

    private bool IsInQueue(string text)
    {
        foreach (var narration in _narrationQueue)
        {
            if (string.Equals(narration.Text, text, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void AddToQueue(Narration narration)
    {
        _narrationQueue.Add(narration);
    }

    /// <summary>
    /// Draws the given <see cref="Narration"/> on the GUI.
    /// </summary>
    private void DrawNarration(Narration narration)
    {
        var style = GUI.skin.GetStyle("narrationText");
        var totalContent = new GUIContent(narration.Text + "\n\n" + narration.Footer);
        var totalSize = style.CalcSize(totalContent);

        var backgroundSize = DrawBackground(totalSize.y);

        var textArea = new Rect(GUIManager.Instance.NativeWidth / 2 - totalSize.x / 2,
            backgroundSize.center.y - totalSize.y / 2, totalSize.x, totalSize.y);

        GUILayout.BeginArea(textArea);

        var textSize = style.CalcSize(new GUIContent(narration.Text));
        var footerSize = style.CalcSize(new GUIContent(narration.Footer));

        // If text width is smaller than footer width, align text to center
        style.alignment = textSize.x < footerSize.x ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;

        GUILayout.Label(narration.Text, style);

        GUILayout.FlexibleSpace();

        // Align footer to center
        style.alignment = TextAnchor.UpperCenter;

        GUILayout.Label(narration.Footer, style);

        GUILayout.EndArea();
    }

    /// <summary>
    /// Draws the given <see cref="ItemReveal"/> on the GUI.
    /// </summary>
    private void DrawItemReveal(ItemReveal narration)
    {
        var style = GUI.skin.GetStyle("narrationText");
        var textContent = new GUIContent(narration.Text + "\n\n" + narration.Footer);
        var textSize = style.CalcSize(textContent);

        var windowStyle = GUI.skin.GetStyle("itemWindow");
        var windowWidth = windowStyle.padding.horizontal + narration.Image.width;
        var windowHeight = windowStyle.padding.vertical + textSize.y + narration.Image.height + 100;

        _itemWindow = new Rect(GUIManager.Instance.NativeWidth / 2 - windowWidth / 2,
            GUIManager.Instance.NativeHeight / 2 - windowHeight / 2, windowWidth, windowHeight);

        var originalColor = GUI.color;
        var color = Color.white;
        color.a = Mathf.Clamp01(ItemRevealBackgroundAlpha);

        GUI.color = color;
        _itemWindow = GUI.Window(0, _itemWindow, DrawItemRevealWindow, "Item Found", windowStyle);
        GUI.color = originalColor;
    }

    /// <summary>
    /// Draws the window portion of the <see cref="ItemReveal"/> narration.
    /// </summary>
    private void DrawItemRevealWindow(int windowId)
    {
        var narration = _narrationQueue[0] as ItemReveal;

        var windowStyle = GUI.skin.GetStyle("itemWindow");
        var textStyle = GUI.skin.GetStyle("narrationText");

        var textContent = new GUIContent(narration.Text + "\n\n" + narration.Footer);
        var textSize = textStyle.CalcSize(textContent);

        var imageArea = new Rect((_itemWindow.width - narration.Image.width) / 2, 100,
            narration.Image.width, narration.Image.height);

        GUI.DrawTexture(imageArea, narration.Image);

        var textArea = new Rect((_itemWindow.width - textSize.x) / 2, imageArea.yMax + 30,
            textSize.x, textSize.y);

        GUILayout.BeginArea(textArea);

        textStyle.alignment = TextAnchor.UpperLeft;

        GUILayout.Label(narration.Text, textStyle);

        GUILayout.FlexibleSpace();

        textStyle.alignment = TextAnchor.UpperCenter;

        GUILayout.Label(narration.Footer, textStyle);

        GUILayout.EndArea();
    }

    /// <summary>
    /// Draws the narration background texture and returns its size and position.
    /// </summary>
    private Rect DrawBackground(float textHeight)
    {
        if (textHeight < MinBackgroundHeight)
            textHeight = MinBackgroundHeight;

        var originalColor = GUI.color;
        var color = Color.white;
        color.a = Mathf.Clamp01(BackgroundAlpha);

        var totalHeight = textHeight + (BackgroundPadding * 2);
        var backgroundArea = new Rect(0, GUIManager.Instance.NativeHeight - totalHeight,
            GUIManager.Instance.NativeWidth, totalHeight);

        GUI.color = color;
        GUI.DrawTexture(backgroundArea, Background);
        GUI.color = originalColor;

        return backgroundArea;
    }
}
