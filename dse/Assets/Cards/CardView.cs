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
    [HideInInspector] public bool isDragging = false;
    [HideInInspector] public bool destroyOnDragEnd = false;

    private Canvas rootCanvas;
    private GameObject ghostCard;
    private CanvasGroup ghostCanvasGroup;
    
    // Static reference so it can be cleaned up even if the card is destroyed
    private static GameObject activeGhost;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        Debug.Log($"[CardView] Awake on '{gameObject.name}' — rootCanvas={(rootCanvas != null ? rootCanvas.name : "NULL")}, cardData={(cardData != null ? cardData.displayName : "NULL")}");
        RefreshVisuals();
    }

    public void Setup(CardData data)
    {
        Debug.Log($"[CardView] Setup('{(data != null ? data.displayName : "NULL")}') called on '{gameObject.name}'");
        cardData = data;
        RefreshVisuals();
    }

    void RefreshVisuals()
{
    if (cardData == null)
    {
        Debug.LogWarning($"[CardView] RefreshVisuals on '{gameObject.name}' aborted — cardData is NULL");
        return;
    }

    Debug.Log($"[CardView] RefreshVisuals '{cardData.displayName}' on '{gameObject.name}' — " +
        $"cardBackground={(cardBackground != null ? "OK" : "NULL")} " +
        $"(active={(cardBackground != null ? cardBackground.gameObject.activeInHierarchy.ToString() : "n/a")}, " +
        $"sprite={(cardBackground != null ? (cardBackground.sprite != null ? cardBackground.sprite.name : "NULL") : "n/a")}), " +
        $"color={cardData.cardColor}, selfActive={gameObject.activeInHierarchy}, parent={transform.parent?.name}");

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
    isDragging = true;

    // Clean up any leftover ghost from a previous drag
    if (activeGhost != null)
        Destroy(activeGhost);

    rootCanvas = GetComponentInParent<Canvas>();
    ghostCard = Instantiate(gameObject, rootCanvas.transform);
    activeGhost = ghostCard;

    CardView ghostView = ghostCard.GetComponent<CardView>();
    if (ghostView != null) Destroy(ghostView);

    ghostCanvasGroup = ghostCard.GetComponent<CanvasGroup>();
    if (ghostCanvasGroup == null)
        ghostCanvasGroup = ghostCard.AddComponent<CanvasGroup>();

    ghostCanvasGroup.alpha = 0.75f;
    ghostCanvasGroup.blocksRaycasts = false;

    RectTransform ghostRect = ghostCard.GetComponent<RectTransform>();
    ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
    ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
    ghostRect.pivot = new Vector2(0.5f, 0.5f);
ghostRect.sizeDelta = new Vector2(150, 180);
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
    isDragging = false;

    // Destroy via static reference in case this object was destroyed
    if (activeGhost != null)
    {
        Destroy(activeGhost);
        activeGhost = null;
    }

    ghostCard = null;

    if (destroyOnDragEnd)
        Destroy(gameObject);

    if (isPlacedCard && parentDropZone != null)
    {
        DropZone targetZone = eventData.pointerCurrentRaycast.gameObject?
            .GetComponentInParent<DropZone>();
        if (targetZone == null)
            parentDropZone.ClearCard();
    }
}

public static void CleanupGhost()
{
    if (activeGhost != null)
    {
        Destroy(activeGhost);
        activeGhost = null;
    }
}
}
