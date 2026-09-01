using System;
using UnityEditor;
using UnityEngine;

namespace NeonHorde.EditorTools
{
    /// <summary>
    /// Textures in Resources/gfx/ are auto-imported as Sprites so SpriteBank can load
    /// them. Weapon sprites (wpn_*) get a left-of-centre pivot so they rotate about the
    /// grip when aimed.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string p = assetPath.Replace('\\', '/');
            if (p.IndexOf("/Resources/gfx/", StringComparison.OrdinalIgnoreCase) < 0) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 512;
            ti.textureCompression = TextureImporterCompression.Compressed;

            string name = System.IO.Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            if (name.StartsWith("wpn_"))
            {
                var tis = new TextureImporterSettings();
                ti.ReadTextureSettings(tis);
                tis.spriteAlignment = (int)SpriteAlignment.Custom;
                tis.spritePivot = new Vector2(0.22f, 0.5f);   // near the grip
                ti.SetTextureSettings(tis);
            }
        }
    }
}
