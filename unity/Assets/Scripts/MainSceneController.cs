using UnityEngine;

public sealed class MainSceneController : MonoBehaviour
{
    private Transform beacon;
    private float energy;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;

    private void Start()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.035f, 0.06f, 0.16f);
        RenderSettings.ambientEquatorColor = new Color(0.08f, 0.03f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.01f, 0.01f, 0.03f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.025f, 0.02f, 0.08f);
        RenderSettings.fogDensity = 0.025f;

        CreateCamera();
        CreateLights();
        CreatePlatform();
        CreateBeacon();
        CreateStars();
    }

    private void Update()
    {
        energy = Mathf.Clamp01(energy - Time.deltaTime * 0.08f);
        if (beacon != null)
        {
            beacon.Rotate(12f * Time.deltaTime, 35f * Time.deltaTime, 0f, Space.World);
            beacon.localPosition = new Vector3(0f, 1.9f + Mathf.Sin(Time.time * 1.4f) * 0.12f, 0f);
        }
    }

    private void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 3.4f, -11f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.3f, 0f));
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.008f, 0.055f);
        camera.fieldOfView = 48f;
    }

    private static void CreateLights()
    {
        AddLight("Key Light", new Vector3(-4f, 6f, -4f), new Color(0.25f, 0.75f, 1f), 4.5f, 12f);
        AddLight("Magenta Light", new Vector3(4f, 3f, -1f), new Color(1f, 0.15f, 0.72f), 3.2f, 10f);
        AddLight("Rim Light", new Vector3(0f, 5f, 5f), new Color(0.4f, 0.2f, 1f), 3f, 14f);
    }

    private static void AddLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.position = position;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private static void CreatePlatform()
    {
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Guild Platform";
        platform.transform.localScale = new Vector3(4.2f, 0.2f, 4.2f);
        platform.transform.position = new Vector3(0f, -0.15f, 0f);
        platform.GetComponent<Renderer>().material.color = new Color(0.07f, 0.07f, 0.2f);

        for (var i = 0; i < 12; i++)
        {
            var angle = i * Mathf.PI * 2f / 12f;
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Signal Node";
            pillar.transform.position = new Vector3(Mathf.Cos(angle) * 3.3f, 0.22f, Mathf.Sin(angle) * 3.3f);
            pillar.transform.localScale = new Vector3(0.08f, 0.45f + (i % 3) * 0.18f, 0.08f);
            pillar.GetComponent<Renderer>().material.color = Color.HSVToRGB((0.53f + i * 0.035f) % 1f, 0.65f, 1f);
        }
    }

    private void CreateBeacon()
    {
        var root = new GameObject("Quest Beacon");
        beacon = root.transform;
        beacon.position = new Vector3(0f, 1.9f, 0f);

        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "AI Core";
        core.transform.SetParent(beacon, false);
        core.transform.localScale = Vector3.one * 1.15f;
        core.GetComponent<Renderer>().material.color = new Color(0.15f, 0.8f, 1f);

        for (var i = 0; i < 3; i++)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Orbit Ring";
            ring.transform.SetParent(beacon, false);
            ring.transform.localScale = new Vector3(1.85f + i * 0.32f, 0.025f, 1.85f + i * 0.32f);
            ring.transform.localRotation = Quaternion.Euler(65f + i * 24f, i * 55f, 0f);
            ring.GetComponent<Renderer>().material.color = Color.HSVToRGB(0.52f + i * 0.12f, 0.7f, 1f);
        }
    }

    private static void CreateStars()
    {
        Random.InitState(3006);
        for (var i = 0; i < 110; i++)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Data Star";
            star.transform.position = new Vector3(Random.Range(-16f, 16f), Random.Range(-2f, 10f), Random.Range(2f, 18f));
            var scale = Random.Range(0.015f, 0.055f);
            star.transform.localScale = Vector3.one * scale;
            star.GetComponent<Renderer>().material.color = new Color(0.5f, 0.8f, 1f);
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        var width = Screen.width;
        GUI.Label(new Rect(32, 28, width - 64, 54), "PHYSICAL AI QUEST GUILD", titleStyle);
        GUI.Label(new Rect(35, 86, width - 70, 42), "Build the bridge from imagination to motion.", bodyStyle);

        var panel = new Rect(32, Screen.height - 156, Mathf.Min(530, width - 64), 120);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x + 18, panel.y + 16, panel.width - 36, 26), "QUEST BEACON ONLINE", bodyStyle);
        GUI.Label(new Rect(panel.x + 18, panel.y + 47, panel.width - 36, 24), energy > 0.1f ? "Signal received — prototype energy rising." : "Tap the beacon to begin your first experiment.", bodyStyle);
        if (GUI.Button(new Rect(panel.x + 18, panel.y + 78, 170, 30), "SEND A SIGNAL", buttonStyle))
            energy = 1f;

        if (energy > 0.1f)
        {
            GUI.Label(new Rect(width - 260, Screen.height - 64, 230, 28), "● LINK ESTABLISHED", bodyStyle);
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.45f, 0.9f, 1f) } };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = new Color(0.88f, 0.9f, 1f) } };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
    }
}
