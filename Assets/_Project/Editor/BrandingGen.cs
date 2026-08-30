using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonHorde.EditorTools
{
    /// <summary>Generates the neon logo mark + app icon PNGs and wires splash / icons.</summary>
    public static class BrandingGen
    {
        const string Dir = "Assets/_Project/Resources/branding";
        const string MarkPath = Dir + "/logo_mark.png";
        const string IconPath = Dir + "/icon_1024.png";

        public static void Apply()
        {
            Directory.CreateDirectory(Dir);
            WritePng(MarkPath, DrawMark(512, transparentBg: true));
            WritePng(IconPath, DrawMark(1024, transparentBg: false));
            AssetDatabase.ImportAsset(MarkPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);

            ConfigureTexture(MarkPath, isSprite: true);
            ConfigureTexture(IconPath, isSprite: false);
            AssetDatabase.Refresh();

            var markSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkPath);
            var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);

            // Splash
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            if (markSprite != null)
            {
                PlayerSettings.SplashScreen.logos = new[]
                {
                    PlayerSettings.SplashScreenLogo.Create(2.5f, markSprite)
                };
            }

            // Icons (Unity 6 API)
            if (iconTex != null)
            {
                var arr = new[] { iconTex };
                try
                {
                    PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, arr, IconKind.Any);
                    PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, arr, IconKind.Application);
                    PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.iOS, arr, IconKind.Application);
                }
                catch (System.Exception e) { Debug.LogWarning("[Branding] icon set skipped: " + e.Message); }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Branding] logo mark + icon generated, splash & icons set.");
        }

        static void ConfigureTexture(string path, bool isSprite)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) return;
            ti.textureType = isSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        static void WritePng(string path, Texture2D tex)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // A "horde burst": bright core, ring, and shapes radiating outward.
        static Texture2D DrawMark(int size, bool transparentBg)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            Color bg = transparentBg ? new Color(0, 0, 0, 0) : new Color(0.02f, 0.02f, 0.05f, 1f);
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            float cx = size * 0.5f, cy = size * 0.5f;
            Color cyan = new Color(0.35f, 1.7f, 2.4f, 1f);
            Color mag = new Color(2.4f, 0.5f, 1.9f, 1f);

            // radiating shapes
            int arms = 12;
            for (int a = 0; a < arms; a++)
            {
                float ang = a / (float)arms * Mathf.PI * 2f;
                for (int ring = 0; ring < 3; ring++)
                {
                    float dist = size * (0.20f + ring * 0.12f);
                    float px0 = cx + Mathf.Cos(ang) * dist;
                    float py0 = cy + Mathf.Sin(ang) * dist;
                    float rad = size * (0.055f - ring * 0.012f);
                    Color col = Color.Lerp(cyan, mag, ring / 2f);
                    Blob(px, size, px0, py0, rad, col, 2.2f);
                }
            }
            // outer ring
            RingStroke(px, size, cx, cy, size * 0.34f, size * 0.018f, cyan * 1.2f);
            // core
            Blob(px, size, cx, cy, size * 0.14f, Color.white * 1.4f, 1.6f);
            Blob(px, size, cx, cy, size * 0.09f, cyan * 2f, 1.2f);

            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static void Blob(Color[] px, int size, float cx, float cy, float r, Color col, float exp)
        {
            int x0 = Mathf.Max(0, (int)(cx - r * 2)), x1 = Mathf.Min(size - 1, (int)(cx + r * 2));
            int y0 = Mathf.Max(0, (int)(cy - r * 2)), y1 = Mathf.Min(size - 1, (int)(cy + r * 2));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d * 0.5f), exp);
                if (a <= 0f) continue;
                int i = y * size + x;
                px[i] = AddClamp(px[i], col, a);
            }
        }

        static void RingStroke(Color[] px, int size, float cx, float cy, float radius, float w, Color col)
        {
            int x0 = Mathf.Max(0, (int)(cx - radius - w * 3)), x1 = Mathf.Min(size - 1, (int)(cx + radius + w * 3));
            int y0 = Mathf.Max(0, (int)(cy - radius - w * 3)), y1 = Mathf.Min(size - 1, (int)(cy + radius + w * 3));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d / (w * 3f)), 2f);
                if (a <= 0f) continue;
                int i = y * size + x;
                px[i] = AddClamp(px[i], col, a);
            }
        }

        static Color AddClamp(Color a, Color b, float k)
        {
            return new Color(
                a.r + b.r * k, a.g + b.g * k, a.b + b.b * k,
                Mathf.Clamp01(a.a + k));
        }
    }
}
