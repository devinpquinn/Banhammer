using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ChatEntryDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform originalParent;
    public RectTransform placeholder;
    public Canvas canvas;
    public float returnSpeed = 1000f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 returnTarget;
    private bool returning;

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
        Destroy(gameObject);
        StartCoroutine(CollapseAndDestroyPlaceholder());
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
            if (Vector3.Distance(transform.position, returnTarget) < 1f)
            {
                transform.SetParent(originalParent);
                transform.SetSiblingIndex(placeholder.GetSiblingIndex());
                Destroy(placeholder.gameObject);
                returning = false;
            }
        }
    }

    IEnumerator CollapseAndDestroyPlaceholder()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float startHeight = placeholder.sizeDelta.y;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float h = Mathf.Lerp(startHeight, 0, elapsed / duration);
            placeholder.sizeDelta = new Vector2(placeholder.sizeDelta.x, h);
            yield return null;
        }

        Destroy(placeholder.gameObject);
    }

    // Assign this prefab in the inspector
    public GameObject placeholderPrefab;
}
