using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Platformer2DSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameVerifier
{
    private const string RunningKey = "GameVerifier.Running";
    private const string CaptureFolder = "Captures";

    [MenuItem("Ginebro/4. Capture Screenshots")]
    public static void CaptureScreens()
    {
        Directory.CreateDirectory(CaptureFolder);
        foreach (string name in SceneNames.All)
        {
            var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity", OpenSceneMode.Single);
            var cam = Camera.main;
            foreach (var canvas in Object.FindObjectsByType<Canvas>())
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            foreach (var group in Object.FindObjectsByType<CanvasGroup>()) group.alpha = 1f;
            var pause = GameObject.Find("PausePanel");
            if (pause != null) pause.SetActive(false);
            var victory = GameObject.Find("VictoryPanel");
            if (victory != null) victory.SetActive(false);

            Render(cam, $"{CaptureFolder}/{name}.png");

            if (name.StartsWith("Level"))
            {
                var hud = GameObject.Find("HudCanvas");
                if (hud != null) hud.SetActive(false);
                var follow = cam.GetComponent<Platformer2DSystem.Example.CameraFollow>();
                var so = new SerializedObject(follow);
                Vector2 min = so.FindProperty("minPosition").vector2Value;
                Vector2 max = so.FindProperty("maxPosition").vector2Value;
                var parallax = Object.FindAnyObjectByType<ParallaxController>();
                Vector3 start = cam.transform.position;
                for (int i = 1; i <= 3; i++)
                {
                    float t = i / 3f;
                    Vector3 target = new(Mathf.Lerp(min.x, max.x, t), Mathf.Lerp(start.y, min.y, 0.5f * (i % 2)), -10f);
                    Vector3 delta = target - cam.transform.position;
                    cam.transform.position = target;
                    if (parallax != null)
                        foreach (var layer in parallax.layers)
                            layer.target.position += new Vector3(delta.x * layer.parallaxMultiplier.x, delta.y * layer.parallaxMultiplier.y, 0f);
                    Render(cam, $"{CaptureFolder}/{name}_{i}.png");
                }
                if (victory != null && name == SceneNames.LevelFinal)
                {
                    if (hud != null) hud.SetActive(true);
                    victory.SetActive(true);
                    var boss = GameObject.Find("BossPanel");
                    if (boss != null) boss.SetActive(false);
                    Render(cam, $"{CaptureFolder}/{name}_Victory.png");
                }
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _ = scene;
        }
        Debug.Log("[GameVerifier] Screenshots saved to " + Path.GetFullPath(CaptureFolder));
    }

    private static void Render(Camera cam, string path)
    {
        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        foreach (var scaler in Object.FindObjectsByType<CanvasScaler>()) scaler.enabled = false;
        foreach (var scaler in Object.FindObjectsByType<CanvasScaler>()) scaler.enabled = true;
        Canvas.ForceUpdateCanvases();
        foreach (var t in Object.FindObjectsByType<TMP_Text>()) t.ForceMeshUpdate(true, true);
        foreach (var canvas in Object.FindObjectsByType<Canvas>())
            Debug.Log($"[GameVerifier] {path}: canvas {canvas.name} size={((RectTransform)canvas.transform).rect.size} scale={canvas.transform.localScale.x:0.000} cam={cam.pixelWidth}x{cam.pixelHeight}");
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    private static readonly Stack<IEnumerator> routine = new();
    private static double waitUntil;
    private static readonly StringBuilder report = new();
    private static int failures;

    static GameVerifier()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Ginebro/5. Run Smoke Test (Play Mode)")]
    public static void RunSmokeTest()
    {
        SessionState.SetBool(RunningKey, true);
        EditorSceneManager.OpenScene($"Assets/Scenes/{SceneNames.LevelFinal}.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
        SessionState.SetBool(RunningKey, false);
        report.Clear();
        failures = 0;
        routine.Clear();
        routine.Push(Scenario());
        waitUntil = 0;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || routine.Count == 0) return;
        if (EditorApplication.timeSinceStartup < waitUntil) return;

        while (routine.Count > 0)
        {
            var current = routine.Peek();
            bool more;
            try
            {
                more = current.MoveNext();
            }
            catch (System.Exception e)
            {
                Check(false, "Exception: " + e);
                routine.Clear();
                break;
            }

            if (!more)
            {
                routine.Pop();
                continue;
            }

            if (current.Current is IEnumerator nested)
            {
                routine.Push(nested);
                continue;
            }

            if (current.Current is float seconds) waitUntil = EditorApplication.timeSinceStartup + seconds;
            return;
        }

        EditorApplication.update -= Tick;
        report.AppendLine(failures == 0 ? "RESULT: ALL CHECKS PASSED" : $"RESULT: {failures} CHECK(S) FAILED");
        Directory.CreateDirectory(CaptureFolder);
        File.WriteAllText($"{CaptureFolder}/SmokeTest.txt", report.ToString());
        Debug.Log("[SmokeTest]\n" + report);
        EditorApplication.ExitPlaymode();
        if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) failures++;
        report.AppendLine((ok ? "[PASS] " : "[FAIL] ") + message);
    }

    private static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
    {
        double end = EditorApplication.timeSinceStartup + timeout;
        while (!condition() && EditorApplication.timeSinceStartup < end) yield return null;
    }

    private static IEnumerator LoadScene(string name)
    {
        SceneManager.LoadScene(name);
        yield return null;
        yield return 0.6f;
    }

    private static IEnumerator Stomp(Enemy enemy, float height, float timeout)
    {
        int livesBefore = enemy.Lives;
        PlayerActor.velocity = Vector2.zero;
        PlayerActor.Teleport(enemy.transform.position + new Vector3(0f, height, 0f));
        double end = EditorApplication.timeSinceStartup + timeout;
        while (enemy != null && enemy.Lives == livesBefore && EditorApplication.timeSinceStartup < end)
        {
            var a = PlayerActor;
            if (a.velocity.y <= 0f) a.Teleport(new Vector2(enemy.transform.position.x, a.Position.y));
            yield return null;
        }
    }

    private static Actor PlayerActor => Object.FindAnyObjectByType<PlayerHealth>().GetComponent<Actor>();

    private static IEnumerator Scenario()
    {
        yield return 1f;

        Check(SceneManager.GetActiveScene().name == SceneNames.LevelFinal, "LevelFinal loaded");
        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var boss = Object.FindAnyObjectByType<BossEnemy>();
        Check(player != null && boss != null, "LevelFinal has a player and a boss");
        Check(boss.MaxLives == 3 && boss.Lives == 3, $"Boss starts with 3 lives (lives={boss.Lives})");
        int normalEnemies = Object.FindObjectsByType<Enemy>().Count(e => !(e is BossEnemy));
        Check(normalEnemies > 0, $"LevelFinal has other enemies besides the boss ({normalEnemies})");

        int before = player.CurrentHealth;
        PlayerActor.velocity = Vector2.zero;
        PlayerActor.Teleport(boss.transform.position + new Vector3(0f, 1.2f, 0f));
        yield return 0.15f;
        int bossDamage = before - player.CurrentHealth;
        Check(bossDamage == 2, $"Boss contact damage = {bossDamage} (normal enemies deal 1)");

        PlayerActor.velocity = Vector2.zero;
        PlayerActor.Teleport(new Vector2(30f, -10f));
        yield return 1.5f;
        Check(boss.Lives == 3, $"Boss still has 3 lives before the stomp test (lives={boss.Lives})");

        for (int hit = 1; hit <= 3; hit++)
        {
            player.Heal(99);
            yield return WaitFor(() => !boss.IsInvulnerable && !player.IsInvulnerable, 3f);
            int livesBefore = boss.Lives;
            yield return Stomp(boss, 6f, 3f);
            Check(boss.Lives == livesBefore - 1, $"Stomp #{hit}: boss lives {livesBefore} -> {boss.Lives}");
            yield return 0.2f;
        }

        double defeatedAt = EditorApplication.timeSinceStartup;
        yield return 0.5f;
        var victory = GameObject.Find("VictoryPanel");
        var victoryText = victory != null ? victory.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t => t.name == "VictoryText") : null;
        Check(victory != null && victory.activeInHierarchy, "Victory panel shown after defeating the boss");
        Check(victoryText != null && victoryText.text == "YOU WON!", $"Victory text = \"{victoryText?.text}\"");

        yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Credits, 14f);
        double elapsed = EditorApplication.timeSinceStartup - defeatedAt;
        Check(SceneManager.GetActiveScene().name == SceneNames.Credits && elapsed >= 9.8f && elapsed <= 11.5f,
            $"Credits opened automatically {elapsed:0.0}s after the boss died");

        yield return 0.6f;
        var creditsText = Object.FindObjectsByType<TMP_Text>().Select(t => t.text).ToList();
        Check(creditsText.Any(t => t.Contains(GameBuilder.Author)), "Credits show the author's name");
        var back = GameObject.Find("BackButton")?.GetComponent<Button>();
        Check(back != null, "Credits has a Back button");
        back?.onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.MainMenu, 3f);
        Check(SceneManager.GetActiveScene().name == SceneNames.MainMenu, "Back button opens the main menu");

        yield return 0.6f;
        Check(GameObject.Find("PlayButton") && GameObject.Find("CreditsButton") && GameObject.Find("QuitButton"), "Main menu has Play, Credits and Quit");
        GameObject.Find("PlayButton").GetComponent<Button>().onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Level1, 3f);
        Check(SceneManager.GetActiveScene().name == SceneNames.Level1, "Play opens Level 1");

        yield return 0.8f;
        Check(Object.FindObjectsByType<PatrolEnemy>().Length == 5 && Object.FindObjectsByType<FlyingEnemy>().Length == 0,
            $"Level1: {Object.FindObjectsByType<PatrolEnemy>().Length} normal enemies, {Object.FindObjectsByType<FlyingEnemy>().Length} flying");

        var wallet = Object.FindAnyObjectByType<PlayerWallet>();
        var coin = Object.FindObjectsByType<Coin>().OrderBy(c => Vector2.Distance(c.transform.position, wallet.transform.position)).First();
        PlayerActor.Teleport(coin.transform.position);
        yield return 0.3f;
        var coinLabel = Object.FindObjectsByType<TMP_Text>().FirstOrDefault(t => t.name == "CoinLabel");
        Check(wallet.Coins == 1 && coinLabel != null && coinLabel.text == "x 1", $"Coin collected: wallet={wallet.Coins}, UI=\"{coinLabel?.text}\"");

        player = Object.FindAnyObjectByType<PlayerHealth>();
        var snail = Object.FindObjectsByType<PatrolEnemy>().OrderBy(s => s.transform.position.x).First();
        before = player.CurrentHealth;
        PlayerActor.velocity = Vector2.zero;
        PlayerActor.Teleport(snail.transform.position + new Vector3(0f, -0.1f, 0f));
        yield return 0.3f;
        Check(player.CurrentHealth == before - 1, $"Snail contact damage: health {before} -> {player.CurrentHealth}");
        var slider = GameObject.Find("HealthBar")?.GetComponent<Slider>();
        yield return 0.5f;
        Check(slider != null && slider.value < 0.99f, $"Health bar updated (value={slider?.value:0.00})");

        yield return WaitFor(() => !player.IsInvulnerable, 3f);
        yield return Stomp(snail, 4f, 2f);
        Check(snail == null || !snail.IsAlive, "Jumping on a snail defeats it");
        yield return 0.5f;
        Check(Object.FindObjectsByType<PatrolEnemy>().Length == 4, "Defeated snail removed from the level");

        PlayerActor.Teleport(new Vector2(28.5f, -15f));
        yield return WaitFor(() => Object.FindAnyObjectByType<PlayerHealth>() == null || Object.FindAnyObjectByType<PlayerHealth>().IsDead, 2f);
        Check(Object.FindAnyObjectByType<PlayerHealth>().IsDead, "Falling into a pit kills the player");
        yield return 3f;
        Check(SceneManager.GetActiveScene().name == SceneNames.Level1 && Object.FindObjectsByType<PatrolEnemy>().Length == 5, "Level restarts after dying");

        var portal = Object.FindAnyObjectByType<Portal>();
        PlayerActor.Teleport(portal.transform.position + new Vector3(0f, 1f, 0f));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Level2, 3f);
        Check(SceneManager.GetActiveScene().name == SceneNames.Level2, "Level 1 portal opens Level 2");

        yield return 0.8f;
        Check(Object.FindObjectsByType<PatrolEnemy>().Length == 5 && Object.FindObjectsByType<FlyingEnemy>().Length == 5,
            $"Level2: {Object.FindObjectsByType<PatrolEnemy>().Length} normal enemies, {Object.FindObjectsByType<FlyingEnemy>().Length} flying");
        var bee = Object.FindObjectsByType<FlyingEnemy>().OrderBy(b => b.transform.position.x).First();
        player = Object.FindAnyObjectByType<PlayerHealth>();
        yield return Stomp(bee, 3f, 3f);
        Check(bee == null || !bee.IsAlive, "Jumping on a bee defeats it");

        portal = Object.FindAnyObjectByType<Portal>();
        PlayerActor.Teleport(portal.transform.position + new Vector3(0f, 1f, 0f));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.LevelFinal, 3f);
        Check(SceneManager.GetActiveScene().name == SceneNames.LevelFinal, "Level 2 portal opens the final level");

        yield return LoadScene(SceneNames.Splash);
        Check(GameObject.Find("CompanyName")?.GetComponent<TMP_Text>()?.text == "GINEBRÓ GAMES", "Splash shows the GINEBRÓ GAMES logo");
        yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.MainMenu, 7f);
        Check(SceneManager.GetActiveScene().name == SceneNames.MainMenu, "Splash goes to the main menu");

        var audio = Object.FindAnyObjectByType<AudioManager>();
        var sources = audio != null ? audio.GetComponents<AudioSource>() : new AudioSource[0];
        Check(sources.Any(s => s.loop && s.isPlaying && s.clip != null), $"Background music playing ({sources.FirstOrDefault(s => s.loop)?.clip?.name})");
        Check(audio != null && audio.Jump && audio.Coin && audio.Stomp && audio.Hurt && audio.BossHit && audio.Victory, "Sound effects assigned");
    }
}
