using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

public class FIGMASwitch : MonoBehaviour, IPointerDownHandler
{
    bool activated = false;
    [SerializeField] RectTransform oval;
    [SerializeField] Image background;
    [SerializeField] Text text;
    System.Action<bool> act;
    readonly Color dead = new Color(0.1647059f, 0.2352941f, 0.2666667f, 1);
    readonly Color alive = new Color(0.2392157f, 0.8352941f, 0.5960785f, 1);

    public void SetAction(System.Action<bool> action) { this.act = action; }
    public bool GetBool() { return activated; }

    public void SetBool(bool active)
    {
        activated = active;
        //StartCoroutine(Change());
    }

    public void SetText(string word) { text.text = word; }

    float f;
    private void OnEnable()
    {
        f = Manager.mainInstance.frameTime * 5;
        //Debug.Log((f).ToString());
    }

    private void Update()
    {
        background.color = Color.Lerp(background.color, activated ? alive : dead, f);
        oval.anchoredPosition = new Vector2(Mathf.Lerp(oval.anchoredPosition.x, activated ? 19.4f : -19.4f, f), -5.5f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetBool(!activated);
        if (act != null) act(activated);
    }

    //bool running = false;
    /*private IEnumerator Change()
    {
        if (!running)
        {
            running = true;
            
            while ((alive.g - background.color.g < 0.001f && activated) || (background.color.g - dead.g < 0.001f && !activated))
            {
                
                yield return new WaitForEndOfFrame();
            }
            background.color = activated ? alive : dead;
            running = false;
        }
    }*/
}
