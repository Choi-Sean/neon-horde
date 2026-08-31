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

            // The NORMAL enemy face is read back on the CPU at runtime to derive a "hurt"
            // variant (FaceArt.DeriveHurt) — GetPixels32 needs readable + an uncompressed
            // (RGBA32) copy. Cap it small to keep the APK light. The big face and other
            // art stay compressed (no pixel read-back on them).
            bool isNormalFace = p.IndexOf("enemy_face", StringComparison.OrdinalIgnoreCase) >= 0
                                && p.IndexOf("enemy_face_big", StringComparison.OrdinalIgnoreCase) < 0;
            ti.isReadable = isNormalFace;
            ti.maxTextureSize = isNormalFace ? 512 : 1024;
            ti.textureCompression = isNormalFace
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.Compressed;
        }
    }
}
