using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Placeholder : MonoBehaviour
{
    private RectTransform placeholder;
    private float collapseTime = 0.1f;
    
    void Awake()
    {
        placeholder = GetComponent<RectTransform>();
    }

    public void Collapse()
    {
        StartCoroutine(CollapseAndDestroy());
    }
    
    IEnumerator CollapseAndDestroy()
    {
        float duration = collapseTime;
        float elapsed = 0f;
        float startHeight = placeholder.sizeDelta.y;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float h = Mathf.Lerp(startHeight, 0, elapsed / duration);
            placeholder.sizeDelta = new Vector2(placeholder.sizeDelta.x, h);
            yield return null;
        }

        Destroy(gameObject);
    }
}
