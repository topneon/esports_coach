using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TranslateObject : MonoBehaviour
{
    public enum Language { English = 0, Russian, German, Portuguese, French }
    public static Language language = Language.English;
    public List<string> modify;
    public List<string> defaultTexts;
    public int indexString = 0;

    [ContextMenu("Create Lang")]
    public void Create()
    {
        defaultTexts = new List<string>(256);
        for (short i = 0; i < 256; i++) defaultTexts.Add(string.Empty);
    }

    /*[ContextMenu("CopyToDefault")]
    public void Modify()
    {
        List<string> vs = new List<string>(512);
        for (int i = 0; i < 256; i++)
        {
            vs.Add(defaultTexts[i]);
        }
        for (int i = 0; i < 256; i++)
        {
            vs.Add(string.Empty);
        }
        defaultTexts = vs;
    }*/

    [ContextMenu("Add Lang")]
    public void Add()
    {
        int c = defaultTexts.Capacity / 256;
        List<string> vs = new List<string>((c + 1) * 256);
        for (int i = 0, p = 0; i < ((c + 1) * 256); i++)
        {
            if ((i) % (c + 1) == c) { vs.Add(string.Empty); continue; }
            vs.Add(defaultTexts[p]);
            p++;
        }
        defaultTexts = vs;
    }

    /*[ContextMenu("GG")]
    public void GG()
    {
        defaultTexts = new List<string[]>();
        TranslateObject[] translateObjects = FindObjectsOfType<TranslateObject>(true);
        for (int i = 0; i < translateObjects.Length; i++)
        {
            defaultTexts.Add(translateObjects[i].text);
            translateObjects[i].indexString = i;
        }
    }*/

    public void OnEnable() { GetComponent<Text>().text = Manager.mainInstance.GetTranslate(indexString); }
    public void Save() { PlayerPrefs.SetInt("language", (int)language); }
    public int Load() { return PlayerPrefs.GetInt("language"); }
}
