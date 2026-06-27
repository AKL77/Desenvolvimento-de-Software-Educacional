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

void Start()
{
    Debug.Log("CardCarousel Start — leftArrow null: " + (leftArrow == null) + ", rightArrow null: " + (rightArrow == null));
    leftArrow.onClick.AddListener(ScrollLeft);
    rightArrow.onClick.AddListener(ScrollRight);
    basePosition = content.anchoredPosition;
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
