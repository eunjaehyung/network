//Taesang CompressTexture
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TextureData
{
    public string Name;
    public bool bAlpha;
    public string AlphaName;
}

[AddComponentMenu("NGUI/UI/CompressTexture")]
public class UICompressTextureList : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    List<TextureData> textureName = new List<TextureData>();

    public List<TextureData> textureList
    {
        get
        {
            return textureName;
        }
        set
        {
            textureName = value;
        }
    }

    public BetterList<string> GetTextureList()
    {
        BetterList<string> list = new BetterList<string>();

        for (int i = 0, imax = textureName.Count; i < imax; ++i)
        {
            TextureData td = textureName[i];
            if (td != null && !string.IsNullOrEmpty(td.Name)) list.Add(td.Name);
        }
        return list;
    }
}
//End Taesang