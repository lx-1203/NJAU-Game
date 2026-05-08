using UnityEngine;

/// <summary>
/// Bridges milestone story events to code-only systems that cannot be expressed cleanly in JSON.
/// </summary>
public class StoryMainEventHooks : MonoBehaviour
{
    public static StoryMainEventHooks Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.OnEventCompleted += HandleEventCompleted;
        }
    }

    private void OnDestroy()
    {
        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.OnEventCompleted -= HandleEventCompleted;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleEventCompleted(EventDefinition evt)
    {
        if (evt == null)
        {
            return;
        }

        switch (evt.id)
        {
            case "CHECK_002":
                if (StoryRouteResolver.Instance != null)
                {
                    StoryRouteResolver.Instance.ResolveAndLockRoute();
                }
                break;

            case "END_000":
                if (EndingEvaluator.Instance != null)
                {
                    EndingEvaluator.Instance.EvaluateAndTriggerEnding();
                }
                break;
        }
    }
}
