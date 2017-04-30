//Taesang CompressTexture
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AlphaTextureInfo
{
    public string Name;
    public int Width;
    public int Height;
    public Texture2D AlphaTexture;
}

public class LastAlphaTextureInfo
{
    public string Name;
    public Color32[] Buffer;
    public int Width;
    public int Height;
}

public class TextureListInfo
{
    public string Name;
    public string AlphaName;
    public int Type;
}

public class UITextureCompressor : EditorWindow
{
    static public UITextureCompressor instance;

    Vector2 mScroll = Vector2.zero;
    List<string> mDelNames = new List<string>();

    List<AlphaTextureInfo> mAlphaTextureList = new List<AlphaTextureInfo>();
    LastAlphaTextureInfo mLastAlphaTextureInfo = null;

    void OnEnable() { instance = this; UpdateAlphaTextureList(); }
    void OnDisable() { instance = null; }

    UICompressTextureList mTextureList;
    UICompressTextureList mMainTextureList;
    int mMaxID = 0;


    const string strUIPath = "Assets/Resources/UI/";

    void OnSelectionChange() { 
        mDelNames.Clear(); 
        Repaint();

        mMaxID = -1;

        List<TextureData> textureDataList = NGUISettings.compressTextureList.textureList;
        string name = NGUISettings.compressTextureList.name;
        if (textureDataList != null)
        {
            for( int i = 0; i < textureDataList.Count; i++ )
            {
                TextureData d = textureDataList[i];
                if( d.AlphaName.StartsWith( name + "_" ) )
                {
                    char [] delimiterChars = { '_' };
                    string []text = d.AlphaName.Split( delimiterChars );

                    if( text[text.Length - 1] == "Alpha" && text.Length > 3 )
                    {
                        int num;
                        if(  System.Int32.TryParse( text[text.Length -2], out num ) )
                        {
                            if( num > mMaxID )
                                mMaxID = num;
                        }   
                    }
                }
            }
        }
    }

    void UpdateAlphaTextureList()
    {
        mLastAlphaTextureInfo = null;
        mAlphaTextureList.Clear();

        if (NGUISettings.compressTextureList == null)
            return;

        List<TextureData> textureDataList = NGUISettings.compressTextureList.textureList;

        for (int i = 0; i < textureDataList.Count; i++)
        {
            TextureData data = textureDataList[i];
            if (data.bAlpha && data.AlphaName != string.Empty)
            {
                bool bExist = false;
                foreach (AlphaTextureInfo info in mAlphaTextureList)
                {
                    if (info.Name == data.AlphaName)
                    {
                        bExist = true;
                        break;
                    }
                }
                if (!bExist)
                {
                    AlphaTextureInfo info = new AlphaTextureInfo();
                    info.Name = data.AlphaName;
                    string alphaTexturePath = strUIPath + data.AlphaName + ".png";
                    Texture2D alphaTex = AssetDatabase.LoadAssetAtPath(alphaTexturePath, typeof(Texture2D)) as Texture2D;
                    if (alphaTex == null)
                        continue;

                    info.AlphaTexture = alphaTex;
                    info.Width = alphaTex.width;
                    info.Height = alphaTex.height;
                    mAlphaTextureList.Add(info);
                }
            }
        }

    }

    void OnSelectTextureList(Object obj)
    {
        if (NGUISettings.compressTextureList != obj)
        {
            NGUISettings.compressTextureList = obj as UICompressTextureList;
            UpdateAlphaTextureList();
            Repaint();
        }
    }

