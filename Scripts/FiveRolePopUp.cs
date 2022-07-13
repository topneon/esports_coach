using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FiveRolePopUp : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image[] images;
    public void OnPointerDown(PointerEventData eventData)
    {
        for (byte i = 0; i < 5; i++)
        {
            if (images[i] == GetComponent<Image>())
                images[i].color = new Color(0.25f, 0.46875f, 1.0f, 1.0f);
            else images[i].color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
    }
}
