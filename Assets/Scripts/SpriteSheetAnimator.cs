using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSheetAnimator : MonoBehaviour
{
    [System.Serializable]
    public class AnimationClip
    {
        public string name;
        public Sprite[] frames;
        public float frameRate = 8f;
        public bool loop = true;

        [HideInInspector] public float timer;
        [HideInInspector] public int currentFrame;
    }

    [Header("Animations")]
    [SerializeField] private List<AnimationClip> animations = new List<AnimationClip>();

    private static readonly Regex TrailingNumberRegex = new Regex(@"(\d+)$", RegexOptions.Compiled);

    private SpriteRenderer spriteRenderer;
    private AnimationClip currentClip;
    private bool isPlaying;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void LoadFromResources(string resourcePath, string clipName, float frameRate = 8f, bool loop = true, bool reverse = false)
    {
        Sprite[] allSprites = Resources.LoadAll<Sprite>(resourcePath);
        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogWarning($"[SpriteSheetAnimator] No sprites found in Resources/{resourcePath}");
            return;
        }

        allSprites = SortSpritesByFrameOrder(allSprites);
        if (reverse)
        {
            allSprites = allSprites.Reverse().ToArray();
        }

        AnimationClip existing = animations.Find(c => c.name == clipName);
        if (existing != null)
        {
            existing.frames = allSprites;
            existing.frameRate = frameRate;
            existing.loop = loop;
        }
        else
        {
            animations.Add(new AnimationClip
            {
                name = clipName,
                frames = allSprites,
                frameRate = frameRate,
                loop = loop
            });
        }

        Debug.Log($"[SpriteSheetAnimator] Loaded '{clipName}' with {allSprites.Length} frames from Resources/{resourcePath}");
    }

    private static Sprite[] SortSpritesByFrameOrder(IEnumerable<Sprite> sprites)
    {
        return sprites
            .OrderBy(sprite => GetNamePrefix(sprite.name))
            .ThenBy(sprite => GetTrailingNumber(sprite.name))
            .ThenBy(sprite => sprite.name)
            .ToArray();
    }

    private static string GetNamePrefix(string spriteName)
    {
        Match match = TrailingNumberRegex.Match(spriteName);
        return match.Success ? spriteName.Substring(0, match.Index) : spriteName;
    }

    private static int GetTrailingNumber(string spriteName)
    {
        Match match = TrailingNumberRegex.Match(spriteName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int frameNumber))
        {
            return frameNumber;
        }

        return int.MaxValue;
    }

    private void Update()
    {
        if (!isPlaying || currentClip == null || currentClip.frames == null || currentClip.frames.Length == 0)
        {
            return;
        }

        currentClip.timer += Time.deltaTime;
        float frameInterval = 1f / currentClip.frameRate;

        while (currentClip.timer >= frameInterval)
        {
            currentClip.timer -= frameInterval;
            currentClip.currentFrame++;

            if (currentClip.currentFrame >= currentClip.frames.Length)
            {
                if (currentClip.loop)
                {
                    currentClip.currentFrame = 0;
                }
                else
                {
                    currentClip.currentFrame = currentClip.frames.Length - 1;
                    isPlaying = false;
                    return;
                }
            }

            spriteRenderer.sprite = currentClip.frames[currentClip.currentFrame];
        }
    }

    public void Play(string clipName, bool forceRestart = false)
    {
        AnimationClip clip = animations.Find(c => c.name == clipName);
        if (clip == null || clip.frames == null || clip.frames.Length == 0)
        {
            Debug.LogWarning($"[SpriteSheetAnimator] Animation '{clipName}' not found or has no frames.");
            return;
        }

        if (!forceRestart && isPlaying && currentClip != null && currentClip.name == clipName)
        {
            return;
        }

        currentClip = clip;
        currentClip.currentFrame = 0;
        currentClip.timer = 0f;
        isPlaying = true;

        spriteRenderer.sprite = currentClip.frames[0];
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void Resume()
    {
        if (currentClip != null)
        {
            isPlaying = true;
        }
    }

    public bool IsPlaying => isPlaying;
    public string CurrentClipName => currentClip?.name;
}
