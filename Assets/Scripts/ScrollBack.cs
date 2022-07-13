using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollBack : MonoBehaviour
{
    [SerializeField] private float top, bottom;
    private RectTransform rectTransform;
    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(RectCheck());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public IEnumerator RectCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.0078125f);
            if (rectTransform.anchoredPosition.y < top)
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, top);
            if (rectTransform.anchoredPosition.y > bottom)
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, bottom);
        }
    }
}
