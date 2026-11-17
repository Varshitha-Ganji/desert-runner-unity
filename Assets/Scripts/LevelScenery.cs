using UnityEngine;

public static partial class LevelBuilder
{
    const float FloorY = -9f;

    // ======================================================================= finish temple
    static void FinishTemple(out float finishZ)
    {
        float z0 = z;
        Platform(26f);                     // forecourt + hall floor
        float gateZ = z0 + 9f;
        finishZ = gateZ;

        var t = new GameObject("Temple").transform;
        t.SetParent(root, false);
        t.localPosition = new Vector3(0f, 0f, z0);
        var stone = DesertFactory.Stone;
        var sand = DesertFactory.Sandstone;
        var gold = DesertFactory.Gold;

        // Facade: two colossal pillars, lintel, side walls, pyramid roof
        for (int s = -1; s <= 1; s += 2)
        {
            DesertFactory.Box(t, "Pillar", new Vector3(s * 6.2f, 6f, 8f), new Vector3(2.6f, 12f, 2.6f), stone, true);
            DesertFactory.Box(t, "PillarBase", new Vector3(s * 6.2f, 0.6f, 8f), new Vector3(3.4f, 1.2f, 3.4f), sand);
            DesertFactory.Box(t, "PillarCap", new Vector3(s * 6.2f, 12.2f, 8f), new Vector3(3.4f, 0.8f, 3.4f), sand);
            DesertFactory.Box(t, "SideWall", new Vector3(s * 13.5f, 5f, 12f), new Vector3(12f, 10f, 10f), sand, true);
            DesertFactory.Box(t, "Gold Inlay", new Vector3(s * 6.2f, 8f, 6.65f), new Vector3(0.5f, 6f, 0.15f), gold);
            Brazier(t, new Vector3(s * 4.0f, 0f, 5f));
        }
        DesertFactory.Box(t, "Lintel", new Vector3(0f, 12.4f, 8f), new Vector3(18f, 2.2f, 3.2f), sand, true);
        DesertFactory.MeshObj(MeshUtil.Pyramid(4), t, "Roof", new Vector3(0f, 13.5f, 8f), new Vector3(24f, 9f, 8f), stone);
        for (int i = 0; i < 5; i++)
            DesertFactory.Box(t, "Hieroglyph", new Vector3(-6f + i * 3f, 12.4f, 6.35f), new Vector3(1.1f, 1.1f, 0.12f), gold);

        // Interior: dark hall with glowing back wall
        DesertFactory.Box(t, "HallCeiling", new Vector3(0f, 11f, 18f), new Vector3(10f, 1f, 20f), stone);
        DesertFactory.Box(t, "BackWall", new Vector3(0f, 5f, 28f), new Vector3(28f, 12f, 1f), new Color(1f, 0.7f, 0.2f), true);
        for (int s = -1; s <= 1; s += 2)
            DesertFactory.Box(t, "HallWall", new Vector3(s * 5.2f, 5f, 18f), new Vector3(1f, 10f, 20f), DesertFactory.Brown, true);

        // Golden arch right at the finish line + banner
        var arch = new GameObject("FinishArch").transform;
        arch.SetParent(root, false);
        arch.localPosition = new Vector3(0f, 0f, gateZ);
        for (int s = -1; s <= 1; s += 2)
            DesertFactory.Prim(PrimitiveType.Cube, arch, "ArchPost", new Vector3(s * 4.8f, 3.5f, 0f), new Vector3(0.7f, 7f, 0.7f),
                DesertFactory.Mat(gold, 0.8f, 0.5f));
        DesertFactory.Prim(PrimitiveType.Cube, arch, "ArchBeam", new Vector3(0f, 7.1f, 0f), new Vector3(10.4f, 0.8f, 0.8f),
            DesertFactory.Mat(gold, 0.8f, 0.5f));
        AddBannerText(arch, "FINISH", new Vector3(0f, 8.6f, 0f));

        // Checkered finish line on the floor
        const int cols = 12;
        float cw = PlatformWidth / cols;
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < cols; c++)
                DesertFactory.Box(arch, "Check", new Vector3(-PlatformWidth * 0.5f + (c + 0.5f) * cw, 0.03f, -0.5f + r),
                    new Vector3(cw, 0.04f, 1f), (c + r) % 2 == 0 ? Color.white : new Color(0.1f, 0.08f, 0.06f));

        // The trigger that ends the run
        var trig = new GameObject("FinishTrigger");
        trig.transform.SetParent(root, false);
        trig.transform.localPosition = new Vector3(0f, 3f, gateZ);
        var bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(PlatformWidth, 6f, 1.2f);
        var rb = trig.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        trig.AddComponent<FinishGate>();

