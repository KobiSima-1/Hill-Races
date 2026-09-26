using System.Collections.Generic;
using UnityEngine;

//
[RequireComponent(typeof(EdgeCollider2D))]
public class CourseMeshBuilder : MonoBehaviour
{
    private const string FillSortingLayer = "TerrainFill";
    private const string EdgeSortingLayer = "TerrainEdge";

    [Header("Curve")]
    [Tooltip("How many points are generated between every two authored points.")]
    [SerializeField, Min(1)] private int _samplesPerSegment = 8;
    [Tooltip("Write the smoothed curve back into the collider so physics matches the visuals.")]
    [SerializeField] private bool _applySmoothedCurveToCollider = true;

    [Header("Grass strip")]
    [SerializeField] private Material _grassMaterial;
    [SerializeField, Min(0.05f)] private float _grassThickness = 0.4f;
    [Tooltip("World units of surface covered by one repeat of the grass texture.")]
    [SerializeField, Min(0.1f)] private float _grassTileLength = 1f;

    [Header("Dirt fill")]
    [SerializeField] private Material _dirtMaterial;
    [Tooltip("Local Y the dirt extends down to. Must be below the lowest point of the course.")]
    [SerializeField] private float _floorY = -20f;
    [Tooltip("World units covered by one repeat of the dirt texture, in both directions.")]
    [SerializeField, Min(0.1f)] private float _dirtTileSize = 2.5f;

    private EdgeCollider2D _edge;

    private void Awake()
    {
        _edge = GetComponent<EdgeCollider2D>();

        if (_grassMaterial == null || _dirtMaterial == null)
        {
            Debug.LogError($"{nameof(CourseMeshBuilder)} on '{name}' is missing a material.", this);
            return;
        }

        List<Vector2> curve = BuildSmoothCurve(_edge.points);
        if (_applySmoothedCurveToCollider)
        {
            _edge.SetPoints(curve);
        }

        List<Vector2> normals = ComputeNormals(curve);

        // Fill first, strip second: the strip is drawn on the sorting layer above it anyway.
        CreateMeshObject("DirtFill", BuildFillMesh(curve, normals), _dirtMaterial, FillSortingLayer);
        CreateMeshObject("GrassStrip", BuildStripMesh(curve, normals), _grassMaterial, EdgeSortingLayer);
    }

    // ---------- Curve ----------

    private List<Vector2> BuildSmoothCurve(Vector2[] control)
    {
        var curve = new List<Vector2>();
        if (control.Length < 3)
        {
            curve.AddRange(control);
            return curve;
        }

        int last = control.Length - 1;
        for (int i = 0; i < last; i++)
        {
            Vector2 p1 = control[i];
            Vector2 p2 = control[i + 1];
            // At the two ends there is no neighbour, so mirror the segment to invent one.
            Vector2 p0 = i > 0 ? control[i - 1] : p1 + (p1 - p2);
            Vector2 p3 = i + 2 <= last ? control[i + 2] : p2 + (p2 - p1);

            for (int s = 0; s < _samplesPerSegment; s++)
            {
                float t = s / (float)_samplesPerSegment;
                curve.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        curve.Add(control[last]);
        return curve;
    }

    /// <summary>
    /// Centripetal Catmull-Rom: passes through every authored point, and unlike the
    /// uniform version it never forms loops or cusps where points are unevenly spaced.
    /// </summary>
    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
    {
        float t0 = 0f;
        float t1 = t0 + KnotInterval(p0, p1);
        float t2 = t1 + KnotInterval(p1, p2);
        float t3 = t2 + KnotInterval(p2, p3);
        float t = Mathf.Lerp(t1, t2, u);

        Vector2 a1 = Remap(p0, p1, t0, t1, t);
        Vector2 a2 = Remap(p1, p2, t1, t2, t);
        Vector2 a3 = Remap(p2, p3, t2, t3, t);
        Vector2 b1 = Remap(a1, a2, t0, t2, t);
        Vector2 b2 = Remap(a2, a3, t1, t3, t);
        return Remap(b1, b2, t1, t2, t);
    }

    private static float KnotInterval(Vector2 a, Vector2 b)
    {
        // Square root of the distance is what makes it "centripetal".
        return Mathf.Max(Mathf.Sqrt(Vector2.Distance(a, b)), 0.0001f);
    }

    private static Vector2 Remap(Vector2 a, Vector2 b, float ta, float tb, float t)
    {
        return Vector2.LerpUnclamped(a, b, (t - ta) / (tb - ta));
    }

    private static List<Vector2> ComputeNormals(List<Vector2> curve)
    {
        var normals = new List<Vector2>(curve.Count);
        for (int i = 0; i < curve.Count; i++)
        {
            Vector2 previous = curve[Mathf.Max(i - 1, 0)];
            Vector2 next = curve[Mathf.Min(i + 1, curve.Count - 1)];
            Vector2 tangent = (next - previous).normalized;
            // Rotate the tangent 90° counter-clockwise: for a left-to-right course this points up.
            normals.Add(new Vector2(-tangent.y, tangent.x));
        }
        return normals;
    }

    // ---------- Meshes ----------

    private Mesh BuildStripMesh(List<Vector2> curve, List<Vector2> normals)
    {
        int count = curve.Count;
        var vertices = new Vector3[count * 2];
        var uvs = new Vector2[count * 2];
        float distance = 0f;

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                distance += Vector2.Distance(curve[i - 1], curve[i]);
            }

            // Offset along the normal, not straight down, so the strip keeps its thickness on slopes.
            Vector2 top = curve[i];
            Vector2 bottom = curve[i] - normals[i] * _grassThickness;

            // U follows the distance along the surface, so the texture never stretches on slopes.
            float u = distance / _grassTileLength;
            vertices[i * 2] = top;
            vertices[i * 2 + 1] = bottom;
            uvs[i * 2] = new Vector2(u, 1f);
            uvs[i * 2 + 1] = new Vector2(u, 0f);
        }

        return CreateMesh("GrassStrip", vertices, uvs, count);
    }

