using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds Assets/Scenes/Game.unity and the prefabs it uses, so the scene can be rebuilt from code.
/// Menu: Lab05 > Build Game Scene
/// </summary>
public static class Lab05SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("Lab05/Build Game Scene")]
    public static void Build()
    {
        EnsureFolder("Assets", "Scenes"); EnsureFolder("Assets", "Materials"); EnsureFolder("Assets", "Prefabs");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        BuildArena();
        var ball = BuildPlayer();
        var coin = BuildCoinPrefab("Coin", new Color(1f, .78f, .1f), 1f, CoinKind.Normal);
        var redCoin = BuildCoinPrefab("RedCoin", new Color(.95f, .12f, .12f), 1.3f, CoinKind.Red);
        var blueCoin = BuildCoinPrefab("BlueCoin", new Color(.15f, .45f, 1f), 0.9f, CoinKind.Blue);
        var spawner = BuildSpawner(coin, redCoin, blueCoin, ball.transform);
        BuildUIAndGameManager(spawner);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Lab05] built " + ScenePath);
    }

    // ---------------------------------------------------------------- helpers
    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    static Material Mat(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Box(string name, Vector3 pos, Vector3 scale, Material m, Transform parent = null)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = pos; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    // ---------------------------------------------------------------- the scene
    // A 20 m x 20 m floor with four low walls, and the camera looking down at it.
    static void BuildArena()
    {
        var floor = Mat("Floor", new Color(.18f, .20f, .26f));
        var wall = Mat("Wall", new Color(.45f, .48f, .56f));
        var arena = new GameObject("Arena").transform;

        Box("Floor", new Vector3(0, -.5f, 0), new Vector3(20, 1, 20), floor, arena);
        Box("Wall North", new Vector3(0, .75f, 10.5f), new Vector3(22, 1.5f, 1), wall, arena);
        Box("Wall South", new Vector3(0, .75f, -10.5f), new Vector3(22, 1.5f, 1), wall, arena);
        Box("Wall East", new Vector3(10.5f, .75f, 0), new Vector3(1, 1.5f, 22), wall, arena);
        Box("Wall West", new Vector3(-10.5f, .75f, 0), new Vector3(1, 1.5f, 22), wall, arena);

        var cam = Camera.main;
        cam.transform.position = new Vector3(0, 15f, -12f);
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
    }

    // The ball: a Sphere with a Rigidbody, tagged Player, with PlayerController.
    static GameObject BuildPlayer()
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Player";
        ball.tag = "Player";
        ball.transform.position = new Vector3(0, .5f, 0);
        ball.GetComponent<Renderer>().sharedMaterial = Mat("Ball", new Color(.95f, .95f, .95f));

        var rb = ball.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 1.2f;                                    // gives a steady push a top speed
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        ball.AddComponent<PlayerController>();
        return ball;
    }

    // A coin: a cylinder standing on its edge, with a Sphere Collider that has Is Trigger ticked, and Coin.cs.
    // The root is what spins around Y, so the disc flips like a real coin.
    static GameObject BuildCoinPrefab(string name, Color color, float size, CoinKind kind)
    {
        var root = new GameObject(name);
        var trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.6f * size;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());      // the visual has no collider of its own
        visual.transform.SetParent(root.transform, false);
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        visual.transform.localScale = new Vector3(0.9f * size, 0.06f * size, 0.9f * size);
        visual.GetComponent<Renderer>().sharedMaterial = Mat(name, color);

        var coinScript = root.AddComponent<Coin>();
        var coinSo = new SerializedObject(coinScript);
        coinSo.FindProperty("kind").enumValueIndex = (int)kind;
        coinSo.ApplyModifiedPropertiesWithoutUndo();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // An empty Spawner in the middle of the arena, with Spawner.cs.
    static Spawner BuildSpawner(GameObject coinPrefab, GameObject redCoinPrefab, GameObject blueCoinPrefab, Transform player)
    {
        var go = new GameObject("Spawner");
        var spawner = go.AddComponent<Spawner>();
        var so = new SerializedObject(spawner);
        so.FindProperty("coinPrefab").objectReferenceValue = coinPrefab;
        so.FindProperty("redCoinPrefab").objectReferenceValue = redCoinPrefab;
        so.FindProperty("blueCoinPrefab").objectReferenceValue = blueCoinPrefab;
        so.FindProperty("player").objectReferenceValue = player;
        so.ApplyModifiedPropertiesWithoutUndo();
        return spawner;
    }

    // ---------------------------------------------------------------- UI and the GameManager
    static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, TextAlignmentOptions align,
        Vector2 anchor, Vector2 pos, Vector2 box, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = box;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
        return t;
    }

    // A Canvas with the score text and a win panel (starts switched off) with a Restart button,
    // and a GameManager that owns the score.
    static void BuildUIAndGameManager(Spawner spawner)
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var scoreText = MakeText("Score Text", canvasGo.transform, "Score: 0 / 20", 56, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(40, -30), new Vector2(900, 90), Color.white);

        // the bonus text: switched off in the scene, and shown when the red coin is collected
        var bonusText = MakeText("Bonus Text", canvasGo.transform, "BonusCoins: 0/50", 56, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(40, -105), new Vector2(900, 90), new Color(.35f, .65f, 1f));
        bonusText.gameObject.SetActive(false);

        // the win panel: a dark layer over everything, a message and a button
        var panel = new GameObject("Win Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, .75f);

        MakeText("Win Text", panel.transform, "YOU WIN!", 120, TextAlignmentOptions.Center,
            new Vector2(.5f, .5f), new Vector2(-600, 80), new Vector2(1200, 160), Color.white);

        var buttonGo = new GameObject("Restart Button", typeof(RectTransform));
        buttonGo.transform.SetParent(panel.transform, false);
        var brt = (RectTransform)buttonGo.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(.5f, .5f);
        brt.anchoredPosition = new Vector2(0, -110); brt.sizeDelta = new Vector2(420, 120);
        buttonGo.AddComponent<Image>().color = new Color(.2f, .55f, .95f);
        var button = buttonGo.AddComponent<Button>();
        MakeText("Label", buttonGo.transform, "Restart", 64, TextAlignmentOptions.Center,
            new Vector2(.5f, .5f), new Vector2(-210, 40), new Vector2(420, 80), Color.white);
        panel.SetActive(false);

        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var so = new SerializedObject(gm);
        so.FindProperty("scoreText").objectReferenceValue = scoreText;
        so.FindProperty("winPanel").objectReferenceValue = panel;
        so.FindProperty("spawner").objectReferenceValue = spawner;
        so.FindProperty("bonusText").objectReferenceValue = bonusText;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(button.onClick, gm.Restart);   // the button calls GameManager.Restart
    }
}
