using UnityEngine;

/// <summary>
/// Procedurally builds the whole desert course. Lanes are at x = -3, 0, +3.
/// The run is a chain of segments laid out along +Z, each one introducing a different challenge.
/// </summary>
public static partial class LevelBuilder
{
    public const float LaneWidth = 3f;
    public const float PlatformWidth = 9f;
    public const float Thickness = 2.5f;
    public static readonly Vector3 PlayerStart = new Vector3(0f, 0.05f, 2f);

    static Transform root;      // all gameplay objects
    static float z;             // build cursor

    static float LaneX(int lane) => lane * LaneWidth;

    public static GameObject Build(out float finishZ)
    {
        Random.InitState(20261110);
        var go = new GameObject("Level");
        root = go.transform;
        z = 0f;

        // 1. Warm-up: wide open sand with a trail of coins
        Platform(40f);
        CoinLine(0, 0.9f, 8f, 38f);

        // 2. Cactus field - change lanes
        Platform(46f);
        Cactus(-1, z - 36f); CoinLine(0, 0.9f, z - 40f, z - 30f);
        Cactus(0, z - 28f);  CoinLine(1, 0.9f, z - 33f, z - 23f);
        Cactus(1, z - 20f);  CoinLine(-1, 0.9f, z - 26f, z - 16f);
        Cactus(0, z - 12f); Cactus(-1, z - 12f); CoinLine(1, 0.9f, z - 18f, z - 6f);

        // 3. First gap - jump!
        Gap(5.5f, 0f);
        Platform(14f);
        CoinLine(0, 0.9f, z - 12f, z - 3f);
        // 4. Wooden barriers - jump (or switch lanes where they don't cover everything)
        Platform(34f);
        Barrier(-1, 0, z - 26f);
        Barrier(0, 1, z - 18f);
        Barrier(-1, 1, z - 10f);   // full width: must jump
        CoinLine(1, 0.9f, z - 31f, z - 21f);
        CoinArc(0, 0.9f, 1.9f, z - 12.5f, z - 7.5f, 4);

        // 5. Narrow wooden bridge - stay in the middle lane!
        Bridge(34f);
        CoinLine(0, 0.9f, z - 31f, z - 3f);
        Platform(18f);

        // 6. Rolling boulders
        Platform(56f);
        RollingRock(-1, z - 40f);
        RollingRock(1, z - 30f);
        RollingRock(0, z - 20f);
        RollingRock(-1, z - 8f);
        CoinLine(1, 0.9f, z - 54f, z - 38f);
        CoinLine(0, 0.9f, z - 36f, z - 26f);
        CoinLine(-1, 0.9f, z - 28f, z - 16f);

        // 7. Missing platforms - floating islands of varying height
        float[] heights = { 0f, 0.6f, 1.2f, 0.6f, 0.0f, 0.8f, 0.0f };
        for (int i = 0; i < heights.Length; i++)
        {
            float y0 = i == 0 ? 0f : heights[i - 1];
            Gap(5.5f, y0, heights[i]);
            Island(8f, heights[i]);
            if (i % 2 == 1) Cactus(i % 4 == 1 ? -1 : 1, z - 4f, heights[i]);
            CoinArc(0, Mathf.Max(y0, heights[i]) + 0.9f, 1.7f, z - 8f - 5.5f, z - 8f - 0.5f, 5);
            CoinLine((i % 3) - 1, heights[i] + 0.9f, z - 7f, z - 2f);
        }
        Gap(5.5f, heights[heights.Length - 1], 0f);

        // 8. Sand pits
        SandPits();

        // 9. Rotating obstacles
        Platform(14f);
        Platform(66f);
        Spinner(z - 52f, 0f, -80f, 1.15f, true);
        Spinner(z - 32f, 0f, 95f, 1.35f, true);
        Spinner(z - 12f, 0f, -110f, 1.2f, true);

        // 10. Falling rocks
        Platform(56f);
        FallingRock(0, z - 44f);
        FallingRock(-1, z - 34f);
        FallingRock(1, z - 26f);
        FallingRock(0, z - 16f);
        CoinLine(1, 0.9f, z - 52f, z - 38f);
        CoinLine(-1, 0.9f, z - 22f, z - 8f);

        // 11. Final dash to the temple
        Gap(5.5f, 0f);
        Platform(40f);
        CoinLine(-1, 0.9f, z - 36f, z - 4f);
        CoinLine(1, 0.9f, z - 36f, z - 4f);
        Platform(10f);
        FinishTemple(out finishZ);

        BuildScenery(z);
        return go;
    }

