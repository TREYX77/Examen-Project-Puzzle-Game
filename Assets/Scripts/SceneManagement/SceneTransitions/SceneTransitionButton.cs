using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Per-button scene change. Drop this on a UI Button, type the scene name, done: the
/// Button's onClick is hooked automatically, so there is nothing to wire in the inspector.
///
/// <see cref="Go"/> is public, so it can also be picked from a Button's On Click list, or
/// from any other UnityEvent such as an Animation Event or a Timeline signal.
/// </summary>
[AddComponentMenu("Scene Management/Scene Transition Button")]
public class SceneTransitionButton : MonoBehaviour
{
    [SerializeField] private TransitionSettings transition = new TransitionSettings();

    [Tooltip("Hook this object's Button automatically. Turn off if you would rather wire " +
             "Go() into the Button's On Click list yourself.")]
    [SerializeField] private bool autoHookButton = true;

    [Tooltip("Optional. Left empty, a Button on this object is used.")]
    [SerializeField] private Button button;

    /// <summary>The transition this button runs, so code can retune it at runtime.</summary>
    public TransitionSettings Transition => transition;

    void Awake()
    {
        if (!autoHookButton)
        {
            return;
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(Go);
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Go);
        }
    }

    /// <summary>Starts the transition. Safe to call twice: a running fade ignores the second call.</summary>
    public void Go()
    {
        SceneTransition.Load(transition);
    }

    /// <summary>Loads a scene by name, ignoring this component's own destination.</summary>
    public void Go(string sceneName)
    {
        TransitionSettings overridden = transition.Clone();
        overridden.sceneName = sceneName;
        SceneTransition.Load(overridden);
    }
}