    void OnGUI()
    {
        if (mMainTextureList == null)
        {
            if (NGUISettings.mainCompressTextureList == null)
            {
                string path = "Assets/Resources/CompressTextureList.prefab";
                Object prefab = PrefabUtility.CreateEmptyPrefab(path);

                GameObject go = new GameObject("CompressTextureList");
                go.AddComponent<UICompressTextureList>();

                PrefabUtility.ReplacePrefab(go, prefab);
                DestroyImmediate(go);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                NGUISettings.mainCompressTextureList = go.GetComponent<UICompressTextureList>();
            }
            mMainTextureList = NGUISettings.mainCompressTextureList;
        }

        bool delete = false;
        if (mTextureList != NGUISettings.compressTextureList)
            mTextureList = NGUISettings.compressTextureList;

        NGUIEditorTools.SetLabelWidth(80f);
        GUILayout.Space(3f);

        NGUIEditorTools.DrawHeader("Info");
        NGUIEditorTools.BeginContents();

        GUILayout.BeginHorizontal();
        {
            ComponentSelector.Draw<UICompressTextureList>("TextureList", NGUISettings.compressTextureList, OnSelectTextureList, true, GUILayout.MinWidth(100f));

            if (GUILayout.Button("New", GUILayout.Width(40f)))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Compressed Texture List Path", "TextureList.prefab", "prefab", "Save compress texture list as...", NGUISettings.currentPath);
                if (!string.IsNullOrEmpty(path))
                {
                    string listName = path.Replace(".prefab", "");
                    listName = listName.Substring(path.LastIndexOfAny(new char[] { '/', '\\' }) + 1);

                    Object prefab = PrefabUtility.CreateEmptyPrefab(path);

                    GameObject go = new GameObject(listName);
                    go.AddComponent<UICompressTextureList>();

                    PrefabUtility.ReplacePrefab(go, prefab);
                    DestroyImmediate(go);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                    NGUISettings.compressTextureList = go.GetComponent<UICompressTextureList>();
                    mTextureList = NGUISettings.compressTextureList;
                    Selection.activeGameObject = go;
                }
            }
        }
        GUILayout.EndHorizontal();

        List<Texture> textures = GetSelectedTextures();
        List<TextureListInfo> textureList = GetTextureList(textures);

