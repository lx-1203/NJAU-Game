using UnityEngine;

public enum EndingId
{
    None,
    Graduate_S,
    Graduate_A,
    Graduate_C,
    Civil_S,
    Civil_A,
    Civil_C,
    Startup_S,
    Startup_A,
    Startup_C,
    Education_S,
    Education_A,
    Education_B,
    Employment_S,
    Employment_A,
    Employment_B_Hometown,
    Open_A_UnnamedTomorrow,
    Balanced_B_StillOnTheRoad,
    Bad_C_PushedByTime
}

/// <summary>
/// Evaluates the narrative graduation ending and triggers the matching ending event.
/// </summary>
public class EndingEvaluator : MonoBehaviour
{
    public static EndingEvaluator Instance { get; private set; }

    public EndingId LastEvaluatedEnding { get; private set; } = EndingId.None;

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

    public EndingId EvaluateEnding()
    {
        if (PlayerAttributes.Instance == null || StoryRouteState.Instance == null || EventHistory.Instance == null)
        {
            Debug.LogWarning("[EndingEvaluator] Missing PlayerAttributes, StoryRouteState, or EventHistory. Falling back to bad ending.");
            return EndingId.Bad_C_PushedByTime;
        }

        return EvaluateEnding(PlayerAttributes.Instance, StoryRouteState.Instance, EventHistory.Instance);
    }

    public EndingId EvaluateEnding(PlayerAttributes p, StoryRouteState r, EventHistory h)
    {
        MainRoute route = r.lockedRoute;
        if (route == MainRoute.None)
        {
            route = r.currentRoute != MainRoute.None ? r.currentRoute : r.GetHighestTendencyRoute();
        }

        if (route == MainRoute.GraduateExam)
        {
            if (p.Study >= 85 &&
                r.researchTendency >= 80 &&
                p.Mood >= 50 &&
                p.Stress < 75 &&
                !h.GetFlag("graduate_exam_abandoned"))
                return EndingId.Graduate_S;

            if (p.Study >= 70 &&
                r.researchTendency >= 60 &&
                p.Mood >= 40)
                return EndingId.Graduate_A;

            if (p.Mood >= 50 &&
                (p.Study >= 50 || r.researchTendency >= 50))
                return EndingId.Graduate_C;
        }

        if (route == MainRoute.CivilService)
        {
            if (p.Leadership >= 75 &&
                r.civilServiceTendency >= 80 &&
                p.Study >= 60 &&
                p.Mood >= 50 &&
                p.Stress < 80)
                return EndingId.Civil_S;

            if (p.Leadership >= 60 &&
                r.civilServiceTendency >= 65 &&
                p.Mood >= 45)
                return EndingId.Civil_A;

            if (p.Mood >= 45)
                return EndingId.Civil_C;
        }

        if (route == MainRoute.Startup)
        {
            bool startupKeyFlag =
                h.GetFlag("startup_competition_finalist") ||
                h.GetFlag("cheng_project_joined") ||
                h.GetFlag("summer_startup_project") ||
                h.GetFlag("startup_business_plan_polished");

            if (p.Physique >= 85 &&
                r.startupTendency >= 80 &&
                p.Charm >= 65 &&
                p.Mood >= 45 &&
                startupKeyFlag &&
                !h.GetFlag("startup_team_disbanded"))
                return EndingId.Startup_S;

            if (p.Physique >= 70 &&
                r.startupTendency >= 60)
                return EndingId.Startup_A;

            if (p.Mood >= 40 ||
                (h.GetFlag("startup_team_disbanded") && (p.Physique >= 50 || r.startupTendency >= 50)))
                return EndingId.Startup_C;
        }

        if (route == MainRoute.Education)
        {
            if (r.educationTendency >= 80 &&
                p.Study >= 65 &&
                p.Charm >= 55 &&
                p.Mood >= 50)
                return EndingId.Education_S;

            if (r.educationTendency >= 65 &&
                p.Mood >= 55 &&
                h.GetFlag("volunteer_teaching_completed"))
                return EndingId.Education_A;

            if (r.educationTendency >= 55 &&
                p.Charm >= 45)
                return EndingId.Education_B;
        }

        if (route == MainRoute.Employment)
        {
            bool employmentKeyFlag =
                h.GetFlag("internship_completed") ||
                h.GetFlag("internship_interest_based") ||
                h.GetFlag("academic_award_resume_added") ||
                h.GetFlag("graduation_defense_project_explained");

            if (((p.Physique >= 80 && r.employmentTendency >= 75 && p.Charm >= 55 && p.Mood >= 45 && employmentKeyFlag) ||
                 (p.Physique >= 88 && r.employmentTendency >= 85 && p.Charm >= 65 && p.Mood >= 45)))
                return EndingId.Employment_S;

            if (p.Physique >= 60 &&
                r.employmentTendency >= 55)
                return EndingId.Employment_A;

            bool hometownFlag =
                h.GetFlag("hometown_choice_completed") ||
                CountFlags(h, "summer_rest_home", "final_choice_accept", "graduation_review_memory") >= 2;

            if (r.employmentTendency >= 50 &&
                p.Mood >= 55 &&
                hometownFlag)
                return EndingId.Employment_B_Hometown;
        }

        if ((route == MainRoute.Confused || r.confusedTendency >= 60 || p.Stress >= 60) &&
            p.Mood >= 60 &&
            CountBaseAttributesAtLeast(p, 50) >= 2)
            return EndingId.Open_A_UnnamedTomorrow;

        if (route == MainRoute.Balanced ||
            (p.Study >= 55 &&
             p.Charm >= 55 &&
             p.Physique >= 55 &&
             p.Mood >= 55 &&
             p.Stress < 60 &&
             r.MaxRouteTendency() < 75))
            return EndingId.Balanced_B_StillOnTheRoad;

        return EndingId.Bad_C_PushedByTime;
    }

