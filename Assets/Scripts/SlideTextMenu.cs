using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlideTextMenu : MonoBehaviour
{
    [SerializeField] private RectTransform[] texts;
    [SerializeField] private Text[] text;
    [SerializeField] private int page = 2;
    private bool transition;
    public void Left()
    {
        if (transition) return;
        if (page == 0) return;
        StartCoroutine(LeftTime());
    }
    public void Right()
    {
        if (transition) return;
        if (page == texts.Length - 1) return;
        StartCoroutine(RightTime());
    }

    private IEnumerator LeftTime()
    {
        transition = true;
        if (Manager.mainInstance.rateSetting == 0)
        {
            for (byte i = 0; i < 32; i++)
            {
                for (int j = 0; j < texts.Length; j++)
                {
                    if (page - 3 == j)
                    {
                        if (i < 27) text[j].fontSize += 1;
                    }
                    else if (page - 2 == j)
                    {
                        text[j].color += new Color(0.0078125f, 0.0078125f, 0.0078125f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize += 1;
                        texts[j].anchoredPosition += new Vector2(3.90625f, 0.0f);
                    }
                    else if (page - 1 == j)
                    {
                        text[j].color += new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize += 1;
                        texts[j].anchoredPosition += new Vector2(5.46875f, 0.0f);
                    }
                    else if (page == j)
                    {
                        text[j].color -= new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize -= 1;
                        texts[j].anchoredPosition += new Vector2(5.46875f, 0.0f);
                    }
                    else if (page + 1 == j)
                    {
                        text[j].color -= new Color(0.0078125f, 0.0078125f, 0.0078125f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize -= 1;
                        texts[j].anchoredPosition += new Vector2(3.90625f, 0.0f);
                    }
                    else if (page + 2 == j)
                    {
                        if (i < 27) text[j].fontSize -= 1;
                    }
                }
                yield return new WaitForSeconds(Manager.mainInstance.frameTime);
            }
        }
        else if (Manager.mainInstance.rateSetting == 1)
        {
            for (byte i = 0; i < 16; i++)
            {
                for (int j = 0; j < texts.Length; j++)
                {
                    if (page - 3 == j)
                    {
                        if (i < 9) text[j].fontSize += 3;
                    }
                    else if (page - 2 == j)
                    {
                        text[j].color += new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize += 1;
                        texts[j].anchoredPosition += new Vector2(7.8125f, 0.0f);
                    }
                    else if (page - 1 == j)
                    {
                        text[j].color += new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize += 1;
                        texts[j].anchoredPosition += new Vector2(10.9375f, 0.0f);
                    }
                    else if (page == j)
                    {
                        text[j].color -= new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize -= 1;
                        texts[j].anchoredPosition += new Vector2(10.9375f, 0.0f);
                    }
                    else if (page + 1 == j)
                    {
                        text[j].color -= new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize -= 1;
                        texts[j].anchoredPosition += new Vector2(7.8125f, 0.0f);
                    }
                    else if (page + 2 == j)
                    {
                        if (i < 9) text[j].fontSize -= 3;
                    }
                }
                yield return new WaitForSeconds(Manager.mainInstance.frameTime);
            }
        }
        else
        {
            for (byte i = 0; i < 8; i++)
            {
                for (int j = 0; j < texts.Length; j++)
                {
                    if (page - 3 == j)
                    {
                        if (i < 7)
                        { text[j].fontSize += 4; if (text[j].fontSize > 27) text[j].fontSize = 27; }
                    }
                    else if (page - 2 == j)
                    {
                        text[j].color += new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize += 2;
                        texts[j].anchoredPosition += new Vector2(15.625f, 0.0f);
                    }
                    else if (page - 1 == j)
                    {
                        text[j].color += new Color(0.0625f, 0.0625f, 0.0625f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize += 2;
                        texts[j].anchoredPosition += new Vector2(21.875f, 0.0f);
                    }
                    else if (page == j)
                    {
                        text[j].color -= new Color(0.0625f, 0.0625f, 0.0625f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize -= 2;
                        texts[j].anchoredPosition += new Vector2(21.875f, 0.0f);
                    }
                    else if (page + 1 == j)
                    {
                        text[j].color -= new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize -= 2;
                        texts[j].anchoredPosition += new Vector2(15.625f, 0.0f);
                    }
                    else if (page + 2 == j)
                    {
                        if (i < 7) text[j].fontSize -= text[j].fontSize % 4;
                    }
                }
                yield return new WaitForSeconds(Manager.mainInstance.frameTime);
            }
        }
        page--;
        transition = false;
    }

    private IEnumerator RightTime()
    {
        transition = true;
        if (Manager.mainInstance.rateSetting == 0)
        {
            for (byte i = 0; i < 32; i++)
            {
                for (byte j = 0; j < texts.Length; j++)
                {
                    if (page - 2 == j)
                    {
                        if (i < 27) text[j].fontSize -= 1;
                    }
                    else if (page - 1 == j)
                    {
                        text[j].color -= new Color(0.0078125f, 0.0078125f, 0.0078125f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize -= 1;
                        texts[j].anchoredPosition -= new Vector2(3.90625f, 0.0f);
                    }
                    else if (page == j)
                    {
                        text[j].color -= new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize -= 1;
                        texts[j].anchoredPosition -= new Vector2(5.46875f, 0.0f);
                    }
                    else if (page + 1 == j)
                    {
                        text[j].color += new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize += 1;
                        texts[j].anchoredPosition -= new Vector2(5.46875f, 0.0f);
                    }
                    else if (page + 2 == j)
                    {
                        text[j].color += new Color(0.0078125f, 0.0078125f, 0.0078125f, 0.0f);
                        if (i != 0 && i % 3 == 0) text[j].fontSize += 1;
                        texts[j].anchoredPosition -= new Vector2(3.90625f, 0.0f);
                    }
                    else if (page + 3 == j)
                    {
                        if (i < 27) text[j].fontSize += 1;
                    }
                }
                yield return new WaitForSeconds(Manager.mainInstance.frameTime);
            }
        }
        else if (Manager.mainInstance.rateSetting == 1)
        {
            for (byte i = 0; i < 16; i++)
            {
                for (byte j = 0; j < texts.Length; j++)
                {
                    if (page - 2 == j)
                    {
                        if (i < 9) text[j].fontSize -= 3;
                    }
                    else if (page - 1 == j)
                    {
                        text[j].color -= new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize -= 1;
                        texts[j].anchoredPosition -= new Vector2(7.8125f, 0.0f);
                    }
                    else if (page == j)
                    {
                        text[j].color -= new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize -= 1;
                        texts[j].anchoredPosition -= new Vector2(10.9375f, 0.0f);
                    }
                    else if (page + 1 == j)
                    {
                        text[j].color += new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize += 1;
                        texts[j].anchoredPosition -= new Vector2(10.9375f, 0.0f);
                    }
                    else if (page + 2 == j)
                    {
                        text[j].color += new Color(0.015625f, 0.015625f, 0.015625f, 0.0f);
                        if (i != 0 && i < 11) text[j].fontSize += 1;
                        texts[j].anchoredPosition -= new Vector2(7.8125f, 0.0f);
                    }
                    else if (page + 3 == j)
                    {
                        if (i < 9) text[j].fontSize += 3;
                    }
                }
                yield return new WaitForSeconds(Manager.mainInstance.frameTime);
            }
        }
        else
        {
            for (byte i = 0; i < 8; i++)
            {
                for (byte j = 0; j < texts.Length; j++)
                {
                    if (page - 2 == j)
                    {
                        if (i < 7) text[j].fontSize -= text[j].fontSize % 4;
                    }
                    else if (page - 1 == j)
                    {
                        text[j].color -= new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize -= 2;
                        texts[j].anchoredPosition -= new Vector2(15.625f, 0.0f);
                    }
                    else if (page == j)
                    {
                        text[j].color -= new Color(0.0625f, 0.0625f, 0.0625f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize -= 2;
                        texts[j].anchoredPosition -= new Vector2(21.875f, 0.0f);
                    }
                    else if (page + 1 == j)
                    {
                        text[j].color += new Color(0.0625f, 0.0625f, 0.0625f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize += 2;
                        texts[j].anchoredPosition -= new Vector2(21.875f, 0.0f);
                    }
                    else if (page + 2 == j)
                    {
                        text[j].color += new Color(0.03125f, 0.03125f, 0.03125f, 0.0f);
                        if (i != 0 && i < 6) text[j].fontSize += 2;
                        texts[j].anchoredPosition -= new Vector2(15.625f, 0.0f);
                    }
                    else if (page + 3 == j)
                    {
                        if (i < 7) 
                        { text[j].fontSize += 4; if (text[j].fontSize > 27) text[j].fontSize = 27; }
                    }
                }
                yield return new WaitForSeconds(Manager.mainInstance.frameTime);
            }
        }
        page++;
        transition = false;
    }
}
