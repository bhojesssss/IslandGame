using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Step 1 & 2 of the builder: project settings, generated sprites, tiles, character animations and prefabs.
/// </summary>
static class AssetsBuilder
{
    const string Cainos = "Assets/Cainos/Pixel Art Top Down - Basic";
    const string Generated = BuilderUtil.Root + "/Art/Generated";
    const string Tiles = BuilderUtil.Root + "/Tiles";
    const string Animations = BuilderUtil.Root + "/Animations";
    const string Prefabs = BuilderUtil.Root + "/Prefabs";
    public const float CharacterPPU = 16f;

    // ===================================================================== Project settings

    public static void SetupProject()
    {
        // Sorting layers / physics layers used by the Cainos tileset prefabs.
        var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var preset = AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>(
            $"{Cainos}/Preset/Tags & Layers - Pixel Art Top Down - Basic.preset");
        if (preset != null && SortingLayer.NameToID("Layer 1") == 0 && preset.CanBeAppliedTo(tagManager))
            preset.ApplyTo(tagManager);

        // Extra physics layer for water (arrows fly over it).
        var so = new SerializedObject(tagManager);
        var layers = so.FindProperty("layers");
        bool hasWater = false;
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == "Water") hasWater = true;
        if (!hasWater)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                layers.GetArrayElementAtIndex(i).stringValue = "Water";
                break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Top-down Y sorting: objects lower on screen are drawn in front.
        var renderer2D = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Renderer2D.asset");
        if (renderer2D != null)
        {
            var rso = new SerializedObject(renderer2D);
            var mode = rso.FindProperty("m_TransparencySortMode");
            var axis = rso.FindProperty("m_TransparencySortAxis");
            if (mode != null) mode.enumValueIndex = (int)TransparencySortMode.CustomAxis;
            if (axis != null) axis.vector3Value = new Vector3(0f, 1f, 0f);
            if (mode == null || axis == null) Debug.LogWarning("[Builder] Could not set the 2D renderer sort axis.");
            rso.ApplyModifiedPropertiesWithoutUndo();
        }
        UnityEngine.Rendering.GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        UnityEngine.Rendering.GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);

        // TextMeshPro essential resources (fonts + settings), imported without any dialog.
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            var importer = System.Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro")
                           ?? System.Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor");
            var method = importer?.GetMethod("ImportResources", System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                object instance = method.IsStatic ? null : System.Activator.CreateInstance(importer);
                method.Invoke(instance, new object[] { true, false, false });
            }
            else Debug.LogError("[Builder] Could not find TMP_PackageResourceImporter.ImportResources");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Builder] Project setup done.");
    }

    // ===================================================================== Generated sprites & tiles

    public class Art
    {
        public Sprite White, Coin, Crystal, HealthPotion, ManaPotion, DigMark, Exclamation, Sparkle,
            Scroll, Shadow, Glow, Portal, Sand, Arrow, MapDot;
        public Sprite[] Water;
        public Tile[] WaterTiles;
        public Tile SandTile, BlockerTile;
    }

    public static Art BuildArt()
    {
        BuilderUtil.EnsureFolder(Generated);
        BuilderUtil.EnsureFolder(Tiles);
        var center = new Vector2(0.5f, 0.5f);
        var bottom = new Vector2(0.5f, 0f);

        var art = new Art
        {
            White = BuilderUtil.SaveSprite(PixelArt.White(), $"{Generated}/White.png", 4, center),
            Coin = BuilderUtil.SaveSprite(PixelArt.Coin(), $"{Generated}/Coin.png", 24, bottom),
            Crystal = BuilderUtil.SaveSprite(PixelArt.Crystal(), $"{Generated}/Crystal.png", 20, bottom),
            HealthPotion = BuilderUtil.SaveSprite(PixelArt.Potion(new Color32(214, 54, 54, 255), new Color32(150, 30, 40, 255)),
                $"{Generated}/PotionHealth.png", 24, bottom),
            ManaPotion = BuilderUtil.SaveSprite(PixelArt.Potion(new Color32(60, 110, 230, 255), new Color32(36, 64, 160, 255)),
                $"{Generated}/PotionMana.png", 24, bottom),
            DigMark = BuilderUtil.SaveSprite(PixelArt.DigMark(), $"{Generated}/DigMark.png", 20, center),
            Exclamation = BuilderUtil.SaveSprite(PixelArt.Exclamation(), $"{Generated}/Exclamation.png", 16, bottom),
            Sparkle = BuilderUtil.SaveSprite(PixelArt.Sparkle(), $"{Generated}/Sparkle.png", 16, center),
            Scroll = BuilderUtil.SaveSprite(PixelArt.Scroll(), $"{Generated}/Scroll.png", 20, center),
            Shadow = BuilderUtil.SaveSprite(PixelArt.Shadow(), $"{Generated}/Shadow.png", 18, center),
            Glow = BuilderUtil.SaveSprite(PixelArt.Glow(), $"{Generated}/Glow.png", 32, center),
            Portal = BuilderUtil.SaveSprite(PixelArt.PortalSwirl(), $"{Generated}/PortalSwirl.png", 28, center),
            Sand = BuilderUtil.SaveSprite(PixelArt.Sand(), $"{Generated}/Sand.png", 32, center),
            MapDot = BuilderUtil.SaveSprite(PixelArt.MapDot(), $"{Generated}/MapDot.png", 16, center),
        };

        // Water atlas -> 16 tiles (index = land mask N1 E2 S4 W8).
        string waterPath = $"{Generated}/Water.png";
        System.IO.File.WriteAllBytes(waterPath, PixelArt.WaterAtlas().EncodeToPNG());
        AssetDatabase.ImportAsset(waterPath, ImportAssetOptions.ForceSynchronousImport);
        BuilderUtil.ConfigurePixelTexture(waterPath, 32, SpriteImportMode.Multiple);
        art.Water = BuilderUtil.SliceGrid(waterPath, 32, 32, center, "Water");
        art.WaterTiles = new Tile[16];
        for (int i = 0; i < 16; i++)
            art.WaterTiles[i] = BuilderUtil.CreateTile($"{Tiles}/Water_{i:00}.asset", art.Water[i], Tile.ColliderType.Grid);

        art.SandTile = BuilderUtil.CreateTile($"{Tiles}/Sand.asset", art.Sand);
        art.BlockerTile = BuilderUtil.CreateTile($"{Tiles}/Blocker.asset", art.White, Tile.ColliderType.Grid);

        // Arrow from the Tiny RPG pack.
        string arrowPath = "Assets/Tiny RPG Character Asset Pack v1.03 -Free Soldier&Orc/Arrow(Projectile)/Arrow01(32x32).png";
        BuilderUtil.ConfigurePixelTexture(arrowPath, 24, SpriteImportMode.Single, center);
        art.Arrow = AssetDatabase.LoadAssetAtPath<Sprite>(arrowPath);

        AssetDatabase.SaveAssets();
        return art;
    }

    public static Art LoadArt()
    {
        Sprite S(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Generated}/{file}.png");
        var art = new Art
        {
            White = S("White"), Coin = S("Coin"), Crystal = S("Crystal"), HealthPotion = S("PotionHealth"),
            ManaPotion = S("PotionMana"), DigMark = S("DigMark"), Exclamation = S("Exclamation"), Sparkle = S("Sparkle"),
            Scroll = S("Scroll"), Shadow = S("Shadow"), Glow = S("Glow"), Portal = S("PortalSwirl"), Sand = S("Sand"),
            MapDot = S("MapDot"),
            Arrow = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Tiny RPG Character Asset Pack v1.03 -Free Soldier&Orc/Arrow(Projectile)/Arrow01(32x32).png"),
            Water = BuilderUtil.LoadSprites($"{Generated}/Water.png"),
            SandTile = AssetDatabase.LoadAssetAtPath<Tile>($"{Tiles}/Sand.asset"),
            BlockerTile = AssetDatabase.LoadAssetAtPath<Tile>($"{Tiles}/Blocker.asset"),
        };
        art.WaterTiles = Enumerable.Range(0, 16).Select(i => AssetDatabase.LoadAssetAtPath<Tile>($"{Tiles}/Water_{i:00}.asset")).ToArray();
        return art;
    }

    // ===================================================================== Characters

    public class CharacterDef
    {
        public string Name;
        public string Folder;
        public string FilePrefix;      // e.g. "Soldier-" or "Demon_A_"
        public bool HasAttack3;
        // Filled in by BuildCharacter:
        public RuntimeAnimatorController Controller;
        public Sprite FirstFrame;
        public float CenterOffset;     // feet -> middle of body, in units
    }

    public static readonly CharacterDef Soldier = new()
    {
        Name = "Soldier", HasAttack3 = true, FilePrefix = "Soldier-",
        Folder = "Assets/Tiny RPG Character Asset Pack v1.03 -Free Soldier&Orc/Characters(100x100)/Soldier/Soldier"
    };
    public static readonly CharacterDef Orc = new()
    {
        Name = "Orc", FilePrefix = "Orc-",
        Folder = "Assets/Tiny RPG Character Asset Pack v1.03 -Free Soldier&Orc/Characters(100x100)/Orc/Orc"
    };
    public static readonly CharacterDef Demon = new()
    {
        Name = "Demon", FilePrefix = "Demon_A_", Folder = "Assets/Characters(100x100 split)/Demon_A/Demon_A"
    };
    public static readonly CharacterDef BloodMonster = new()
    {
        Name = "BloodMonster", FilePrefix = "Blood Monster_A_",
        Folder = "Assets/Characters(100x100 split)/Blood Monster_A/Blood Monster_A"
    };

    static readonly (string state, string file, float fps, bool loop)[] AnimationTable =
    {
        (CharacterAnimator.Idle, "Idle", 8f, true),
        (CharacterAnimator.Walk, "Walk", 10f, true),
        (CharacterAnimator.Attack1, "Attack01", 12f, false),
        (CharacterAnimator.Attack2, "Attack02", 12f, false),
        (CharacterAnimator.Attack3, "Attack03", 12f, false),
        (CharacterAnimator.Hurt, "Hurt", 12f, false),
        (CharacterAnimator.Death, "Death", 8f, false),
    };

    public static void BuildCharacter(CharacterDef def)
    {
        string folder = $"{Animations}/{def.Name}";
        BuilderUtil.EnsureFolder(folder);

        // Find where the feet are in the idle frame so the pivot sits on the ground.
        var idle = BuilderUtil.ReadPng($"{def.Folder}/{def.FilePrefix}Idle.png");
        int minY = 100, maxY = 0;
        for (int y = 0; y < 100; y++)
        for (int x = 0; x < 100; x++)
        {
            if (idle.GetPixel(x, y).a < 0.1f) continue;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }
        var pivot = new Vector2(0.5f, minY / 100f);
        def.CenterOffset = (maxY - minY + 1) * 0.5f / CharacterPPU;
        Debug.Log($"[Builder] {def.Name}: feet at y={minY}px, height={maxY - minY + 1}px");

        var controllerPath = $"{folder}/{def.Name}.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var stateMachine = controller.layers[0].stateMachine;

        foreach (var (state, file, fps, loop) in AnimationTable)
        {
            if (state == CharacterAnimator.Attack3 && !def.HasAttack3) continue;
            string path = $"{def.Folder}/{def.FilePrefix}{file}.png";
            BuilderUtil.ConfigurePixelTexture(path, CharacterPPU, SpriteImportMode.Multiple);
            var frames = BuilderUtil.SliceGrid(path, 100, 100, pivot, $"{def.Name}_{state}");

            var clip = new AnimationClip { frameRate = fps };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[^1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string clipPath = $"{folder}/{state}.anim";
            AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);

            var animatorState = stateMachine.AddState(state);
            animatorState.motion = clip;
            if (state == CharacterAnimator.Idle)
            {
                stateMachine.defaultState = animatorState;
                def.FirstFrame = frames[0];
            }
        }

        def.Controller = controller;
        AssetDatabase.SaveAssets();
    }

    // ===================================================================== Prefabs

    public class PrefabSet
    {
        public GameObject Player, Orc, Demon, BloodMonster, Arrow, Gold, HealthPotion, ManaPotion, Chest;
    }

    static GameObject CharacterBase(string name, CharacterDef def, Art art, float colliderRadius, float shadowScale)
    {
        var root = new GameObject(name);
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = root.AddComponent<CircleCollider2D>();
        col.radius = colliderRadius;
        col.offset = new Vector2(0f, colliderRadius * 0.6f);

        var shadow = BuilderUtil.Child(root, "Shadow", new Vector3(0f, 0.02f, 0f));
        BuilderUtil.AddSprite(shadow, art.Shadow, 1);
        shadow.transform.localScale = Vector3.one * shadowScale;

        var visual = BuilderUtil.Child(root, "Visual");
        BuilderUtil.AddSprite(visual, def.FirstFrame, 2);
        visual.AddComponent<Animator>().runtimeAnimatorController = def.Controller;
        visual.AddComponent<CharacterAnimator>();
        return root;
    }

    static GameObject SavePrefab(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static PrefabSet BuildPrefabs(Art art)
    {
        BuilderUtil.EnsureFolder($"{Prefabs}/Characters");
        BuilderUtil.EnsureFolder($"{Prefabs}/Items");
        BuilderUtil.EnsureFolder($"{Prefabs}/World");
        var set = new PrefabSet();
        int waterLayer = LayerMask.NameToLayer("Water");

        // ----- Arrow -----
        {
            var root = new GameObject("Arrow");
            BuilderUtil.AddSprite(root, art.Arrow, 3).spriteSortPoint = SpriteSortPoint.Center;
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 0.12f);
            var projectile = root.AddComponent<Projectile>();
            BuilderUtil.Set(projectile, "ignoreLayers", (LayerMask)(waterLayer >= 0 ? 1 << waterLayer : 0));
            set.Arrow = SavePrefab(root, $"{Prefabs}/Items/Arrow.prefab");
        }

        // ----- Consumables (Consumable -> GoldCoin, Potion -> HealthPotion / ManaPotion) -----
        GameObject MakeConsumable<T>(string name, Sprite sprite, int amount) where T : Consumable
        {
            var root = new GameObject(name);
            var shadow = BuilderUtil.Child(root, "Shadow");
            BuilderUtil.AddSprite(shadow, art.Shadow, 1);
            shadow.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            var visual = BuilderUtil.Child(root, "Visual");
            BuilderUtil.AddSprite(visual, sprite, 2);
            var item = root.AddComponent<T>();
            BuilderUtil.Set(item, "amount", amount);
            BuilderUtil.Set(item, "visual", visual.transform);
            return SavePrefab(root, $"{Prefabs}/Items/{name}.prefab");
        }
        set.Gold = MakeConsumable<GoldCoin>("Gold", art.Coin, 5);
        set.HealthPotion = MakeConsumable<HealthPotion>("HealthPotion", art.HealthPotion, 1);
        set.ManaPotion = MakeConsumable<ManaPotion>("ManaPotion", art.ManaPotion, 1);

        // ----- Player -----
        {
            var root = CharacterBase("Player", Soldier, art, 0.22f, 0.9f);
            root.tag = "Player";
            root.GetComponent<Rigidbody2D>().mass = 1f;
            var health = root.AddComponent<Health>();
            BuilderUtil.Set(health, "team", (int)Team.Player);
            BuilderUtil.Set(health, "maxHealth", 100f);
            BuilderUtil.Set(health, "invulnerableTime", 0.6f);
            root.AddComponent<PlayerStats>();
            root.AddComponent<PlayerMana>();
            root.AddComponent<PlayerInventory>();
            var controller = root.AddComponent<PlayerController>();
            BuilderUtil.Set(controller, "body", root.transform.Find("Visual").GetComponent<CharacterAnimator>());
            BuilderUtil.Set(controller, "bodyCenterOffset", new Vector2(0f, Soldier.CenterOffset));
            var combat = root.AddComponent<PlayerCombat>();
            BuilderUtil.Set(combat, "arrowPrefab", set.Arrow.GetComponent<Projectile>());
            BuilderUtil.Set(combat, "attackHitDelay", 0.2f);
            root.AddComponent<PlayerInteractor>();
            set.Player = SavePrefab(root, $"{Prefabs}/Characters/Player.prefab");
        }

        // ----- Enemies -----
        GameObject MakeEnemy(string name, CharacterDef def, float hp, float radius, float mass, float scale,
            System.Action<EnemyAI> configure, Vector2Int gold, float potionChance)
        {
            var root = CharacterBase(name, def, art, radius, 1f * scale);
            root.GetComponent<Rigidbody2D>().mass = mass;
            root.transform.Find("Visual").localScale = Vector3.one * scale;

            var health = root.AddComponent<Health>();
            BuilderUtil.Set(health, "team", (int)Team.Enemy);
            BuilderUtil.Set(health, "maxHealth", hp);
            BuilderUtil.Set(health, "invulnerableTime", 0.15f);

            var ai = root.AddComponent<EnemyAI>();
            BuilderUtil.Set(ai, "body", root.transform.Find("Visual").GetComponent<CharacterAnimator>());
            BuilderUtil.Set(ai, "bodyCenterOffset", new Vector2(0f, def.CenterOffset * scale));
            configure(ai);

            var loot = root.AddComponent<LootDropper>();
            BuilderUtil.Set(loot, "goldPrefab", set.Gold.GetComponent<GoldCoin>());
            BuilderUtil.Set(loot, "goldRange", gold);
            BuilderUtil.Set(loot, "goldChance", 0.9f);
            BuilderUtil.Set(loot, "healthPotionPrefab", set.HealthPotion.GetComponent<Potion>());
            BuilderUtil.Set(loot, "manaPotionPrefab", set.ManaPotion.GetComponent<Potion>());
            BuilderUtil.Set(loot, "potionChance", potionChance);

            // Health bar above the head.
            float barY = def.CenterOffset * scale * 2f + 0.25f;
            var bar = BuilderUtil.Child(root, "HealthBar", new Vector3(0f, barY, 0f));
            var bg = BuilderUtil.Child(bar, "Background");
            var bgSr = BuilderUtil.AddSprite(bg, art.White, 20, "Layer 3");
            bgSr.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);
            bg.transform.localScale = new Vector3(0.86f, 0.14f, 1f);
            var fill = BuilderUtil.Child(bar, "Fill");
            var fillSr = BuilderUtil.AddSprite(fill, art.White, 21, "Layer 3");
            fillSr.color = new Color(0.9f, 0.2f, 0.2f);
            fill.transform.localScale = new Vector3(0.8f, 0.08f, 1f);
            var hb = bar.AddComponent<WorldHealthBar>();
            BuilderUtil.Set(hb, "health", health);
            BuilderUtil.Set(hb, "background", bgSr);
            BuilderUtil.Set(hb, "fill", fillSr);

            return SavePrefab(root, $"{Prefabs}/Characters/{name}.prefab");
        }

        set.Orc = MakeEnemy("Orc", Orc, 40f, 0.24f, 1.5f, 1f, ai =>
        {
            BuilderUtil.Set(ai, "wanderSpeed", 1.1f);
            BuilderUtil.Set(ai, "chaseSpeed", 2.3f);
            BuilderUtil.Set(ai, "detectRadius", 4.5f);
            BuilderUtil.Set(ai, "attackRange", 0.85f);
            BuilderUtil.Set(ai, "attackDamage", 10f);
            BuilderUtil.Set(ai, "attackCooldown", 1.3f);
        }, new Vector2Int(3, 8), 0.25f);

        set.Demon = MakeEnemy("Demon", Demon, 30f, 0.22f, 1f, 1f, ai =>
        {
            BuilderUtil.Set(ai, "wanderSpeed", 1.4f);
            BuilderUtil.Set(ai, "chaseSpeed", 3.0f);
            BuilderUtil.Set(ai, "detectRadius", 5.5f);
            BuilderUtil.Set(ai, "loseRadius", 9f);
            BuilderUtil.Set(ai, "attackRange", 0.85f);
            BuilderUtil.Set(ai, "attackDamage", 12f);
            BuilderUtil.Set(ai, "attackCooldown", 1.1f);
            BuilderUtil.Set(ai, "attackWindup", 0.3f);
        }, new Vector2Int(4, 10), 0.3f);

        set.BloodMonster = MakeEnemy("BloodMonster", BloodMonster, 110f, 0.32f, 4f, 1.25f, ai =>
        {
            BuilderUtil.Set(ai, "wanderSpeed", 0.9f);
            BuilderUtil.Set(ai, "chaseSpeed", 2.0f);
            BuilderUtil.Set(ai, "detectRadius", 5f);
            BuilderUtil.Set(ai, "leashRadius", 9f);
            BuilderUtil.Set(ai, "attackRange", 1.1f);
            BuilderUtil.Set(ai, "attackDamage", 18f);
            BuilderUtil.Set(ai, "attackCooldown", 1.6f);
            BuilderUtil.Set(ai, "attackWindup", 0.45f);
            BuilderUtil.Set(ai, "knockbackSpeed", 2f);
        }, new Vector2Int(15, 25), 0.7f);

        // ----- Treasure chest -----
        {
            var closed = BuilderUtil.LoadPrefab($"{Cainos}/Prefab/Props/PF Props - Chest 01.prefab").GetComponent<SpriteRenderer>().sprite;
            var open = BuilderUtil.LoadPrefab($"{Cainos}/Prefab/Props/PF Props - Chest 01 Open.prefab").GetComponent<SpriteRenderer>().sprite;

            var root = new GameObject("Chest");
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.45f);
            col.offset = new Vector2(0f, 0.25f);

            var shadow = BuilderUtil.Child(root, "Shadow", new Vector3(0f, 0.08f, 0f));
            BuilderUtil.AddSprite(shadow, art.Shadow, 1).transform.localScale = new Vector3(1.1f, 0.9f, 1f);
            var sprite = BuilderUtil.Child(root, "Sprite");
            var spriteRenderer = BuilderUtil.AddSprite(sprite, closed, 2);

            var sparkle = BuilderUtil.Child(root, "Sparkle", new Vector3(0.3f, 0.95f, 0f));
            BuilderUtil.AddSprite(sparkle, art.Sparkle, 5, "Layer 2");
            var bob = sparkle.AddComponent<Bobbing>();
            BuilderUtil.Set(bob, "pulseAlpha", true);

            var dig = BuilderUtil.Child(root, "DigMark", new Vector3(0f, 0.2f, 0f));
            BuilderUtil.AddSprite(dig, art.DigMark, 1);

            var chest = root.AddComponent<TreasureChest>();
            BuilderUtil.Set(chest, "chestRenderer", spriteRenderer);
            BuilderUtil.Set(chest, "closedSprite", closed);
            BuilderUtil.Set(chest, "openSprite", open);
            BuilderUtil.Set(chest, "sparkle", sparkle);
            BuilderUtil.Set(chest, "digMark", dig);
            BuilderUtil.Set(chest, "interactRadius", 1.2f);
            set.Chest = SavePrefab(root, $"{Prefabs}/World/Chest.prefab");
        }

        AssetDatabase.SaveAssets();
        return set;
    }

    public static PrefabSet LoadPrefabs()
    {
        GameObject P(string p) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{p}.prefab");
        return new PrefabSet
        {
            Player = P("Characters/Player"), Orc = P("Characters/Orc"), Demon = P("Characters/Demon"),
            BloodMonster = P("Characters/BloodMonster"), Arrow = P("Items/Arrow"), Gold = P("Items/Gold"),
            HealthPotion = P("Items/HealthPotion"), ManaPotion = P("Items/ManaPotion"), Chest = P("World/Chest")
        };
    }
}
