using System;
using System.Linq;
using UnityEngine;

public sealed class PlayerIdleAnimator : MonoBehaviour
{
    [SerializeField] private string spriteResourcePath = "Images/characters/player/prototipo1";
    [SerializeField] private int frameWidth = 48;
    [SerializeField] private int frameHeight = 48;
    [SerializeField] private int frameCount = 10;
    [SerializeField] private bool useImportedSprites;
    [SerializeField] private float minimumImportedFrameHeight = 16f;
    [SerializeField] private float framesPerSecond = 8f;
    [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0f);
    [SerializeField] private float pixelsPerUnit = 100f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameTimer;
    private int frameIndex;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 10;
        }

        LoadFrames();

        if (frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (frames.Length <= 1 || framesPerSecond <= 0f)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        var frameDuration = 1f / framesPerSecond;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }

    private void LoadFrames()
    {
        if (useImportedSprites)
        {
            var importedSprites = Resources.LoadAll<Sprite>(spriteResourcePath);
            if (importedSprites.Length > 0)
            {
                frames = importedSprites
                    .Where(sprite => sprite.rect.height >= minimumImportedFrameHeight)
                    .OrderBy(sprite => GetFrameIndex(sprite.name))
                    .ThenBy(sprite => sprite.name, StringComparer.Ordinal)
                    .ToArray();

                foreach (var frame in frames)
                {
                    frame.texture.filterMode = FilterMode.Point;
                }

                return;
            }
        }

        var texture = Resources.Load<Texture2D>(spriteResourcePath);
        if (texture == null)
        {
            Debug.LogError($"Player animation texture was not found at Resources/{spriteResourcePath}.", this);
            frames = Array.Empty<Sprite>();
            return;
        }

        texture.filterMode = FilterMode.Point;
        var safeFrameWidth = Mathf.Max(1, frameWidth);
        var safeFrameHeight = Mathf.Max(1, Mathf.Min(frameHeight, texture.height));
        var availableFrameCount = Mathf.Max(1, texture.width / safeFrameWidth);
        frameCount = Mathf.Max(1, Mathf.Min(frameCount, availableFrameCount));

        frames = Enumerable.Range(0, frameCount)
            .Select(index => Sprite.Create(
                texture,
                new Rect(index * safeFrameWidth, 0f, safeFrameWidth, safeFrameHeight),
                pivot,
                pixelsPerUnit))
            .ToArray();
    }

    private static int GetFrameIndex(string spriteName)
    {
        var separatorIndex = spriteName.LastIndexOf('_');
        if (separatorIndex >= 0 && int.TryParse(spriteName.Substring(separatorIndex + 1), out var frameNumber))
        {
            return frameNumber;
        }

        return int.MaxValue;
    }
}
