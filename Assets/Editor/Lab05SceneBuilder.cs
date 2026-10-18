using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        BuildPlayer();
        BuildCoinPrefab();

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
    static void BuildPlayer()
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
    }

    // The coin: a cylinder standing on its edge, with a Sphere Collider that has Is Trigger ticked, and Coin.cs.
    // The root is what spins around Y, so the disc flips like a real coin.
    static GameObject BuildCoinPrefab()
    {
        var root = new GameObject("Coin");
        var trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.6f;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());      // the visual has no collider of its own
        visual.transform.SetParent(root.transform, false);
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        visual.transform.localScale = new Vector3(0.9f, 0.06f, 0.9f);
        visual.GetComponent<Renderer>().sharedMaterial = Mat("Coin", new Color(1f, .78f, .1f));

        root.AddComponent<Coin>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Coin.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }
}
