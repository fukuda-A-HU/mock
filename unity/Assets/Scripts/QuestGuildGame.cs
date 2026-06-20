using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A deliberately small, self-contained demo of Kotoba Quest Guild.
/// A production build can replace BuildPlan with an API-backed planner; this version
/// remains playable offline so a WebGL build never has to contain an API secret.
/// </summary>
public sealed class QuestGuildGame : MonoBehaviour
{
    private const float QuestSeconds = 180f;
    private const float WorkSeconds = 18f;

    private string order = "あと3分。私は着替えるから、ゴミは玄関へ。朝ごはんはすぐ食べられるものにして。洗濯物はたたんで、雨だから折りたたみ傘もバッグに入れて。";
    private bool running;
    private float startedAt;
    private readonly List<RobotJob> jobs = new List<RobotJob>();
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    private void OnGUI()
    {
        SetupStyles();
        var scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        var width = Screen.width / scale;
        var height = Screen.height / scale;

        GUI.Box(new Rect(0, 0, width, height), GUIContent.none);
        GUI.Label(new Rect(38, 25, 700, 48), "ことばのクエストギルド", titleStyle);
        GUI.Label(new Rect(40, 75, 700, 28), "朝の司令官 — 日常の言葉で、3体のAIを同時に動かそう", bodyStyle);

        DrawHouse(new Rect(38, 125, width * .56f, height - 165));
        DrawCommandPanel(new Rect(width * .60f, 125, width * .36f, height - 165));
    }

    private void DrawCommandPanel(Rect area)
    {
        GUI.Box(area, "司令官の依頼");
        GUI.Label(new Rect(area.x + 18, area.y + 32, area.width - 36, 45),
            "何を、どこまで、いつまでにしてほしいかを自分の言葉で伝えてください。", bodyStyle);
        order = GUI.TextArea(new Rect(area.x + 18, area.y + 82, area.width - 36, 132), order, bodyStyle);
        if (GUI.Button(new Rect(area.x + 18, area.y + 225, area.width - 36, 42), running ? "依頼を更新して再配分" : "ギルドに依頼する"))
        {
            BuildPlan();
        }

        if (!running)
        {
            GUI.Label(new Rect(area.x + 18, area.y + 282, area.width - 36, 100), "依頼を送ると、ギルドAIがタスクを分解して3体を同時に出撃させます。", bodyStyle);
            return;
        }

        var remaining = Mathf.Max(0, QuestSeconds - (Time.time - startedAt));
        GUI.Label(new Rect(area.x + 18, area.y + 282, area.width - 36, 34), $"残り {TimeSpan.FromSeconds(remaining):mm\\:ss}", titleStyle);
        GUI.Label(new Rect(area.x + 18, area.y + 330, area.width - 36, 28), "冒険ログ: 指示 → 分解 → 担当決定 → 実行", bodyStyle);
        var y = area.y + 365;
        foreach (var job in jobs)
        {
            var state = JobState(job);
            GUI.Label(new Rect(area.x + 18, y, area.width - 36, 48), $"{job.Icon} {job.Robot}\n{state}: {job.Task}", bodyStyle);
            y += 55;
        }

        if (AllComplete())
        {
            GUI.Box(new Rect(area.x + 18, area.yMax - 98, area.width - 36, 78), "QUEST CLEAR\n朝のタスクを3体で並列完了！");
        }
    }

    private void DrawHouse(Rect area)
    {
        GUI.Box(area, "家の見取り図 — 同時出撃中");
        var rooms = new[]
        {
            new Room("キッチン", new Rect(area.x + 28, area.y + 55, area.width * .42f, area.height * .38f), new Color(.98f, .82f, .46f)),
            new Room("ランドリー", new Rect(area.x + area.width * .52f, area.y + 55, area.width * .42f, area.height * .38f), new Color(.48f, .78f, .93f)),
            new Room("玄関 / バッグ", new Rect(area.x + 28, area.y + area.height * .52f, area.width * .90f, area.height * .37f), new Color(.61f, .86f, .62f))
        };
        foreach (var room in rooms)
        {
            var old = GUI.color;
            GUI.color = room.Color;
            GUI.Box(room.Rect, room.Name);
            GUI.color = old;
        }

        foreach (var job in jobs)
        {
            var room = rooms[job.RoomIndex].Rect;
            var p = Mathf.Clamp01((Time.time - startedAt) / WorkSeconds);
            var x = room.x + 18 + (room.width - 76) * p;
            var y = room.y + room.height * .55f;
            GUI.Box(new Rect(x, y, 58, 38), job.Icon + "\n" + job.Robot);
        }
        if (jobs.Count == 0)
            GUI.Label(new Rect(area.x + 25, area.y + area.height * .44f, area.width - 50, 40), "ロボットたちが司令を待っています。", bodyStyle);
    }

    private void BuildPlan()
    {
        jobs.Clear();
        var text = order ?? string.Empty;
        // Keyword extraction is the offline fallback. It intentionally maps natural Japanese,
        // rather than requiring a command syntax or a fixed template.
        var tidy = ContainsAny(text, "ゴミ", "洗濯", "たたん") ? "ゴミを玄関へ、洗濯物をたたむ" : "散らかった物を玄関へ運ぶ";
        var food = ContainsAny(text, "朝食", "ごはん", "食べ") ? "すぐ食べられる朝食を用意" : "90秒トーストを用意";
        var carry = ContainsAny(text, "雨", "傘", "バッグ", "持ち物") ? "折りたたみ傘と社員証をバッグへ" : "出発用バッグを完成";
        jobs.Add(new RobotJob("🧹", "ホコリス", tidy, 2));
        jobs.Add(new RobotJob("🍳", "モグモグ", food, 0));
        jobs.Add(new RobotJob("🦾", "ハコブン", carry, 1));
        startedAt = Time.time;
        running = true;
    }

    private string JobState(RobotJob job)
    {
        if (Time.time - startedAt >= WorkSeconds) return "完了";
        return "実行中";
    }

    private bool AllComplete() => running && Time.time - startedAt >= WorkSeconds;
    private static bool ContainsAny(string text, params string[] words)
    {
        foreach (var word in words) if (text.Contains(word, StringComparison.Ordinal)) return true;
        return false;
    }

    private void SetupStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = Color.white } };
    }

    private readonly struct RobotJob
    {
        public readonly string Icon, Robot, Task;
        public readonly int RoomIndex;
        public RobotJob(string icon, string robot, string task, int roomIndex) { Icon = icon; Robot = robot; Task = task; RoomIndex = roomIndex; }
    }
    private readonly struct Room
    {
        public readonly string Name;
        public readonly Rect Rect;
        public readonly Color Color;
        public Room(string name, Rect rect, Color color) { Name = name; Rect = rect; Color = color; }
    }
}
