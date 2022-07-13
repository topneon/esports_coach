using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FIGMAMenu : MonoBehaviour
{
    [System.Serializable]
    private struct Data
    {
        public string menuName;
        public string functionCall;
        public Sprite sprite;
    }

    [SerializeField] private List<Data> menuList;
    [SerializeField] private GameObject[] objects;
    [SerializeField] private Text[] texts;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image goaway;

    uint menuPage = 0;
    uint pageCount = 0;

    private void Awake()
    {
        menu = this;
        uint m = (uint)menuList.Count - 1;
        m /= 6;
        menuPage = m + 1;
        pageCount = menuPage;
        m = menuPage;
        for (int i = 0; i < 10; i++) m *= menuPage;
        menuPage = m;
        Menu();
    }

    public void Left() { menuPage--; Menu(); }
    public void Right() { menuPage++; Menu(); }
    public void OpenClose() { opened = !opened; goaway.raycastTarget = opened; }
    bool opened = false;

    public static FIGMAMenu menu;
    public void ShowMenu() { gameObject.SetActive(true); }
    public void HideMenu() { gameObject.SetActive(false); }

    private void Update()
    {
        rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(rectTransform.anchoredPosition.x, opened ? 735 : 1185, 0.125f), 0);
        goaway.color = new Color(0, 0, 0, Mathf.Lerp(goaway.color.a, opened ? 0.625f : 0.0f, 0.125f));
    }

    private void Menu()
    {
        byte m = (byte)(menuPage % pageCount);
        uint a = 6;
        if (m + 1 == pageCount)
        {
            a = (uint)menuList.Count - 1;
            a %= 6;
            a++;
        }
        for (int i = 0; i < 6; i++)
        {
            if (i < a)
            {
                objects[i].SetActive(true);
                texts[i].text = menuList[m * 6 + i].menuName;
            }
            else
            {
                objects[i].SetActive(false);
            }
        }
    }

    public void OnClick(int i)
    {
        Manager.mainInstance.Invoke(menuList[(int)(menuPage % pageCount) * 6 + i].functionCall, 0);
    }
}
