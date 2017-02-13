using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RipAttribute : ScriptableObject
{
    public bool bSelected = true;
    public string semantic;
    public int semanticIndex;
    public int offset;
    public int size;
    public int[] vertexAttribTypesArray;
    public Vector4[] values;
}
