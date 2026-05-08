using UnityEngine;

/// <summary>
/// Resolves the player's long-term route when the story reaches the lock point.
/// </summary>
public class StoryRouteResolver : MonoBehaviour
{
    public static StoryRouteResolver Instance { get; private set; }

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

    public void ResolveAndLockRoute()
    {
        if (StoryRouteState.Instance == null || PlayerAttributes.Instance == null)
        {
            Debug.LogWarning("[StoryRouteResolver] Missing StoryRouteState or PlayerAttributes.");
            return;
        }

        StoryRouteState route = StoryRouteState.Instance;
        PlayerAttributes attr = PlayerAttributes.Instance;

        int currentTendency = route.GetCurrentRouteTendency();

        if (route.currentRoute != MainRoute.None &&
            currentTendency >= 60 &&
            attr.Mood >= 35)
        {
            route.lockedRoute = route.currentRoute;
        }
        else if (currentTendency < 60 && attr.Physique >= 55)
        {
            route.lockedRoute = MainRoute.Employment;
        }
        else if (attr.Stress >= 75)
        {
            route.lockedRoute = MainRoute.Confused;
        }
        else if (attr.Study >= 55 &&
                 attr.Charm >= 55 &&
                 attr.Physique >= 55 &&
                 attr.Mood >= 55 &&
                 attr.Stress < 60)
        {
            route.lockedRoute = MainRoute.Balanced;
        }
        else
        {
            route.lockedRoute = route.currentRoute != MainRoute.None
                ? route.currentRoute
                : route.GetHighestTendencyRoute();
        }

        Debug.Log($"[StoryRouteResolver] Route locked at end of junior year: {route.lockedRoute}");

        if (EventHistory.Instance != null)
        {
            EventHistory.Instance.SetFlag($"locked_route_{route.lockedRoute}", true);
        }
    }
}
