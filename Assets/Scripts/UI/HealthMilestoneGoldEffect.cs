using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Owns the transfer independently of the boss, which may be replaced while coins are in flight.
public sealed class HealthMilestoneGoldEffect : MonoBehaviour
{
    private IEnumerator transfer;

    public static void Play(ShopManager shop, Vector2[] screenOrigins, int amount)
    {
        var go = new GameObject("HealthMilestoneGoldCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(HealthMilestoneGoldEffect));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20001;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var effect = go.GetComponent<HealthMilestoneGoldEffect>();
        effect.transfer = VictoryGoldTransfer.Play(canvas, null, null, null, shop, amount, screenOrigins);
        effect.StartCoroutine(effect.Run());
    }

    private IEnumerator Run()
    {
        yield return transfer;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Dispose executes the transfer's finally block and pays any coins still in flight.
        (transfer as IDisposable)?.Dispose();
        transfer = null;
    }
}
