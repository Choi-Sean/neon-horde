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
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
