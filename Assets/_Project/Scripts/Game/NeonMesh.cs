using UnityEngine;
using UnityEngine.Rendering;

namespace NeonHorde
{
    /// <summary>
    /// Shared quad + instanced unlit-HDR material factory + batched instanced draw.
    /// Backbone of the geometric/neon renderers (enemies, projectiles, gems).
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
                _quad.RecalculateBounds();
                return _quad;
            }
        }

        public static Material NewUnlit(Color hdrColor)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = "neon_unlit", enableInstancing = true };
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