    private Mesh BuildFillMesh(List<Vector2> curve, List<Vector2> normals)
    {
        int count = curve.Count;
        var vertices = new Vector3[count * 2];
        var uvs = new Vector2[count * 2];

        for (int i = 0; i < count; i++)
        {
            // Start halfway down the grass strip so no gap can show between the two meshes.
            Vector2 top = curve[i] - normals[i] * (_grassThickness * 0.5f);
            Vector2 bottom = new Vector2(top.x, Mathf.Min(_floorY, top.y));

            vertices[i * 2] = top;
            vertices[i * 2 + 1] = bottom;
            // UVs come from position, so the dirt tiles continuously in both directions.
            uvs[i * 2] = top / _dirtTileSize;
            uvs[i * 2 + 1] = bottom / _dirtTileSize;
        }

        return CreateMesh("DirtFill", vertices, uvs, count);
    }

    /// <summary>
    /// Both meshes share one layout: vertex 2i is on the top edge, 2i+1 on the bottom edge,
    /// and every pair of neighbouring columns becomes one quad (two triangles).
    /// </summary>
    private static Mesh CreateMesh(string meshName, Vector3[] vertices, Vector2[] uvs, int columnCount)
    {
        var triangles = new int[(columnCount - 1) * 6];
        for (int i = 0; i < columnCount - 1; i++)
        {
            int topLeft = i * 2;
            int bottomLeft = i * 2 + 1;
            int topRight = i * 2 + 2;
            int bottomRight = i * 2 + 3;
            int t = i * 6;

            // Clockwise winding = facing the 2D camera.
            triangles[t] = topLeft;
            triangles[t + 1] = topRight;
            triangles[t + 2] = bottomLeft;
            triangles[t + 3] = bottomLeft;
            triangles[t + 4] = topRight;
            triangles[t + 5] = bottomRight;
        }

        // The 2D sprite shaders multiply by vertex colour, so it must be set to white explicitly.
        var colors = new Color[vertices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }

        var mesh = new Mesh { name = meshName };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateMeshObject(string objectName, Mesh mesh, Material material, string sortingLayer)
    {
        var child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        // The collider's Offset shifts its points, so the visuals must be shifted by the same amount.
        child.transform.localPosition = _edge.offset;

        child.AddComponent<MeshFilter>().sharedMesh = mesh;

        var meshRenderer = child.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerName = sortingLayer;
    }
}
