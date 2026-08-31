using System;
using UnityEditor;
using UnityEngine;

namespace NeonHorde.EditorTools
{
    /// <summary>
    /// Any texture dropped into a Resources/art/ folder is auto-imported as a Sprite so
    /// SpriteBank.Get(...) can load it — no manual importer fiddling. Drop
    /// enemy_face.png / ground.png / revive_gag.png in and rebuild.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string p = assetPath.Replace('\\', '/');
            if (p.IndexOf("/Resources/art/", StringComparison.OrdinalIgnoreCase) < 0) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Repeat;   // ground tiles need it; harmless for faces
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 512;               // "너무 크게 만들지말고" + keeps the APK light

            // Normal soldier faces are read back on the CPU at runtime to derive a "hurt"
            // variant (FaceArt.DeriveHurt) — GetPixels32 needs readable + an uncompressed
            // (RGBA32) copy. mid-boss / boss / ground / gag stay compressed.
            string name = System.IO.Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            bool readback = name == "mon1" || name == "mon2" || name == "mon3"
                            || name == "enemy_face" || name == "enemy_face_2" || name == "enemy_face_3";
            ti.isReadable = readback;
            ti.textureCompression = readback
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.Compressed;
        }
    }
}
