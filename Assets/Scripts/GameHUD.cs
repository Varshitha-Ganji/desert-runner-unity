using UnityEngine;

/// <summary>
/// Immediate-mode HUD (no Canvas / packages needed): coins, score, best score, progress bar,
/// and the menu / pause / game-over / victory screens.
/// </summary>
public class GameHUD : MonoBehaviour
{
    GameManager gm;
    GUIStyle title, big, mid, small, center;
    Texture2D coinTex;
    float shownScore;
    float stateTime;
    GameState lastState = (GameState)(-1);

    static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
    static readonly Color Cream = new Color(1f, 0.95f, 0.8f);

    void Start()
    {
        gm = GameManager.Instance;
        coinTex = MakeCoinTexture();
    }

    void Update()
    {
        if (gm == null) return;
        if (gm.State != lastState) { lastState = gm.State; stateTime = 0f; }
        stateTime += Time.unscaledDeltaTime;
        shownScore = Mathf.Lerp(shownScore, gm.Score, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        if (gm.State != GameState.Playing && gm.State != GameState.Paused) shownScore = gm.Score;
    }

    // ------------------------------------------------------------------ drawing helpers
    void MakeStyles()
    {
        if (title != null) return;
        GUIStyle S(int size, TextAnchor a, FontStyle f = FontStyle.Bold)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = a, fontStyle = f, wordWrap = false, richText = false };
            st.normal.textColor = Color.white;
            return st;
        }
        title = S(78, TextAnchor.MiddleCenter);
        big = S(44, TextAnchor.MiddleCenter);
        mid = S(28, TextAnchor.MiddleLeft);
        small = S(20, TextAnchor.MiddleCenter, FontStyle.Normal);
        center = S(28, TextAnchor.MiddleCenter);
    }

    static Texture2D MakeCoinTexture()
    {
        const int n = 64;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n / 2f + 0.5f) / (n / 2f), dy = (y - n / 2f + 0.5f) / (n / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color c = Color.clear;
                if (d < 0.97f) c = d > 0.78f ? new Color(0.75f, 0.5f, 0.05f) : Color.Lerp(new Color(1f, 0.9f, 0.4f), new Color(1f, 0.72f, 0.1f), d);
                // simple embossed bar in the middle
                if (d < 0.6f && Mathf.Abs(dx) < 0.12f) c = new Color(0.85f, 0.6f, 0.1f);
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return t;
    }

    static void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Text(Rect r, string s, GUIStyle st, Color c)
    {
        st.normal.textColor = new Color(0f, 0f, 0f, 0.55f * c.a);
        GUI.Label(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    // ------------------------------------------------------------------ OnGUI
    void OnGUI()
    {
        if (gm == null) return;
        MakeStyles();

        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = 720f;

        switch (gm.State)
        {
            case GameState.Menu: DrawMenu(W, H); break;
            case GameState.Playing: DrawHud(W, H); DrawHint(W, H); break;
            case GameState.Paused: DrawHud(W, H); DrawPause(W, H); break;
            case GameState.GameOver: DrawHud(W, H); DrawEnd(W, H, false); break;
            case GameState.Victory: DrawHud(W, H); DrawEnd(W, H, true); break;
        }
    }

    void DrawHud(float W, float H)
    {
        // Coin counter (top-left)
        Fill(new Rect(20, 18, 190, 58), new Color(0.15f, 0.08f, 0.02f, 0.55f));
        GUI.DrawTexture(new Rect(30, 24, 46, 46), coinTex);
        Text(new Rect(86, 18, 120, 58), "x " + gm.Coins, mid, Gold);

        // Score (top-center)
        Fill(new Rect(W / 2f - 160, 14, 320, 76), new Color(0.15f, 0.08f, 0.02f, 0.55f));
        Text(new Rect(W / 2f - 160, 12, 320, 28), "SCORE", small, Cream);
        Text(new Rect(W / 2f - 160, 36, 320, 54), Mathf.RoundToInt(shownScore).ToString("N0"), big, Color.white);

        // Best + distance (top-right)
        Fill(new Rect(W - 250, 18, 230, 58), new Color(0.15f, 0.08f, 0.02f, 0.55f));
        Text(new Rect(W - 250, 16, 230, 28), "BEST  " + Mathf.Max(gm.BestScore, gm.Score).ToString("N0"), small, Gold);
        Text(new Rect(W - 250, 40, 230, 28), Mathf.FloorToInt(gm.Distance) + " m / " + Mathf.FloorToInt(gm.TotalLength) + " m", small, Cream);

        // Progress bar toward the temple
        float bw = Mathf.Min(560f, W - 80f), bx = W / 2f - bw / 2f, by = H - 40f;
        Fill(new Rect(bx - 3, by - 3, bw + 6, 18), new Color(0.15f, 0.08f, 0.02f, 0.6f));
        Fill(new Rect(bx, by, bw, 12), new Color(0.45f, 0.3f, 0.15f, 0.9f));
        Fill(new Rect(bx, by, bw * gm.Progress, 12), Gold);
        Fill(new Rect(bx + bw * gm.Progress - 4, by - 5, 8, 22), Color.white);
        Text(new Rect(bx + bw + 6, by - 10, 90, 32), "TEMPLE", small, Cream);
    }

    void DrawHint(float W, float H)
    {
        if (stateTime > 6f) return;
        float a = Mathf.Clamp01(6f - stateTime);
        Text(new Rect(0, H - 120, W, 40), "A / D  or  Left / Right  -  change lane", small, new Color(1, 1, 1, a));
        Text(new Rect(0, H - 92, W, 40), "Space / W / Up  -  jump  (press again in mid-air to double jump)", small, new Color(1, 1, 1, a));
    }

    void Dim(float W, float H, float a) => Fill(new Rect(0, 0, W, H), new Color(0.12f, 0.06f, 0.02f, a));

    void DrawMenu(float W, float H)
    {
        Dim(W, H, 0.38f);
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2f) * 0.02f;
        var old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), new Vector2(W / 2f, 170f));
        Text(new Rect(0, 110, W, 100), "DESERT RUNNER", title, Gold);
        GUI.matrix = old;
        Text(new Rect(0, 192, W, 40), "Run the ruins. Cross the dunes. Reach the lost temple.", small, Cream);

        Fill(new Rect(W / 2f - 300, 270, 600, 210), new Color(0.15f, 0.08f, 0.02f, 0.65f));
        string[] lines =
        {
            "A / D  or  Left / Right      change lane",
            "Space / W / Up                    jump  (twice = double jump)",
            "Esc / P                                  pause",
            "Collect coins  -  dodge hazards  -  don't fall!",
        };
        for (int i = 0; i < lines.Length; i++)
            Text(new Rect(W / 2f - 280, 285 + i * 40, 560, 36), lines[i], small, i == 3 ? Gold : Color.white);

        Text(new Rect(0, 505, W, 40), "BEST SCORE  " + gm.BestScore.ToString("N0"), center, Gold);
        if (Button(new Rect(W / 2f - 150, 560, 300, 64), "START RUN  (Space)")) gm.StartRun();
    }

    void DrawPause(float W, float H)
    {
        Dim(W, H, 0.5f);
        Text(new Rect(0, 230, W, 80), "PAUSED", title, Gold);
        Text(new Rect(0, 320, W, 36), "Esc / P / Space - resume        R - restart", small, Cream);
        if (Button(new Rect(W / 2f - 150, 380, 300, 56), "RESUME")) gm.Resume();
        if (Button(new Rect(W / 2f - 150, 450, 300, 56), "RESTART")) gm.Restart();
    }

    void DrawEnd(float W, float H, bool win)
    {
        Dim(W, H, 0.55f);
        float appear = Mathf.Clamp01(stateTime * 2.5f);
        var c = new Color(1, 1, 1, appear);
        Text(new Rect(0, 90, W, 90), win ? "TEMPLE REACHED!" : "RUN OVER", title, win ? new Color(1f, 0.85f, 0.2f, appear) : new Color(1f, 0.55f, 0.35f, appear));
        if (!win) Text(new Rect(0, 172, W, 36), gm.DeathReason, small, new Color(1f, 0.95f, 0.8f, appear));

        Fill(new Rect(W / 2f - 270, 225, 540, 270), new Color(0.15f, 0.08f, 0.02f, 0.7f * appear));
        Row(W, 240, "Coins collected", gm.Coins + "  (+" + gm.Coins * GameManager.CoinValue + ")", appear);
        Row(W, 285, "Distance", Mathf.FloorToInt(gm.Distance) + " m  (+" + Mathf.FloorToInt(gm.Distance) + ")", appear);
        Row(W, 330, "Finish bonus", win ? "+" + GameManager.FinishBonus : "-", appear);
        Fill(new Rect(W / 2f - 240, 378, 480, 2), new Color(1f, 0.85f, 0.3f, 0.6f * appear));
        Row(W, 388, "TOTAL SCORE", gm.Score.ToString("N0"), appear, true);
        Row(W, 438, "Best score", gm.BestScore.ToString("N0"), appear);
        if (gm.NewBest)
            Text(new Rect(0, 500, W, 40), "NEW BEST SCORE!", center, new Color(1f, 0.85f, 0.2f, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f)));

        if (Button(new Rect(W / 2f - 150, 560, 300, 64), win ? "PLAY AGAIN  (R)" : "TRY AGAIN  (R)")) gm.Restart();
    }

    void Row(float W, float y, string label, string value, float a, bool strong = false)
    {
        var l = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft, fontSize = strong ? 34 : 26 };
        var r = new GUIStyle(mid) { alignment = TextAnchor.MiddleRight, fontSize = strong ? 34 : 26 };
        Text(new Rect(W / 2f - 240, y, 260, 44), label, l, strong ? new Color(1f, 0.85f, 0.2f, a) : new Color(1f, 0.95f, 0.8f, a));
        Text(new Rect(W / 2f - 20, y, 260, 44), value, r, new Color(1f, 1f, 1f, a));
    }

    bool Button(Rect r, string label)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        Fill(r, hover ? new Color(1f, 0.78f, 0.2f, 0.95f) : new Color(0.85f, 0.55f, 0.12f, 0.92f));
        Fill(new Rect(r.x, r.yMax - 5, r.width, 5), new Color(0.45f, 0.25f, 0.05f, 0.9f));
        Text(r, label, center, hover ? new Color(0.25f, 0.12f, 0.02f) : Color.white);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
}
