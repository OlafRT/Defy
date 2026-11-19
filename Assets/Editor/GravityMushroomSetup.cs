using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RetroPlatformer;

public static class GravityMushroomSetup
{
    const string SpritePath = "Assets/Art/PowerUps/GravityMushroom.png";
    const string PrefabPath = "Assets/Prefabs/GravityMushroom.prefab";

    [MenuItem("Tools/Level Setup/Make Selected Block Gravity Mushroom")]
    static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var selection = Selection.activeGameObject;
        var block = selection ? selection.GetComponentInParent<Block>() : null;
        if (!block || !block.gameObject.scene.IsValid() || EditorUtility.IsPersistent(block))
        { Debug.LogWarning("Select the question block in the level scene first."); return; }
        var prefab = block.gravityMushroomPrefab;
        if (!prefab)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing)
            {
                prefab = existing.GetComponent<Walker>();
                if (!prefab || prefab.kind != Walker.Kind.GravityMushroom)
                { Debug.LogError("The existing GravityMushroom prefab is not configured as a gravity mushroom. It was left untouched."); return; }
            }
            else prefab = CreatePrefab(block);
        }
        if (!prefab || prefab.kind != Walker.Kind.GravityMushroom) { Debug.LogError("Assign a Gravity Mushroom Walker prefab."); return; }
        Undo.RecordObject(block, "Assign gravity mushroom");
        block.kind = Block.Kind.GravityMushroom;
        block.gravityMushroomPrefab = prefab;
        PrefabUtility.RecordPrefabInstancePropertyModifications(block);
        EditorSceneManager.MarkSceneDirty(block.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Gravity mushroom assigned to " + block.name + ". Save the scene.", block);
    }

    static Walker CreatePrefab(Block block)
    {
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (!importer) { Debug.LogError("Missing " + SpritePath); return null; }
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (!sprite) return null;
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var go = new GameObject("GravityMushroom");
        try
        {
            var body = go.AddComponent<Rigidbody2D>(); body.freezeRotation = true;
            var box = go.AddComponent<BoxCollider2D>(); box.size = new Vector2(.82f,.86f);
            var source = block.mushroomPrefab;
            if (source)
            {
                var sourceCollider = source.GetComponent<BoxCollider2D>();
                if (sourceCollider) box.sharedMaterial = sourceCollider.sharedMaterial;
                go.layer = source.gameObject.layer;
            }
            var walker = go.AddComponent<Walker>();
            walker.kind = Walker.Kind.GravityMushroom;
            walker.speed = 3.75f; walker.startDirection = -1; walker.waitForCamera = false;
            var art = new GameObject("Visual"); art.transform.SetParent(go.transform, false);
            var renderer = art.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 2;
            if (source && source.artwork) renderer.sharedMaterial = source.artwork.sharedMaterial;
            else if (block.artwork) renderer.sharedMaterial = block.artwork.sharedMaterial;
            walker.artwork = renderer;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            return prefab ? prefab.GetComponent<Walker>() : null;
        }
        finally { Object.DestroyImmediate(go); }
    }
}