        if (NGUISettings.compressTextureList != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Num", GUILayout.Width(100f));
            GUILayout.Label(NGUISettings.compressTextureList.textureList.Count.ToString(), GUILayout.MinWidth(70f));
            GUILayout.EndHorizontal();

            if (NGUISettings.compressTextureList == NGUISettings.mainCompressTextureList)
            {
                NGUIEditorTools.EndContents();
                GUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                if (GUILayout.Button("UpdateList"))
                {
                    UpdateMainCompressTextureList();
                }
                GUILayout.Space(20f);
                GUILayout.EndHorizontal();


                NGUIEditorTools.DrawHeader("Textures", true);
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(3f);
                    GUILayout.BeginVertical();

                    mScroll = GUILayout.BeginScrollView(mScroll);
                    int index = 0;
                    foreach (TextureListInfo info in textureList)
                    //foreach (KeyValuePair<string, int> iter in textureList)
                    {
                        ++index;

                        GUILayout.Space(-1f);
                        GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                        GUILayout.Label(index.ToString(), GUILayout.Width(24f));

                        if (GUILayout.Button(info.Name, "OL TextField", GUILayout.Height(20f)))
                        {
                            foreach (TextureData data in NGUISettings.compressTextureList.textureList)
                            {
                                if (data.Name == info.Name)
                                {
                                    string textureName = strUIPath + data.Name;
                                    Texture t = AssetDatabase.LoadAssetAtPath(textureName, typeof(Texture)) as Texture;
                                    Selection.activeObject = t;
                                    break;
                                }
                            }
                        }

                        GUILayout.Label(info.AlphaName, GUILayout.Width(200f));
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    GUILayout.Space(3f);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Delete Num", GUILayout.Width(100f));
                GUILayout.Label(mDelNames.Count.ToString(), GUILayout.MinWidth(70f));

                EditorGUI.BeginDisabledGroup(mDelNames.Count == 0);
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(100f)))
                    delete = true;
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();

                int updateNum = 0;
                int addNum = 0;
                foreach( TextureListInfo info in textureList )
                {
                    if (info.Type == 1)
                        updateNum++;
                    else if (info.Type == 2)
                        addNum++;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Update Num", GUILayout.Width(100f));
                GUILayout.Label(updateNum.ToString(), GUILayout.MinWidth(70f));
                EditorGUI.BeginDisabledGroup(updateNum == 0);
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("DeleteUpdate", GUILayout.Width(100f)))
                {
                    EditorUtility.DisplayProgressBar("Delete", " Deleta alpha texture, Please Wait...", 0);
                    List<TextureData> compressTextureList = NGUISettings.compressTextureList.textureList;
                    int num = 0;
                    float delNum = updateNum;
                    for (int i = compressTextureList.Count; i > 0; )
                    {
                        TextureData data = compressTextureList[--i];

                        foreach( TextureListInfo info in textureList )
                        {
                            if( info.Name == data.Name )
                            {
                                num++;
                                EditorUtility.DisplayProgressBar("Delete", " Delete alpha texture, Please Wait...", num / delNum);
                                RevertTexture(data);
                                compressTextureList.RemoveAt(i);
                            }
                        }

                        //if (textureList[data.Name] == 1)
                        //{
                        //    num++;
                        //    EditorUtility.DisplayProgressBar("Delete", " Delete alpha texture, Please Wait...", num / delNum);
                        //    RevertTexture(data);
                        //    compressTextureList.RemoveAt(i);
                        //}
                    }
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    mDelNames.Clear();
                    NGUISettings.compressTextureList.textureList = compressTextureList;
                    NGUITools.SetDirty(NGUISettings.compressTextureList.gameObject);
                    AssetDatabase.SaveAssets();
                    EditorUtility.ClearProgressBar();
                }
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Add Num", GUILayout.Width(100f));
                GUILayout.Label(addNum.ToString(), GUILayout.MinWidth(70f));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                NGUISettings.atlasTrimming = EditorGUILayout.Toggle("Trim Alpha", NGUISettings.atlasTrimming, GUILayout.Width(100f));
                GUILayout.Label("Remove empty space");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.Label("AlphaNum", GUILayout.Width(100f));
                GUILayout.Label(mAlphaTextureList.Count.ToString(), GUILayout.MinWidth(70f));
                GUILayout.Label("Range", GUILayout.Width(50f));
                NGUISettings.errorRange = GUILayout.TextField(NGUISettings.errorRange, GUILayout.Width(30f));

                if( GUILayout.Button("Rescan", GUILayout.Width(100f) ) )
                {
                    int iRange;
                    if( int.TryParse( NGUISettings.errorRange, out iRange ) )
                    {
                        
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Range Error", "Must Number", "OK");
                        NGUISettings.errorRange = "0";
                        return;                        
                    }
                    BetterList<string> textureName = NGUISettings.compressTextureList.GetTextureList();

                    mLastAlphaTextureInfo = null;
                    mAlphaTextureList.Clear();

                    mMaxID = -1;
                    int num = 0;
                    float total = textureName.size;
                    foreach (string name in textureName)
                    {
                        EditorUtility.DisplayProgressBar("Update", "Phase 1 of 4, Please Wait...", num++ / total);
                        MakeTextureReadable(name);
                        MakeTextureReadable(name + "_Alpha");
                    }

                    num = 0;
                    foreach (string name in textureName)
                    {
                        EditorUtility.DisplayProgressBar("Update", "Phase 2 of 4, Please Wait...", num++ / total);
                        AddCommonAlphaTexture(name, true);
                    }

                    num = 0;
                    foreach (string name in textureName)
                    {
                        EditorUtility.DisplayProgressBar("Update", "Phase 3 of 4, Please Wait...", num++ / total);
                        string texture = strUIPath + name + ".png";
                        TextureImporter importerMain = AssetImporter.GetAtPath(texture) as TextureImporter;
                        if (importerMain == null)
                            continue;
                        importerMain.textureType = TextureImporterType.Default;
                        importerMain.mipmapEnabled = false;
                        importerMain.isReadable = false;
                        //importerMain.SetPlatformTextureSettings("Android", 2048, TextureImporterFormat.ETC_RGB4, 100, false);
                        //importerMain.SetPlatformTextureSettings("iPhone", 2048, TextureImporterFormat.PVRTC_RGB4, 100, false);

                        importerMain.SetPlatformTextureSettings("Android", 2048, TextureImporterFormat.RGBA32, 100, false);
                        importerMain.SetPlatformTextureSettings("iPhone", 2048, TextureImporterFormat.RGBA32, 100, false);
                        AssetDatabase.ImportAsset(texture);
                    }

                    num = 0;
                    total = mAlphaTextureList.Count;
                    foreach( AlphaTextureInfo info in mAlphaTextureList )
                    {
                        EditorUtility.DisplayProgressBar("Update", "Phase 4 of 4, Please Wait...", num++ / total);
                        UpdateTextureAsset(info.Name);
                    }
                    NGUITools.SetDirty(NGUISettings.compressTextureList.gameObject);
                    AssetDatabase.SaveAssets();
                    EditorUtility.ClearProgressBar();
                }

                GUILayout.EndHorizontal();

                NGUIEditorTools.EndContents();

                if (textures.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20f);

                    if (GUILayout.Button("Add/Update"))
                    {
                        UpdateCompressTextureList(textureList, textures);
                    }

                    GUILayout.Space(20f);
                    GUILayout.EndHorizontal();
                }

