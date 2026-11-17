using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural low-poly meshes (flat shaded) for pyramids, cones and peaks.</summary>
public static class MeshUtil
{
    static readonly Dictionary<int, Mesh> pyramidCache = new Dictionary<int, Mesh>();

    /// <summary>
    /// A flat-shaded pyramid with <paramref name="sides"/> faces. Base radius 0.5, base at y=0, height 1.
    /// sides = 4 gives a classic Egyptian pyramid (rotated so faces point along the axes).
    /// </summary>
    public static Mesh Pyramid(int sides)
    {
        if (pyramidCache.TryGetValue(sides, out var cached)) return cached;

        var verts = new List<Vector3>();
        var tris = new List<int>();
        var apex = new Vector3(0f, 1f, 0f);
        float offset = sides == 4 ? Mathf.PI / 4f : 0f;

        Vector3 Ring(int i)
        {
            float a = offset + i * Mathf.PI * 2f / sides;
            return new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
            // note: sides=4 with +45deg offset gives a square with edges along X/Z
        }

        for (int i = 0; i < sides; i++)
        {
            var a = Ring(i);
            var b = Ring((i + 1) % sides);
            int s = verts.Count;
            verts.Add(a); verts.Add(apex); verts.Add(b);
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
        }
        // base cap
        int c = verts.Count;
        verts.Add(Vector3.zero);
        for (int i = 0; i < sides; i++) verts.Add(Ring(i));
        for (int i = 0; i < sides; i++)
        {
            tris.Add(c);
            tris.Add(c + 1 + i);
            tris.Add(c + 1 + (i + 1) % sides);
        }

        var mesh = new Mesh { name = "Pyramid" + sides };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        pyramidCache[sides] = mesh;
        return mesh;
    }
}
