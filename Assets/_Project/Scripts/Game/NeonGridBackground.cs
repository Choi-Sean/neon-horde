using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Infinite scrolling neon grid. A camera-sized quad with a repeat-wrapped line
    /// texture, snapped to the camera in tile-sized steps, sells an endless plane
    /// in a single draw call.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class NeonGridBackground : MonoBehaviour
    {
        [SerializeField] float tileWorldSize = 1f;
        [SerializeField] int coverageTiles = 240;

        Transform _cam;

        void OnEnable()
        {
            Build();
            CacheCamera();
        }

        void CacheCamera()
        {
            var c = Camera.main;
            _cam = c != null ? c.transform : null;
        }

        void Build()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf.sharedMesh == null || mf.sharedMesh.name != "neon_grid_quad")
                mf.sharedMesh = BuildQuad(coverageTiles);

            var mr = GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "neon_grid_mat" };

            var tex = BuildTexture();
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", new Vector2(coverageTiles, coverageTiles));
                mat.SetColor("_BaseColor", Color.white);
            }
            else
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = new Vector2(coverageTiles, coverageTiles);
            }
            mr.sharedMaterial = mat;
            mr.sortingOrder = -1000;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            transform.position = new Vector3(0f, 0f, 1f);
        }

        static Texture2D BuildTexture()
        {
            const int cell = 32;
            var tex = new Texture2D(cell, cell, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "neon_grid_tex"
            };
            Color32 line = Palette.GridLine;
            Color32 fill = Palette.Background;
            var px = new Color32[cell * cell];
            for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
                px[y * cell + x] = (x < 2 || y < 2) ? line : fill;
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Mesh BuildQuad(float size)
        {
            float h = size * 0.5f;
            var mesh = new Mesh { name = "neon_grid_quad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f),
                new Vector3(h, h, 0f), new Vector3(-h, h, 0f)
            });
            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void LateUpdate()
        {
            if (_cam == null)
            {
                CacheCamera();
                if (_cam == null) return;
            }
            Vector3 p = _cam.position;
            transform.position = new Vector3(
                Mathf.Round(p.x / tileWorldSize) * tileWorldSize,
                Mathf.Round(p.y / tileWorldSize) * tileWorldSize,
                1f);
        }
    }
}