                NGUIEditorTools.DrawHeader("Textures", true);
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(3f);
                    GUILayout.BeginVertical();

                    mScroll = GUILayout.BeginScrollView(mScroll);
                    int index = 0;
                    foreach( TextureListInfo info in textureList )
                    //foreach (KeyValuePair<string, int> iter in textureList)
                    {
                        ++index;

                        GUILayout.Space(-1f);
                        GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                        GUILayout.Label(index.ToString(), GUILayout.Width(24f));

                        if (GUILayout.Button(info.Name, "OL TextField", GUILayout.Height(20f)))
                        {
                            foreach (TextureData data in NGUISettings.compressTextureList.textureList)
                            {
                                if (data.Name == info.Name)
                                {
                                    string textureName = strUIPath + data.Name;
                                    Texture t = AssetDatabase.LoadAssetAtPath(textureName, typeof(Texture)) as Texture;
                                    Selection.activeObject = t;
                                    break;
                                }
                            }
                        }

                        GUILayout.Label( info.AlphaName, GUILayout.Width(200f));

                        if (info.Type == 2)
                        {
                            GUI.color = Color.green;
                            GUILayout.Label("Add", GUILayout.Width(27f));
                            GUI.color = Color.white;
                        }
                        else if (info.Type == 1)
                        {
                            GUI.color = Color.cyan;
                            GUILayout.Label("Update", GUILayout.Width(45f));
                            GUI.color = Color.white;
                        }
                        else
                        {
                            if (mDelNames.Contains(info.Name))
                            {
                                GUI.backgroundColor = Color.red;

                                if (GUILayout.Button("Delete", GUILayout.Width(60f)))
                                {
                                    delete = true;
                                }
                                GUI.backgroundColor = Color.green;
                                if (GUILayout.Button("X", GUILayout.Width(22f)))
                                {
                                    mDelNames.Remove(info.Name);
                                    delete = false;
                                }
                                GUI.backgroundColor = Color.white;
                            }
                            else
                            {
                                if (GUILayout.Button("X", GUILayout.Width(22f)))
                                    mDelNames.Add(info.Name);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    GUILayout.Space(3f);
                    GUILayout.EndHorizontal();

                    if (delete)
                    {
                        EditorUtility.DisplayProgressBar("Delete", " Deleta alpha texture, Please Wait...", 0);
                        List<TextureData> compressTextureList = NGUISettings.compressTextureList.textureList;
                        int num = 0;
                        float delNum = mDelNames.Count;
                        for (int i = compressTextureList.Count; i > 0; )
                        {
                            TextureData data = compressTextureList[--i];
                            if (mDelNames.Contains(data.Name))
                            {
                                num++;
                                EditorUtility.DisplayProgressBar("Delete", " Delete alpha texture, Please Wait...", num / delNum);
                                RevertTexture(data);
                                compressTextureList.RemoveAt(i);
                            }
                        }
                        mDelNames.Clear();
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        NGUISettings.compressTextureList.textureList = compressTextureList;
                        NGUITools.SetDirty(NGUISettings.compressTextureList.gameObject);
                        AssetDatabase.SaveAssets();
                        EditorUtility.ClearProgressBar();
                    }
                }
            }
        }

    }

    List<Texture> GetSelectedTextures()
    {
        List<Texture> textures = new List<Texture>();

        if (Selection.objects != null && Selection.objects.Length > 0)
        {
            Object[] objects = EditorUtility.CollectDependencies(Selection.objects);

            foreach (Object o in objects)
            {
                Texture tex = o as Texture;
                if (tex == null || tex.name == "Font Texture") continue;

                if (tex.name.EndsWith("_Alpha")) continue;

                string path = GetUIPathbyTexture(tex);
                if (path == string.Empty)
                    continue;

                textures.Add(tex);
            }
        }
        return textures;
    }

    /// <summary>
    /// Helper function that creates a single sprite list from both the atlas's sprites as well as selected textures.
    /// Dictionary value meaning:
    /// 0 = No change
    /// 1 = Update
    /// 2 = Add
    /// </summary>

    List<TextureListInfo> GetTextureList( List<Texture> textures )
    {
        List<TextureListInfo> textureList = new List<TextureListInfo>();

        List<string> texNames = new List<string>();
        foreach (Texture tex in textures)
            texNames.Add(GetUIPathbyTexture(tex));
        texNames.Sort();

        if (NGUISettings.compressTextureList != null)
        {
            List<TextureData> compList =  NGUISettings.compressTextureList.textureList;

            foreach( TextureData d in compList )
            {
                TextureListInfo info = new TextureListInfo();
                info.Name = d.Name;
                info.AlphaName = d.AlphaName;
                info.Type = 0;
                for( int i = 0; i < texNames.Count; i++ )
                {
                    string name = texNames[i];
                    if( name == info.Name )
                    {
                        info.Type = 1;
                        texNames.RemoveAt(i);
                        break;
                    }
                }
                textureList.Add( info );
            }
        }

        // If we have textures to work with, include them as well
        if (textures.Count > 0)
        {
            foreach (string tex in texNames)
            {
                TextureListInfo info = new TextureListInfo();
                info.Name = tex;
                info.AlphaName = "";
                info.Type = 2;
                textureList.Add( info );
            }
        }
        return textureList;
    }

    void UpdateCompressTextureList( List<TextureListInfo> list, List<Texture> textures)
    {
        int iRange;
        if (int.TryParse(NGUISettings.errorRange, out iRange))
        {

        }
        else
        {
            EditorUtility.DisplayDialog("Range Error", "Must Number", "OK");
            NGUISettings.errorRange = "0";
            return;
        }

        EditorUtility.DisplayProgressBar("Update", " Update alpha texture, Please Wait...", 0);

        float totalNum = 0;
        foreach ( TextureListInfo info in list)
        {
            if ( info.Type != 0)
                totalNum++;
        }

        List<TextureData> textureList = NGUISettings.compressTextureList.textureList;

        Dictionary<string, TextureData> texturListeDic = new Dictionary<string, TextureData>();
        foreach (TextureData data in textureList)
        {
            texturListeDic[data.Name] = data;
        }

        Dictionary<string, Texture> TextureDic = new Dictionary<string, Texture>();
        foreach (Texture tex in textures)
        {
            string path = GetUIPathbyTexture(tex);
            TextureDic[path] = tex;
        }

        int num = 0;
        List<TextureData> UpdateList = new List<TextureData>();
        foreach ( TextureListInfo info in list)
        {
            if (info.Type == 0)
                continue;

            Texture tex = TextureDic[info.Name];
            if (tex != null)
            {
                num++;
                if (info.Type == 1)
                {
                    TextureData data = texturListeDic[info.Name];
                    UpdateList.Add(data);
                }
                else if (info.Type == 2)
                {
                    TextureData data = new TextureData();
                    data.Name = info.Name;
                    string path = GetUIPathbyTexture(tex);
                    if (path == string.Empty)
                    {
                        EditorUtility.DisplayDialog("Error", "UI Texture must locate in Assets/Resources/UI", "ok");
                        continue;
                    }
                    data.Name = path;
                    textureList.Add(data);
                    UpdateList.Add(data);
                }
            }
        }

        for (int i = 0; i < totalNum; i++)
        {
            EditorUtility.DisplayProgressBar("Update", "Phase 1 of 5, Please Wait...", i / totalNum);
            TextureData d = UpdateList[i];
            MakeTextureReadable(d.Name);
            MakeTextureReadable(d.AlphaName);
        }

        float total = mAlphaTextureList.Count;
        for (int i = 0; i < mAlphaTextureList.Count; i++)
        {
            EditorUtility.DisplayProgressBar("Update", "Phase 2 of 5, Please Wait...", i / total);
            AlphaTextureInfo info = mAlphaTextureList[i];
            MakeTextureReadable(info.Name);
        }


        for (int i = 0; i < totalNum; i++)
        {
            EditorUtility.DisplayProgressBar("Update", "Phase 3 of 5, Please Wait...", i / totalNum);
            TextureData d = UpdateList[i];
            AddCommonAlphaTexture(d.Name, true);
            //MakeAlphaTexture(d);
        }

        //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        for (int i = 0; i < totalNum; i++)
        {
            EditorUtility.DisplayProgressBar("Update", "Phase 4 of 5, Please Wait...", i / totalNum);
            TextureData d = UpdateList[i];
            UpdateTextureAsset(d.Name);
        }

        for (int i = 0; i < mAlphaTextureList.Count; i++ )
        {
            EditorUtility.DisplayProgressBar("Update", "Phase 5 of 5, Please Wait...", i /total);
            AlphaTextureInfo info = mAlphaTextureList[i];
            UpdateTextureAsset(info.Name);
        }
            
        //EditorUtility.DisplayProgressBar("Update", "Phase 4 of 4, Please Wait...", 0);

        UpdateList.Clear();

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        NGUISettings.compressTextureList.textureList = textureList;
        NGUITools.SetDirty(NGUISettings.compressTextureList.gameObject);
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
        Selection.activeObject = null;
    }

    void RevertTexture(TextureData data)
    {
        string textureName = strUIPath + data.Name + ".png";
        string alphaName = strUIPath + data.Name + "_Alpha.png";

        DeleteAlphaTexture(data);
        data.AlphaName = "";

        TextureImporter importerMain = AssetImporter.GetAtPath(textureName) as TextureImporter;
        importerMain.textureType = TextureImporterType.Default;
        importerMain.mipmapEnabled = false;
        importerMain.ClearPlatformTextureSettings("Android");
        importerMain.ClearPlatformTextureSettings("iPhone");
        AssetDatabase.ImportAsset(textureName);
    }

    void MakeTextureReadable(string name)
    {
        string textureName = strUIPath + name + ".png";

        TextureImporter ti = AssetImporter.GetAtPath(textureName) as TextureImporter;
        if (ti == null)
        {
            return;
        }

        ti.isReadable = true;
        ti.textureFormat = TextureImporterFormat.ARGB32;
        ti.ClearPlatformTextureSettings("Android");
        ti.ClearPlatformTextureSettings("iPhone");
        AssetDatabase.ImportAsset(textureName);
    }

    void UpdateTextureAsset(string name )
    {
        string textureName = strUIPath + name + ".png";
        TextureImporter importerMain = AssetImporter.GetAtPath(textureName) as TextureImporter;
        if (importerMain == null)
            return;
        importerMain.textureType = TextureImporterType.Default;
        importerMain.mipmapEnabled = false;
        importerMain.isReadable = false;
        importerMain.SetPlatformTextureSettings("Android", 2048, TextureImporterFormat.ETC_RGB4, 100, false);
        importerMain.SetPlatformTextureSettings("iPhone", 2048, TextureImporterFormat.PVRTC_RGB4, 100, false);
        AssetDatabase.ImportAsset(textureName);
    }

    string GetUIPathbyTexture(Texture tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);

        if (path.StartsWith(strUIPath) == false)
            return string.Empty;
        path = path.Substring(strUIPath.Length);
        int dot = path.LastIndexOf('.');
        path = path.Substring(0, dot);
        return path;
    }

    void UpdateMainCompressTextureList()
    {
        NGUISettings.mainCompressTextureList.textureList.Clear();

        System.Type listType = typeof(UICompressTextureList);
        string[] paths = AssetDatabase.FindAssets("t:Prefab");
        BetterList<Object> list = new BetterList<Object>();

        for (int i = 0; i < paths.Length; ++i)
        {
            string path = paths[i];
            string prefabName = AssetDatabase.GUIDToAssetPath(path);
            Object obj = AssetDatabase.LoadMainAssetAtPath(prefabName);

            if (obj == null || list.Contains(obj)) continue;
            if (obj.name == "CompressTextureList") continue;

            Object t = (obj as GameObject).GetComponent(listType);
            if (t != null && !list.Contains(t))
            {
                list.Add(t);
            }
        }
        list.Sort(delegate(Object a, Object b) { return a.name.CompareTo(b.name); });

        foreach (UICompressTextureList textureList in list)
        {
            foreach (TextureData data in textureList.textureList)
            {
                TextureData d = new TextureData();
                d.Name = data.Name;
                d.bAlpha = data.bAlpha;
                d.AlphaName = data.AlphaName;
                NGUISettings.mainCompressTextureList.textureList.Add(d);
            }
        }

        NGUITools.SetDirty(NGUISettings.mainCompressTextureList.gameObject);
        AssetDatabase.SaveAssets();
    }

    void DeleteAlphaTexture( TextureData data )
    {
        if ((data.AlphaName != null && data.AlphaName != "" && data.AlphaName != string.Empty))
        {
            if (data.AlphaName != mLastAlphaTextureInfo.Name)
            {
                bool bExist = false;
                foreach (TextureData d in NGUISettings.compressTextureList.textureList)
                {
                    if (d.Name == data.Name)
                        continue;

                    if (d.AlphaName == data.AlphaName)
                    {
                        bExist = true;
                        break;
                    }
                }
                if (!bExist)
                {
                    string alphaTexPath = strUIPath + data.AlphaName;
                    Texture2D alphaTexture = AssetDatabase.LoadAssetAtPath(alphaTexPath, typeof(Texture2D)) as Texture2D;
                    if (alphaTexture != null)
                    {
                        string path = AssetDatabase.GetAssetPath(alphaTexture);
                        AssetDatabase.DeleteAsset(path);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    }
                }
            }
        }
        else
        {
            if (mLastAlphaTextureInfo.Name != data.Name + "_Alpha")
            {
                string alphaTexPath = strUIPath + data.Name + "_Alpha.png";
                Texture2D alphaTexture = AssetDatabase.LoadAssetAtPath(alphaTexPath, typeof(Texture2D)) as Texture2D;
                if (alphaTexture != null)
                {
                    string path = AssetDatabase.GetAssetPath(alphaTexture);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }
    }

    void AddCommonAlphaTexture( string name, bool bDeleteAlphaTexture )
    {
        string textureName = strUIPath + name + ".png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath(textureName, typeof(Texture2D)) as Texture2D;

        if (tex == null)
            return;

        int width = tex.width;
        int height = tex.height;

        bool hasAlpha = false;
        Color32[] pixels = tex.GetPixels32();
        Color32[] alphaBuf = new Color32[width * height];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int index = y * width + x;
                alphaBuf[index].r = pixels[index].a;
                alphaBuf[index].g = pixels[index].a;
                alphaBuf[index].b = pixels[index].a;
                alphaBuf[index].a = pixels[index].a;
                if (pixels[index].a != 255)
                    hasAlpha = true;
            }
        }
        if (!hasAlpha)
            return;

        bool SameAlpha = true;

        int errorRange = int.Parse( NGUISettings.errorRange );

        if (mLastAlphaTextureInfo!= null)
        {
            if (mLastAlphaTextureInfo.Width == width && mLastAlphaTextureInfo.Height == height)
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        int index = y * width + x;
                        int odd = (int)alphaBuf[index].r - (int)mLastAlphaTextureInfo.Buffer[index].r;
                        if( odd < -errorRange || odd > errorRange )
                        {
                            SameAlpha = false;
                            break;
                        }
                    }
                    if (!SameAlpha)
                        break;
                }
            }
            else
                SameAlpha = false;

            if (SameAlpha)
            {
                List<TextureData> textureList = NGUISettings.compressTextureList.textureList;

                for (int i = 0; i < textureList.Count; i++ )
                {
                    TextureData data = textureList[i];
                    if (data.Name == name)
                    {
                        if( bDeleteAlphaTexture )
                            DeleteAlphaTexture(data);
                        data.AlphaName = mLastAlphaTextureInfo.Name;
                        data.bAlpha = true;
                        textureList[i] = data;
                        return;
                    }
                }
            }
        }

        foreach (AlphaTextureInfo texture in mAlphaTextureList )
        {
            if (mLastAlphaTextureInfo != null && texture.Name == mLastAlphaTextureInfo.Name)
                continue;

            if (texture.Width != width || texture.Height != height)
                continue;

            string texturePath = strUIPath + texture.Name + ".png";
            Texture2D alphaTex = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
            if (alphaTex == null)
                continue;
            Color32[] textureAlpha = alphaTex.GetPixels32();

            bool bSame = true;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int index = y * width + x;
                    int odd = (int)alphaBuf[index].r - (int)textureAlpha[index].r;
                    if (odd < -errorRange || odd > errorRange)
                    {
                        bSame = false;
                        break;
                    }
                }
                if (!bSame)
                    break;
            }

            if (bSame)
            {
                alphaTex = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
                texture.AlphaTexture = alphaTex;

                if (mLastAlphaTextureInfo == null)
                    mLastAlphaTextureInfo = new LastAlphaTextureInfo();
                mLastAlphaTextureInfo.Name = texture.Name;
                mLastAlphaTextureInfo.Width = width;
                mLastAlphaTextureInfo.Height = height;
                mLastAlphaTextureInfo.Buffer = textureAlpha;

                List<TextureData> textureList = NGUISettings.compressTextureList.textureList;

                for (int i = 0; i < textureList.Count; i++ )
                {
                    TextureData data = textureList[i];

                    if (data.Name == name)
                    {
                        if (bDeleteAlphaTexture)
                            DeleteAlphaTexture(data);

                        data.AlphaName = texture.Name;
                        data.bAlpha = true;
                        textureList[i] = data;
                        return;
                    }
                }
                break;
            }
        }

        {
            mMaxID++;
            string alphaName = strUIPath + NGUISettings.compressTextureList.name + "/" + NGUISettings.compressTextureList.name + "_" + mMaxID.ToString() + "_Alpha.png";

            if (mLastAlphaTextureInfo == null)
                mLastAlphaTextureInfo = new LastAlphaTextureInfo();
            mLastAlphaTextureInfo.Name = NGUISettings.compressTextureList.name + "/" + NGUISettings.compressTextureList.name + "_" + mMaxID.ToString() + "_Alpha";
            mLastAlphaTextureInfo.Width = width;
            mLastAlphaTextureInfo.Height = height;
            mLastAlphaTextureInfo.Buffer = alphaBuf;

            Texture2D alphaTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            alphaTex.SetPixels32(alphaBuf);
            alphaTex.Apply();

            byte[] bytes = alphaTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(alphaName, bytes);
            bytes = null;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importerMain = AssetImporter.GetAtPath(textureName) as TextureImporter;
            TextureWrapMode wrap = importerMain.wrapMode;

            TextureImporter importer = AssetImporter.GetAtPath(alphaName) as TextureImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.textureFormat = TextureImporterFormat.ARGB32;
            importer.ClearPlatformTextureSettings("Android");
            importer.ClearPlatformTextureSettings("iPhone");
            importer.wrapMode = wrap;

            AssetDatabase.ImportAsset(alphaName);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Texture2D alphatex = AssetDatabase.LoadAssetAtPath(alphaName, typeof(Texture2D)) as Texture2D;

            AlphaTextureInfo info = new AlphaTextureInfo();

            info.Name = NGUISettings.compressTextureList.name + "/" + NGUISettings.compressTextureList.name + "_" + mMaxID.ToString() + "_Alpha";
            info.Width = width;
            info.Height = height;
            info.AlphaTexture = alphatex;
            mAlphaTextureList.Add(info);

            List<TextureData> textureList = NGUISettings.compressTextureList.textureList;

            for (int i = 0; i < textureList.Count; i++)
            {
                TextureData data = textureList[i];
                if (data.Name == name)
                {
                    if (bDeleteAlphaTexture)
                        DeleteAlphaTexture(data);
                    data.AlphaName = mLastAlphaTextureInfo.Name;
                    data.bAlpha = true;
                    textureList[i] = data;
                    return;
                }
            }
        }
    }
}
//End Taesang
