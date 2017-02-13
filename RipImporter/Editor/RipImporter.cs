using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class RipImporter : EditorWindow
{
    [MenuItem("Window/Rip Model Importer")]
    static void OpenImporter()
    {
        RipImporter window = (RipImporter)EditorWindow.GetWindow(typeof(RipImporter));
        window.Show();
    }

    static string extension = ".rip";        // Our custom extension
    static string newExtension = ".asset";        // Extension of newly created asset - it MUST be ".asset", nothing else is allowed...

    public Object oldSelection;
    RipModel model;
    Vector2 scroll;

    public bool flipUV;
    public bool flipUV2;
    public float scale = 1;


    void OnSelectionChange()
    {
        //LoadModel();
        Repaint();
    }

    int[] GetAttributeArray(int size)
    {
        List<int> rlt = new List<int>();
        for (int i = 0; i<model.attributes.Count; i++) {
            if (model.attributes[i].size == size) {
                rlt.Add(i);
            }
        }
        return rlt.ToArray();
    }

    string[] GetAttributeNames(List<RipAttribute>attrs,int[] indices)
    {
        string[] rlt = new string[indices.Length + 1];
        rlt[0] = "Ignore";
        for(int i = 0; i<indices.Length;i++) {
            RipAttribute attr = attrs[indices[i]];
            rlt[i + 1] = attrs[indices[i]].semantic + ((attr.semanticIndex != 0)?(attr.semanticIndex + 1).ToString() :"");
        }
        return rlt;
    }

    int GetDefaultIndexValue(List<RipAttribute> attrs, string defaultName)
    {
        for(int i = 0; i< attrs.Count; i++) {
            RipAttribute attr = attrs[i];
            string attrName = attr.semantic + ((attr.semanticIndex != 0) ? (attr.semanticIndex + 1).ToString() : "");
            if (attrName == defaultName) {
                return i;
            }
        }
        return -1;
    }

    void LoadModel()
    {
        oldSelection = Selection.activeObject;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path.ToLower().EndsWith(".rip")) {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using (BinaryReader bin = new BinaryReader(fs)) {
                    model = ReadModel(bin);
                    model.textures = new Texture2D[model.textureFiles.Length];
                    for (int i = 0; i < model.textureFiles.Length; i++) {
                        string texPath = GetParentPath(path) + "/" + model.textureFiles[i];
                        model.textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                        if (model.textures[i] == null) {
                            model.textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath.Replace(".dds", ".tga"));
                        }
                    }
                }
            }
        }
        for (int i = 0; i < model.meshAttrNames.Length; i++) {
            model.meshAttrIndex[i] = GetDefaultIndexValue(model.attributes, model.meshAttrDefault[i]);
        }
    }

    void OnGUI()
    {
        if (Selection.activeObject != oldSelection) {
            LoadModel();
        }

        if (model) {
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < model.textures.Length; i++) {
                EditorGUILayout.ObjectField(model.textureFiles[i], model.textures[i], typeof(Texture2D), false);
            }
            for(int i = 0; i<model.meshAttrNames.Length; i++) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("get " + model.meshAttrNames[i] + " from ");

                int[] indexArray = GetAttributeArray(model.meshAttrSize[i]);//get valid indices
                string[] names = GetAttributeNames(model.attributes, indexArray);//get valid names

                int selected = 0;
                for(int x =0;x<indexArray.Length;x++) {
                    if(indexArray[x] == model.meshAttrIndex[i]) {
                        selected = x + 1;
                    }
                }
                selected = EditorGUILayout.Popup(selected, names, GUILayout.Width(150));
                model.meshAttrIndex[i] = selected != 0? indexArray[selected - 1]:-1;
                GUILayout.EndHorizontal();
            }
            //for (int i = 0; i < model.attributes.Count; i++) {
            //    model.attributes[i].bSelected = GUILayout.Toggle(model.attributes[i].bSelected, model.attributes[i].semantic);
            //}
            flipUV = GUILayout.Toggle(flipUV, "Flip UV");
            flipUV2 = GUILayout.Toggle(flipUV2, "Flip UV2");
            scale = EditorGUILayout.FloatField("Scale", scale);
            if (GUILayout.Button("Import")) {
                Mesh mesh = ImportMesh();
                SaveMesh(mesh);
            }
            GUILayout.EndScrollView();
        }
    }

    Vector3[] ToVector3Array(Vector4[] values,float scale = 1)
    {
        Vector3[] rlt = new Vector3[values.Length];
        for (int i = 0; i < values.Length; i++) {
            rlt[i] = values[i] * scale;
        }
        return rlt;
    }

    Vector2[] ToVector2Array(Vector4[] values,bool flipUV)
    {
        Vector2[] rlt = new Vector2[values.Length];
        for (int i = 0; i < values.Length; i++) {
            rlt[i] = new Vector2(values[i].x, flipUV ? 1 - values[i].y : values[i].y);
        }
        return rlt;
    }

    Color[] ToColorArray(Vector4[] values)
    {
        Color[] rlt = new Color[values.Length];
        for (int i = 0; i < values.Length; i++) {
            rlt[i] = values[i];
        }
        return rlt;
    }

    public Mesh ImportMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "RipMesh";
        int vertexNum = model.vertexesCnt;
        int uvIndex = 0;

        if(model.meshAttrIndex[0] != -1) {
            mesh.vertices = ToVector3Array(model.attributes[model.meshAttrIndex[0]].values,scale);
        }

        if(model.meshAttrIndex[1] != -1) {
            mesh.normals = ToVector3Array(model.attributes[model.meshAttrIndex[1]].values);
        }

        if(model.meshAttrIndex[2] != -1) {
            mesh.colors = ToColorArray(model.attributes[model.meshAttrIndex[2]].values);
        }

        if(model.meshAttrIndex[3] != -1) {
            mesh.uv = ToVector2Array(model.attributes[model.meshAttrIndex[3]].values,flipUV);
        }
        if (model.meshAttrIndex[4] != -1) {
            mesh.uv2 = ToVector2Array(model.attributes[model.meshAttrIndex[4]].values,flipUV2);
        }
        if (model.meshAttrIndex[5] != -1) {
            mesh.uv3 = ToVector2Array(model.attributes[model.meshAttrIndex[5]].values,false);
        }
        if (model.meshAttrIndex[6] != -1) {
            mesh.uv4 = ToVector2Array(model.attributes[model.meshAttrIndex[6]].values,false);
        }
        if(model.meshAttrIndex[7] != -1) {
            mesh.tangents = model.attributes[model.meshAttrIndex[7]].values;
        }
        
        mesh.triangles = model.faces;
        return mesh;
    }

    public void SaveMesh(Mesh mesh)
    {
        string path = ConvertToInternalPath(AssetDatabase.GetAssetPath(Selection.activeObject));
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static string ConvertToInternalPath(string asset)
    {
        string left = asset.Substring(0, asset.Length - extension.Length);
        return left + newExtension;
    }

    public static string GetParentPath(string asset)
    {
        int lastSep = asset.LastIndexOf("/");
        return asset.Substring(0, lastSep);
    }

    static string ReadString(BinaryReader bin)
    {
        List<char> chars = new List<char>();
        char c = bin.ReadChar();
        while (c != 0) {
            chars.Add(c);
            c = bin.ReadChar();
        }
        return new string(chars.ToArray());
    }

    static RipModel ReadModel(BinaryReader bin)
    {
        RipModel model = ScriptableObject.CreateInstance<RipModel>();
        model.signature = bin.ReadUInt32();
        model.version = bin.ReadUInt32();
        int facesCnt = bin.ReadInt32();
        int vertexesCnt = bin.ReadInt32();
        int vertexSize = bin.ReadInt32();
        int textureFilesCnt = bin.ReadInt32();
        int shaderFilesCnt = bin.ReadInt32();
        int vertexAttributesCnt = bin.ReadInt32();

        model.vertexesCnt = vertexesCnt;
        model.faces = new int[facesCnt * 3];
        model.textureFiles = new string[textureFilesCnt];
        model.shaderFiles = new string[shaderFilesCnt];
        model.attributes = new List<RipAttribute>();

        for (int i = 0; i < vertexAttributesCnt; i++) {
            RipAttribute attr = ScriptableObject.CreateInstance<RipAttribute>();
            attr.semantic = ReadString(bin);
            attr.semanticIndex = bin.ReadInt32();
            attr.offset = bin.ReadInt32();
            attr.size = bin.ReadInt32();
            int elementCnt = bin.ReadInt32();//3
            attr.vertexAttribTypesArray = new int[elementCnt];
            for (int e = 0; e < elementCnt; e++) {
                int typeElement = bin.ReadInt32();
                attr.vertexAttribTypesArray[e] = typeElement;
            }
            model.attributes.Add(attr);
        }
        for (int t = 0; t < model.textureFiles.Length; t++) {
            model.textureFiles[t] = ReadString(bin);
        }
        for (int s = 0; s < model.shaderFiles.Length; s++) {
            model.shaderFiles[s] = ReadString(bin);
        }
        for (int f = 0; f < facesCnt; f++) {
            model.faces[f * 3 + 0] = bin.ReadInt32();
            model.faces[f * 3 + 1] = bin.ReadInt32();
            model.faces[f * 3 + 2] = bin.ReadInt32();
        }
        for (int a = 0; a < vertexAttributesCnt; a++) {
            model.attributes[a].values = new Vector4[vertexesCnt];
        }
        for (int v = 0; v < vertexesCnt; v++) {
            for (int a = 0; a < vertexAttributesCnt; a++) {
                float[] values = new float[4];
                for (int i = 0; i < model.attributes[a].vertexAttribTypesArray.Length; i++) {
                    values[i] = bin.ReadSingle();
                }
                model.attributes[a].values[v] = new Vector4(values[0], values[1], values[2], values[3]);
            }
        }
        return model;
    }
}

