using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class GameSection : MonoBehaviour
{

    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private CanvasGroup canvasGroup;

    public LayoutElement LayoutElement => layoutElement;
    public CanvasGroup CanvasGroup => canvasGroup;

    private void Reset()
    {
        layoutElement = GetComponent<LayoutElement>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }


    }
}