    public string ConvertEndingToEventId(EndingId ending)
    {
        switch (ending)
        {
            case EndingId.Graduate_S: return "END_GRAD_S";
            case EndingId.Graduate_A: return "END_GRAD_A";
            case EndingId.Graduate_C: return "END_GRAD_C";
            case EndingId.Civil_S: return "END_CIVIL_S";
            case EndingId.Civil_A: return "END_CIVIL_A";
            case EndingId.Civil_C: return "END_CIVIL_C";
            case EndingId.Startup_S: return "END_STARTUP_S";
            case EndingId.Startup_A: return "END_STARTUP_A";
            case EndingId.Startup_C: return "END_STARTUP_C";
            case EndingId.Education_S: return "END_EDU_S";
            case EndingId.Education_A: return "END_EDU_A";
            case EndingId.Education_B: return "END_EDU_B";
            case EndingId.Employment_S: return "END_EMP_S";
            case EndingId.Employment_A: return "END_EMP_A";
            case EndingId.Employment_B_Hometown: return "END_EMP_B_HOME";
            case EndingId.Open_A_UnnamedTomorrow: return "END_OPEN_A";
            case EndingId.Balanced_B_StillOnTheRoad: return "END_BALANCED_B";
            default: return "END_BAD_C";
        }
    }

    public void EvaluateAndTriggerEnding()
    {
        EndingId ending = EvaluateEnding();
        LastEvaluatedEnding = ending;
        string eventId = ConvertEndingToEventId(ending);

        if (EventHistory.Instance != null)
        {
            EventHistory.Instance.SetFlag("ending_seen", true);
            EventHistory.Instance.SetFlag($"evaluated_{eventId.ToLowerInvariant()}", true);
        }

        Debug.Log($"[EndingEvaluator] Ending evaluated: {ending} -> {eventId}");

        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.EnqueueEvent(eventId);
        }
        else
        {
            Debug.LogWarning($"[EndingEvaluator] EventScheduler missing; cannot enqueue ending event {eventId}.");
        }
    }

    private int CountBaseAttributesAtLeast(PlayerAttributes p, int value)
    {
        int count = 0;
        if (p.Study >= value) count++;
        if (p.Charm >= value) count++;
        if (p.Physique >= value) count++;
        if (p.Leadership >= value) count++;
        if (p.Mood >= value) count++;
        return count;
    }

    private int CountFlags(EventHistory h, params string[] flags)
    {
        int count = 0;
        for (int i = 0; i < flags.Length; i++)
        {
            if (h.GetFlag(flags[i]))
            {
                count++;
            }
        }

        return count;
    }
}
