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

            // Optional real ground: drop Resources/art/ground.png (tileable, top-down).
            var groundSprite = SpriteBank.Get("ground");
            Texture tex;
            float repeat;
            if (groundSprite != null)
            {
                tex = groundSprite.texture;
                tex.wrapMode = TextureWrapMode.Repeat;
                repeat = coverageTiles / 8f; // each repeat ~8 world units
            }
            else
            {
                tex = BuildLawnTexture(); // "White House South Lawn" vibe for the joke build
                repeat = coverageTiles / 3f;
            }

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", new Vector2(repeat, repeat));
                mat.SetColor("_BaseColor", Color.white);
            }
            else
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = new Vector2(repeat, repeat);
            }
            mr.sharedMaterial = mat;
            mr.sortingOrder = -1000;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            transform.position = new Vector3(0f, 0f, 1f);
        }

        // Mown-lawn texture: green base + alternating lighter/darker mowing stripes + noise.
        static Texture2D BuildLawnTexture()
        {
            const int c = 128;
            var tex = new Texture2D(c, c, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "lawn_tex" };
            var baseCol = new Color(0.10f, 0.30f, 0.12f, 1f);
            var px = new Color[c * c];
            for (int y = 0; y < c; y++)
            for (int x = 0; x < c; x++)
            {
                float stripe = (((x / 16) + (y / 64)) % 2 == 0) ? 1.06f : 0.92f;
                float n = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.12f - 0.06f;
                var col = baseCol * stripe;
                col.r = Mathf.Clamp01(col.r + n * 0.4f);
                col.g = Mathf.Clamp01(col.g + n);
                col.b = Mathf.Clamp01(col.b + n * 0.3f);
                col.a = 1f;
                px[y * c + x] = col;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D BuildTexture()
        {
            const int cell = 64;
            var tex = new Texture2D(cell, cell, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "neon_grid_tex"
            };
            Color line = Palette.GridLine;
            Color fill = Palette.Background;
            var px = new Color[cell * cell];
            for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
            {
                // distance (in px) to nearest grid line (x==0 or y==0)
                float dx = Mathf.Min(x, cell - x);
                float dy = Mathf.Min(y, cell - y);
                float d = Mathf.Min(dx, dy);
                float core = d < 1f ? 1f : 0f;
                float glow = Mathf.Exp(-d * 0.55f) * 0.6f;
                float k = Mathf.Clamp01(core + glow);
                Color c = Color.Lerp(fill, line, k);
                // faint bright node at intersections
                if (dx < 2f && dy < 2f) c = line * 1.6f;
                px[y * cell + x] = c;
            }
            tex.SetPixels(px);
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
