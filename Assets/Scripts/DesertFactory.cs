using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds every visual in the game from primitives: materials, the explorer character,
/// and all desert scenery (cacti, palms, dead trees, rocks, dunes, pyramids, mountains, ruins).
/// </summary>
public static class DesertFactory
{
    // ---------------------------------------------------------------- palette
    public static readonly Color Sand = new Color(0.94f, 0.79f, 0.46f);
    public static readonly Color DarkSand = new Color(0.82f, 0.58f, 0.28f);
    public static readonly Color Orange = new Color(0.88f, 0.46f, 0.16f);
    public static readonly Color Brown = new Color(0.45f, 0.28f, 0.14f);
    public static readonly Color Stone = new Color(0.74f, 0.62f, 0.46f);
    public static readonly Color Sandstone = new Color(0.78f, 0.55f, 0.32f);
    public static readonly Color Wood = new Color(0.55f, 0.35f, 0.17f);
    public static readonly Color Cactus = new Color(0.30f, 0.52f, 0.28f);
    public static readonly Color Gold = new Color(1f, 0.80f, 0.15f);
    public static readonly Color Sky = new Color(0.52f, 0.77f, 0.95f);
    public static readonly Color Fog = new Color(0.96f, 0.82f, 0.60f);

    // ---------------------------------------------------------------- materials
    static Shader shader;
    static readonly Dictionary<long, Material> cache = new Dictionary<long, Material>();

    static Shader DefaultShader()
    {
        if (shader != null) return shader;
        // Borrow the render pipeline's default lit shader from a primitive: works in Built-in, URP and HDRP.
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shader = tmp.GetComponent<Renderer>().sharedMaterial.shader;
        Object.Destroy(tmp);
        return shader;
    }

