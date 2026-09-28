using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class EncounterProgression : MonoBehaviour
{
    private BossData[] encounters;
    private BossUI opponent;
    private int current;
    private bool awaitingSelection;
    private GameObject overlay;
    private static readonly Color Gold = new Color(1f, .72f, .18f);

    public void Initialize(BossUI view)
    {
        if (encounters != null) return;
        opponent = view;
        encounters = Resources.LoadAll<BossData>("ScriptableObjects/Bosses")
            .Where(x => x.EncounterOrder > 0).OrderBy(x => x.EncounterOrder).ToArray();
        if (encounters.Length > 0) opponent.SetBoss(encounters[0]);
    }

    public void ShowAfterVictory()
    {
        if (awaitingSelection || encounters == null || opponent.CurrentHealth > 0) return;
        current++;
        awaitingSelection = true;
        BuildSelector();
    }

    private void SelectEncounter(int index)
    {
        if (!awaitingSelection || index != current || index >= encounters.Length) return;
        awaitingSelection = false;
        FindFirstObjectByType<ShopManager>()?.PrepareNextEncounter();
        GetComponent<BattleVictorySequence>().ResetVictory();
        opponent.SetBoss(encounters[index]);
        Destroy(overlay);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }

    private void BuildSelector()
    {
        overlay = new GameObject("EncounterSelector", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 21000;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var background = Panel(overlay.transform, "Backdrop", new Color(.025f, .09f, .10f, .98f), Vector2.zero, Vector2.zero);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
        bool complete = current >= encounters.Length;
        Label(overlay.transform, complete ? "LA CIÉNAGA HA CAÍDO" : "ELIGE TU SIGUIENTE CONTRINCANTE", new Vector2(0, 405), new Vector2(1700, 85), 48, Gold);
        Label(overlay.transform, complete ? "Has derrotado a los cinco guardianes." : "LA CIÉNAGA  /  " + current + " DE " + encounters.Length + " DERROTADOS", new Vector2(0, 325), new Vector2(1500, 60), 25, Color.white);
        for (int i = 0; i < encounters.Length; i++)
        {
            var data = encounters[i];
            bool defeated = i < current;
            bool available = i == current;
            Color accent = data.IsFinalBoss ? new Color(.8f, .3f, .4f) : Gold;
            float x = (i - (encounters.Length - 1) * .5f) * 342;
            var border = Panel(overlay.transform, "Encounter_" + (i + 1), available ? accent : new Color(.18f, .3f, .32f), new Vector2(x, -20), new Vector2(320, available ? 610 : 570));
            var card = Panel(border.transform, "Card", new Color(.065f, .14f, .16f), Vector2.zero, new Vector2(308, available ? 598 : 558));
            Label(card.transform, defeated ? "DERROTADO" : available ? "DISPONIBLE" : "PRÓXIMAMENTE", new Vector2(0, 225), new Vector2(290, 45), 22, available ? Gold : new Color(.5f, .65f, .65f));
            Label(card.transform, data.DisplayName, new Vector2(0, 155), new Vector2(280, 80), 30, Color.white);
            var badge = Panel(card.transform, "Emblem", data.IsFinalBoss ? new Color(.32f, .1f, .17f) : new Color(.13f, .26f, .26f), new Vector2(0, 40), new Vector2(132, 132));
            Label(badge.transform, defeated ? "✓" : data.IsFinalBoss ? "V" : (i + 1).ToString(), Vector2.zero, new Vector2(130, 130), 66, accent);
            Label(card.transform, data.IsFinalBoss ? "JEFE FINAL" : "ENEMIGO " + (i + 1), new Vector2(0, -55), new Vector2(285, 40), 22, accent);
            Label(card.transform, "VIDA  " + data.MaximumHealth + "\nATAQUE  " + data.AttackDamage, new Vector2(0, -120), new Vector2(280, 90), 27, Color.white);
            int index = i;
            var button = Panel(card.transform, "Select", available ? Gold : new Color(.14f, .22f, .23f), new Vector2(0, -225), new Vector2(272, 62));
            var click = button.gameObject.AddComponent<Button>();
            click.targetGraphic = button;
            click.interactable = available;
            click.onClick.AddListener(() => SelectEncounter(index));
            Label(button.transform, available ? "SELECCIONAR" : defeated ? "COMPLETADO" : "BLOQUEADO", Vector2.zero, new Vector2(265, 60), 24, available ? new Color(.08f, .12f, .13f) : new Color(.5f, .6f, .6f));
        }
        Label(overlay.transform, "Tu equipo, vida y oro se conservan entre combates.", new Vector2(0, -400), new Vector2(1500, 50), 25, Color.white);
        if (complete)
        {
            var restart = Panel(overlay.transform, "NewRun", Gold, new Vector2(0, -475), new Vector2(360, 60));
            var button = restart.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => SceneTransitionManager.LoadScene(SceneManager.GetActiveScene().name));
            Label(restart.transform, "NUEVA PARTIDA", Vector2.zero, new Vector2(350, 55), 25, Color.black);
        }
    }

    private static Image Panel(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    private static void Label(Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = fontSize;
        text.color = color;
        text.raycastTarget = false;
    }
}
