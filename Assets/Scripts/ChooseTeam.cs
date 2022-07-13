using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChooseTeam : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private static short row = 0;
    private short myPlace;
    private byte f = 0;
    bool presseddown = false;

    private void OnEnable()
    {
        myPlace = row++;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        presseddown = true;
        StartCoroutine(CheckUp());
    }

    public IEnumerator CheckUp()
    {
        while (presseddown)
        {
            if (++f > 48)
            { Manager.mainInstance.TeamOfferPro(myPlace); presseddown = false; Destroy(gameObject); }
            yield return new WaitForSeconds(0.03125f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        presseddown = false;
        f = 0;
    }
}
