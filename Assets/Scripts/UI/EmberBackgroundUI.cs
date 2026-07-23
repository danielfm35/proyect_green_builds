using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public sealed class EmberBackgroundUI : MonoBehaviour
{
    private const string SceneName = "BuildScene";
    private const string LayerName = "EmberBackground";
    private const int ParticleCount = 500;
    private const int SmokeParticleCount = 90;

    [SerializeField] private Color coreColor = new Color(1f, 0.74f, 0.18f, 1f);
    [SerializeField] private Color edgeColor = new Color(1f, 0.2f, 0.02f, 0.75f);
    [SerializeField] private Vector2 sizeRange = new Vector2(3f, 10f);
    [SerializeField] private Vector2 speedRange = new Vector2(34f, 105f);
    [SerializeField] private Vector2 driftRange = new Vector2(-42f, 42f);
    [SerializeField] private Color smokeInnerColor = new Color(0.95f, 0.08f, 0.05f, 0.18f);
    [SerializeField] private Color smokeOuterColor = new Color(0.32f, 0.02f, 0.01f, 0.04f);
    [SerializeField] private Vector2 smokeSizeRange = new Vector2(130f, 320f);
    [SerializeField] private Vector2 smokeSpeedRange = new Vector2(10f, 32f);
    [SerializeField] private Vector2 smokeDriftRange = new Vector2(-22f, 22f);
    [SerializeField] private float spawnPadding = 90f;

    private readonly List<EmberParticle> particles = new List<EmberParticle>(ParticleCount);
    private readonly List<SmokeParticle> smokeParticles = new List<SmokeParticle>(SmokeParticleCount);
    private RectTransform rectTransform;
    private Sprite emberSprite;
    private Sprite smokeSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForBuildScene()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreate();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate();
    }

    private static void TryCreate()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
            return;

        if (FindFirstObjectByType<EmberBackgroundUI>() != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform root = canvas.transform.Find("UI_Root");
        Transform parent = root != null ? root : canvas.transform;

        GameObject layerObject = new GameObject(LayerName, typeof(RectTransform), typeof(CanvasGroup));
        layerObject.layer = canvas.gameObject.layer;
        layerObject.transform.SetParent(parent, false);

        RectTransform layerRect = layerObject.GetComponent<RectTransform>();
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        layerRect.pivot = new Vector2(0.5f, 0.5f);

        CanvasGroup canvasGroup = layerObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        int siblingIndex = 0;
        Transform background = parent.Find("Background_Table");
        if (background != null)
            siblingIndex = background.GetSiblingIndex() + 1;

        layerObject.transform.SetSiblingIndex(siblingIndex);
        layerObject.AddComponent<EmberBackgroundUI>();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        smokeSprite = CreateSmokeSprite();
        emberSprite = CreateEmberSprite();

        for (int i = 0; i < SmokeParticleCount; i++)
            smokeParticles.Add(CreateSmokeParticle(i));

        for (int i = 0; i < ParticleCount; i++)
            particles.Add(CreateParticle(i));
    }

    private void Start()
    {
        for (int i = 0; i < smokeParticles.Count; i++)
            ResetSmokeParticle(smokeParticles[i], true);

        for (int i = 0; i < particles.Count; i++)
            ResetParticle(particles[i], true);
    }

    private void Update()
    {
        Rect rect = rectTransform.rect;
        float deltaTime = Time.unscaledDeltaTime;

        for (int i = 0; i < smokeParticles.Count; i++)
        {
            SmokeParticle smoke = smokeParticles[i];
            smoke.Age += deltaTime;
            smoke.Position += new Vector2(smoke.Drift + Mathf.Sin((Time.unscaledTime * smoke.WaveSpeed) + smoke.WaveOffset) * smoke.WaveAmplitude, smoke.Speed) * deltaTime;
            smoke.Rotation += smoke.RotationSpeed * deltaTime;

            float lifePercent = Mathf.Clamp01(smoke.Age / smoke.Lifetime);
            float fade = Mathf.Sin(lifePercent * Mathf.PI);
            float scale = Mathf.Lerp(0.7f, 1.65f, lifePercent) * smoke.Size;
            float pulse = 0.86f + Mathf.PingPong((Time.unscaledTime + smoke.WaveOffset) * 0.18f, 0.14f);

            smoke.Rect.anchoredPosition = smoke.Position;
            smoke.Rect.localRotation = Quaternion.Euler(0f, 0f, smoke.Rotation);
            smoke.Rect.sizeDelta = new Vector2(scale * pulse, scale * smoke.Stretch);
            smoke.Image.color = Color.Lerp(smokeOuterColor, smokeInnerColor, smoke.Intensity) * fade;

            if (smoke.Position.y > rect.yMax + spawnPadding || smoke.Age >= smoke.Lifetime)
                ResetSmokeParticle(smoke, false);
        }

        for (int i = 0; i < particles.Count; i++)
        {
            EmberParticle particle = particles[i];
            particle.Age += deltaTime;
            particle.Position += new Vector2(particle.Drift, particle.Speed) * deltaTime;
            particle.Rotation += particle.RotationSpeed * deltaTime;

            float lifePercent = Mathf.Clamp01(particle.Age / particle.Lifetime);
            float fade = Mathf.Sin(lifePercent * Mathf.PI);
            float flicker = 0.68f + Mathf.PingPong((Time.unscaledTime + particle.FlickerOffset) * particle.FlickerSpeed, 0.32f);
            float scale = Mathf.Lerp(0.55f, 1.1f, fade) * particle.Size;

            particle.Rect.anchoredPosition = particle.Position;
            particle.Rect.localRotation = Quaternion.Euler(0f, 0f, particle.Rotation);
            particle.Rect.sizeDelta = new Vector2(scale, scale * particle.Stretch);
            particle.Image.color = Color.Lerp(edgeColor, coreColor, particle.Heat) * (fade * flicker);

            if (particle.Position.y > rect.yMax + spawnPadding || particle.Age >= particle.Lifetime)
                ResetParticle(particle, false);
        }
    }

    private EmberParticle CreateParticle(int index)
    {
        GameObject particleObject = new GameObject("Ember_" + index.ToString("00"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        particleObject.layer = gameObject.layer;
        particleObject.transform.SetParent(transform, false);

        Image image = particleObject.GetComponent<Image>();
        image.sprite = emberSprite;
        image.raycastTarget = false;
        image.preserveAspect = false;

        RectTransform particleRect = particleObject.GetComponent<RectTransform>();
        particleRect.anchorMin = new Vector2(0.5f, 0.5f);
        particleRect.anchorMax = new Vector2(0.5f, 0.5f);
        particleRect.pivot = new Vector2(0.5f, 0.5f);

        return new EmberParticle
        {
            Rect = particleRect,
            Image = image
        };
    }

    private SmokeParticle CreateSmokeParticle(int index)
    {
        GameObject particleObject = new GameObject("RedSmoke_" + index.ToString("00"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        particleObject.layer = gameObject.layer;
        particleObject.transform.SetParent(transform, false);

        Image image = particleObject.GetComponent<Image>();
        image.sprite = smokeSprite;
        image.raycastTarget = false;
        image.preserveAspect = false;

        RectTransform particleRect = particleObject.GetComponent<RectTransform>();
        particleRect.anchorMin = new Vector2(0.5f, 0.5f);
        particleRect.anchorMax = new Vector2(0.5f, 0.5f);
        particleRect.pivot = new Vector2(0.5f, 0.5f);

        return new SmokeParticle
        {
            Rect = particleRect,
            Image = image
        };
    }

    private void ResetParticle(EmberParticle particle, bool randomizeAge)
    {
        Rect rect = rectTransform.rect;

        particle.Position = new Vector2(
            Random.Range(rect.xMin - spawnPadding, rect.xMax + spawnPadding),
            Random.Range(rect.yMin - spawnPadding, rect.yMax + spawnPadding));
        particle.Speed = Random.Range(speedRange.x, speedRange.y);
        particle.Drift = Random.Range(driftRange.x, driftRange.y);
        particle.Size = Random.Range(sizeRange.x, sizeRange.y);
        particle.Stretch = Random.Range(1.1f, 2.6f);
        particle.Rotation = Random.Range(0f, 360f);
        particle.RotationSpeed = Random.Range(-90f, 90f);
        particle.Lifetime = Random.Range(2.2f, 5.8f);
        particle.Age = randomizeAge ? Random.Range(0f, particle.Lifetime) : 0f;
        particle.Heat = Random.Range(0.25f, 1f);
        particle.FlickerOffset = Random.Range(0f, 8f);
        particle.FlickerSpeed = Random.Range(2.5f, 7.5f);
    }

    private void ResetSmokeParticle(SmokeParticle smoke, bool randomizeAge)
    {
        Rect rect = rectTransform.rect;

        smoke.Position = new Vector2(
            Random.Range(rect.xMin - spawnPadding, rect.xMax + spawnPadding),
            Random.Range(rect.yMin - spawnPadding, rect.yMax + spawnPadding));
        smoke.Speed = Random.Range(smokeSpeedRange.x, smokeSpeedRange.y);
        smoke.Drift = Random.Range(smokeDriftRange.x, smokeDriftRange.y);
        smoke.Size = Random.Range(smokeSizeRange.x, smokeSizeRange.y);
        smoke.Stretch = Random.Range(0.72f, 1.18f);
        smoke.Rotation = Random.Range(0f, 360f);
        smoke.RotationSpeed = Random.Range(-12f, 12f);
        smoke.Lifetime = Random.Range(6.5f, 12.5f);
        smoke.Age = randomizeAge ? Random.Range(0f, smoke.Lifetime) : 0f;
        smoke.Intensity = Random.Range(0.35f, 1f);
        smoke.WaveOffset = Random.Range(0f, 12f);
        smoke.WaveSpeed = Random.Range(0.45f, 1.15f);
        smoke.WaveAmplitude = Random.Range(4f, 18f);
    }

    private static Sprite CreateEmberSprite()
    {
        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "GeneratedEmberSprite",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (textureSize * 0.5f);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateSmokeSprite()
    {
        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "GeneratedRedSmokeSprite",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pixel = new Vector2(x, y);
                float distance = Vector2.Distance(pixel, center) / (textureSize * 0.5f);
                float softEdge = Mathf.Clamp01(1f - distance);
                float swirl = Mathf.PerlinNoise((x * 0.105f) + 19.3f, (y * 0.105f) + 4.7f);
                float alpha = Mathf.Pow(softEdge, 2.2f) * Mathf.Lerp(0.45f, 1f, swirl);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private sealed class EmberParticle
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Position;
        public float Speed;
        public float Drift;
        public float Size;
        public float Stretch;
        public float Rotation;
        public float RotationSpeed;
        public float Lifetime;
        public float Age;
        public float Heat;
        public float FlickerOffset;
        public float FlickerSpeed;
    }

    private sealed class SmokeParticle
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Position;
        public float Speed;
        public float Drift;
        public float Size;
        public float Stretch;
        public float Rotation;
        public float RotationSpeed;
        public float Lifetime;
        public float Age;
        public float Intensity;
        public float WaveOffset;
        public float WaveSpeed;
        public float WaveAmplitude;
    }
}
