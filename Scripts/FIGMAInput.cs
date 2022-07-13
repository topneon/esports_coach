using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine;

public class FIGMAInput : MonoBehaviour
{
    private Action<string> callback;
    [SerializeField] InputField value;
    [SerializeField] Text text;
    private string prefix = string.Empty;

    public void SendString(string word)
    {
        if (callback == null) return;
        callback(prefix + word);
    }

    public void SetPrefix(string text) { prefix = text; }

    public void SetAction(Action<string> call) { callback = call; }

    public void SetMode(InputField.ContentType type) { value.contentType = type; }

    public string GetString() { return value.text; }

    public void SetString(string word) { value.SetTextWithoutNotify(word); }

    public void SetText(string word) { text.text = word; }
}
