using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIImpactScaleAnimation : MonoBehaviour
{
    [SerializeField] private float scaleMultiplier = 1.5f;
    [SerializeField] private float scaleUpDuration = 0.32f;
    [SerializeField] private float scaleDownDuration = 0.2f;

    private Coroutine animationRoutine;

    public float Duration => scaleUpDuration + scaleDownDuration;

    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(PlayRoutine());
    }

    public IEnumerator PlayAndWait()
    {
        if (!isActiveAndEnabled)
            yield break;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        yield return PlayRoutine();
    }

    private IEnumerator PlayRoutine()
    {
        GameObject overlay = CreateOverlay();
        RectTransform target = overlay != null
            ? overlay.GetComponent<RectTransform>()
            : transform as RectTransform;

        if (target == null)
        {
            animationRoutine = null;
            yield break;
        }

        Vector3 normalScale = Vector3.one;
        Vector3 peakScale = normalScale * Mathf.Max(1f, scaleMultiplier);

        target.localScale = normalScale;
        yield return AnimateScale(target, normalScale, peakScale, scaleUpDuration);
        yield return AnimateScale(target, peakScale, normalScale, scaleDownDuration);
        target.localScale = normalScale;

        if (overlay != null)
            Destroy(overlay);

        animationRoutine = null;
    }

    private GameObject CreateOverlay()
    {
        RectTransform sourceRoot = transform as RectTransform;
        Transform parent = transform.parent;
        if (sourceRoot == null || parent == null)
            return null;

        GameObject overlay = new GameObject($"{gameObject.name}_ImpactScaleOverlay", typeof(RectTransform));
        overlay.transform.SetParent(parent, false);
        overlay.transform.SetAsLastSibling();

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        CopyRectTransform(sourceRoot, overlayRect);
        overlayRect.localScale = Vector3.one;

        CopyImageTree(sourceRoot, overlay.transform, sourceRoot);
        return overlay;
    }

    private void CopyImageTree(RectTransform sourceRoot, Transform overlayParent, RectTransform source)
    {
        Image sourceImage = source.GetComponent<Image>();
        Transform targetParent = overlayParent;

        if (sourceImage != null && sourceImage.enabled && sourceImage.gameObject.activeInHierarchy)
        {
            GameObject imageObject = new GameObject(source.gameObject.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(overlayParent, false);

            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            CopyRectTransformRelativeToRoot(sourceRoot, source, imageRect);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sourceImage.sprite;
            image.color = sourceImage.color;
            image.material = sourceImage.material;
            image.type = sourceImage.type;
            image.fillMethod = sourceImage.fillMethod;
            image.fillAmount = sourceImage.fillAmount;
            image.fillClockwise = sourceImage.fillClockwise;
            image.fillOrigin = sourceImage.fillOrigin;
            image.preserveAspect = sourceImage.preserveAspect;
            image.raycastTarget = false;

            targetParent = imageObject.transform;
        }

        for (int i = 0; i < source.childCount; i++)
        {
            if (source.GetChild(i) is RectTransform childRect)
                CopyImageTree(sourceRoot, targetParent, childRect);
        }
    }

    private void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.offsetMin = source.offsetMin;
        target.offsetMax = source.offsetMax;
    }

    private void CopyRectTransformRelativeToRoot(RectTransform sourceRoot, RectTransform source, RectTransform target)
    {
        if (source == sourceRoot)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            target.pivot = source.pivot;
            return;
        }

        CopyRectTransform(source, target);
    }

    private IEnumerator AnimateScale(RectTransform target, Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        target.localScale = to;
    }
}
