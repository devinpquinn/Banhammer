using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ChatEntryDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform originalParent;
    private RectTransform placeholder;
    private Canvas canvas;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 returnTarget;
    private bool returning;

    // Assign in inspector
    public GameObject placeholderPrefab;

    // Tuning values
    private float returnSpeed = 50f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        placeholder = Instantiate(placeholderPrefab, originalParent).GetComponent<RectTransform>();
        placeholder.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y);
        placeholder.SetSiblingIndex(transform.GetSiblingIndex());

        transform.SetParent(canvas.transform); // drag on top
        canvasGroup.blocksRaycasts = false;
        returning = false;
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
            placeholder.GetComponent<Placeholder>().Collapse();
            Destroy(gameObject);
        }
        else
        {
            // Return to original spot
            returnTarget = placeholder.position;
            returning = true;
            canvasGroup.blocksRaycasts = true;
        }
    }


    void Update()
    {
        if (returning)
        {
            transform.position = Vector3.MoveTowards(transform.position, returnTarget, returnSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, returnTarget) < 0.01f)
            {
                transform.SetParent(originalParent);
                transform.SetSiblingIndex(placeholder.GetSiblingIndex());
                Destroy(placeholder.gameObject);
                returning = false;
            }
        }
    }
}
