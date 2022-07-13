using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChooseLoader : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int loader;
    private short f = 0;
    public bool presseddown = false;
    [SerializeField] RectTransform rect, rectText;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (going) return;
        presseddown = true;
        StartCoroutine(CheckUp());
    }
    bool going = false;
    public IEnumerator CheckUp()
    {
        going = true;
        while (presseddown)
        {
            if (++f > 90)
            {
                Manager.mainInstance.LoadGame(loader);
                presseddown = false;
            }
            rect.anchoredPosition = new Vector2(-876 - Mathf.Cos(f * 0.03491f) * 761, 24.0f);
            rectText.anchoredPosition = new Vector2(-876 - Mathf.Cos(f * 0.03491f) * 761, 24.0f);
            yield return new WaitForSeconds(0.01667f);
        }
        while (f != 0)
        {
            f -= 2;
            if (f < 0) f = 0;
            rect.anchoredPosition = new Vector2(-876 - Mathf.Cos(f * 0.03491f) * 761, 24.0f);
            rectText.anchoredPosition = new Vector2(-876 - Mathf.Cos(f * 0.03491f) * 761, 24.0f);
            yield return new WaitForSeconds(0.01667f);
        }
        going = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        presseddown = false;
    }
}
