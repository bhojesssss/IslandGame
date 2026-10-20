using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Small helpers shared by the Pulau Kristal builder scripts.</summary>
static class BuilderUtil
{
    public const string Root = "Assets/_Game";

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // ---------- SerializedObject setters (for private [SerializeField] fields) ----------

    public static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError($"[Builder] Field '{field}' not found on {target.GetType().Name}");
            return;
        }
        Assign(prop, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Assign(SerializedProperty prop, object value)
    {
        switch (value)
        {
            case null: prop.objectReferenceValue = null; break;
            case Object o: prop.objectReferenceValue = o; break;
            case float f: prop.floatValue = f; break;
            case int i when prop.propertyType == SerializedPropertyType.Enum: prop.enumValueIndex = i; break;
            case int i: prop.intValue = i; break;
            case bool b: prop.boolValue = b; break;
            case string s: prop.stringValue = s; break;
            case Vector2 v: prop.vector2Value = v; break;
            case Vector2Int v: prop.vector2IntValue = v; break;
            case Vector3 v: prop.vector3Value = v; break;
            case Color c: prop.colorValue = c; break;
            case System.Enum e: prop.enumValueIndex = System.Convert.ToInt32(e); break;
            case LayerMask m: prop.intValue = m.value; break;
            case System.Collections.IList list:
                prop.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++) Assign(prop.GetArrayElementAtIndex(i), list[i]);
                break;
            default: Debug.LogError($"[Builder] Unsupported value type {value.GetType()} for {prop.name}"); break;
        }
    }

    // ---------- Textures & sprites ----------

    public static void ConfigurePixelTexture(string path, float ppu, SpriteImportMode mode, Vector2? pivot = null)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        if (pivot.HasValue)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot.Value;
        }
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    /// <summary>Writes a generated texture to disk as a single pixel-art sprite.</summary>
    public static Sprite SaveSprite(Texture2D texture, string path, float ppu, Vector2 pivot)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelTexture(path, ppu, SpriteImportMode.Single, pivot);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>Slices a texture into a grid of equal frames (left to right, then top to bottom).</summary>
    public static Sprite[] SliceGrid(string path, int frameWidth, int frameHeight, Vector2 pivot, string namePrefix)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int columns = texture.width / frameWidth;
        int rows = texture.height / frameHeight;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < columns; col++)
        {
            rects.Add(new SpriteRect
            {
                name = $"{namePrefix}_{rects.Count}",
                rect = new Rect(col * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = GUID.Generate()
            });
        }
        provider.SetSpriteRects(rects.ToArray());

        var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();

        return LoadSprites(path);
    }

    public static Sprite[] LoadSprites(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => -s.rect.y).ThenBy(s => s.rect.x).ToArray();

    /// <summary>Reads a PNG straight from disk (works even if the texture isn't marked readable).</summary>
    public static Texture2D ReadPng(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }

    // ---------- Tiles ----------

    public static Tile CreateTile(string path, Sprite sprite, Tile.ColliderType collider = Tile.ColliderType.None)
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.colliderType = collider;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // ---------- Scene objects ----------

    public static GameObject Child(GameObject parent, string name, Vector3 localPosition = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPosition;
        return go;
    }

    public static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int order, string sortingLayer = "Layer 1")
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        return sr;
    }

    public static GameObject InstantiatePrefab(GameObject prefab, Transform parent, Vector3 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        return go;
    }

    public static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) Debug.LogError($"[Builder] Prefab not found: {path}");
        return prefab;
    }
}
