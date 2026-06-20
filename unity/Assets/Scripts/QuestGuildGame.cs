using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A deliberately small, self-contained demo of Kotoba Quest Guild.
/// A production build can replace BuildPlan with an API-backed planner; this version
/// remains playable offline so a WebGL build never has to contain an API secret.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class QuestGuildGame : MonoBehaviour
{
    private const float QuestSeconds = 180f;
    private const float WorkSeconds = 18f;
    private const string DefaultOrder =
        "あと3分。私は着替えるから、ゴミは玄関へ。朝ごはんはすぐ食べられるものにして。洗濯物はたたんで、雨だから折りたたみ傘もバッグに入れて。";

    [SerializeField] private VisualTreeAsset uiAsset;

    private string order = DefaultOrder;
    private bool running;
    private float startedAt;
    private readonly List<RobotJob> jobs = new List<RobotJob>();
    private readonly List<Label> robotMarkers = new List<Label>();

    private UIDocument uiDocument;
    private TextField orderInput;
    private Button submitButton;
    private Label hintLabel;
    private Label timerLabel;
    private Label logLabel;
    private Label houseIdleLabel;
    private Label clearBanner;
    private VisualElement jobsList;
    private VisualElement houseMap;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        uiDocument = GetComponent<UIDocument>();

        var panelSettings = Resources.Load<PanelSettings>("UI/QuestGuildPanelSettings");
        if (panelSettings != null)
            uiDocument.panelSettings = panelSettings;
        else if (uiDocument.panelSettings == null)
            uiDocument.panelSettings = CreateFallbackPanelSettings();

        if (uiAsset == null)
            uiAsset = Resources.Load<VisualTreeAsset>("UI/QuestGuild");

        uiDocument.visualTreeAsset = uiAsset;
    }

    private static PanelSettings CreateFallbackPanelSettings()
    {
        var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(1280, 720);
        panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panelSettings.match = 0.5f;
#if UNITY_EDITOR
        panelSettings.themeStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(
            "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
#endif
        return panelSettings;
    }

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        root.style.flexGrow = 1;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);

        orderInput = root.Q<TextField>("order-input");
        submitButton = root.Q<Button>("submit-button");
        hintLabel = root.Q<Label>("hint-label");
        timerLabel = root.Q<Label>("timer-label");
        logLabel = root.Q<Label>("log-label");
        houseIdleLabel = root.Q<Label>("house-idle");
        clearBanner = root.Q<Label>("clear-banner");
        jobsList = root.Q<VisualElement>("jobs-list");
        houseMap = root.Q<VisualElement>("house-map");

        orderInput.value = order;
        orderInput.RegisterValueChangedCallback(evt => order = evt.newValue);
        submitButton.clicked += BuildPlan;
        houseMap.RegisterCallback<GeometryChangedEvent>(_ => UpdateRobotMarkers());
        RefreshUi();
    }

    private void OnDisable()
    {
        if (submitButton != null)
            submitButton.clicked -= BuildPlan;
    }

    private void Update()
    {
        if (!running)
            return;

        UpdateRobotMarkers();
        RefreshStatusPanel();
    }

    private void RefreshUi()
    {
        RefreshStatusPanel();
        UpdateRobotMarkers();
        houseIdleLabel.style.display = jobs.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RefreshStatusPanel()
    {
        submitButton.text = running ? "依頼を更新して再配分" : "ギルドに依頼する";
        hintLabel.style.display = running ? DisplayStyle.None : DisplayStyle.Flex;

        if (!running)
        {
            timerLabel.text = string.Empty;
            logLabel.text = string.Empty;
            jobsList.Clear();
            clearBanner.RemoveFromClassList("clear-banner--visible");
            return;
        }

        var remaining = Mathf.Max(0, QuestSeconds - (Time.time - startedAt));
        timerLabel.text = $"残り {TimeSpan.FromSeconds(remaining):mm\\:ss}";
        logLabel.text = "冒険ログ: 指示 → 分解 → 担当決定 → 実行";

        jobsList.Clear();
        foreach (var job in jobs)
        {
            var state = JobState(job);
            var row = new Label($"{job.Icon} {job.Robot}\n{state}: {job.Task}");
            row.AddToClassList("job-row");
            jobsList.Add(row);
        }

        if (AllComplete())
            clearBanner.AddToClassList("clear-banner--visible");
        else
            clearBanner.RemoveFromClassList("clear-banner--visible");
    }

    private void UpdateRobotMarkers()
    {
        foreach (var marker in robotMarkers)
            marker.RemoveFromHierarchy();
        robotMarkers.Clear();

        if (!running || houseMap == null)
            return;

        var mapWidth = houseMap.resolvedStyle.width;
        var mapHeight = houseMap.resolvedStyle.height;
        if (float.IsNaN(mapWidth) || mapWidth <= 0f || float.IsNaN(mapHeight) || mapHeight <= 0f)
            return;

        foreach (var job in jobs)
        {
            var room = RoomLayout.For(job.RoomIndex);
            var progress = Mathf.Clamp01((Time.time - startedAt) / WorkSeconds);
            var marker = new Label($"{job.Icon}\n{job.Robot}");
            marker.AddToClassList("robot-marker");
            marker.style.left = mapWidth * room.Left + mapWidth * room.Width * 0.05f + mapWidth * room.Width * 0.75f * progress;
            marker.style.top = mapHeight * room.Top + mapHeight * room.Height * 0.45f;
            houseMap.Add(marker);
            robotMarkers.Add(marker);
        }
    }

    private void BuildPlan()
    {
        order = orderInput.value;
        jobs.Clear();
        var text = order ?? string.Empty;
        var tidy = ContainsAny(text, "ゴミ", "洗濯", "たたん") ? "ゴミを玄関へ、洗濯物をたたむ" : "散らかった物を玄関へ運ぶ";
        var food = ContainsAny(text, "朝食", "ごはん", "食べ") ? "すぐ食べられる朝食を用意" : "90秒トーストを用意";
        var carry = ContainsAny(text, "雨", "傘", "バッグ", "持ち物") ? "折りたたみ傘と社員証をバッグへ" : "出発用バッグを完成";
        jobs.Add(new RobotJob("🧹", "ホコリス", tidy, 2));
        jobs.Add(new RobotJob("🍳", "モグモグ", food, 0));
        jobs.Add(new RobotJob("🦾", "ハコブン", carry, 1));
        startedAt = Time.time;
        running = true;
        RefreshUi();
    }

    private string JobState(RobotJob job)
    {
        if (Time.time - startedAt >= WorkSeconds) return "完了";
        return "実行中";
    }

    private bool AllComplete() => running && Time.time - startedAt >= WorkSeconds;

    private static bool ContainsAny(string text, params string[] words)
    {
        foreach (var word in words)
        {
            if (text.Contains(word, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private readonly struct RobotJob
    {
        public readonly string Icon, Robot, Task;
        public readonly int RoomIndex;

        public RobotJob(string icon, string robot, string task, int roomIndex)
        {
            Icon = icon;
            Robot = robot;
            Task = task;
            RoomIndex = roomIndex;
        }
    }

    private readonly struct RoomLayout
    {
        public readonly float Left, Top, Width, Height;

        private RoomLayout(float left, float top, float width, float height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public static RoomLayout For(int roomIndex) => roomIndex switch
        {
            0 => new RoomLayout(0.04f, 0.08f, 0.42f, 0.36f),
            1 => new RoomLayout(0.52f, 0.08f, 0.42f, 0.36f),
            _ => new RoomLayout(0.04f, 0.52f, 0.90f, 0.36f)
        };
    }
}
