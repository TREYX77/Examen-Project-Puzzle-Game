using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The one entry point for changing scenes. Every route into a transition goes through
/// here, so fades behave the same whether they came from a button, a trigger volume or a
/// line of gameplay code.
///
/// <code>
/// SceneTransition.Load("MainMenu");            // default fade
/// SceneTransition.Load("TesLevel", 1.5f);      // slower fade
/// SceneTransition.Reload();                    // restart the current puzzle
/// </code>
///
/// This is a static class rather than a component, which is why the file name and the type
/// name are allowed to differ. Nothing needs to be added to a scene for it to work.
/// </summary>
public static class SceneTransition
{
    /// <summary>True while a fade is running. Gameplay can check this to ignore input.</summary>
    public static bool IsTransitioning => SceneTransitionFade.Instance.IsTransitioning;

    /// <summary>Load progress, 0 to 1, raised while a transition is running.</summary>
    public static event Action<float> Progress
    {
        add { SceneTransitionFade.Instance.Progress += value; }
        remove { SceneTransitionFade.Instance.Progress -= value; }
    }

    /// <summary>Loads a scene with the default fade.</summary>
    public static void Load(string sceneName)
    {
        Load(new TransitionSettings(sceneName));
    }

    /// <summary>Loads a scene, choosing how long each half of the fade takes.</summary>
    public static void Load(string sceneName, float fadeDuration)
    {
        Load(new TransitionSettings(sceneName, fadeDuration));
    }

    /// <summary>Loads a scene with a fade of a given length and colour.</summary>
    public static void Load(string sceneName, float fadeDuration, Color color)
    {
        TransitionSettings settings = new TransitionSettings(sceneName, fadeDuration)
        {
            color = color
        };

        Load(settings);
    }

    /// <summary>Loads a scene using a transition authored in the inspector.</summary>
    public static void Load(TransitionSettings settings)
    {
        SceneTransitionFade.Instance.Play(settings);
    }

    /// <summary>Reloads the active scene. Handy for restarting a puzzle.</summary>
    public static void Reload(float fadeDuration = 0.4f)
    {
        Load(new TransitionSettings(SceneManager.GetActiveScene().name, fadeDuration));
    }

    /// <summary>
    /// Fades to black and quits. In the editor this stops play mode instead, so the
    /// quit button in a main menu can be tested without building.
    /// </summary>
    public static void Quit(float fadeDuration = 0.4f)
    {
        SceneTransitionFade fade = SceneTransitionFade.Instance;
        TransitionSettings settings = new TransitionSettings(null, fadeDuration);

        fade.StartCoroutine(QuitRoutine(fade, settings));
    }

    private static System.Collections.IEnumerator QuitRoutine(
        SceneTransitionFade fade, TransitionSettings settings)
    {
        yield return fade.FadeOut(settings);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
