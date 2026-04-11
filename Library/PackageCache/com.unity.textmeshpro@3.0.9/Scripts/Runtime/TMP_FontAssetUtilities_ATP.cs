using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro
{
    public partial class TMP_FontAssetUtilities
    {
        public static TMP_Character GetCharacterByGlyphIndexFromFontAsset(uint glyphIndex, TMP_FontAsset sourceFontAsset,  bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface)
        {
            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            return GetCharacterByGlyphIndexFromFontAsset_Internal(glyphIndex, sourceFontAsset, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface);
        }
        
        private static TMP_Character GetCharacterByGlyphIndexFromFontAsset_Internal(uint glyphIndex, TMP_FontAsset sourceFontAsset,  bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface)
        {
            isAlternativeTypeface = false;
            TMP_Character character = null;
            Glyph glyph = null;
            #region FONT WEIGHT AND FONT STYLE HANDLING
            // Determine if a font weight or style is used. If so check if an alternative typeface is assigned for the given weight and / or style.
            bool isItalic = (fontStyle & FontStyles.Italic) == FontStyles.Italic;

            if (isItalic || fontWeight != FontWeight.Regular)
            {
                // Get reference to the font weight pairs of the given font asset.
                TMP_FontWeightPair[] fontWeights = sourceFontAsset.fontWeightTable;

                int fontWeightIndex = 4;
                switch (fontWeight)
                {
                    case FontWeight.Thin:
                        fontWeightIndex = 1;
                        break;
                    case FontWeight.ExtraLight:
                        fontWeightIndex = 2;
                        break;
                    case FontWeight.Light:
                        fontWeightIndex = 3;
                        break;
                    case FontWeight.Regular:
                        fontWeightIndex = 4;
                        break;
                    case FontWeight.Medium:
                        fontWeightIndex = 5;
                        break;
                    case FontWeight.SemiBold:
                        fontWeightIndex = 6;
                        break;
                    case FontWeight.Bold:
                        fontWeightIndex = 7;
                        break;
                    case FontWeight.Heavy:
                        fontWeightIndex = 8;
                        break;
                    case FontWeight.Black:
                        fontWeightIndex = 9;
                        break;
                }

                TMP_FontAsset temp = isItalic ? fontWeights[fontWeightIndex].italicTypeface : fontWeights[fontWeightIndex].regularTypeface;

                if (temp != null)
                {
                    if (temp.glyphLookupTable.TryGetValue(glyphIndex, out glyph))
                    {
                        isAlternativeTypeface = true;
                        character = new TMP_Character(0xFFFFFFFF, temp, glyph);
                        return character;
                    }
                    
                    if (temp.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    {
                        if (temp.TryAddCharacterByGlyphIndexInternal(glyphIndex, out character))
                        {
                            isAlternativeTypeface = true;

                            return character;
                        }

                        // Check if the source font file contains the requested character.
                        //if (TryGetCharacterFromFontFile(unicode, fontAsset, out characterData))
                        //{
                        //    isAlternativeTypeface = true;

                        //    return characterData;
                        //}

                        // If we find the requested character, we add it to the font asset character table
                        // and return its character data.
                        // We also add this character to the list of characters we will need to add to the font atlas.
                        // We assume the font atlas has room otherwise this font asset should not be marked as dynamic.
                        // Alternatively, we could also add multiple pages of font atlas textures (feature consideration).
                    }

                    // At this point, we were not able to find the requested character in the alternative typeface
                    // so we check the source font asset and its potential fallbacks.
                }

            }
            #endregion

            // Search the source font asset for the requested character.
            if (sourceFontAsset.glyphLookupTable.TryGetValue(glyphIndex, out glyph))
            {
                character = new TMP_Character(0xFFFFFFFF, sourceFontAsset, glyph);
                return character;
            }
            
            if (sourceFontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                if (sourceFontAsset.TryAddCharacterByGlyphIndexInternal(glyphIndex, out character))
                    return character;
                Debug.LogWarning("Add glyph to texture failed, please check font asset setting");
            }

            // Search fallback font assets if we still don't have a valid character and include fallback is set to true.
            if (character == null && includeFallbacks && sourceFontAsset.fallbackFontAssetTable != null)
            {
                // Get reference to the list of fallback font assets.
                List<TMP_FontAsset> fallbackFontAssets = sourceFontAsset.fallbackFontAssetTable;
                int fallbackCount = fallbackFontAssets.Count;

                if (fallbackCount == 0)
                    return null;

                for (int i = 0; i < fallbackCount; i++)
                {
                    TMP_FontAsset temp = fallbackFontAssets[i];

                    if (temp == null)
                        continue;

                    int id = temp.instanceID;

                    // Try adding font asset to search list. If already present skip to the next one otherwise check if it contains the requested character.
                    if (k_SearchedAssets.Add(id) == false)
                        continue;

                    // Add reference to this search query
                    sourceFontAsset.FallbackSearchQueryLookup.Add(id);

                    character = GetCharacterByGlyphIndexFromFontAsset_Internal(glyphIndex, temp,  true, fontStyle, fontWeight, out isAlternativeTypeface);

                    if (character != null)
                        return character;
                }
            }

            return null;
        }
        
        public static TMP_Character GetCharacterByGlyphIndexFromFontAssets(uint glyphIndex, TMP_FontAsset sourceFontAsset, List<TMP_FontAsset> fontAssets, bool includeFallbacks, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface)
        {
            isAlternativeTypeface = false;

            // Make sure font asset list is valid
            if (fontAssets == null || fontAssets.Count == 0)
                return null;

            if (includeFallbacks)
            {
                if (k_SearchedAssets == null)
                    k_SearchedAssets = new HashSet<int>();
                else
                    k_SearchedAssets.Clear();
            }

            int fontAssetCount = fontAssets.Count;

            for (int i = 0; i < fontAssetCount; i++)
            {
                TMP_FontAsset fontAsset = fontAssets[i];

                if (fontAsset == null) continue;

                // Add reference to this search query
                sourceFontAsset.FallbackSearchQueryLookup.Add(fontAsset.instanceID);

                TMP_Character character = GetCharacterByGlyphIndexFromFontAsset_Internal(glyphIndex, fontAsset, includeFallbacks, fontStyle, fontWeight, out isAlternativeTypeface);

                if (character != null)
                    return character;
            }

            return null;
        }
    }
}