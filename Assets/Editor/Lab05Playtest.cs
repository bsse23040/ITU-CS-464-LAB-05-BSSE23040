using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// An automatic play test. It opens Game.unity, enters Play mode and plays like a person would: it puts the ball on
/// coins to collect them and checks what happens, from the first coin to the win and the restart.
/// Results go to the Console as "[Playtest] ...". It also saves a few screenshots to Docs/screenshots.
/// Menu: Lab05 > Run Playtest. You can also start it by creating the file Temp/run_playtest.
/// </summary>
[InitializeOnLoad]
public static class Lab05Playtest
{
    const string KActive = "Lab05Playtest.active";
    static IEnumerator scenario;
    static double waitUntil;
    static int failures, checks;

    static Lab05Playtest()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += WatchTrigger;
        if (SessionState.GetBool(KActive, false) && EditorApplication.isPlaying) StartScenario();
    }

    [MenuItem("Lab05/Run Playtest")]
    public static void Run()
    {
        SessionState.SetBool(KActive, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    static void WatchTrigger()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists("Temp/run_playtest")) { File.Delete("Temp/run_playtest"); Run(); }
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(KActive, false)) return;
        var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.delayCall += () => { var w = EditorWindow.GetWindow(gameView); w.Focus(); w.maximized = true; };
            StartScenario();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(KActive, false);
            EditorApplication.update -= Step;
            var w = EditorWindow.GetWindow(gameView); if (w.maximized) w.maximized = false;
        }
    }

    static void StartScenario()
    {
        failures = checks = 0;
        scenario = Scenario();
        waitUntil = 0;
        EditorApplication.update -= Step; EditorApplication.update += Step;
    }

    static void Step()
    {
        if (scenario == null || EditorApplication.timeSinceStartup < waitUntil) return;
        bool more;
        try { more = scenario.MoveNext(); }
        catch (System.Exception e) { Debug.LogError("[Playtest] exception: " + e); more = false; failures++; }
        if (more)
        {
            if (scenario.Current is float seconds) waitUntil = EditorApplication.timeSinceStartup + seconds;
        }
        else
        {
            scenario = null;
            Debug.Log("[Playtest] " + (failures == 0 ? "ALL " + checks + " CHECKS PASSED" : failures + " OF " + checks + " CHECKS FAILED"));
            EditorApplication.update -= Step;
            EditorApplication.isPlaying = false;
        }
    }

    // ---------------------------------------------------------------- helpers
    static void Check(bool ok, string what)
    {
        checks++;
        if (!ok) failures++;
        Debug.Log("[Playtest] " + (ok ? "ok   " : "FAIL ") + what);
    }

    static T Private<T>(object o, string field)
    {
        return (T)o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
    }

    static GameManager GM { get { return Object.FindFirstObjectByType<GameManager>(); } }
    static string ScoreText() { return Private<TMP_Text>(GM, "scoreText").text; }
    static TMP_Text BonusText() { return Private<TMP_Text>(GM, "bonusText"); }

    static Coin[] CoinsNamed(string prefix)
    {
        return Object.FindObjectsByType<Coin>(FindObjectsSortMode.None).Where(c => c.name.StartsWith(prefix)).ToArray();
    }

    static Transform Ball { get { return Object.FindFirstObjectByType<PlayerController>().transform; } }

    // put the ball on top of a thing, so its trigger fires on the next physics step
    static void MoveBallTo(Vector3 where)
    {
        var rb = Ball.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.position = new Vector3(where.x, 0.5f, where.z);
        rb.transform.position = rb.position;
        Physics.SyncTransforms();
    }

    static void Screenshot(string name)
    {
        Directory.CreateDirectory("Docs/screenshots");
        ScreenCapture.CaptureScreenshot("Docs/screenshots/" + name + ".png");
    }

    // ---------------------------------------------------------------- the scenario
    static IEnumerator Scenario()
    {
        // --- the base game (Lecture 06)
        var spawner = Object.FindFirstObjectByType<Spawner>();
        // the spawner starts after 1 s and adds a coin every 1.5 s (a try is skipped when it lands within 2 m of the ball),
        // so wait until the fifth coin is out instead of guessing a time
        float waited = 0f;
        while (spawner.transform.childCount < 5 && waited < 15f) { yield return 0.5f; waited += 0.5f; }
        Check(spawner.transform.childCount == 5, "the spawner stops at maxCoins: " + spawner.transform.childCount + " coins out (wanted 5)");
        Check(ScoreText() == "Score: 0 / 20", "the score starts at 0: \"" + ScoreText() + "\"");
        Check(!BonusText().gameObject.activeSelf, "the BonusCoins text is hidden at the start");
        Check(CoinsNamed("RedCoin").Length == 0, "there is no red coin at score 0");
        Screenshot("1-playing");

        // --- task 1: one red coin, at a random score from 1 to 10
        int r = Private<int>(GM, "redCoinScore");
        Check(r >= 1 && r <= 10, "the red coin's score was picked in 1..10: " + r);

        // the score is read from the game: the ball can touch two neighbouring coins at once, so counting moves would drift
        while (Private<int>(GM, "score") < r)
        {
            while (spawner.transform.childCount == 0) yield return 0.3f;
            MoveBallTo(spawner.transform.GetChild(0).position);
            yield return 0.35f;
            int now = Private<int>(GM, "score");
            if (now < r) Check(CoinsNamed("RedCoin").Length == 0, "no red coin yet at score " + now + " (it comes at " + r + ")");
        }
        int reached = Private<int>(GM, "score");
        Check(ScoreText() == "Score: " + reached + " / 20", "the text matches the score " + reached + ": \"" + ScoreText() + "\"");
        Check(CoinsNamed("RedCoin").Length == 1, "exactly one red coin appears at score " + r + ": " + CoinsNamed("RedCoin").Length);
        Screenshot("2-red-coin");

        for (int i = 0; i < 2; i++)
        {
            while (spawner.transform.childCount == 0) yield return 0.3f;
            MoveBallTo(spawner.transform.GetChild(0).position);
            yield return 0.35f;
        }
        Check(CoinsNamed("RedCoin").Length == 1, "still only one red coin after two more coins: " + CoinsNamed("RedCoin").Length);

        // --- task 2: collecting the red coin spawns 50 blue coins in a circle around the player
        var red = CoinsNamed("RedCoin")[0];
        MoveBallTo(red.transform.position);
        Vector3 centre = Ball.position;
        yield return 0.4f;
        var blue = CoinsNamed("BlueCoin");
        Check(CoinsNamed("RedCoin").Length == 0, "the red coin is gone once collected");
        Check(blue.Length == 50, "50 blue coins spawn: " + blue.Length);
        float nearest = blue.Min(c => Vector2.Distance(new Vector2(c.transform.position.x, c.transform.position.z), new Vector2(centre.x, centre.z)));
        float farthest = blue.Max(c => Vector2.Distance(new Vector2(c.transform.position.x, c.transform.position.z), new Vector2(centre.x, centre.z)));
        Check(farthest <= 8.01f && nearest >= 1.49f, "they are all in the circle around the player: " + nearest.ToString("0.0") + " m to " + farthest.ToString("0.0") + " m (wanted 1.5 to 8)");
        Check(blue.All(c => Mathf.Abs(c.transform.position.x) <= 9.31f && Mathf.Abs(c.transform.position.z) <= 9.31f), "they are all inside the walls");

        // --- task 3: the hidden BonusCoins text
        Check(BonusText().gameObject.activeSelf, "the BonusCoins text shows up");
        Check(BonusText().text == "BonusCoins: 0/50", "it says 0/50: \"" + BonusText().text + "\"");
        Screenshot("3-blue-coins");
        yield return 0.3f;

        string scoreBefore = ScoreText();
        for (int i = 0; i < 10; i++)
        {
            MoveBallTo(CoinsNamed("BlueCoin")[0].transform.position);
            yield return 0.12f;
        }
        int gone = 50 - CoinsNamed("BlueCoin").Length;
        Check(gone >= 10 && BonusText().text == "BonusCoins: " + gone + "/50", gone + " blue coins are gone and the text agrees: \"" + BonusText().text + "\"");
        Check(ScoreText() == scoreBefore, "blue coins do not change the score: \"" + ScoreText() + "\"");

        while (CoinsNamed("BlueCoin").Length > 0)
        {
            MoveBallTo(CoinsNamed("BlueCoin")[0].transform.position);
            yield return 0.12f;
        }
        Check(BonusText().text == "BonusCoins: 50/50", "all 50 collected: \"" + BonusText().text + "\"");

        // --- back to the base game: collect to the win
        int guard = 0;
        while (Time.timeScale > 0f && guard++ < 80)
        {
            if (spawner.transform.childCount > 0) MoveBallTo(spawner.transform.GetChild(0).position);
            yield return 0.3f;
        }
        var panel = Private<GameObject>(GM, "winPanel");
        Check(panel.activeSelf && Time.timeScale == 0f, "the win panel shows and time is frozen at the target score");
        yield return 0.3f;
        Screenshot("4-win");
        yield return 0.6f;

        // --- restart
        GM.Restart();
        yield return 1f;
        Check(Time.timeScale == 1f, "Restart puts timeScale back to 1");
        Check(ScoreText() == "Score: 0 / 20", "after Restart the score is 0 again: \"" + ScoreText() + "\"");
        Check(!BonusText().gameObject.activeSelf, "after Restart the BonusCoins text is hidden again");
        Check(CoinsNamed("BlueCoin").Length == 0 && CoinsNamed("RedCoin").Length == 0, "after Restart there are no red or blue coins");
    }
}
