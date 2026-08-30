using UnityEngine;
using UnityEngine.Rendering;

namespace NeonHorde
{
    /// <summary>
    /// Shared quad + additive-transparent instanced materials with a neon glow texture.
    /// Backbone of the entity renderers. Additive blend so overlapping glows bloom.
    /// </summary>
    public static class NeonMesh
    {
        static Mesh _quad;

        public static Mesh Quad
        {
            get
            {
                if (_quad != null) return _quad;
                _quad = new Mesh { name = "neon_quad" };
                _quad.SetVertices(new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),   new Vector3(-0.5f, 0.5f, 0f)
                });
                _quad.SetUVs(0, new[]
                {
                    new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(1f, 1f), new Vector2(0f, 1f)
                });
                _quad.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
                _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
                return _quad;
            }
        }

        static Shader _unlit;
        static Shader UnlitShader
        {
            get
            {
                if (_unlit == null)
                {
                    _unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    if (_unlit == null) _unlit = Shader.Find("Sprites/Default");
                }
                return _unlit;
            }
        }

        /// <summary>Glow-textured, GPU-instanced material tinted with an HDR colour. Additive by default.</summary>
        public static Material NewGlow(Color hdrColor, Texture tex = null, bool additive = true)
        {
            var m = new Material(UnlitShader) { name = "neon_glow", enableInstancing = true };
            if (tex == null) tex = NeonArt.Glow(128, 2.0f);

            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);          // transparent
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 1f : 0f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent + 10;

            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            else m.mainTexture = tex;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdrColor);
            else m.color = hdrColor;
            return m;
        }

        public static void RenderInstanced(Material material, Mesh mesh, Matrix4x4[] matrices, int count)
        {
            if (count <= 0 || material == null) return;
            var rp = new RenderParams(material)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000f),
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false
            };
            int i = 0;
            while (i < count)
            {
                int n = Mathf.Min(1023, count - i);
                Graphics.RenderMeshInstanced(rp, mesh, 0, matrices, n, i);
                i += n;
            }
        }
    }
}