    public static Material Mat(Color c, float smoothness = 0.08f, float emission = 0f)
    {
        long key = ((long)Mathf.RoundToInt(c.r * 255) << 40) | ((long)Mathf.RoundToInt(c.g * 255) << 32)
                 | ((long)Mathf.RoundToInt(c.b * 255) << 24) | ((long)Mathf.RoundToInt(smoothness * 100) << 12)
                 | (long)Mathf.RoundToInt(emission * 10);
        if (cache.TryGetValue(key, out var m) && m != null) return m;

        m = new Material(DefaultShader()) { color = c };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", emission > 0f ? 0.6f : 0f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
        }
        cache[key] = m;
        return m;
    }

    // ---------------------------------------------------------------- primitives
    public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale,
                                  Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Color c, bool collider = false)
        => Prim(PrimitiveType.Cube, parent, name, pos, scale, Mat(c), collider);

    public static GameObject MeshObj(Mesh mesh, Transform parent, string name, Vector3 pos, Vector3 scale, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = Mat(c);
        return go;
    }

    // ---------------------------------------------------------------- explorer
    public class ExplorerRig
    {
        public Transform root, legL, legR, armL, armR;
    }

    public static ExplorerRig BuildExplorer(Transform parent)
    {
        var rig = new ExplorerRig();
        var root = new GameObject("Explorer").transform;
        root.SetParent(parent, false);
        rig.root = root;

        var skin = new Color(0.86f, 0.62f, 0.45f);
        var shirt = new Color(0.93f, 0.90f, 0.80f);
        var pants = new Color(0.40f, 0.30f, 0.18f);
        var scarf = new Color(0.78f, 0.15f, 0.12f);

        Prim(PrimitiveType.Cube, root, "Torso", new Vector3(0, 1.15f, 0), new Vector3(0.62f, 0.75f, 0.36f), Mat(shirt));
        Prim(PrimitiveType.Sphere, root, "Head", new Vector3(0, 1.78f, 0), new Vector3(0.44f, 0.46f, 0.44f), Mat(skin));
        // Explorer hat: brim + crown
        Prim(PrimitiveType.Cylinder, root, "HatBrim", new Vector3(0, 1.97f, 0), new Vector3(0.80f, 0.02f, 0.80f), Mat(Brown));
        Prim(PrimitiveType.Cylinder, root, "HatTop", new Vector3(0, 2.07f, 0), new Vector3(0.44f, 0.10f, 0.44f), Mat(Brown));
        Prim(PrimitiveType.Cube, root, "Scarf", new Vector3(0, 1.55f, -0.02f), new Vector3(0.50f, 0.12f, 0.42f), Mat(scarf));
        Prim(PrimitiveType.Cube, root, "Backpack", new Vector3(0, 1.15f, -0.27f), new Vector3(0.42f, 0.50f, 0.18f), Mat(Wood));

        rig.legL = Limb(root, "LegL", new Vector3(-0.16f, 0.78f, 0), new Vector3(0.22f, 0.78f, 0.24f), pants);
        rig.legR = Limb(root, "LegR", new Vector3(0.16f, 0.78f, 0), new Vector3(0.22f, 0.78f, 0.24f), pants);
        rig.armL = Limb(root, "ArmL", new Vector3(-0.42f, 1.45f, 0), new Vector3(0.17f, 0.66f, 0.19f), skin);
        rig.armR = Limb(root, "ArmR", new Vector3(0.42f, 1.45f, 0), new Vector3(0.17f, 0.66f, 0.19f), skin);
        return rig;
    }

    // A limb pivots from its top: pivot object + a cube hanging below it.
    static Transform Limb(Transform parent, string name, Vector3 pivotPos, Vector3 size, Color c)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = pivotPos;
        Prim(PrimitiveType.Cube, pivot, "Mesh", new Vector3(0, -size.y * 0.5f, 0), size, Mat(c));
        return pivot;
    }

    // ---------------------------------------------------------------- scenery
    public static GameObject MakeCactus(Transform parent, Vector3 pos, float scale = 1f, bool collide = false)
    {
        var root = new GameObject("Cactus").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        root.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        root.localScale = Vector3.one * scale;
        var m = Mat(Cactus, 0.15f);
        var dark = Mat(new Color(0.22f, 0.40f, 0.21f), 0.15f);

        float h = 2.4f;
        Prim(PrimitiveType.Capsule, root, "Trunk", new Vector3(0, h * 0.5f, 0), new Vector3(0.7f, h * 0.5f, 0.7f), m);
        // two arms
        Arm(root, new Vector3(0.35f, 1.1f, 0), 1f, m);
        Arm(root, new Vector3(-0.35f, 1.6f, 0), -1f, dark);
        // spines (tiny pale cubes)
        for (int i = 0; i < 8; i++)
        {
            var s = Prim(PrimitiveType.Cube, root, "Spine", new Vector3(0, 0.4f + i * 0.28f, 0), new Vector3(0.04f, 0.04f, 0.9f), Mat(new Color(0.95f, 0.92f, 0.75f)));
            s.transform.localRotation = Quaternion.Euler(0, i * 45f, 0);
        }
        return root.gameObject;
    }

    static void Arm(Transform root, Vector3 at, float side, Material m)
    {
        Prim(PrimitiveType.Capsule, root, "ArmH", at + new Vector3(0.3f * side, 0, 0), new Vector3(0.28f, 0.28f, 0.28f), m)
            .transform.localRotation = Quaternion.Euler(0, 0, 90);
        Prim(PrimitiveType.Capsule, root, "ArmV", at + new Vector3(0.62f * side, 0.42f, 0), new Vector3(0.28f, 0.42f, 0.28f), m);
    }

    public static GameObject MakePalm(Transform parent, Vector3 pos, float scale = 1f)
    {
        var root = new GameObject("Palm").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        root.localScale = Vector3.one * scale;
        root.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        var trunkMat = Mat(new Color(0.58f, 0.40f, 0.22f));
        var leafMat = Mat(new Color(0.34f, 0.58f, 0.24f), 0.2f);
        float lean = Random.Range(-8f, 8f);
        var segs = 5;
        var p = Vector3.zero;
        float ang = lean;
        for (int i = 0; i < segs; i++)
        {
            float len = 1.1f;
            var seg = Prim(PrimitiveType.Cylinder, root, "Trunk", p + new Vector3(0, len * 0.5f, 0) * 0f, new Vector3(0.42f - i * 0.03f, len * 0.5f, 0.42f - i * 0.03f), trunkMat);
            seg.transform.localRotation = Quaternion.Euler(0, 0, ang);
            var dir = seg.transform.localRotation * Vector3.up;
            seg.transform.localPosition = p + dir * len * 0.5f;
            p += dir * len;
            ang += lean * 0.25f;
        }
        for (int i = 0; i < 7; i++)
        {
            var frond = Prim(PrimitiveType.Cube, root, "Frond", p, new Vector3(0.45f, 0.06f, 2.6f), leafMat);
            frond.transform.localRotation = Quaternion.Euler(Random.Range(10f, 35f), i * (360f / 7f), 0);
            frond.transform.localPosition = p + frond.transform.localRotation * new Vector3(0, 0, 1.2f);
        }
        Prim(PrimitiveType.Sphere, root, "Coconuts", p + new Vector3(0, -0.15f, 0), Vector3.one * 0.5f, Mat(Brown));
        return root.gameObject;
    }

    public static GameObject MakeDeadTree(Transform parent, Vector3 pos, float scale = 1f)
    {
        var root = new GameObject("DeadTree").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        root.localScale = Vector3.one * scale;
        root.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        var m = Mat(new Color(0.40f, 0.30f, 0.22f));
        Prim(PrimitiveType.Cylinder, root, "Trunk", new Vector3(0, 1.4f, 0), new Vector3(0.35f, 1.4f, 0.35f), m);
        for (int i = 0; i < 4; i++)
        {
            var b = Prim(PrimitiveType.Cylinder, root, "Branch", Vector3.zero, new Vector3(0.14f, 0.9f, 0.14f), m);
            b.transform.localRotation = Quaternion.Euler(0, i * 90f + 20f, Random.Range(35f, 60f));
            b.transform.localPosition = new Vector3(0, 1.8f + i * 0.3f, 0) + b.transform.localRotation * new Vector3(0, 0.8f, 0);
        }
        return root.gameObject;
    }

    public static GameObject MakeRock(Transform parent, Vector3 pos, float size, bool collide = false)
    {
        var go = Prim(PrimitiveType.Sphere, parent, "Rock", pos,
            new Vector3(size * Random.Range(0.9f, 1.3f), size * Random.Range(0.6f, 0.9f), size * Random.Range(0.9f, 1.3f)),
            Mat(Color.Lerp(Stone, Brown, Random.Range(0.1f, 0.5f)), 0.05f), collide);
        go.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
        return go;
    }

    public static GameObject MakeDune(Transform parent, Vector3 pos, Vector3 size)
    {
        var c = Color.Lerp(Sand, DarkSand, Random.Range(0.0f, 0.45f));
        return Prim(PrimitiveType.Sphere, parent, "Dune", pos, size, Mat(c, 0.02f));
    }

    public static GameObject MakePyramid(Transform parent, Vector3 pos, float baseSize, float height)
    {
        var root = new GameObject("Pyramid");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        MeshObj(MeshUtil.Pyramid(4), root.transform, "Body", Vector3.zero, new Vector3(baseSize * 1.414f, height, baseSize * 1.414f), Stone);
        // weathered stepped base blocks
        Box(root.transform, "Plinth", new Vector3(0, 0.6f, 0), new Vector3(baseSize * 1.25f, 1.2f, baseSize * 1.25f), Sandstone);
        return root;
    }

    public static GameObject MakeMountain(Transform parent, Vector3 pos, float radius, float height)
    {
        var c = Color.Lerp(DarkSand, Brown, Random.Range(0.2f, 0.7f));
        var go = MeshObj(MeshUtil.Pyramid(7), parent, "Mountain", pos, new Vector3(radius * 2f, height, radius * 2f), c);
        go.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        return go;
    }

    public static GameObject MakeRuin(Transform parent, Vector3 pos, float scale = 1f)
    {
        var root = new GameObject("Ruin").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        root.localScale = Vector3.one * scale;
        root.localRotation = Quaternion.Euler(0, Random.Range(-25f, 25f), 0);

        Box(root, "Base", new Vector3(0, 0.25f, 0), new Vector3(7f, 0.5f, 4f), Stone);
        for (int i = 0; i < 4; i++)
        {
            float x = -2.6f + i * 1.75f;
            float h = i == 1 || i == 3 ? Random.Range(1.5f, 2.5f) : Random.Range(3.5f, 4.5f);
            Box(root, "Column", new Vector3(x, 0.5f + h * 0.5f, 0), new Vector3(0.8f, h, 0.8f), Stone);
            if (h > 3f) Box(root, "Capital", new Vector3(x, 0.5f + h + 0.15f, 0), new Vector3(1.1f, 0.3f, 1.1f), Sandstone);
        }
        var lintel = Box(root, "Lintel", new Vector3(-1.7f, 4.9f, 0), new Vector3(2.8f, 0.5f, 1.0f), Sandstone);
        lintel.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-4f, 4f));
        // rubble
        for (int i = 0; i < 4; i++)
        {
            var r = Box(root, "Rubble", new Vector3(Random.Range(-3f, 3f), 0.8f, Random.Range(-1.8f, 1.8f)),
                new Vector3(Random.Range(0.4f, 0.9f), 0.5f, Random.Range(0.4f, 0.9f)), Stone);
            r.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 90f), Random.Range(-15f, 15f));
        }
        return root.gameObject;
    }
}