    // ======================================================================= platforms
    /// <summary>Solid ground of the given length at height y, appended at the cursor.</summary>
    static GameObject Platform(float length, float y = 0f, float width = PlatformWidth)
    {
        var p = new GameObject("Platform");
        p.transform.SetParent(root, false);
        float len = length + 0.05f;
        p.transform.localPosition = new Vector3(0f, y, z + length * 0.5f);

        var body = DesertFactory.Box(p.transform, "Body", new Vector3(0f, -Thickness * 0.5f, 0f),
            new Vector3(width, Thickness, len), DesertFactory.Sandstone, true);
        body.name = "Ground";
        DesertFactory.Box(p.transform, "SandCap", new Vector3(0f, -0.13f, 0f),
            new Vector3(width + 0.15f, 0.3f, len + 0.05f), DesertFactory.Sand);
        // carved stripes along the edges
        DesertFactory.Box(p.transform, "TrimL", new Vector3(-width * 0.5f - 0.02f, -0.9f, 0f),
            new Vector3(0.12f, 0.25f, len), DesertFactory.Brown);
        DesertFactory.Box(p.transform, "TrimR", new Vector3(width * 0.5f + 0.02f, -0.9f, 0f),
            new Vector3(0.12f, 0.25f, len), DesertFactory.Brown);

        z += length;
        return p;
    }

    /// <summary>Floating island: a platform with a rocky tapered underside.</summary>
    static void Island(float length, float y)
    {
        var p = Platform(length, y, 8f);
        DesertFactory.MeshObj(MeshUtil.Pyramid(4), p.transform, "Underside", new Vector3(0f, -Thickness, 0f),
            new Vector3(8f * 1.35f, -7f, length * 1.35f), DesertFactory.Brown).transform.localRotation = Quaternion.identity;
    }

    /// <summary>Empty space between platforms, with a quicksand-coloured floor far below and a kill trigger.</summary>
    static void Gap(float length, float yFrom, float yTo = float.NaN)
    {
        if (float.IsNaN(yTo)) yTo = yFrom;
        var g = new GameObject("Gap");
        g.transform.SetParent(root, false);
        g.transform.localPosition = new Vector3(0f, Mathf.Min(yFrom, yTo) - 2.4f, z + length * 0.5f);
        var sand = DesertFactory.Box(g.transform, "FloorSand", Vector3.zero, new Vector3(PlatformWidth, 0.2f, length), DesertFactory.DarkSand);
        KillZone.AddBox(sand, Vector3.zero, new Vector3(1f, 20f, 1f), "You fell into the gap!"); // local size: scaled to 9 x 4 x gap length
        z += length;
    }

    /// <summary>Narrow wooden bridge: only the middle lane is walkable.</summary>
    static void Bridge(float length)
    {
        var b = new GameObject("Bridge");
        b.transform.SetParent(root, false);
        b.transform.localPosition = new Vector3(0f, 0f, z + length * 0.5f);
        const float w = 2.6f;

        // invisible-ish solid walkway
        DesertFactory.Box(b.transform, "Deck", new Vector3(0f, -0.2f, 0f), new Vector3(w, 0.4f, length + 0.05f), DesertFactory.Wood, true);

        // planks
        int n = Mathf.RoundToInt(length / 0.9f);
        for (int i = 0; i < n; i++)
        {
            float pz = -length * 0.5f + (i + 0.5f) * (length / n);
            var c = Color.Lerp(DesertFactory.Wood, DesertFactory.Brown, Random.Range(0f, 0.6f));
            DesertFactory.Box(b.transform, "Plank", new Vector3(0f, 0.02f, pz), new Vector3(w + 0.1f, 0.1f, length / n - 0.08f), c);
        }
        // ropes and posts
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * (w * 0.5f + 0.05f);
            DesertFactory.Box(b.transform, "Rope", new Vector3(x, 1.0f, 0f), new Vector3(0.09f, 0.09f, length), new Color(0.7f, 0.58f, 0.38f));
            for (int i = 0; i <= 6; i++)
            {
                float pz = -length * 0.5f + i * (length / 6f);
                DesertFactory.Box(b.transform, "Post", new Vector3(x, 0.55f, pz), new Vector3(0.18f, 1.2f, 0.18f), DesertFactory.Brown);
            }
        }
        // stone pylons below
        for (int i = 0; i <= 2; i++)
            DesertFactory.Box(b.transform, "Support", new Vector3(0f, -4f, -length * 0.5f + i * length * 0.5f), new Vector3(1.2f, 7f, 1.2f), DesertFactory.Stone);

