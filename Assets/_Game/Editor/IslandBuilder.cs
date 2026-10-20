using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds the Island scene from Assets/_Game/Data/IslandMap.txt.
/// Each character in the text file is one 1x1 tile. Edit the text file and run the builder again to change the map.
///
///   ~ water      : sand       . grass      , flowers     = stone plaza   _ stone path
///   # forest (blocked)        T tree       b bush        r rock
///   P player     U upgrade shrine          O portal      L lantern      k well     g gravestone
///   A B C D clues             6 7 8 9 rune pillars (N E S W)          G vault gate
///   1 2 3 crystal chests      $ gold chest
///   o orc        d demon      m blood monster            h / n health / mana potion
///   x crate/barrel            p pot (both breakable)
/// </summary>
static class IslandBuilder
{
    const string MapPath = BuilderUtil.Root + "/Data/IslandMap.txt";
    public const string ScenePath = BuilderUtil.Root + "/Scenes/Island.unity";
    const string CainosRoot = "Assets/Cainos/Pixel Art Top Down - Basic";
    const string TerrainChars = "~:.,=_";

    static string[] rows;
    static int width, height;
    static char[,] ground;
    static System.Random rng;
    static AssetsBuilder.Art art;
    static AssetsBuilder.PrefabSet prefabs;
    static readonly Dictionary<string, GameObject> prefabCache = new();

    static char At(int x, int y) => x >= 0 && y >= 0 && x < width && y < height ? rows[height - 1 - y][x] : '~';
    static Vector3 Cell(int x, int y, float dy = 0.35f) => new(x + 0.5f, y + dy, 0f);
    static GameObject Cainos(string path) => Cached($"{CainosRoot}/Prefab/{path}.prefab");

    static GameObject Cached(string path)
    {
        if (!prefabCache.TryGetValue(path, out var prefab) || prefab == null)
            prefabCache[path] = prefab = BuilderUtil.LoadPrefab(path);
        return prefab;
    }

    public static void Build(AssetsBuilder.Art generatedArt, AssetsBuilder.PrefabSet prefabSet)
    {
        art = generatedArt;
        prefabs = prefabSet;
        rng = new System.Random(2024);
        prefabCache.Clear();

        rows = File.ReadAllLines(MapPath).Where(l => l.Length > 0).ToArray();
        height = rows.Length;
        width = rows[0].Length;

        BuilderUtil.EnsureFolder(BuilderUtil.Root + "/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // Opening a new scene can unload assets that were loaded before it, so load them again.
        art = AssetsBuilder.LoadArt();
        prefabs = AssetsBuilder.LoadPrefabs();

        ClassifyGround();
        BuildTilemaps();
        var world = new World();
        PlaceObjects(world);
        WireGameplay(world);
        BuildCameraAndManagers(world);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[Builder] Island scene saved ({width}x{height}).");
    }

    // ===================================================================== Ground

    static void ClassifyGround()
    {
        ground = new char[width, height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            char c = At(x, y);
            if (TerrainChars.Contains(c)) ground[x, y] = c;
            else if ("#Tbr".Contains(c)) ground[x, y] = '.';
            else if (c == 'G') ground[x, y] = '=';
            else
            {
                // Objects stand on whatever ground most of their neighbours have.
                var counts = new Dictionary<char, int>();
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    char n = At(x + dx, y + dy);
                    if (n == '~' || !TerrainChars.Contains(n)) continue;
                    counts[n] = counts.TryGetValue(n, out var v) ? v + 1 : 1;
                }
                ground[x, y] = counts.Count == 0 ? '.' : counts.OrderByDescending(kv => kv.Value).First().Key;
            }
        }
    }

    static char Ground(int x, int y) => x >= 0 && y >= 0 && x < width && y < height ? ground[x, y] : '~';

