using System;
using UnityEngine;

public enum MainRoute
{
    None,
    GraduateExam,
    CivilService,
    Startup,
    Education,
    Employment,
    Confused,
    Balanced
}

/// <summary>
/// Manages the player's long-term story route tendencies and locked main route.
/// </summary>
public class StoryRouteState : MonoBehaviour, ISaveable
{
    public static StoryRouteState Instance { get; private set; }

    [Header("当前路线")]
    public MainRoute currentRoute = MainRoute.None;

    [Header("大三结束后锁定路线")]
    public MainRoute lockedRoute = MainRoute.None;

    [Header("路线倾向")]
    public int researchTendency;
    public int civilServiceTendency;
    public int startupTendency;
    public int educationTendency;
    public int employmentTendency;
    public int confusedTendency;

    [Header("路线进度")]
    public int routeProgress;

    [Header("结局修正")]
    public int endingQualityBonus;

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

    public void AddTendency(string tendencyName, int value)
    {
        switch (tendencyName)
        {
            case "研究倾向":
            case "research":
                researchTendency += value;
                break;

            case "公职倾向":
            case "civil":
                civilServiceTendency += value;
                break;

            case "创业倾向":
            case "startup":
                startupTendency += value;
                break;

            case "教育倾向":
            case "education":
                educationTendency += value;
                break;

            case "就业倾向":
            case "employment":
                employmentTendency += value;
                break;

            case "迷茫倾向":
            case "confused":
                confusedTendency += value;
                break;

            default:
                Debug.LogWarning($"[StoryRouteState] 未知倾向：{tendencyName}");
                break;
        }

        ClampAll();
    }

    public int GetTendency(string tendencyName)
    {
        switch (tendencyName)
        {
            case "研究倾向":
            case "research":
                return researchTendency;

            case "公职倾向":
            case "civil":
                return civilServiceTendency;

            case "创业倾向":
            case "startup":
                return startupTendency;

            case "教育倾向":
            case "education":
                return educationTendency;

            case "就业倾向":
            case "employment":
                return employmentTendency;

            case "迷茫倾向":
            case "confused":
                return confusedTendency;

            default:
                return 0;
        }
    }

    public void SetRoute(string routeName)
    {
        if (TryParseRoute(routeName, out MainRoute route))
        {
            currentRoute = route;
            Debug.Log($"[StoryRouteState] 当前路线设置为：{currentRoute}");
        }
        else
        {
            Debug.LogWarning($"[StoryRouteState] 未知路线：{routeName}");
        }
    }

    public void LockRoute(string routeName)
    {
        if (TryParseRoute(routeName, out MainRoute route))
        {
            lockedRoute = route;
            Debug.Log($"[StoryRouteState] 锁定路线设置为：{lockedRoute}");
        }
        else
        {
            Debug.LogWarning($"[StoryRouteState] 未知锁定路线：{routeName}");
        }
    }

    public MainRoute GetHighestTendencyRoute()
    {
        int max = researchTendency;
        MainRoute route = MainRoute.GraduateExam;

        if (civilServiceTendency > max)
        {
            max = civilServiceTendency;
            route = MainRoute.CivilService;
        }

        if (startupTendency > max)
        {
            max = startupTendency;
            route = MainRoute.Startup;
        }

        if (educationTendency > max)
        {
            max = educationTendency;
            route = MainRoute.Education;
        }

        if (employmentTendency > max)
        {
            max = employmentTendency;
            route = MainRoute.Employment;
        }

        if (confusedTendency > max)
        {
            route = MainRoute.Confused;
        }

        return route;
    }

    public int GetCurrentRouteTendency()
    {
        switch (currentRoute)
        {
            case MainRoute.GraduateExam:
                return researchTendency;
            case MainRoute.CivilService:
                return civilServiceTendency;
            case MainRoute.Startup:
                return startupTendency;
            case MainRoute.Education:
                return educationTendency;
            case MainRoute.Employment:
                return employmentTendency;
            case MainRoute.Confused:
                return confusedTendency;
            default:
                return 0;
        }
    }

    public int MaxRouteTendency()
    {
        return Mathf.Max(
            researchTendency,
            civilServiceTendency,
            startupTendency,
            educationTendency,
            employmentTendency,
            confusedTendency
        );
    }

    public void AddRouteProgress(int value)
    {
        routeProgress += value;
        ClampAll();
    }

    public void AddEndingQualityBonus(int value)
    {
        endingQualityBonus += value;
        ClampAll();
    }

    public void SaveToData(SaveData data)
    {
        data.currentRoute = currentRoute.ToString();
        data.lockedRoute = lockedRoute.ToString();
        data.researchTendency = researchTendency;
        data.civilServiceTendency = civilServiceTendency;
        data.startupTendency = startupTendency;
        data.educationTendency = educationTendency;
        data.employmentTendency = employmentTendency;
        data.confusedTendency = confusedTendency;
        data.routeProgress = routeProgress;
        data.endingQualityBonus = endingQualityBonus;
    }

    public void LoadFromData(SaveData data)
    {
        if (TryParseRoute(data.currentRoute, out MainRoute loadedCurrentRoute))
        {
            currentRoute = loadedCurrentRoute;
        }

        if (TryParseRoute(data.lockedRoute, out MainRoute loadedLockedRoute))
        {
            lockedRoute = loadedLockedRoute;
        }

        researchTendency = data.researchTendency;
        civilServiceTendency = data.civilServiceTendency;
        startupTendency = data.startupTendency;
        educationTendency = data.educationTendency;
        employmentTendency = data.employmentTendency;
        confusedTendency = data.confusedTendency;
        routeProgress = data.routeProgress;
        endingQualityBonus = data.endingQualityBonus;
        ClampAll();
    }

    public static bool TryParseRoute(string routeName, out MainRoute route)
    {
        switch (routeName)
        {
            case "考研":
            case "研究":
            case "研究生":
                route = MainRoute.GraduateExam;
                return true;
            case "考公":
            case "公职":
                route = MainRoute.CivilService;
                return true;
            case "创业":
                route = MainRoute.Startup;
                return true;
            case "教育":
            case "教师":
                route = MainRoute.Education;
                return true;
            case "就业":
            case "工作":
                route = MainRoute.Employment;
                return true;
            case "迷茫":
                route = MainRoute.Confused;
                return true;
            case "均衡":
            case "平衡":
                route = MainRoute.Balanced;
                return true;
        }

        return Enum.TryParse(routeName, true, out route);
    }

    private void ClampAll()
    {
        researchTendency = Mathf.Clamp(researchTendency, 0, 100);
        civilServiceTendency = Mathf.Clamp(civilServiceTendency, 0, 100);
        startupTendency = Mathf.Clamp(startupTendency, 0, 100);
        educationTendency = Mathf.Clamp(educationTendency, 0, 100);
        employmentTendency = Mathf.Clamp(employmentTendency, 0, 100);
        confusedTendency = Mathf.Clamp(confusedTendency, 0, 100);
        routeProgress = Mathf.Clamp(routeProgress, 0, 100);
        endingQualityBonus = Mathf.Clamp(endingQualityBonus, -10, 10);
    }
}
