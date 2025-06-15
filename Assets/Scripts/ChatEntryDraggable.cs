using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ChatEntryDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    private Transform originalParent;
    private RectTransform placeholder;
    private Canvas canvas;
    private RectTransform parentRect;
    private float storedParentHeight;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    
    //  View in inspector
    public ChatSpawner.ChatCategory category;

    // Assign in inspector
    public GameObject placeholderPrefab;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        placeholder = Instantiate(placeholderPrefab, originalParent).GetComponent<RectTransform>();
        placeholder.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y);
        placeholder.SetSiblingIndex(transform.GetSiblingIndex());

        if(canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
        
        if(parentRect == null)
        {
            parentRect = transform.parent.GetComponent<RectTransform>();
        }
        
        // Adjust position to compensate for any scrolling
        float differenceY = parentRect.rect.height - storedParentHeight;
        Vector2 offsetY = rectTransform.anchoredPosition;
        offsetY.y -= differenceY;
        rectTransform.anchoredPosition = offsetY;
        
        transform.SetParent(canvas.transform); // Drag on top
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        GameObject hitBin = null;
        foreach (var result in results)
        {
            if (result.gameObject.CompareTag("Bin"))
            {
                hitBin = result.gameObject;
                break;
            }
        }

        if (hitBin != null)
        {
            // Dropped in a bin
            hitBin.GetComponent<BinReceiver>().Receive(this);
            placeholder.GetComponent<Placeholder>().Collapse();
            Destroy(gameObject);
        }
        else
        {
            // Return to original spot
            canvasGroup.blocksRaycasts = true;
            transform.position = placeholder.position; // Snap to placeholder
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(placeholder.GetSiblingIndex());
            Destroy(placeholder.gameObject);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(parentRect == null)
        {
            parentRect = transform.parent.GetComponent<RectTransform>();
        }
        
        storedParentHeight = parentRect.rect.height;
    }
}