public class OurImporter 
{
    static string extension = ".rip";        // Our custom extension
    static string newExtension = ".asset";        // Extension of newly created asset - it MUST be ".asset", nothing else is allowed...

    public static bool HasExtension(string asset)
    {
        return asset.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase);
    }

    public static string ConvertToInternalPath(string asset)
    {
        string left = asset.Substring(0, asset.Length - extension.Length);
        return left + newExtension;
    }

    public static string GetParentPath(string asset)
    {
        int lastSep = asset.LastIndexOf("/");
        return asset.Substring(0, lastSep);
    }

    static string ReadString(BinaryReader bin)
    {
        List<char> chars = new List<char>();
        char c = bin.ReadChar();
        while (c != 0) {
            chars.Add(c);
            c = bin.ReadChar();
        }
        return new string(chars.ToArray());
    }

    // This is called always when importing something
    static void OnPostprocessAllAssets
        (
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
    {
        foreach (string asset in importedAssets) {
            if (HasExtension(asset)) {
                using (FileStream fs = new FileStream(asset, FileMode.Open, FileAccess.Read)) {
                    using (BinaryReader bin = new BinaryReader(fs)) {
                        RipModel model = ReadModel(bin);
                        model.textures = new Texture2D[model.textureFiles.Length];
                        for(int i = 0; i<model.textureFiles.Length; i++) {
                            string texPath = GetParentPath(asset) + "/" + model.textureFiles[i];
                            model.textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                            if(model.textures[i] == null) {
                                model.textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath.Replace(".dds",".tga"));
                            }
                        }
                        string newPath = ConvertToInternalPath(asset);
                        //Object obj = AssetDatabase.LoadAssetAtPath<Object>(asset);
                        //AssetDatabase.AddObjectToAsset(model, obj);                        
                        AssetDatabase.CreateAsset(model, newPath);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
    }

    static RipModel ReadModel(BinaryReader bin)
    {
        RipModel model = ScriptableObject.CreateInstance<RipModel>();
        model.signature = bin.ReadUInt32();
        model.version = bin.ReadUInt32();
        int facesCnt = bin.ReadInt32();
        int vertexesCnt = bin.ReadInt32();
        int vertexSize = bin.ReadInt32();
        int textureFilesCnt = bin.ReadInt32();
        int shaderFilesCnt = bin.ReadInt32();
        int vertexAttributesCnt = bin.ReadInt32();

        model.vertexesCnt = vertexesCnt;
        model.faces = new int[facesCnt * 3];
        model.textureFiles = new string[textureFilesCnt];
        model.shaderFiles = new string[shaderFilesCnt];
        model.attributes = new List<RipAttribute>();// new RipAttribute[vertexAttributesCnt];//8

        for (int i = 0; i < vertexAttributesCnt; i++) {
            RipAttribute attr = ScriptableObject.CreateInstance<RipAttribute>();
            attr.semantic = ReadString(bin);
            attr.semanticIndex = bin.ReadInt32();
            attr.offset = bin.ReadInt32();
            attr.size = bin.ReadInt32();
            int elementCnt = bin.ReadInt32();//3
            attr.vertexAttribTypesArray = new int[elementCnt];
            for (int e = 0; e < elementCnt; e++) {
                int typeElement = bin.ReadInt32();
                attr.vertexAttribTypesArray[e] = typeElement;
            }
            model.attributes.Add(attr);
        }
        for (int t = 0; t < model.textureFiles.Length; t++) {
            model.textureFiles[t] = ReadString(bin);
        }
        for (int s = 0; s < model.shaderFiles.Length; s++) {
            model.shaderFiles[s] = ReadString(bin);
        }
        for (int f = 0; f < facesCnt; f++) {
            model.faces[f * 3 + 0] = bin.ReadInt32();
            model.faces[f * 3 + 1] = bin.ReadInt32();
            model.faces[f * 3 + 2] = bin.ReadInt32();
        }
        for (int a = 0; a < vertexAttributesCnt; a++) {
            model.attributes[a].values = new Vector4[vertexesCnt];
        }
        for (int v = 0; v < vertexesCnt; v++) {
            for (int a = 0; a < vertexAttributesCnt; a++) {
                float[] values = new float[4];
                for (int i = 0; i < model.attributes[a].vertexAttribTypesArray.Length; i++) {
                    values[i] = bin.ReadSingle();
                }
                model.attributes[a].values[v] = new Vector4(values[0], values[1], values[2], values[3]);
            }
        }
        return model;
    }
    
}