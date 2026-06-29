using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardCarousel : MonoBehaviour
{
    [Header("References")]
    public RectTransform content;
    public Button leftArrow;
    public Button rightArrow;

    [Header("Settings")]
    public int visibleCards = 5;
    public float cardWidth = 150f;
    public float spacing = 10f;
    public float scrollDuration = 0.3f;

    private int currentIndex = 0;
    private int totalCards = 0;
    private Vector2 basePosition;
    private bool hasBasePosition = false;

void Start()
{
    Debug.Log("CardCarousel Start — leftArrow null: " + (leftArrow == null) + ", rightArrow null: " + (rightArrow == null));
    leftArrow.onClick.AddListener(ScrollLeft);
    rightArrow.onClick.AddListener(ScrollRight);
    basePosition = content.anchoredPosition;
}

// CardCarousel lives under CardsPanel, which starts inactive (fullscreen map
// mode) — Awake/Start on inactive GameObjects is deferred until SetActive,
// but PhaseManager.Start() calls Refresh() earlier than that. Capture
// basePosition lazily on first real use instead of relying on either
// lifecycle method having run yet.
//
// basePosition.x is 0 (not the scene's hand-tuned -161.8) because that old
// offset was calibrated against the RectMask2D viewport window, which is
// currently disabled (see PROJETO.md "Dívidas técnicas conhecidas"). Content
// has pivot (0, 0.5), so x=0 puts the first card flush against CardsPanel's
// inner-left edge.
void EnsureBasePosition()
{
    if (hasBasePosition) return;
    basePosition = new Vector2(0f, content.anchoredPosition.y);
    hasBasePosition = true;
}

public void ScrollLeft()
{
    Debug.Log($"ScrollLeft called at frame {Time.frameCount}");
    if (totalCards <= visibleCards) return;
    currentIndex--;
    if (currentIndex < 0) currentIndex = totalCards - 1;
    SnapToIndex(currentIndex, true);
}

public void Refresh(int cardCount)
{
    Debug.Log($"Refresh called with cardCount: {cardCount}");
    totalCards = cardCount;
    currentIndex = 0;
    SnapToIndex(currentIndex, false);
}	



    public void ScrollRight()
    {
        if (totalCards <= visibleCards) return;
        currentIndex++;
        if (currentIndex > totalCards - 1) currentIndex = 0;
        SnapToIndex(currentIndex, true);
    }

void SnapToIndex(int index, bool animate)
{
    //EnsureBasePosition();
    float step = cardWidth + spacing;
    float targetX = basePosition.x - (index * step);

    Debug.Log($"Starting tween — from {content.anchoredPosition.x} to {targetX}");

    if (animate)
    {
content.DOAnchorPosX(targetX, scrollDuration)
    .SetEase(Ease.OutCubic)
    .OnComplete(() => {
        Debug.Log($"Tween complete — content pos: {content.anchoredPosition.x}");
        if (content.childCount > 0)
        {
            RectTransform firstCard = content.GetChild(0).GetComponent<RectTransform>();
            Debug.Log($"First card world pos: {firstCard.position}, local pos: {firstCard.anchoredPosition}");
        }
    });
    }
    else
    {
        content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);
    }
}
}