    static Tilemap CreateTilemap(Grid grid, string name, int order, bool solid, string layer = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        if (layer != null && LayerMask.NameToLayer(layer) >= 0) go.layer = LayerMask.NameToLayer(layer);
        if (solid)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var tileCollider = go.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }
        return tilemap;
    }

    static Dictionary<string, TileBase> LoadPaletteTiles(string folder)
    {
        var result = new Dictionary<string, TileBase>();
        foreach (var guid in AssetDatabase.FindAssets("t:Tile", new[] { folder }))
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid));
            if (tile != null && tile.sprite != null) result[tile.sprite.name] = tile;
        }
        return result;
    }

    static void BuildTilemaps()
    {
        var grid = new GameObject("Grid").AddComponent<Grid>();
        var water = CreateTilemap(grid, "Water", -10, true, "Water");
        var sand = CreateTilemap(grid, "Sand", -9, false);
        var grass = CreateTilemap(grid, "Grass", -8, false);
        var stone = CreateTilemap(grid, "Stone", -7, false);
        var blockers = CreateTilemap(grid, "Blockers", -6, true);
        blockers.GetComponent<TilemapRenderer>().enabled = false;

        var grassTiles = LoadPaletteTiles($"{CainosRoot}/Tile Palette/TP Grass");
        var stoneTiles = LoadPaletteTiles($"{CainosRoot}/Tile Palette/TP Stone Ground");
        TileBase GrassTile(int x, int y, bool flowers)
        {
            int index = (3 - (y & 3)) * 4 + (x & 3); // the 4x4 grass block tiles seamlessly
            return grassTiles[flowers ? $"TX Tileset Grass Flower {index}" : $"TX Tileset Grass {index}"];
        }
        TileBase StoneTile(int index) => stoneTiles[$"TX Tileset Stone Ground_{index}"];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var pos = new Vector3Int(x, y, 0);
            char g = ground[x, y];
            if (g == '~')
            {
                int mask = (Ground(x, y + 1) != '~' ? 1 : 0) | (Ground(x + 1, y) != '~' ? 2 : 0) |
                           (Ground(x, y - 1) != '~' ? 4 : 0) | (Ground(x - 1, y) != '~' ? 8 : 0);
                water.SetTile(pos, art.WaterTiles[mask]);
                continue;
            }
            if (g == ':')
            {
                sand.SetTile(pos, art.SandTile);
                continue;
            }

            grass.SetTile(pos, GrassTile(x, y, g == ','));
            if (g == '=') stone.SetTile(pos, StoneTile(PlazaIndex(x, y)));
            if (g == '_') stone.SetTile(pos, StoneTile(PathIndex(x, y)));
            if (At(x, y) == '#') blockers.SetTile(pos, art.BlockerTile);
        }

        // A ring of water outside the map so the camera never shows empty space.
        for (int y = -12; y < height + 12; y++)
        for (int x = -16; x < width + 16; x++)
            if (x < 0 || y < 0 || x >= width || y >= height)
                water.SetTile(new Vector3Int(x, y, 0), art.WaterTiles[0]);
    }

    /// <summary>9-slice tile for stone plazas (3x3 slab at the top-left of the Stone Ground sheet).</summary>
    static int PlazaIndex(int x, int y)
    {
        bool n = Ground(x, y + 1) == '=', s = Ground(x, y - 1) == '=', e = Ground(x + 1, y) == '=', w = Ground(x - 1, y) == '=';
        if (!n) return !w ? 0 : !e ? 2 : 1;
        if (!s) return !w ? 16 : !e ? 18 : 17;
        return !w ? 8 : !e ? 10 : 9;
    }

    /// <summary>Path strips: horizontal (24/25/26), vertical (3/11/19), junctions use the plain slab centre.</summary>
    static int PathIndex(int x, int y)
    {
        bool Link(int cx, int cy) => Ground(cx, cy) is '_' or '=';
        bool n = Link(x, y + 1), s = Link(x, y - 1), e = Link(x + 1, y), w = Link(x - 1, y);
        bool vertical = n || s, horizontal = e || w;
        if (vertical && horizontal) return 9;
        if (horizontal) return !w ? 24 : !e ? 26 : 25;
        if (vertical) return !n ? 3 : !s ? 19 : 11;
        return 9;
    }

    // ===================================================================== Objects

    class World
    {
        public Transform Environment, Props, Enemies, Items, Interactables;
        public GameObject Player;
        public readonly Dictionary<char, List<GameObject>> ByChar = new();
        public readonly List<(EnemyAI ai, Vector2 pos)> Orcs = new();

        public void Add(char c, GameObject go)
        {
            if (!ByChar.TryGetValue(c, out var list)) ByChar[c] = list = new List<GameObject>();
            list.Add(go);
        }

        public GameObject First(char c) => ByChar.TryGetValue(c, out var list) ? list[0] : null;
    }

    static float Jitter(float amount) => (float)(rng.NextDouble() * 2 - 1) * amount;
    static T Pick<T>(params T[] items) => items[rng.Next(items.Length)];

    static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position) =>
        BuilderUtil.InstantiatePrefab(prefab, parent, position);

    static void PlaceObjects(World world)
    {
        world.Environment = new GameObject("Environment").transform;
        world.Props = new GameObject("Props").transform;
        world.Enemies = new GameObject("Enemies").transform;
        world.Items = new GameObject("Items").transform;
        world.Interactables = new GameObject("Interactables").transform;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            char c = At(x, y);
            var go = PlaceCell(world, c, x, y);
            if (go != null) world.Add(c, go);
        }
    }

    static GameObject PlaceCell(World world, char c, int x, int y)
    {
        switch (c)
        {
            case '.':
                if (rng.NextDouble() < 0.12)
                    Spawn(Cainos($"Plant/PF Plant - Grass {rng.Next(1, 16):00}"), world.Environment,
                        new Vector3(x + 0.5f + Jitter(0.35f), y + 0.5f + Jitter(0.35f)));
                return null;
            case ',':
                if (rng.NextDouble() < 0.3)
                    Spawn(Cainos("Plant/PF Plant - Flower 01"), world.Environment,
                        new Vector3(x + 0.5f + Jitter(0.3f), y + 0.5f + Jitter(0.3f)));
                return null;
            case '#':
                // Dense forest: trees on a staggered grid, bushes in between. Collision comes from the Blockers tilemap.
                if (y % 2 == 0 && (x + (y % 4 == 0 ? 0 : 1)) % 3 == 0)
                    Spawn(Cainos($"Plant/PF Plant - Tree {rng.Next(1, 4):00}"), world.Environment,
                        new Vector3(x + 0.5f + Jitter(0.3f), y + 0.2f + Jitter(0.2f)));
                else if (rng.NextDouble() < 0.35)
                    Spawn(Cainos($"Plant/PF Plant - Bush {rng.Next(1, 7):00}"), world.Environment,
                        new Vector3(x + 0.5f + Jitter(0.3f), y + 0.3f + Jitter(0.3f)));
                return null;
            case 'T':
                return Spawn(Cainos($"Plant/PF Plant - Tree {rng.Next(1, 4):00}"), world.Environment, Cell(x, y, 0.2f));
            case 'b':
                return Spawn(Cainos($"Plant/PF Plant - Bush {rng.Next(1, 7):00}"), world.Environment, Cell(x, y, 0.3f));
            case 'r':
                return Spawn(Cainos($"Props/PF Props - Stone {rng.Next(1, 6):00}"), world.Props, Cell(x, y, 0.3f));
            case 'L':
                {
                    var lantern = Spawn(Cainos("Props/PF Props - Stone Lantern 01"), world.Props, Cell(x, y, 0.2f));
                    var glow = BuilderUtil.Child(lantern, "Glow", new Vector3(0f, 1.1f, 0f));
                    var sr = BuilderUtil.AddSprite(glow, art.Glow, 1, "Layer 2");
                    sr.color = new Color(1f, 0.85f, 0.5f, 0.35f);
                    glow.transform.localScale = Vector3.one * 1.6f;
                    return lantern;
                }
            case 'k': return Spawn(Cainos("Props/PF Props - Well 01"), world.Props, Cell(x, y, 0.1f));
            case 'g': return Spawn(Cainos($"Props/PF Props - Gravestone {rng.Next(1, 4):00}"), world.Props, Cell(x, y, 0.2f));
            case 'x': return MakeBreakable(Cainos(Pick("Props/PF Props - Crate 01", "Props/PF Props - Crate 02", "Props/PF Props - Barrel 01")),
                world.Props, Cell(x, y, 0.2f), 15f, new Color(0.55f, 0.4f, 0.28f), new Vector2Int(2, 6), 0.6f, 0.2f);
            case 'p': return MakeBreakable(Cainos($"Props/PF Props - Pot {rng.Next(1, 4):00}"),
                world.Props, Cell(x, y, 0.2f), 8f, new Color(0.62f, 0.38f, 0.26f), new Vector2Int(1, 5), 0.5f, 0.35f);

            case 'P':
                world.Player = Spawn(prefabs.Player, null, Cell(x, y));
                return world.Player;
            case 'o':
                {
                    var orc = Spawn(prefabs.Orc, world.Enemies, Cell(x, y));
                    world.Orcs.Add((orc.GetComponent<EnemyAI>(), new Vector2(x, y)));
                    return orc;
                }
            case 'd': return Spawn(prefabs.Demon, world.Enemies, Cell(x, y));
            case 'm': return Spawn(prefabs.BloodMonster, world.Enemies, Cell(x, y));
            case 'h': return Spawn(prefabs.HealthPotion, world.Items, Cell(x, y, 0.4f));
            case 'n': return Spawn(prefabs.ManaPotion, world.Items, Cell(x, y, 0.4f));

            case '1': return MakeChest(world, x, y, TreasureChest.Content.Crystal, 0, locked: true);
            case '2': return MakeChest(world, x, y, TreasureChest.Content.Crystal, 0);
            case '3': return MakeChest(world, x, y, TreasureChest.Content.Crystal, 0, buried: true);
            case '$': return MakeChest(world, x, y, TreasureChest.Content.Gold, Pick(30, 40, 50));

            case 'U': return MakeShrine(world, x, y);
            case 'O': return MakePortal(world, x, y);
            case 'G': return MakeGate(world, x, y);
            case '6': return MakeRune(world, x, y, "Utara", "Props/PF Props - Rune Pillar X3");
            case '7': return MakeRune(world, x, y, "Timur", "Props/PF Props - Rune Pillar X2");
            case '8': return MakeRune(world, x, y, "Selatan", "Props/PF Props - Rune Pillar X3");
            case '9': return MakeRune(world, x, y, "Barat", "Props/PF Props - Rune Pillar X2");
            case 'A': return MakeClue(world, x, y, null, ClueTexts.CaptainTitle, ClueTexts.Captain);
            case 'B': return MakeClue(world, x, y, "Props/PF Props - Gravestone 01", ClueTexts.RuneTitle, ClueTexts.Rune);
            case 'C': return MakeClue(world, x, y, null, ClueTexts.MapTitle, ClueTexts.Map);
            case 'D': return MakeClue(world, x, y, "Props/PF Props - Road Sign 01 E", ClueTexts.OrcTitle, ClueTexts.Orc);
        }
        return null;
    }

    static GameObject MakeBreakable(GameObject prefab, Transform parent, Vector3 position, float hp, Color debris,
        Vector2Int gold, float goldChance, float potionChance)
    {
        var go = Spawn(prefab, parent, position);
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Static;
        var health = go.AddComponent<Health>();
        BuilderUtil.Set(health, "team", (int)Team.Neutral);
        BuilderUtil.Set(health, "maxHealth", hp);
        BuilderUtil.Set(health, "invulnerableTime", 0.1f);
        var loot = go.AddComponent<LootDropper>();
        BuilderUtil.Set(loot, "goldPrefab", prefabs.Gold.GetComponent<GoldCoin>());
        BuilderUtil.Set(loot, "goldRange", gold);
        BuilderUtil.Set(loot, "goldChance", goldChance);
        BuilderUtil.Set(loot, "healthPotionPrefab", prefabs.HealthPotion.GetComponent<Potion>());
        BuilderUtil.Set(loot, "manaPotionPrefab", prefabs.ManaPotion.GetComponent<Potion>());
        BuilderUtil.Set(loot, "potionChance", potionChance);
        var breakable = go.AddComponent<Breakable>();
        BuilderUtil.Set(breakable, "debrisSprite", art.White);
        BuilderUtil.Set(breakable, "debrisColor", debris);
        return go;
    }

    static GameObject MakeChest(World world, int x, int y, TreasureChest.Content content, int gold, bool locked = false, bool buried = false)
    {
        var go = Spawn(prefabs.Chest, world.Interactables, Cell(x, y, 0.2f));
        go.name = content == TreasureChest.Content.Crystal ? "CrystalChest" : "GoldChest";
        var chest = go.GetComponent<TreasureChest>();
        BuilderUtil.Set(chest, "content", (int)content);
        BuilderUtil.Set(chest, "goldAmount", gold);
        BuilderUtil.Set(chest, "locked", locked);
        BuilderUtil.Set(chest, "buried", buried);
        if (locked) BuilderUtil.Set(chest, "lockedMessage", "Peti ini disegel sihir para orc. Kalahkan semua penjaganya untuk membukanya!");
        return go;
    }

    static GameObject MakeMarker(GameObject parent, float height)
    {
        var marker = BuilderUtil.Child(parent, "UnreadMarker", new Vector3(0f, height, 0f));
        BuilderUtil.AddSprite(marker, art.Exclamation, 5, "Layer 2");
        marker.AddComponent<Bobbing>();
        return marker;
    }

    static GameObject MakeClue(World world, int x, int y, string propPath, string title, string body)
    {
        GameObject go;
        float markerHeight;
        if (propPath == null)
        {
            go = new GameObject("Clue - " + title);
            go.transform.SetParent(world.Interactables);
            go.transform.position = Cell(x, y, 0.4f);
            BuilderUtil.AddSprite(go, art.Scroll, 1);
            markerHeight = 0.7f;
        }
        else
        {
            go = Spawn(Cainos(propPath), world.Interactables, Cell(x, y, 0.2f));
            go.name = "Clue - " + title;
            markerHeight = 1.5f;
        }
        var clue = go.AddComponent<ClueNote>();
        BuilderUtil.Set(clue, "title", title);
        BuilderUtil.Set(clue, "body", body);
        BuilderUtil.Set(clue, "unreadMarker", MakeMarker(go, markerHeight));
        BuilderUtil.Set(clue, "interactRadius", 1.4f);
        return go;
    }

    static GameObject MakeShrine(World world, int x, int y)
    {
        var go = Spawn(Cainos("Props/PF Props - Statue 01"), world.Interactables, Cell(x, y, 0.2f));
        go.name = "UpgradeShrine";
        var shrine = go.AddComponent<UpgradeShrine>();
        BuilderUtil.Set(shrine, "interactRadius", 1.6f);
        var icon = BuilderUtil.Child(go, "CoinIcon", new Vector3(0f, 2.3f, 0f));
        BuilderUtil.AddSprite(icon, art.Coin, 5, "Layer 2");
        icon.AddComponent<Bobbing>();
        return go;
    }

    static GameObject MakePortal(World world, int x, int y)
    {
        var root = new GameObject("Portal");
        root.transform.SetParent(world.Interactables);
        root.transform.position = Cell(x, y, 0f);
        Spawn(Cainos("Props/PF Props - Altar 01"), root.transform, root.transform.position);

        var active = BuilderUtil.Child(root, "ActiveVisual");
        var glow = BuilderUtil.Child(active, "Glow", new Vector3(0f, 1.15f, 0f));
        var glowSr = BuilderUtil.AddSprite(glow, art.Glow, 2);
        glowSr.color = new Color(0.55f, 0.85f, 1f, 0.55f);
        glow.transform.localScale = new Vector3(3.2f, 2.2f, 1f);
        var swirl = BuilderUtil.Child(active, "Swirl", new Vector3(0f, 1.15f, 0f));
        BuilderUtil.AddSprite(swirl, art.Portal, 2);

        var portal = root.AddComponent<Portal>();
        BuilderUtil.Set(portal, "activeVisual", active);
        BuilderUtil.Set(portal, "swirl", swirl.transform);
        BuilderUtil.Set(portal, "interactRadius", 2.2f);
        BuilderUtil.Set(portal, "interactOffset", new Vector2(0f, 1.1f));
        return root;
    }

    static GameObject MakeGate(World world, int x, int y)
    {
        var root = new GameObject("VaultGate");
        root.transform.SetParent(world.Props);
        root.transform.position = Cell(x, y, 0.1f);
        var closed = Spawn(Cainos("Props/PF Props - Wooden Gate 01"), root.transform, root.transform.position);
        var blocker = closed.AddComponent<BoxCollider2D>(); // make sure the 1-tile opening is fully closed
        blocker.size = new Vector2(1.1f, 0.6f);
        blocker.offset = new Vector2(0f, 0.2f);
        var opened = Spawn(Cainos("Props/PF Props - Wooden Gate 01 Opened"), root.transform, root.transform.position);
        var gate = root.AddComponent<Gate>();
        BuilderUtil.Set(gate, "closedVersion", closed);
        BuilderUtil.Set(gate, "openedVersion", opened);
        opened.SetActive(false);
        return root;
    }

    static GameObject MakeRune(World world, int x, int y, string runeName, string prefab)
    {
        var go = Spawn(Cainos(prefab), world.Interactables, Cell(x, y, 0.2f));
        go.name = "Rune " + runeName;
        var glows = new List<SpriteRenderer>();
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject.name != "Glow") continue;
            glows.Add(sr);
            // The tileset animates the glow on its own; the puzzle controls it instead.
            foreach (var mb in sr.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "SpriteColorAnimation") Object.DestroyImmediate(mb);
        }
        var rune = go.AddComponent<RuneStone>();
        BuilderUtil.Set(rune, "runeName", runeName);
        BuilderUtil.Set(rune, "glowRenderers", glows.ToArray());
        BuilderUtil.Set(rune, "interactRadius", 1.3f);
        return go;
    }

    // ===================================================================== Wiring

    static void WireGameplay(World world)
    {
        // Rune puzzle: East -> South -> West -> North opens the vault gate.
        var puzzleGo = new GameObject("RunePuzzle");
        puzzleGo.transform.SetParent(world.Interactables);
        var puzzle = puzzleGo.AddComponent<RunePuzzle>();
        var east = world.First('7').GetComponent<RuneStone>();
        var south = world.First('8').GetComponent<RuneStone>();
        var west = world.First('9').GetComponent<RuneStone>();
        var north = world.First('6').GetComponent<RuneStone>();
        BuilderUtil.Set(puzzle, "correctOrder", new Object[] { east, south, west, north });
        foreach (var rune in new[] { east, south, west, north }) BuilderUtil.Set(rune, "puzzle", puzzle);
        var gate = world.First('G').GetComponent<Gate>();
        UnityEventTools.AddPersistentListener(puzzle.OnSolved, gate.Open);

        // Orc camp: chest 1 unlocks once its guards are defeated.
        var campChest = world.First('1');
        var guardGroup = campChest.AddComponent<GuardGroup>();
        var guards = world.Orcs.Where(o => Vector2.Distance(o.pos, (Vector2)campChest.transform.position) < 5f).Select(o => o.ai).ToArray();
        BuilderUtil.Set(guardGroup, "guards", guards.Cast<Object>().ToArray());
        BuilderUtil.Set(guardGroup, "defeatedMessage", "Semua penjaga orc tumbang! Segel peti mereka terlepas.");
        UnityEventTools.AddPersistentListener(guardGroup.OnAllDefeated, campChest.GetComponent<TreasureChest>().Unlock);
        foreach (var guard in guards)
        {
            BuilderUtil.Set(guard, "wanderRadius", 1.5f);
            BuilderUtil.Set(guard, "leashRadius", 8f);
        }

        // The torn map at the well reveals where chest 3 is buried.
        var mapClue = world.First('C').GetComponent<ClueNote>();
        UnityEventTools.AddPersistentListener(mapClue.OnFirstRead, world.First('3').GetComponent<TreasureChest>().Reveal);

        // Blood monsters stay close to what they guard.
        foreach (var monster in world.ByChar['m'])
        {
            var ai = monster.GetComponent<EnemyAI>();
            BuilderUtil.Set(ai, "wanderRadius", 1.5f);
            BuilderUtil.Set(ai, "leashRadius", 7f);
        }
    }

    static void BuildCameraAndManagers(World world)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(62, 121, 153, 255);
        camGo.transform.position = new Vector3(world.Player.transform.position.x, world.Player.transform.position.y, -10f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();
        var follow = camGo.AddComponent<CameraFollow>();
        BuilderUtil.Set(follow, "target", world.Player.transform);
        BuilderUtil.Set(follow, "boundsMin", new Vector2(0f, 0f));
        BuilderUtil.Set(follow, "boundsMax", new Vector2(width, height));

        // Global light that reaches every sorting layer.
        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        var lso = new SerializedObject(light);
        var targets = lso.FindProperty("m_ApplyToSortingLayers");
        targets.arraySize = SortingLayer.layers.Length;
        for (int i = 0; i < SortingLayer.layers.Length; i++) targets.GetArrayElementAtIndex(i).intValue = SortingLayer.layers[i].id;
        lso.ApplyModifiedPropertiesWithoutUndo();

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        BuilderUtil.Set(manager, "requiredCrystals", 3);

        UIBuilder.BuildGameUI(art, BakeMinimap(), new Vector2(width, height));
        UIBuilder.EventSystem();
    }

    /// <summary>Saves a 1-pixel-per-tile picture of the island for the minimap.</summary>
    static Texture2D BakeMinimap()
    {
        string path = BuilderUtil.Root + "/Art/Generated/Minimap.png";
        var texture = PixelArt.Minimap(width, height, (x, y) =>
        {
            char c = At(x, y);
            if (c == '#' || c == 'T') return c;
            char g = ground[x, y];
            if (g == '~' && (Ground(x + 1, y) != '~' || Ground(x - 1, y) != '~' || Ground(x, y + 1) != '~' || Ground(x, y - 1) != '~'))
                return '*';
            return g;
        });
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ===================================================================== Main menu scene

    public const string MenuScenePath = BuilderUtil.Root + "/Scenes/MainMenu.unity";

    public static void BuildMainMenu(AssetsBuilder.Art generatedArt)
    {
        BuilderUtil.EnsureFolder(BuilderUtil.Root + "/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        generatedArt = AssetsBuilder.LoadArt();
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.14f, 0.18f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<UniversalAdditionalCameraData>();

        Sprite FirstSprite(string path) => BuilderUtil.LoadSprites(path).FirstOrDefault();
        var hero = FirstSprite($"{AssetsBuilder.Soldier.Folder}/{AssetsBuilder.Soldier.FilePrefix}Idle.png");
        var monsters = new[]
        {
            FirstSprite($"{AssetsBuilder.Orc.Folder}/{AssetsBuilder.Orc.FilePrefix}Idle.png"),
            FirstSprite($"{AssetsBuilder.Demon.Folder}/{AssetsBuilder.Demon.FilePrefix}Idle.png"),
        };
        UIBuilder.BuildMainMenu(generatedArt, hero, monsters);
        UIBuilder.EventSystem();
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    public static void RegisterScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true),
        };
    }
}
