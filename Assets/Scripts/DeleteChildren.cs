using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeleteChildren : MonoBehaviour
{
    private void OnDisable()
    {
        Image[] children = GetComponentsInChildren<Image>(true);
        for (short i = 0; i < children.Length; i++) Destroy(children[i].gameObject);
    }
}
