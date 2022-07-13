using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FIGMAStructure : MonoBehaviour
{
    public enum ElementType { Action = -1, Background = 0, TextValue = 1, Subtitle = 2, Icon = 3, Flag = 4 }
    public enum StructureType { Custom = -1, TitleValue = 0, Button = 1, FlagName = 2 }
    public enum PredefinedType { Custom = -1, Player = 0, PlayerStats = 1, PlayerAction = 2, Team = 3 }

    //[SerializeField] Image fadedImage;
    [SerializeField] GameObject[] prefabs;

    GameObject[] menu;
    FIGMAFlags[] _flags;

    static readonly float[] steps = new float[]
    {
        0, 1920, 960, 640, 480, 384, 320, 274.285714286f, 240, 213.333333333f, 192
    };

    public static object[] MakeParams(params FIGMAParameter[] objects)
    {
        return MakeParamsA(objects);
    }
    public static object[] MakeParamsA(FIGMAParameter[] objects)
    {
        //ushort repeat = 0;
        List<object> list = new List<object>();
        //object[] o = new object[5];
        //for (byte j = 0; j < 5; j++) o[j] = null;
        List<byte> ids = new List<byte>();
        for (ushort i = 0; i < objects.Length; i++)
        {
            if (!ids.Contains(objects[i].id))
            {
                ids.Add(objects[i].id);
                list.AddRange(new object[] { null, null, null, null, null });
            }
        }
        for (ushort i = 0; i < objects.Length; i++)
        {
            if (objects[i].type == "text")
            {
                list[ids.IndexOf(objects[i].id) * 5 + 2] = objects[i].value;
            }
            else if (objects[i].type == "color")
            {
                list[ids.IndexOf(objects[i].id) * 5 + 3] = objects[i].value;
            }
            else if (objects[i].type == "sprite")
            {
                list[ids.IndexOf(objects[i].id) * 5 + 4] = (Sprite)objects[i].value;
            }
            else if (objects[i].type == "x")
            {
                list[ids.IndexOf(objects[i].id) * 5] = objects[i].value;
            }
            else if (objects[i].type == "y")
            {
                list[ids.IndexOf(objects[i].id) * 5 + 1] = objects[i].value;
            }
            else if (objects[i].type == "action")
            {
                list[ids.IndexOf(objects[i].id) * 5] = objects[i].value;
            }
        }
        return list.ToArray();
    }

    public static FIGMAFlags[] MakeFlags(PredefinedType type, params object[] objects)
    {
        return MakeFlagsA(type, objects);
    }

    //player: nickname, age = 0 ... teamname, its placement = 1; stats = 0-5; 

    //lengths and types should look like: { 3, 2 } (this is .Player example),
    //{ Flag, TextValue, Subtitle, TextValue, Subtitle }
    public static FIGMAFlags[] MakeFlagsA(PredefinedType type, object[] objects,
        byte[] lengths = null, ElementType[] types = null)
    {
        FIGMAFlags[] flags;
        object[] o;
        switch (type)
        {
            case PredefinedType.Custom:
                flags = new FIGMAFlags[lengths.Length];
                for (byte i = 0, c = 0, d = 0; i < lengths.Length; i++)
                {
                    byte a = (byte)(lengths[i] * 5);
                    o = new object[a];
                    ElementType[] t = new ElementType[lengths[i]];
                    for (byte j = 0; j < a; j++) o[j] = objects[c + j];
                    for (byte j = 0; j < lengths[i]; j++) t[j] = types[d + j]; 
                    flags[i].type = StructureType.Custom;
                    flags[i].customElements = MakeElement(flags[i].type, o, t);
                    c += a;
                    d += lengths[i];
                }
                return flags;
            case PredefinedType.Player:
                flags = new FIGMAFlags[2];
                //flags[0].id = 0;
                o = new object[15];
                for (ushort i = 0; i < 15; i++) o[i] = objects[i];
                flags[0].type = StructureType.FlagName;
                flags[0].customElements = MakeElement(flags[0].type, o);
                o = new object[10];
                for (ushort i = 0; i < 10; i++) o[i] = objects[i + 15];
                flags[1].type = StructureType.TitleValue;
                flags[1].customElements = MakeElement(flags[1].type, o);
                return flags;
            case PredefinedType.PlayerStats:
                flags = new FIGMAFlags[6];
                //flags[0].id = 0;
                for (byte j = 0; j < 6; j++)
                {
                    o = new object[10];
                    for (ushort i = 0; i < 10; i++) o[i] = objects[j * 10 + i];
                    flags[j].type = StructureType.TitleValue;
                    flags[j].customElements = MakeElement(flags[j].type, o);
                }
                return flags;
            case PredefinedType.PlayerAction:
                flags = new FIGMAFlags[3];
                //flags[0].id = 0;
                for (byte j = 0; j < 3; j++)
                {
                    o = new object[15];
                    for (ushort i = 0; i < 15; i++) o[i] = objects[j * 15 + i];
                    flags[j].type = StructureType.Button;
                    flags[j].customElements = MakeElement(flags[j].type, o);
                }
                return flags;
        }
        return null;
    }

    public static FIGMAElement[] MakeElement(StructureType type, object[] objects, ElementType[] types = null)
    {
        FIGMAElement[] elements;
        switch (type)
        {
            case StructureType.Custom:
                elements = new FIGMAElement[types.Length];
                for (byte i = 0; i < types.Length; i++)
                {
                    elements[i].elementType = types[i];
                    elements[i].values = new object[5];
                    for (byte j = 0; j < 5; j++)
                    {
                        elements[i].values[j] = objects[i * 5 + j];
                    }
                }
                return elements;
            case StructureType.TitleValue:
                elements = new FIGMAElement[2];
                elements[0].elementType = ElementType.TextValue;
                elements[0].values = new object[5];
                elements[1].elementType = ElementType.Subtitle;
                elements[1].values = new object[5];
                for (byte i = 0; i < 5; i++)
                {
                    elements[0].values[i] = objects[i];
                    elements[1].values[i] = objects[i + 5];
                }
                return elements;
            case StructureType.Button:
                elements = new FIGMAElement[3];
                elements[0].elementType = ElementType.Icon;
                elements[0].values = new object[5];
                elements[1].elementType = ElementType.Background; // 2A3C44, 0.1647059 0.2352941 0.2666667
                elements[1].values = new object[5];
                elements[2].elementType = ElementType.Action;
                elements[2].values = new object[5];
                for (byte i = 0; i < 5; i++)
                {
                    elements[0].values[i] = objects[i];
                    elements[1].values[i] = objects[i + 5];
                    elements[2].values[i] = objects[i + 10];
                }
                return elements;
            case StructureType.FlagName:
                elements = new FIGMAElement[3];
                elements[0].elementType = ElementType.Flag;
                elements[0].values = new object[5];
                elements[1].elementType = ElementType.TextValue;
                elements[1].values = new object[5];
                elements[2].elementType = ElementType.Subtitle;
                elements[2].values = new object[5];
                for (byte i = 0; i < 5; i++)
                {
                    elements[0].values[i] = objects[i];
                    elements[1].values[i] = objects[i + 5];
                    elements[2].values[i] = objects[i + 10];
                }
                return elements;
        }
        return null;
    }

    public void ResetBars()
    {
        if (menu == null) return;
        for (byte i = 0; i < menu.Length; i++) Destroy(menu[i]);
        menu = null;
    }

    public void AddBar(FIGMAFlags[] flags, int[] lengths = null)
    {
        ResetBars();
        menu = new GameObject[flags.Length];
        //const int width = 1920;
        float step = steps[flags.Length];
        int a = 0, b = -1920;
        if (lengths != null) b += lengths[0];
        for (byte i = 0; i < flags.Length; i++)
        {
            menu[i] = Instantiate(prefabs[0], transform, false);
            RectTransform rt = (RectTransform)menu[i].GetComponent(typeof(RectTransform));
            if (lengths == null)
            {
                rt.offsetMin = new Vector2(i * step, 0);
                rt.offsetMax = new Vector2((flags.Length - i - 1) * -step, 0);
            }
            else
            {
                rt.offsetMin = new Vector2(a, 0);
                rt.offsetMax = new Vector2(b, 0);
                a += lengths[i];
                if (i != lengths.Length - 1) b += lengths[i + 1];
            }
            FIGMAData data = (FIGMAData)menu[i].GetComponent(typeof(FIGMAData));
            flags[i].id = i;
            for (byte j = 1; j < data.data.Length; j++)
            {
                data.data[j].SetActive(false);
            }
            for (byte j = 0; j < flags[i].customElements.Length; j++)
            {
                if (flags[i].customElements[j].elementType == ElementType.Action)
                {
                    data.action = (System.Action<object>)((object[])flags[i].customElements[j].values[0])[0];
                    data.value = ((object[])flags[i].customElements[j].values[0])[1];
                    continue;
                }
                byte id = (byte)flags[i].customElements[j].elementType;
                data.data[id].SetActive(true);
                RectTransform rect = (RectTransform)data.data[id].GetComponent(typeof(RectTransform));
                rect.anchoredPosition = new Vector2(
                    flags[i].customElements[j].values[0] != null ? (float)flags[i].customElements[j].values[0] : rect.anchoredPosition.x,
                    flags[i].customElements[j].values[1] != null ? (float)flags[i].customElements[j].values[1] : rect.anchoredPosition.y);
                Text msg = (Text)data.data[id].GetComponent(typeof(Text));
                if (msg != null) msg.text = (string)flags[i].customElements[j].values[2];
                Image img = (Image)data.data[id].GetComponent(typeof(Image));
                if (img != null)
                {
                    Color c = new Color();
                    c.a = 1;
                    if (ColorUtility.TryParseHtmlString((string)flags[i].customElements[j].values[3], out c))
                        img.color = new Color(c.r, c.g, c.b, c.a);
                    img.sprite = (Sprite)flags[i].customElements[j].values[4];
                }
            }
        }
        _flags = flags;
    }
}

public struct FIGMAFlags
{
    //public bool enabled;
    public byte id;
    public FIGMAStructure.StructureType type;
    public FIGMAElement[] customElements;
    public float[] values;
}

public struct FIGMAElement
{
    //public string objectType;
    public FIGMAStructure.ElementType elementType;
    public object[] values;
}

public struct FIGMAParameter
{
    public readonly string type;
    public readonly object value;
    public readonly byte id;

    public FIGMAParameter(string type, object value, byte id)
    {
        this.type = type;
        this.value = value;
        this.id = id;
    }
}
