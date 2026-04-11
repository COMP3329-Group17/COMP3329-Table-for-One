using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Unity.Profiling;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR && UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2 && !UNITY_2018_4_3 && !UNITY_2018_4_4
using UnityEditor.TextCore.LowLevel;
#endif

namespace TMPro
{
    public partial class TMP_FontAsset : TMP_Asset
    {
        internal bool TryAddCharacterByGlyphIndexInternal(uint glyphIndex, out TMP_Character character)
        {
            k_TryAddCharacterMarker.Begin();
            character = null;

            // Load font face.
            if (FontEngine.LoadFontFace(sourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
            {
                k_TryAddCharacterMarker.End();
                return false;
            }

            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new TMP_Character(0xFFFFFFFF, this, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);

#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                if (UnityEditor.EditorUtility.IsPersistent(this))
                {
                    TMP_EditorResourceManager.RegisterResourceForUpdate(this);
                }
#endif

                k_TryAddCharacterMarker.End();
                return true;
            }

            Glyph glyph = null;

            // TODO: Potential new feature to explore where selected font assets share the same atlas texture(s).
            // Handling if using Dynamic Textures
            //if (m_IsUsingDynamicTextures)
            //{
            //    if (TMP_DynamicAtlasTextureGroup.AddGlyphToManagedDynamicTexture(this, glyphIndex, m_AtlasPadding, m_AtlasRenderMode, out glyph))
            //    {
            //        // Add new glyph to glyph table.
            //        m_GlyphTable.Add(glyph);
            //        m_GlyphLookupDictionary.Add(glyphIndex, glyph);

            //        // Add new character
            //        character = new TMP_Character(unicode, glyph);
            //        m_CharacterTable.Add(character);
            //        m_CharacterLookupDictionary.Add(unicode, character);

            //        m_GlyphIndexList.Add(glyphIndex);

            //        if (TMP_Settings.getFontFeaturesAtRuntime)
            //        {
            //            if (k_FontAssetsToUpdateLookup.Add(instanceID))
            //                k_FontAssetsToUpdate.Add(this);
            //        }

            //        return true;
            //    }
            //}

            // Make sure atlas texture is readable.
            if (m_AtlasTextures[m_AtlasTextureIndex].isReadable == false)
            {
                Debug.LogWarning(
                    "Unable to add the requested character to font asset [" + this.name +
                    "]'s atlas texture. Please make the texture [" + m_AtlasTextures[m_AtlasTextureIndex].name +
                    "] readable.", m_AtlasTextures[m_AtlasTextureIndex]);

                k_TryAddCharacterMarker.End();
                return false;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width == 0 || m_AtlasTextures[m_AtlasTextureIndex].height == 0)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            // Try adding glyph to local atlas texture
            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit,
                    m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex],
                    out glyph))
            {
                // Update glyph atlas index
                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new TMP_Character(0xFFFFFFFF, this, glyph);
                m_CharacterTable.Add(character);

                m_GlyphIndexList.Add(glyphIndex);
                m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                if (TMP_Settings.getFontFeaturesAtRuntime)
                    RegisterFontAssetForFontFeatureUpdate(this);

#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                if (UnityEditor.EditorUtility.IsPersistent(this))
                {
                    TMP_EditorResourceManager.RegisterResourceForUpdate(this);
                }
#endif

                k_TryAddCharacterMarker.End();
                return true;
            }

            // Add glyph which did not fit in current atlas texture to new atlas texture.
            if (m_IsMultiAtlasTexturesEnabled)
            {
                // Create new atlas texture
                SetupNewAtlasTexture();

                // Try adding glyph to newly created atlas texture
                if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit,
                        m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex],
                        out glyph))
                {
                    // Update glyph atlas index
                    glyph.atlasIndex = m_AtlasTextureIndex;

                    // Add new glyph to glyph table.
                    m_GlyphTable.Add(glyph);
                    m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                    // Add new character
                    character = new TMP_Character(0xFFFFFFFF, this, glyph);
                    m_CharacterTable.Add(character);

                    m_GlyphIndexList.Add(glyphIndex);
                    m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                    if (TMP_Settings.getFontFeaturesAtRuntime)
                        RegisterFontAssetForFontFeatureUpdate(this);

#if UNITY_EDITOR
                    //SortGlyphTable();
                    if (UnityEditor.EditorUtility.IsPersistent(this))
                    {
                        TMP_EditorResourceManager.RegisterResourceForUpdate(this);
                    }
#endif

                    k_TryAddCharacterMarker.End();
                    return true;
                }
            }

            k_TryAddCharacterMarker.End();

            return false;
        }
    }
}