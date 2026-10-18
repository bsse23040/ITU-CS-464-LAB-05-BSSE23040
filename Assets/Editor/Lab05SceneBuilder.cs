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
}
