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

    [Header("Bloco (laço)")]
    public Color blockTintColor = new Color(0.35f, 0.55f, 1f, 0.14f);

    [HideInInspector] public int lineIndex;
    [HideInInspector] public CardData placedCard = null;

    private GameObject placedCardObject = null;

    // Profundidade do bloco (0 = topo). Cada nível desloca a carta para a direita.
    private const float BaseCardX = 50f;
    private const float IndentPerLevel = 34f;
    private int blockDepth = 0;

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
    // Clean up any active ghost immediately
    CardView.CleanupGhost();

    CardView draggedCard = eventData.pointerDrag?.GetComponent<CardView>();
    if (draggedCard == null) return;

    if (draggedCard.isPlacedCard && draggedCard.parentDropZone != null)
    {
        DropZone sourceZone = draggedCard.parentDropZone;
        if (sourceZone == this) return;

        if (placedCard != null)
            sourceZone.PlaceCard(placedCard);
        else
            sourceZone.ClearCard();
    }

    PlaceCard(draggedCard.cardData);
}

public void PlaceCard(CardData data)
{
    // Don't destroy immediately if it's being dragged
    if (placedCardObject != null)
    {
        CardView existingView = placedCardObject.GetComponent<CardView>();
        if (existingView != null && existingView.isDragging)
            existingView.destroyOnDragEnd = true;
        else
            Destroy(placedCardObject);
    }

    placedCard = data;

    GameObject cardPrefab = SequenceManager.Instance.cardSlotPrefab;
    placedCardObject = Instantiate(cardPrefab, transform);

    RectTransform rect = placedCardObject.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(0, 0.5f);
    rect.anchorMax = new Vector2(0, 0.5f);
    rect.pivot = new Vector2(0, 0.5f);
    rect.anchoredPosition = new Vector2(BaseCardX + blockDepth * IndentPerLevel, 0);
rect.sizeDelta = new Vector2(160, 210);

    CardView cardView = placedCardObject.GetComponent<CardView>();
    cardView.Setup(data);
    cardView.isPlacedCard = true;
    cardView.parentDropZone = this;

    UpdateVisuals();

    // A carta colocada pode abrir/fechar um bloco — recalcula a indentação de todas as linhas.
    if (SequenceManager.Instance != null)
        SequenceManager.Instance.RefreshBlocks();
}

    // Ajustado pelo SequenceManager.RefreshBlocks(): indenta a carta e tinge a linha quando
    // está dentro de um laço.
    public void SetBlockDepth(int depth)
    {
        blockDepth = depth;
        if (placedCardObject != null)
        {
            RectTransform rect = placedCardObject.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(BaseCardX + blockDepth * IndentPerLevel, 0);
        }
        UpdateVisuals();
    }

    public void ClearCard()
    {
        if (placedCardObject != null)
            Destroy(placedCardObject);

        placedCard = null;
        placedCardObject = null;
        UpdateVisuals();

        // Remover uma carta pode fechar um bloco — recalcula a indentação das demais linhas.
        if (SequenceManager.Instance != null)
            SequenceManager.Instance.RefreshBlocks();
    }

    void UpdateVisuals()
    {
        if (backgroundImage == null) return;
        // Linhas dentro de um laço (depth > 0) ganham uma faixa colorida para parecer um bloco.
        if (blockDepth > 0)
            backgroundImage.color = blockTintColor;
        else
            backgroundImage.color = placedCard != null ? occupiedColor : emptyColor;
    }
}