        // Ancient obelisks lining the forecourt
        for (int i = 0; i < 3; i++)
            for (int s2 = -1; s2 <= 1; s2 += 2)
            {
                float oz = z0 + 0.8f + i * 1.8f;
                DesertFactory.Box(root, "Obelisk", new Vector3(s2 * 4.05f, 1.5f, oz), new Vector3(0.6f, 3f, 0.6f), stone, true);
                DesertFactory.MeshObj(MeshUtil.Pyramid(4), root, "ObeliskTip", new Vector3(s2 * 4.05f, 3f, oz), new Vector3(0.85f, 0.8f, 0.85f), gold);
            }
    }

    static void Brazier(Transform parent, Vector3 pos)
    {
        DesertFactory.Prim(PrimitiveType.Cylinder, parent, "BrazierBase", pos + new Vector3(0, 0.6f, 0), new Vector3(0.6f, 0.6f, 0.6f), DesertFactory.Mat(DesertFactory.Brown));
        DesertFactory.Prim(PrimitiveType.Sphere, parent, "Flame", pos + new Vector3(0, 1.5f, 0), new Vector3(0.7f, 1f, 0.7f),
            DesertFactory.Mat(new Color(1f, 0.55f, 0.1f), 0.2f, 1.6f));
        var l = new GameObject("FlameLight");
        l.transform.SetParent(parent, false);
        l.transform.localPosition = pos + new Vector3(0, 1.8f, 0);
        var light = l.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.25f);
        light.range = 9f;
        light.intensity = 3f;
    }

    static void AddBannerText(Transform parent, string text, Vector3 localPos)
    {
        var go = new GameObject("Banner");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 80;
        tm.characterSize = 0.18f;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.85f, 0.2f);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }

    // ======================================================================= scenery
    static void BuildScenery(float totalZ)
    {
        var s = new GameObject("Scenery").transform;
        s.SetParent(root, false);

        // Endless desert floor far below the elevated course
        DesertFactory.Box(s, "DesertFloor", new Vector3(0f, FloorY - 0.5f, totalZ * 0.5f), new Vector3(1400f, 1f, totalZ + 1400f), DesertFactory.Sand);

        for (float zz = -70f; zz < totalZ + 200f; zz += 18f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                // Sandstone mesas / pillars supporting the elevated path
                if (Random.value < 0.75f)
                {
                    float top = Random.Range(-4.5f, -0.6f);
                    float h = top - FloorY;
                    float w = Random.Range(3f, 6f);
                    float x = side * Random.Range(8.5f, 13f);
                    var col = Random.value < 0.5f ? DesertFactory.Sandstone : DesertFactory.Orange;
                    DesertFactory.Box(s, "Mesa", new Vector3(x, FloorY + h * 0.5f, zz + Random.Range(-5f, 5f)), new Vector3(w, h, Random.Range(4f, 7f)), col);
                    DesertFactory.Box(s, "MesaCap", new Vector3(x, top + 0.2f, zz), new Vector3(w + 0.4f, 0.4f, 7.4f), DesertFactory.Sand);
                }

                // Mid-ground vegetation, rocks and ruins at desert-floor level
                float roll = Random.value;
                float px = side * Random.Range(15f, 30f);
                float pz = zz + Random.Range(-6f, 6f);
                var pos = new Vector3(px, FloorY, pz);
                if (roll < 0.26f) DesertFactory.MakePalm(s, pos, Random.Range(1.4f, 1.9f));
                else if (roll < 0.46f) DesertFactory.MakeCactus(s, pos, Random.Range(2.2f, 3.2f));
                else if (roll < 0.60f) DesertFactory.MakeDeadTree(s, pos, Random.Range(2f, 2.8f));
                else if (roll < 0.80f) DesertFactory.MakeRock(s, pos + Vector3.up * 1.5f, Random.Range(4f, 9f));
                else DesertFactory.MakeRuin(s, pos, Random.Range(1.6f, 2.2f));

                // Dunes
                if (Random.value < 0.55f)
                {
                    var size = new Vector3(Random.Range(40f, 80f), Random.Range(14f, 26f), Random.Range(40f, 80f));
                    DesertFactory.MakeDune(s, new Vector3(side * Random.Range(40f, 85f), FloorY + size.y * 0.12f, zz), size);
                }
            }
        }

        // Ancient pyramids in the middle distance
        for (float zz = 40f; zz < totalZ + 300f; zz += Random.Range(110f, 170f))
        {
            int side = Random.value < 0.5f ? -1 : 1;
            float size = Random.Range(55f, 90f);
            DesertFactory.MakePyramid(s, new Vector3(side * Random.Range(95f, 150f), FloorY, zz), size, size * Random.Range(0.6f, 0.75f));
        }

        // Rocky mountain ranges on the horizon
        for (float zz = -100f; zz < totalZ + 400f; zz += 55f)
            for (int side = -1; side <= 1; side += 2)
                DesertFactory.MakeMountain(s, new Vector3(side * Random.Range(170f, 260f), FloorY, zz + Random.Range(-20f, 20f)),
                    Random.Range(45f, 80f), Random.Range(50f, 110f));

        // A grand pyramid waiting beyond the finish temple
        DesertFactory.MakePyramid(s, new Vector3(0f, FloorY, totalZ + 330f), 180f, 120f);
        DesertFactory.MakeMountain(s, new Vector3(-140f, FloorY, totalZ + 260f), 70f, 90f);
        DesertFactory.MakeMountain(s, new Vector3(150f, FloorY, totalZ + 280f), 80f, 100f);

        try { StaticBatchingUtility.Combine(s.gameObject); } catch { /* batching is only an optimisation */ }
    }
}
