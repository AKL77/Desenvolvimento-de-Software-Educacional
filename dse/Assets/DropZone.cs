using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image backgroundImage;
    public TextMeshProUGUI lineNumberText;

    [Header("Colors")]
    public Color emptyColor = new Color(1f, 1f, 1f, 0.1f);
    public Color highlightColor = new Color(1f, 1f, 1f, 0.3f);
    public Color occupiedColor = new Color(1f, 1f, 1f, 0.05f);

    [HideInInspector] public int lineIndex;
    [HideInInspector] public CardData placedCard = null;

    private GameObject placedCardObject = null;

    public void Setup(int index)
    {
        lineIndex = index;
        if (lineNumberText != null)
            lineNumberText.text = (index + 1).ToString();
        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging)
            backgroundImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    { 
    Debug.Log("OnDrop called on: " + gameObject.name);
    CardView draggedCard = eventData.pointerDrag?.GetComponent<CardView>();
    Debug.Log("Dragged card: " + (draggedCard == null ? "NULL" : draggedCard.cardData.displayName));
    if (draggedCard == null) return;

        // If dragged from another drop zone, handle the swap
        if (draggedCard.isPlacedCard && draggedCard.parentDropZone != null)
        {
            DropZone sourceZone = draggedCard.parentDropZone;

            if (sourceZone == this) return; // dropped on itself, do nothing

            // Swap: put this zone's card into the source zone
            if (placedCard != null)
                sourceZone.PlaceCard(placedCard);
            else
                sourceZone.ClearCard();
        }

        // Place the dragged card here
        PlaceCard(draggedCard.cardData);
    }

public void PlaceCard(CardData data)
{
    if (placedCardObject != null)
        Destroy(placedCardObject);

    placedCard = data;

    GameObject cardPrefab = SequenceManager.Instance.cardSlotPrefab;
    placedCardObject = Instantiate(cardPrefab, transform);

    RectTransform rect = placedCardObject.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(0, 0.5f);
    rect.anchorMax = new Vector2(0, 0.5f);
    rect.pivot = new Vector2(0, 0.5f);
    rect.anchoredPosition = new Vector2(50, 0); // offset from left, past the number
    rect.sizeDelta = new Vector2(100, 100);

    CardView cardView = placedCardObject.GetComponent<CardView>();
    cardView.Setup(data);
    cardView.isPlacedCard = true;
    cardView.parentDropZone = this;

    UpdateVisuals();
}

    public void ClearCard()
    {
        if (placedCardObject != null)
            Destroy(placedCardObject);

        placedCard = null;
        placedCardObject = null;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (backgroundImage == null) return;
        backgroundImage.color = placedCard != null ? occupiedColor : emptyColor;
    }
}
