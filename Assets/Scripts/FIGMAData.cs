using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FIGMAData : MonoBehaviour, IPointerClickHandler
{
    public GameObject[] data;
    //[SerializeField] UnityEvent<object> action;
    public object value;
    public System.Action<object> action;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (action != null) action(value);
    }
}
