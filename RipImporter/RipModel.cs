using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RipModel : ScriptableObject
{
    public uint signature;
    public uint version;
    public int vertexesCnt;
    public int[] faces;
    public string[] textureFiles;
    public string[] shaderFiles;
    public Texture2D[] textures;
    public List<RipAttribute> attributes;

    public string[] meshAttrNames = { "position", "normal", "color", "uv", "uv2", "uv3", "uv4", "tangent" };
    public int[] meshAttrSize = { 12, 12, 16, 8, 8, 8, 8, 12 };
    public string[] meshAttrDefault = { "POSITION", "NORMAL", "COLOR", "TEXCOORD", "TEXCOORD2", "TEXCOORD3", "TEXCOORD4", "TANGENT" };
    public int[] meshAttrIndex = { 0, 0, 0, 0, 0, 0, 0, 0 };
}