        z += length;
    }

    // ======================================================================= obstacles
    static void Cactus(int lane, float zPos, float y = 0f)
    {
        var c = DesertFactory.MakeCactus(root, new Vector3(LaneX(lane), y, zPos), 0.8f);
        KillZone.AddBox(c, new Vector3(0f, 1.15f, 0f), new Vector3(1.3f, 2.3f, 1.3f), "You ran into a cactus!");
    }

    /// <summary>Wooden barrier covering lanes laneFrom..laneTo (inclusive). Jump it, or use a free lane.</summary>
    static void Barrier(int laneFrom, int laneTo, float zPos, float y = 0f)
    {
        float cx = (LaneX(laneFrom) + LaneX(laneTo)) * 0.5f;
        float w = (laneTo - laneFrom + 1) * LaneWidth - 0.3f;
        var b = new GameObject("Barrier");
        b.transform.SetParent(root, false);
        b.transform.localPosition = new Vector3(cx, y, zPos);

        for (int s = -1; s <= 1; s += 2)
            DesertFactory.Box(b.transform, "Post", new Vector3(s * w * 0.5f, 0.6f, 0f), new Vector3(0.25f, 1.2f, 0.25f), DesertFactory.Brown);
        DesertFactory.Box(b.transform, "PlankLow", new Vector3(0f, 0.35f, 0f), new Vector3(w, 0.28f, 0.16f), DesertFactory.Wood);
        DesertFactory.Box(b.transform, "PlankHigh", new Vector3(0f, 0.78f, 0f), new Vector3(w, 0.28f, 0.16f), DesertFactory.Wood);
        // warning stripes
        int stripes = Mathf.Max(3, Mathf.RoundToInt(w / 0.9f));
        for (int i = 0; i < stripes; i++)
        {
            float sx = -w * 0.5f + (i + 0.5f) * (w / stripes);
            DesertFactory.Box(b.transform, "Stripe", new Vector3(sx, 0.78f, -0.09f), new Vector3(w / stripes * 0.45f, 0.28f, 0.03f),
                i % 2 == 0 ? DesertFactory.Orange : new Color(0.95f, 0.9f, 0.75f));
        }
        KillZone.AddBox(b, new Vector3(0f, 0.55f, 0f), new Vector3(w, 1.1f, 0.7f), "You hit a wooden barrier!");
    }

    static void RollingRock(int lane, float zPos, float y = 0f)
    {
        global::RollingRock.Create(root, new Vector3(LaneX(lane), y, zPos));
    }

    static void FallingRock(int lane, float zPos, float y = 0f)
    {
        global::FallingRock.Create(root, new Vector3(LaneX(lane), y, zPos));
    }

    /// <summary>
    /// A platform split into lane-wide slabs with pits cut into some lanes.
    /// Pits are 6 m long: jump them or dodge into an intact lane.
    /// </summary>
    static void SandPits()
    {
        const float len = 60f;
        // (startOffset, length, lanes that are pits)
        var pits = new (float start, float length, int[] lanes)[]
        {
            (12f, 6f, new[] { -1, 0 }),
            (26f, 6f, new[] { 0, 1 }),
            (40f, 6f, new[] { -1, 1 }),
            (52f, 6f, new[] { -1, 0, 1 }),
        };

        for (int lane = -1; lane <= 1; lane++)
        {
            float cursor = 0f;
            foreach (var p in pits)
            {
                if (System.Array.IndexOf(p.lanes, lane) < 0) continue;
                if (p.start > cursor) LaneSlab(lane, z + cursor, p.start - cursor);
                SandPit(lane, z + p.start, p.length);
                cursor = p.start + p.length;
            }
            if (cursor < len) LaneSlab(lane, z + cursor, len - cursor);
        }

        // coins: low over safe lanes, arcs over full pits
        CoinLine(1, 0.9f, z + 2f, z + 11f);
        CoinLine(1, 0.9f, z + 13f, z + 24f);
        CoinLine(-1, 0.9f, z + 28f, z + 38f);
        CoinLine(0, 0.9f, z + 42f, z + 50f);
        CoinArc(0, 0.9f, 2.0f, z + 52f, z + 58f, 5);
        z += len;
    }

    static void LaneSlab(int lane, float z0, float length)
    {
        var p = new GameObject("LaneSlab");
        p.transform.SetParent(root, false);
        p.transform.localPosition = new Vector3(LaneX(lane), 0f, z0 + length * 0.5f);
        DesertFactory.Box(p.transform, "Ground", new Vector3(0f, -Thickness * 0.5f, 0f), new Vector3(LaneWidth, Thickness, length + 0.05f), DesertFactory.Sandstone, true);
        DesertFactory.Box(p.transform, "SandCap", new Vector3(0f, -0.13f, 0f), new Vector3(LaneWidth + 0.02f, 0.3f, length + 0.05f), DesertFactory.Sand);
    }

    static void SandPit(int lane, float z0, float length)
    {
        var p = new GameObject("SandPit");
        p.transform.SetParent(root, false);
        p.transform.localPosition = new Vector3(LaneX(lane), 0f, z0 + length * 0.5f);
        // swirling quicksand surface
        var surf = DesertFactory.Box(p.transform, "Quicksand", new Vector3(0f, -1.7f, 0f), new Vector3(LaneWidth, 0.2f, length), new Color(0.80f, 0.52f, 0.22f));
        KillZone.AddBox(surf, new Vector3(0f, 3f, 0f), new Vector3(0.9f, 6f, 0.98f), "You sank in a sand pit!"); // local size: world ~2.7 x 1.2 x pit length
        for (int i = 0; i < 4; i++)
            DesertFactory.Box(p.transform, "Ripple", new Vector3(0f, -1.58f, -length * 0.5f + (i + 0.5f) * length / 4f),
                new Vector3(LaneWidth * 0.8f, 0.03f, 0.18f), DesertFactory.DarkSand);
    }

    /// <summary>Spinning bar sweeping across all three lanes. Jump over it.</summary>
    static void Spinner(float zPos, float y, float degPerSec, float height, bool onExistingGround)
    {
        var hub = new GameObject("Spinner");
        hub.transform.SetParent(root, false);
        hub.transform.localPosition = new Vector3(0f, y, zPos);

        // hub post (touching it is deadly too)
        var post = DesertFactory.Prim(PrimitiveType.Cylinder, hub.transform, "Hub", new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.9f, 0.9f), DesertFactory.Mat(DesertFactory.Stone));
        KillZone.AddBox(post, new Vector3(0f, 0f, 0f), new Vector3(1.1f, 2.0f, 1.1f), "You hit the spinning obstacle!");
        post.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        var arm = new GameObject("Arm");
        arm.transform.SetParent(hub.transform, false);
        arm.transform.localPosition = new Vector3(0f, 0f, 0f);

        const float armLen = 8.6f;
        var bar = DesertFactory.Box(arm.transform, "Bar", new Vector3(0f, height * 0.5f + 0.05f, 0f), new Vector3(armLen, height, 0.45f), DesertFactory.Wood);
        for (int s = -1; s <= 1; s += 2)
        {
            DesertFactory.Box(arm.transform, "Cap", new Vector3(s * armLen * 0.5f, height * 0.5f + 0.05f, 0f), new Vector3(0.6f, height + 0.3f, 0.7f), DesertFactory.Stone);
            for (int i = 0; i < 3; i++)
                DesertFactory.Box(arm.transform, "Band", new Vector3(s * (1.4f + i * 1.9f), height * 0.5f + 0.05f, 0f), new Vector3(0.35f, height + 0.05f, 0.5f), DesertFactory.Orange);
        }
        KillZone.AddBox(arm, new Vector3(0f, height * 0.5f + 0.05f, 0f), new Vector3(armLen, height, 0.5f), "You were hit by a spinning bar!");
        var rot = arm.AddComponent<Rotator>();
        rot.degreesPerSecond = degPerSec;
        rot.axis = Vector3.up;
        arm.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
    }

    // ======================================================================= coins
    static void CoinLine(int lane, float y, float z0, float z1, float spacing = 2.2f)
    {
        for (float cz = z0; cz <= z1 + 0.01f; cz += spacing)
            Coin.Create(root, new Vector3(LaneX(lane), y, cz));
    }

    /// <summary>Arc of coins rising over a jump (peak height above y).</summary>
    static void CoinArc(int lane, float y, float peak, float z0, float z1, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            Coin.Create(root, new Vector3(LaneX(lane), y + Mathf.Sin(t * Mathf.PI) * peak, Mathf.Lerp(z0, z1, t)));
        }
    }
}
