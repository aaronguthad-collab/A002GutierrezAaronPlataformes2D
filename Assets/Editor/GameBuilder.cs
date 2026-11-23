using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platformer2DSystem;
using Platformer2DSystem.Example;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class GameBuilder
{
    private const string TemplateScene = "Assets/Scenes/Templates/LevelTemplate.unity";
    private const string ScenesFolder = "Assets/Scenes";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string AudioFolder = "Assets/Audio";
    private const string CollisionTilePath = "Assets/Tilesets/Tileset118.asset";
    private const string BeeSheet = "Assets/Sprites/SpritesResources/Mob/SmallBee/Fly/FlySheet.png";
    private const string BoarSheet = "Assets/Sprites/SpritesResources/Mob/Boar/Run/RunSheet.png";
    private const string SkyTexture = "Assets/Sprites/SpritesResources/Background/Background.png";
    private const string FarTreesTexture = "Assets/Sprites/SpritesResources/Trees/Background.png";
    private const string BuildPath = "Builds/Windows/GinebroQuest.exe";

    public const string GameTitle = "GINEBRÓ QUEST";
    public const string Author = "Aaron Gutierrez";

    private static int groundLayer;
    private static int enemiesLayer;
    private static int pickupsLayer;

    [MenuItem("Ginebro/2. Build Game Content")]
    public static void BuildAll()
    {
        EnsureLayers();
        PrepareTemplate();
        ConfigureImports();
        ArtGenerator.GenerateAll();
        CreatePrefabs();

        BuildSplash();
        BuildMainMenu();
        BuildCredits();
        BuildLevel(SceneNames.Level1, "LEVEL 1 · FOREST PATH", SceneNames.Level2, "LevelTheme", false);
        BuildLevel(SceneNames.Level2, "LEVEL 2 · THE HIVE WOODS", SceneNames.LevelFinal, "LevelTheme", false);
        BuildLevel(SceneNames.LevelFinal, "FINAL LEVEL · THE BOAR KING", null, "BossTheme", true);

        ConfigureBuildSettings();
        CleanupBaseProject();
        AssetDatabase.SaveAssets();
        Debug.Log("[GameBuilder] Game content built.");
    }

    [MenuItem("Ginebro/3. Build Windows Player")]
    public static void BuildWindows()
    {
        ConfigureBuildSettings();
        var options = new BuildPlayerOptions
        {
            scenes = SceneNames.All.Select(s => $"{ScenesFolder}/{s}.unity").ToArray(),
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[GameBuilder] Build result: {report.summary.result}, size {report.summary.totalSize / (1024 * 1024)} MB, errors {report.summary.totalErrors}");
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    private static void EnsureLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        void SetLayer(int index, string name)
        {
            var prop = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(prop.stringValue)) prop.stringValue = name;
        }
        SetLayer(7, "Enemies");
        SetLayer(8, "Pickups");
        tagManager.ApplyModifiedProperties();

        groundLayer = LayerMask.NameToLayer("Ground");
        enemiesLayer = LayerMask.NameToLayer("Enemies");
        pickupsLayer = LayerMask.NameToLayer("Pickups");
    }

    private static void PrepareTemplate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Templates")) AssetDatabase.CreateFolder("Assets/Scenes", "Templates");
        if (File.Exists("Assets/Scenes/SampleScene.unity"))
            AssetDatabase.MoveAsset("Assets/Scenes/SampleScene.unity", TemplateScene);

        var scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        AssetRenamer.RenameHierarchy(scene.GetRootGameObjects());
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureImports()
    {
        SliceRow(BeeSheet, 64, 64, new Vector2(0.5f, 0.5f));
        SliceRow(BoarSheet, 48, 32, new Vector2(0.5f, 0f));
        ConfigureBackground(SkyTexture);
        ConfigureBackground(FarTreesTexture);

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            bool music = path.Contains("/Music/");
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = music;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureBackground(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void SliceRow(string path, int cellW, int cellH, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        int count = texture.width / cellW;
        for (int i = 0; i < count; i++)
        {
            rects.Add(new SpriteRect
            {
                name = $"{baseName}{i}",
                rect = new Rect(i * cellW, texture.height - cellH, cellW, cellH),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = GUID.Generate(),
            });
        }
        provider.SetSpriteRects(rects.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Sprite[] SpritesOf(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
    }

    private static AudioClip Clip(string folder, string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{folder}/{name}.wav");
    }

    private static GameObject snailPrefab, beePrefab, bossPrefab, coinPrefab, heartPrefab, portalPrefab;

    private static void CreatePrefabs()
    {
        foreach (var f in new[] { "Enemies", "Pickups", "Level" })
            if (!AssetDatabase.IsValidFolder($"{PrefabsFolder}/{f}")) AssetDatabase.CreateFolder(PrefabsFolder, f);
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

        CreateAudioManagerPrefab();
        snailPrefab = CreateSnailPrefab();
        beePrefab = CreateBeePrefab();
        bossPrefab = CreateBossPrefab();
        coinPrefab = CreateCoinPrefab();
        heartPrefab = CreateHeartPrefab();
        portalPrefab = CreatePortalPrefab();
    }

    private static GameObject SavePrefab(GameObject go, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void CreateAudioManagerPrefab()
    {
        var go = new GameObject("AudioManager");
        var manager = go.AddComponent<AudioManager>();
        var so = new SerializedObject(manager);
        void Set(string field, string clip) => so.FindProperty(field).objectReferenceValue = Clip("Sfx", clip);
        Set("jump", "Jump");
        Set("doubleJump", "DoubleJump");
        Set("coin", "Coin");
        Set("stomp", "Stomp");
        Set("hurt", "Hurt");
        Set("bossHit", "BossHit");
        Set("bossDefeated", "BossDefeated");
        Set("playerDeath", "PlayerDeath");
        Set("victory", "Victory");
        Set("buttonClick", "ButtonClick");
        Set("buttonHover", "ButtonHover");
        Set("portal", "Portal");
        so.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(go, "Assets/Resources/AudioManager.prefab");
    }

    private static GameObject NewEnemyRoot(string name, out GameObject graphics, out BoxCollider2D box)
    {
        var root = new GameObject(name);
        root.layer = enemiesLayer;
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        graphics = new GameObject("Graphics");
        graphics.transform.SetParent(root.transform, false);
        graphics.layer = enemiesLayer;
        graphics.AddComponent<SpriteRenderer>();

        var collision = new GameObject("CollisionBox");
        collision.transform.SetParent(root.transform, false);
        collision.layer = enemiesLayer;
        collision.tag = "Enemy";
        box = collision.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        return root;
    }

    private static void SetEnemyFields(Enemy enemy, SpriteRenderer graphics, int lives, int damage)
    {
        var so = new SerializedObject(enemy);
        so.FindProperty("maxLives").intValue = lives;
        so.FindProperty("contactDamage").intValue = damage;
        so.FindProperty("graphics").objectReferenceValue = graphics;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateSnailPrefab()
    {
        var root = NewEnemyRoot("Snail", out var graphics, out var box);
        var sr = graphics.GetComponent<SpriteRenderer>();
        var snailSprites = SpritesOf("Assets/Sprites/SpritesResources/Mob/Snail/WalkSheet.png");
        sr.sprite = snailSprites.FirstOrDefault();
        sr.sortingOrder = 1;
        var animator = graphics.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Snail/WalkSheet0.controller");
        box.size = new Vector2(1.59f, 1.2f);
        box.offset = new Vector2(0f, -0.38f);

        var enemy = root.AddComponent<PatrolEnemy>();
        SetEnemyFields(enemy, sr, 1, 1);
        var so = new SerializedObject(enemy);
        so.FindProperty("speed").floatValue = 2f;
        so.FindProperty("patrolDistance").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(root, $"{PrefabsFolder}/Enemies/Snail.prefab");
    }

    private static GameObject CreateBeePrefab()
    {
        var root = NewEnemyRoot("Bee", out var graphics, out var box);
        var frames = SpritesOf(BeeSheet);
        var sr = graphics.GetComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = 3;
        var anim = graphics.AddComponent<SpriteAnimator>();
        anim.Frames = frames;
        box.size = new Vector2(1.3f, 1.2f);
        box.offset = new Vector2(0f, -0.1f);

        var enemy = root.AddComponent<FlyingEnemy>();
        SetEnemyFields(enemy, sr, 1, 1);
        return SavePrefab(root, $"{PrefabsFolder}/Enemies/Bee.prefab");
    }

    private static GameObject CreateBossPrefab()
    {
        var root = NewEnemyRoot("BoarKing", out var graphics, out var box);
        graphics.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
        var frames = SpritesOf(BoarSheet);
        var sr = graphics.GetComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = 4;
        var anim = graphics.AddComponent<SpriteAnimator>();
        anim.Frames = frames;
        var animSo = new SerializedObject(anim);
        animSo.FindProperty("framesPerSecond").floatValue = 12f;
        animSo.ApplyModifiedPropertiesWithoutUndo();
        box.size = new Vector2(5.2f, 3.2f);
        box.offset = new Vector2(0f, 1.6f);

        var boss = root.AddComponent<BossEnemy>();
        SetEnemyFields(boss, sr, 3, 2);
        var so = new SerializedObject(boss);
        so.FindProperty("hitInvulnerableTime").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(root, $"{PrefabsFolder}/Enemies/BoarKing.prefab");
    }

    private static GameObject CreateCoinPrefab()
    {
        var root = new GameObject("Coin");
        root.layer = pickupsLayer;
        root.tag = "Coin";
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = ArtGenerator.CoinFrames[0];
        sr.sortingOrder = 2;
        var anim = root.AddComponent<SpriteAnimator>();
        anim.Frames = ArtGenerator.CoinFrames;
        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;
        root.AddComponent<Coin>();
        return SavePrefab(root, $"{PrefabsFolder}/Pickups/Coin.prefab");
    }

    private static GameObject CreateHeartPrefab()
    {
        var root = new GameObject("HeartPickup");
        root.layer = pickupsLayer;
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = ArtGenerator.Heart;
        sr.sortingOrder = 2;
        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;
        root.AddComponent<HealthPickup>();
        return SavePrefab(root, $"{PrefabsFolder}/Pickups/HeartPickup.prefab");
    }

    private static GameObject CreatePortalPrefab()
    {
        var root = new GameObject("Portal");
        root.layer = pickupsLayer;
        root.tag = "Portal";
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 2.6f);
        col.offset = new Vector2(0f, 1.3f);

        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = ArtGenerator.PortalGlow;
        glowSr.sortingOrder = 1;

        var swirl = new GameObject("Swirl");
        swirl.transform.SetParent(root.transform, false);
        swirl.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        swirl.transform.localScale = new Vector3(1f, 1f, 1f);
        var swirlSr = swirl.AddComponent<SpriteRenderer>();
        swirlSr.sprite = ArtGenerator.Portal;
        swirlSr.sortingOrder = 2;

        var portal = root.AddComponent<Portal>();
        var so = new SerializedObject(portal);
        so.FindProperty("visual").objectReferenceValue = swirl.transform;
        so.FindProperty("spinSpeed").floatValue = -160f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(root, $"{PrefabsFolder}/Level/Portal.prefab");
    }

    private static Scene NewMenuScene(string name, Color background, string music)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        camGo.transform.position = new Vector3(0, 0, -10);
        UiFactory.EventSystem();
        if (music != null) AddSceneMusic(music);
        return scene;
    }

    private static void AddSceneMusic(string music)
    {
        var go = new GameObject("SceneMusic");
        var sm = go.AddComponent<SceneMusic>();
        var so = new SerializedObject(sm);
        so.FindProperty("music").objectReferenceValue = Clip("Music", music);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SaveScene(Scene scene, string name)
    {
        AssetRenamer.RenameHierarchy(scene.GetRootGameObjects());
        EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{name}.unity");
    }

    private static void BuildSplash()
    {
        var scene = NewMenuScene(SceneNames.Splash, new Color(0.06f, 0.09f, 0.08f), null);
        var canvas = UiFactory.Canvas("SplashCanvas");

        var logo = UiFactory.Rect("GinebroGamesLogo", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 700));
        var group = logo.gameObject.AddComponent<CanvasGroup>();
        var emblem = UiFactory.Image("Emblem", logo, ArtGenerator.LogoEmblem, Color.white, new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(380, 380));
        emblem.preserveAspect = true;
        UiFactory.Text("CompanyName", logo, "GINEBRÓ GAMES", 110, UiFactory.Gold, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(1100, 140));
        UiFactory.Text("Tagline", logo, "handcrafted games since 2026", 34, new Color(0.75f, 0.85f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(900, 60), TextAlignmentOptions.Center, false);

        var splash = canvas.gameObject.AddComponent<SplashScreen>();
        var so = new SerializedObject(splash);
        so.FindProperty("logoGroup").objectReferenceValue = group;
        so.FindProperty("logo").objectReferenceValue = logo;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder(PrefabsFolder, "UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(logo.gameObject, $"{PrefabsFolder}/UI/GinebroGamesLogo.prefab", InteractionMode.AutomatedAction);

        SaveScene(scene, SceneNames.Splash);
    }

    private static void MenuBackground(Transform canvas)
    {
        var sky = UiFactory.FullScreenImage("Sky", canvas, AssetDatabase.LoadAssetAtPath<Sprite>(SkyTexture), Color.white);
        sky.raycastTarget = false;
        var trees = UiFactory.Image("FarTrees", canvas, AssetDatabase.LoadAssetAtPath<Sprite>(FarTreesTexture), new Color(0.8f, 0.9f, 0.85f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920 * 1.75f, 600));
        trees.raycastTarget = false;
        var shade = UiFactory.FullScreenImage("Shade", canvas, ArtGenerator.Pixel, new Color(0.02f, 0.05f, 0.04f, 0.45f));
        shade.raycastTarget = false;
    }

    private static void BuildMainMenu()
    {
        var scene = NewMenuScene(SceneNames.MainMenu, Color.black, "MenuTheme");
        var canvas = UiFactory.Canvas("MenuCanvas");
        MenuBackground(canvas.transform);

        var emblem = UiFactory.Image("Emblem", canvas.transform, ArtGenerator.LogoEmblem, Color.white, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(170, 170));
        emblem.preserveAspect = true;
        UiFactory.Text("Title", canvas.transform, GameTitle, 140, UiFactory.Gold, new Vector2(0.5f, 1f), new Vector2(0, -240), new Vector2(1600, 170));
        UiFactory.Text("Subtitle", canvas.transform, "a GINEBRÓ GAMES 2D platformer", 40, new Color(0.85f, 0.95f, 0.88f), new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(1200, 60), TextAlignmentOptions.Center, false);

        var size = new Vector2(460, 96);
        var play = UiFactory.Button("PlayButton", canvas.transform, "Play", new Vector2(0.5f, 0.5f), new Vector2(0, -90), size);
        var credits = UiFactory.Button("CreditsButton", canvas.transform, "Credits", new Vector2(0.5f, 0.5f), new Vector2(0, -210), size);
        var quit = UiFactory.Button("QuitButton", canvas.transform, "Quit", new Vector2(0.5f, 0.5f), new Vector2(0, -330), size);
        UiFactory.ChainVertical(play, credits, quit);

        UiFactory.Text("Controls", canvas.transform, "Move: A/D or ←/→   ·   Jump / double jump: Space   ·   Pause: Esc", 30, new Color(0.85f, 0.9f, 0.88f),
            new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1600, 50), TextAlignmentOptions.Center, false);
        UiFactory.Text("Version", canvas.transform, "v1.0", 26, new Color(1f, 1f, 1f, 0.6f), new Vector2(1f, 0f), new Vector2(-30, 30), new Vector2(200, 40), TextAlignmentOptions.Right, false);

        var menu = canvas.gameObject.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("playButton").objectReferenceValue = play;
        so.FindProperty("creditsButton").objectReferenceValue = credits;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedPropertiesWithoutUndo();

        SaveScene(scene, SceneNames.MainMenu);
    }

    private static void BuildCredits()
    {
        var scene = NewMenuScene(SceneNames.Credits, Color.black, "MenuTheme");
        var canvas = UiFactory.Canvas("CreditsCanvas");
        MenuBackground(canvas.transform);

        UiFactory.Text("Title", canvas.transform, "CREDITS", 120, UiFactory.Gold, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1200, 150));

        var content = UiFactory.Rect("CreditsContent", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1400, 560));
        var mask = content.gameObject.AddComponent<RectMask2D>();
        var scroll = UiFactory.Rect("ScrollContent", content, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(1400, 700));
        string body =
            $"<size=46><color=#FFD65A>Game design & programming</color></size>\n<size=64>{Author}</size>\n\n" +
            "<size=40><color=#FFD65A>Made for</color></size>\nGINEBRÓ GAMES\n\n" +
            "<size=40><color=#FFD65A>Base project</color></size>\nUnityPlatformerExample (mphc028)\n\n" +
            "<size=40><color=#FFD65A>Art</color></size>\nLegacy Fantasy – High Forest pixel art pack\nGINEBRÓ GAMES logo, coins, hearts & portal: made for this game\n\n" +
            "<size=40><color=#FFD65A>Music & sound effects</color></size>\nProcedurally generated for this game\n\n" +
            "<size=40><color=#FFD65A>Engine</color></size>\nUnity 6000.6.0f1\n\n" +
            "DAM2 · Activitat 2 · Joc de Plataformes 2D";
        var text = UiFactory.Text("CreditsText", scroll, body, 38, Color.white, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400, 1100), TextAlignmentOptions.Top, false);
        text.rectTransform.pivot = new Vector2(0.5f, 1f);

        var back = UiFactory.Button("BackButton", canvas.transform, "Back to Menu", new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(520, 96));
        back.navigation = new Navigation { mode = Navigation.Mode.None };

        var credits = canvas.gameObject.AddComponent<CreditsMenu>();
        var so = new SerializedObject(credits);
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("scrollContent").objectReferenceValue = scroll;
        so.FindProperty("scrollLimit").floatValue = 520f;
        so.ApplyModifiedPropertiesWithoutUndo();
        _ = mask;

        SaveScene(scene, SceneNames.Credits);
    }

    private sealed class TileSet
    {
        private readonly Dictionary<(int, int), TileBase> tiles = new();
        public TileBase Collision;

        public TileSet()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { "Assets/Tilesets" }))
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid));
                if (tile == null || tile.sprite == null || tile.sprite.texture.name != "Tiles") continue;
                var r = tile.sprite.rect;
                int col = (int)(r.x / 16f);
                int row = (int)((tile.sprite.texture.height - r.yMax) / 16f);
                tiles[(col, row)] = tile;
            }
            Collision = AssetDatabase.LoadAssetAtPath<TileBase>(CollisionTilePath);
        }

        public TileBase Get(int col, int row)
        {
            return tiles.TryGetValue((col, row), out var t) ? t : tiles.GetValueOrDefault((2, 3));
        }
    }

    private static void BuildLevel(string sceneName, string title, string nextScene, string music, bool isFinal)
    {
        string path = $"{ScenesFolder}/{sceneName}.unity";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(TemplateScene, path);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        foreach (var name in new[] { "Snail", "Coin", "Portal", "Canvas" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        string[] map = File.ReadAllLines($"Assets/Levels/{sceneName}.txt").Where(l => l.Length > 0).ToArray();
        int height = map.Length;
        int width = map.Max(l => l.Length);
        char At(int x, int row) => row >= 0 && row < height && x >= 0 && x < map[row].Length ? map[row][x] : '.';
        Vector3 CellBottom(int x, int row) => new(x + 0.5f, -row - 1, 0f);

        var maps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        var collision = maps.First(m => m.name == "Tilemap");
        var terrain = maps.First(m => m.name == "GraphicsLayer1");
        var decoration = maps.First(m => m.name == "GraphicsLayer2");
        PaintTerrain(map, width, height, collision, terrain, decoration);

        var levelRoot = new GameObject("Level").transform;
        var enemiesRoot = new GameObject("Enemies").transform;
        var pickupsRoot = new GameObject("Pickups").transform;
        enemiesRoot.SetParent(levelRoot);
        pickupsRoot.SetParent(levelRoot);

        var player = GameObject.Find("Player");
        ConfigurePlayer(player);

        BossEnemy boss = null;
        for (int row = 0; row < height; row++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = At(x, row);
                Vector3 bottom = CellBottom(x, row);
                Vector3 center = bottom + new Vector3(0f, 0.5f, 0f);
                switch (c)
                {
                    case 'P':
                        player.transform.position = bottom + new Vector3(0f, 0.81f, 0f);
                        break;
                    case 'C':
                        Place(coinPrefab, center, pickupsRoot);
                        break;
                    case 'H':
                        Place(heartPrefab, center, pickupsRoot);
                        break;
                    case 'S':
                        Place(snailPrefab, bottom + new Vector3(0f, 1.04f, 0f), enemiesRoot);
                        break;
                    case 'B':
                        Place(beePrefab, center, enemiesRoot);
                        break;
                    case 'X':
                        boss = Place(bossPrefab, bottom, enemiesRoot).GetComponent<BossEnemy>();
                        break;
                    case 'E':
                        var portal = Place(portalPrefab, bottom, levelRoot).GetComponent<Portal>();
                        portal.SceneToLoad = nextScene;
                        break;
                }
            }
        }

        if (boss != null)
        {
            int row = Mathf.RoundToInt(-boss.transform.position.y) - 1;
            int bx = Mathf.FloorToInt(boss.transform.position.x);
            int left = bx, right = bx;
            while (left > 0 && !IsSolid(At(left - 1, row))) left--;
            while (right < width - 1 && !IsSolid(At(right + 1, row))) right++;
            boss.SetArena(left + 2.8f, right + 1 - 2.8f);
            EditorUtility.SetDirty(boss);
        }

        CreateBounds(levelRoot, width, height);
        ConfigureCamera(player, width, height);
        CreateParallaxBackground(width, height);
        AddSceneMusic(music);
        BuildHud(sceneName, title, player, boss, isFinal);

        SaveScene(scene, sceneName);
    }

    private static bool IsSolid(char c) => c == '#' || c == '=';

    private static GameObject Place(GameObject prefab, Vector3 position, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        return go;
    }

    private static void PaintTerrain(string[] map, int width, int height, Tilemap collision, Tilemap terrain, Tilemap decoration)
    {
        var tiles = new TileSet();
        collision.ClearAllTiles();
        terrain.ClearAllTiles();
        decoration.ClearAllTiles();

        char At(int x, int row)
        {
            if (row >= height) return '#';
            if (row < 0) return '.';
            if (x < 0) x = 0;
            if (x >= width) x = width - 1;
            return x < map[row].Length ? map[row][x] : '.';
        }

        Vector3Int Cell(int x, int row) => new(x, -row - 1, 0);

        for (int row = 0; row < height; row++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = At(x, row);
                if (!IsSolid(c)) continue;

                collision.SetTile(Cell(x, row), tiles.Collision);

                bool up = IsSolid(At(x, row - 1));
                bool down = IsSolid(At(x, row + 1));
                bool left = At(x - 1, row) == c;
                bool right = At(x + 1, row) == c;
                int col = !left ? 0 : !right ? 4 : 1 + (x % 3);

                int tileRow;
                if (c == '#')
                {
                    tileRow = !up ? 1 : !IsSolid(At(x, row - 2)) ? 2 : 3;
                    if (!up) terrain.SetTile(Cell(x, row - 1), tiles.Get(col, 0));
                }
                else
                {
                    tileRow = !up ? 6 : !down ? 9 : 7;
                    if (!up) terrain.SetTile(Cell(x, row - 1), tiles.Get(col, 5));
                }
                terrain.SetTile(Cell(x, row), tiles.Get(col, tileRow));
            }
        }

        var rng = new System.Random(width * 31 + height);
        for (int row = 1; row < height; row++)
        {
            int x = 0;
            while (x < width - 8)
            {
                bool flat = true;
                for (int i = 0; i < 8 && flat; i++)
                    flat = At(x + i, row) == '#' && !IsSolid(At(x + i, row - 1)) && !IsSolid(At(x + i, row - 2)) && !IsSolid(At(x + i, row - 3));
                if (flat && rng.NextDouble() < 0.35)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        decoration.SetTile(Cell(x + i, row - 1), tiles.Get(17 + i, 8));
                        decoration.SetTile(Cell(x + i, row - 2), tiles.Get(17 + i, 7));
                        if (i >= 2 && i <= 5) decoration.SetTile(Cell(x + i, row - 3), tiles.Get(17 + i, 6));
                    }
                    x += 14 + rng.Next(8);
                }
                else
                {
                    x += 3;
                }
            }
        }

        collision.CompressBounds();
        terrain.CompressBounds();
        decoration.CompressBounds();
        var tilemapCollider = collision.GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null) tilemapCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        var composite = collision.GetComponent<CompositeCollider2D>();
        if (composite != null)
        {
            composite.GenerateGeometry();
            Debug.Log($"[GameBuilder] {collision.gameObject.scene.name}: collision paths={composite.pathCount} bounds={composite.bounds}");
        }
    }

    private static void ConfigurePlayer(GameObject player)
    {
        var actor = player.GetComponent<Actor>();
        var actorSo = new SerializedObject(actor);
        actorSo.FindProperty("collisionMask").intValue = 1 << groundLayer;
        actorSo.ApplyModifiedPropertiesWithoutUndo();

        if (!player.TryGetComponent(out PlayerHealth health)) health = player.AddComponent<PlayerHealth>();
        var healthSo = new SerializedObject(health);
        healthSo.FindProperty("graphics").objectReferenceValue = player.transform.Find("Graphics").GetComponent<SpriteRenderer>();
        healthSo.ApplyModifiedPropertiesWithoutUndo();
        if (!player.TryGetComponent(out PlayerWallet _)) player.AddComponent<PlayerWallet>();
        if (!player.TryGetComponent(out PlayerContactSensor _)) player.AddComponent<PlayerContactSensor>();
    }

    private static void CreateBounds(Transform parent, int width, int height)
    {
        var bounds = new GameObject("LevelBounds").transform;
        bounds.SetParent(parent);

        void Wall(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.layer = groundLayer;
            go.transform.SetParent(bounds);
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
        }
        Wall("LeftWall", new Vector2(-0.5f, -height / 2f + 10f), new Vector2(1f, height + 40f));
        Wall("RightWall", new Vector2(width + 0.5f, -height / 2f + 10f), new Vector2(1f, height + 40f));

        var kill = new GameObject("KillZone");
        kill.transform.SetParent(bounds);
        kill.layer = pickupsLayer;
        kill.transform.position = new Vector3(width / 2f, -height - 4f, 0f);
        var box = kill.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width + 20f, 4f);
        kill.AddComponent<KillZone>();
    }

    private static void ConfigureCamera(GameObject player, int width, int height)
    {
        var cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * 16f / 9f;
        var follow = cam.GetComponent<CameraFollow>();
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.FindProperty("speed").floatValue = 6f;
        so.ApplyModifiedPropertiesWithoutUndo();

        float minY = -height + halfH;
        float maxY = Mathf.Max(minY, -halfH + 4f);
        follow.SetBounds(new Vector2(halfW, minY), new Vector2(width - halfW, maxY));
        Vector3 p = player.transform.position;
        cam.transform.position = new Vector3(Mathf.Clamp(p.x, halfW, width - halfW), Mathf.Clamp(p.y + 2f, minY, maxY), -10f);
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.78f);
        EditorUtility.SetDirty(follow);
    }

    private static void CreateParallaxBackground(int width, int height)
    {
        var cam = Camera.main;
        var root = new GameObject("Background");
        var parallax = root.AddComponent<ParallaxController>();
        parallax.cameraTransform = cam.transform;
        parallax.autoPopulateChildren = false;

        float halfH = cam.orthographicSize;
        Vector3 camPos = cam.transform.position;

        var skySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkyTexture);
        var sky = new GameObject("Sky").AddComponent<SpriteRenderer>();
        sky.transform.SetParent(root.transform);
        sky.sprite = skySprite;
        sky.sortingOrder = -100;
        float skyScale = (halfH * 2f + 1f) / skySprite.bounds.size.y;
        sky.transform.localScale = new Vector3(skyScale, skyScale, 1f);
        sky.transform.position = new Vector3(camPos.x, camPos.y - halfH - 0.5f, 10f);

        var treesSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FarTreesTexture);
        var trees = new GameObject("FarTrees").AddComponent<SpriteRenderer>();
        trees.transform.SetParent(root.transform);
        trees.sprite = treesSprite;
        trees.sortingOrder = -50;
        trees.drawMode = SpriteDrawMode.Tiled;
        trees.color = new Color(0.85f, 0.92f, 0.88f);
        float treesWidth = Mathf.Ceil((width * 0.3f + 80f) / treesSprite.bounds.size.x) * treesSprite.bounds.size.x;
        trees.size = new Vector2(treesWidth, treesSprite.bounds.size.y);
        trees.transform.position = new Vector3(camPos.x + width * 0.1f, camPos.y - halfH - 1f, 5f);

        parallax.layers = new[]
        {
            new ParallaxController.ParallaxLayer { target = sky.transform, parallaxMultiplier = new Vector2(1f, 1f) },
            new ParallaxController.ParallaxLayer { target = trees.transform, parallaxMultiplier = new Vector2(0.8f, 0.9f) },
        };
        EditorUtility.SetDirty(parallax);
    }

    private static void BuildHud(string sceneName, string title, GameObject player, BossEnemy boss, bool isFinal)
    {
        var canvas = UiFactory.Canvas("HudCanvas", 10);
        var top = UiFactory.Rect("TopLeftPanel", canvas.transform, new Vector2(0f, 1f), new Vector2(30, -30), new Vector2(560, 170));

        var heartIcon = UiFactory.Image("HeartIcon", top, ArtGenerator.Heart, Color.white, new Vector2(0f, 1f), Vector2.zero, new Vector2(72, 72));
        heartIcon.preserveAspect = true;
        var slider = UiFactory.Slider("HealthBar", top, new Vector2(0f, 1f), new Vector2(90, -10), new Vector2(400, 52), out var fill);
        var healthLabel = UiFactory.Text("HealthLabel", slider.transform, "6 / 6", 32, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 52));
        var healthBar = slider.gameObject.AddComponent<HealthBar>();
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.9f, 0.2f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f), new GradientColorKey(new Color(0.3f, 0.85f, 0.35f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        var hbSo = new SerializedObject(healthBar);
        hbSo.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerHealth>();
        hbSo.FindProperty("slider").objectReferenceValue = slider;
        hbSo.FindProperty("fill").objectReferenceValue = fill;
        hbSo.FindProperty("label").objectReferenceValue = healthLabel;
        hbSo.FindProperty("colorByHealth").gradientValue = gradient;
        hbSo.ApplyModifiedPropertiesWithoutUndo();

        var coinGroup = UiFactory.Rect("CoinCounter", top, new Vector2(0f, 1f), new Vector2(0, -90), new Vector2(300, 72));
        coinGroup.pivot = new Vector2(0f, 0.5f);
        coinGroup.anchoredPosition = new Vector2(0, -126);
        var coinIcon = UiFactory.Image("CoinIcon", coinGroup, ArtGenerator.CoinFrames[0], Color.white, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(64, 64));
        coinIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
        var coinLabel = UiFactory.Text("CoinLabel", coinGroup, "x 0", 52, UiFactory.Gold, new Vector2(0f, 0.5f), new Vector2(80, 0), new Vector2(220, 70), TextAlignmentOptions.Left);
        coinLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
        var counter = coinGroup.gameObject.AddComponent<CoinCounter>();
        var ccSo = new SerializedObject(counter);
        ccSo.FindProperty("wallet").objectReferenceValue = player.GetComponent<PlayerWallet>();
        ccSo.FindProperty("label").objectReferenceValue = coinLabel;
        ccSo.FindProperty("punchTarget").objectReferenceValue = coinIcon.transform;
        ccSo.ApplyModifiedPropertiesWithoutUndo();

        var titleRt = UiFactory.Rect("LevelTitle", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1600, 160));
        var titleText = UiFactory.Text("TitleText", titleRt, title, 90, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 160));
        titleRt.gameObject.AddComponent<CanvasGroup>();
        var lt = titleRt.gameObject.AddComponent<LevelTitle>();
        var ltSo = new SerializedObject(lt);
        ltSo.FindProperty("label").objectReferenceValue = titleText;
        ltSo.FindProperty("title").stringValue = title;
        ltSo.ApplyModifiedPropertiesWithoutUndo();

        if (sceneName == SceneNames.Level1)
        {
            UiFactory.Text("ControlsHint", canvas.transform, "A/D or ←/→ move  ·  Space jump (press twice to double jump)  ·  Jump on enemies to defeat them  ·  Esc pause",
                28, new Color(1f, 1f, 1f, 0.85f), new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(1800, 44), TextAlignmentOptions.Center, false);
        }

        if (isFinal && boss != null) BuildBossUi(canvas.transform, boss);
        BuildPauseMenu(canvas.transform);
        UiFactory.EventSystemIfMissing();
    }

    private static void BuildBossUi(Transform canvas, BossEnemy boss)
    {
        var panel = UiFactory.Rect("BossPanel", canvas, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(700, 150));
        UiFactory.Text("BossName", panel, "THE BOAR KING", 54, new Color(1f, 0.55f, 0.45f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(700, 70));
        var hearts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            hearts[i] = UiFactory.Image($"BossHeart{i + 1}", panel, ArtGenerator.Heart, Color.white, new Vector2(0.5f, 1f), new Vector2((i - 1) * 90, -80), new Vector2(72, 72));
            hearts[i].preserveAspect = true;
        }
        var bar = panel.gameObject.AddComponent<BossHealthBar>();
        var barSo = new SerializedObject(bar);
        barSo.FindProperty("boss").objectReferenceValue = boss;
        var heartsProp = barSo.FindProperty("hearts");
        heartsProp.arraySize = hearts.Length;
        for (int i = 0; i < hearts.Length; i++) heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = hearts[i];
        barSo.ApplyModifiedPropertiesWithoutUndo();

        var victory = UiFactory.Stretch("VictoryPanel", canvas);
        var dim = victory.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        var won = UiFactory.Text("VictoryText", victory, "YOU WON!", 220, UiFactory.Gold, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1600, 260));
        var countdown = UiFactory.Text("CountdownText", victory, "Credits in 10...", 52, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(1200, 80));

        var controllerGo = new GameObject("BossFightController");
        var controller = controllerGo.AddComponent<BossFightController>();
        var so = new SerializedObject(controller);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("victoryPanel").objectReferenceValue = victory.gameObject;
        so.FindProperty("victoryText").objectReferenceValue = won;
        so.FindProperty("countdownText").objectReferenceValue = countdown;
        so.FindProperty("bossHealthPanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("creditsDelay").floatValue = 10f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildPauseMenu(Transform canvas)
    {
        var panel = UiFactory.Stretch("PausePanel", canvas);
        var dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        UiFactory.Text("PausedText", panel, "PAUSED", 130, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1000, 160));
        var size = new Vector2(460, 96);
        var resume = UiFactory.Button("ResumeButton", panel, "Resume", new Vector2(0.5f, 0.5f), new Vector2(0, 60), size);
        var restart = UiFactory.Button("RestartButton", panel, "Restart Level", new Vector2(0.5f, 0.5f), new Vector2(0, -60), size);
        var menu = UiFactory.Button("MenuButton", panel, "Main Menu", new Vector2(0.5f, 0.5f), new Vector2(0, -180), size);
        UiFactory.ChainVertical(resume, restart, menu);

        var pause = canvas.gameObject.AddComponent<PauseMenu>();
        var so = new SerializedObject(pause);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("menuButton").objectReferenceValue = menu;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = SceneNames.All
            .Select(s => new EditorBuildSettingsScene($"{ScenesFolder}/{s}.unity", true))
            .ToArray();

        PlayerSettings.companyName = "Ginebro Games";
        PlayerSettings.productName = "Ginebro Quest";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtGenerator.Folder}/GinebroGamesEmblem.png");
        if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
    }

    private static void CleanupBaseProject()
    {
        foreach (var path in new[] { "Assets/Prefabs/Coin.prefab", "Assets/Scripts/Enemy" })
            if (File.Exists(path) || AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
    }
}
