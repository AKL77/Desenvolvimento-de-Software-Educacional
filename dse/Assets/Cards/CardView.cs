using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Card Data")]
    public CardData cardData;

    [Header("UI References")]
    public Image artworkImage;
    public Image cardBackground;
    public TextMeshProUGUI displayNameText;
    public TextMeshProUGUI repeatValueText;

    // is this card inside a DropZone (placed) or in the hand?
    [HideInInspector] public bool isPlacedCard = false;
    [HideInInspector] public DropZone parentDropZone = null;

    private Canvas rootCanvas;
    private GameObject ghostCard;
    private CanvasGroup ghostCanvasGroup;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        RefreshVisuals();
    }

    public void Setup(CardData data)
    {
        cardData = data;
        RefreshVisuals();
    }

    void RefreshVisuals()
{
    if (cardData == null) return;

    // Apply color to background
    if (cardBackground != null)
        cardBackground.color = cardData.cardColor;

    // Only apply artwork sprite if one is set
    if (artworkImage != null)
    {
        if (cardData.artwork != null)
        {
            artworkImage.sprite = cardData.artwork;
            artworkImage.color = Color.white; // make sure it's visible
        }
        else
        {
            // No artwork set — hide the artwork image entirely
            artworkImage.gameObject.SetActive(false);
        }
    }

    if (displayNameText != null)
        displayNameText.text = cardData.displayName;

    if (repeatValueText != null)
    {
        bool isRepeat = cardData.cardAction == CardAction.Repeat;
        bool isRepeatUntil = cardData.cardAction == CardAction.RepeatUntil;

        repeatValueText.gameObject.SetActive(isRepeat || isRepeatUntil);

        if (isRepeat)
            repeatValueText.text = cardData.repeatValue.ToString();
        else if (isRepeatUntil)
            repeatValueText.text = cardData.conditionTarget.ToString();
    }
}

public void OnBeginDrag(PointerEventData eventData)
{
    rootCanvas = GetComponentInParent<Canvas>();
    ghostCard = Instantiate(gameObject, rootCanvas.transform);

    // Remove CardView only, keep everything else
    CardView ghostView = ghostCard.GetComponent<CardView>();
    if (ghostView != null) Destroy(ghostView);

    // Get existing CanvasGroup or add one
    ghostCanvasGroup = ghostCard.GetComponent<CanvasGroup>();
    if (ghostCanvasGroup == null)
        ghostCanvasGroup = ghostCard.AddComponent<CanvasGroup>();

    ghostCanvasGroup.alpha = 0.75f;
    ghostCanvasGroup.blocksRaycasts = false;

    RectTransform ghostRect = ghostCard.GetComponent<RectTransform>();
    ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
    ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
    ghostRect.pivot = new Vector2(0.5f, 0.5f);
    ghostRect.sizeDelta = new Vector2(100, 100);
}

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostCard == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out Vector2 localPoint
        );

        ghostCard.GetComponent<RectTransform>().localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostCard != null)
            Destroy(ghostCard);

        // If this was a placed card and it wasn't dropped on a new zone,
        // check if it was dropped outside — if so clear its slot
        if (isPlacedCard && parentDropZone != null)
        {
            DropZone targetZone = eventData.pointerCurrentRaycast.gameObject?
                .GetComponentInParent<DropZone>();

            if (targetZone == null)
                parentDropZone.ClearCard();
        }
    }
}
