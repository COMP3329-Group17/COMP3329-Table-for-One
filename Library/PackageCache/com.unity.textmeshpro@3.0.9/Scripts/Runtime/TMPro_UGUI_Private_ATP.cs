using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Linq;
namespace TMPro
{
    public partial class TextMeshProUGUI
    {
#if TUANJIE_1_6_OR_NEWER && !(TUANJIE_1_6_0 || TUANJIE_1_6_1 || TUANJIE_1_6_2 || TUANJIE_1_7_0)
        private List<ATPHandleResult> m_ATPGlyphCache = new List<ATPHandleResult>();
        internal void ProcessTextByATP(StringBuilder sb, UnicodeChar[] newChars, ref int newCharsIndex, UnicodeChar[] originChars, int sbEndIndex)
        {
            if (sb.Length <= 0)
                return;
            var atpRets = AdvanceTextProcessor.GetInstance().ProcessLine(m_fontAsset.sourceFontFile, m_fontAsset.faceInfo.pointSize, isRightToLeftText, sb.ToString());
            if (isRightToLeftText)
            {
                for (int i = atpRets.Length - 1; i >= 0; i--)
                {
                    var atpRet = atpRets[i];
                    m_ATPGlyphCache.Add(atpRet);
                    UnicodeChar unicodeChar = originChars[sbEndIndex - sb.Length + atpRet.cluster];
                    unicodeChar.atpDataIndex = m_ATPGlyphCache.Count;
                    newChars[newCharsIndex++] = unicodeChar;
                }  
            }
            else
            {
                for (int i = 0; i < atpRets.Length; i++)
                {
                    var atpRet = atpRets[i];
                    m_ATPGlyphCache.Add(atpRet);
                    UnicodeChar unicodeChar = originChars[sbEndIndex - sb.Length + atpRet.cluster];
                    unicodeChar.atpDataIndex = m_ATPGlyphCache.Count;
                    newChars[newCharsIndex++] = unicodeChar;
                }
            }
            sb.Clear();
        }
        
        internal void ParseTextAndProcessByATP(UnicodeChar[] unicodeChars)
        {
            m_ATPGlyphCache.Clear();
            
            UnicodeChar[] processAfterATP = new UnicodeChar[unicodeChars.Length];
            int processAfterATPIndex = 0;
            StringBuilder currentText = new StringBuilder();
            int i;
            for (i = 0; i < unicodeChars.Length && unicodeChars[i].unicode != 0; i++)
            {
                unicodeChars[i].atpDataIndex = 0;
                int unicode = unicodeChars[i].unicode;
                if (unicode == 10) // if char '\n'
                {
                    ProcessTextByATP(currentText, processAfterATP, ref processAfterATPIndex, unicodeChars, i);
                    processAfterATP[processAfterATPIndex++] = unicodeChars[i];
                    continue;
                }
                
                if (m_isRichText && unicode == 60) // if Char '<'
                {
                    int startTagIndex = i + 1;
                    int endTagIndex;
                    // Check if Tag is Valid
                    if (ValidateHtmlTag(unicodeChars, startTagIndex, out endTagIndex))
                    {
                        ProcessTextByATP(currentText, processAfterATP, ref processAfterATPIndex, unicodeChars, i);
                        //copy html tag
                        for (int j = i; j <= endTagIndex; j++)
                            processAfterATP[processAfterATPIndex++] = unicodeChars[j];
                        
                        i = endTagIndex;
                        continue;
                    }

                    currentText.Append((char)unicode);
                }
                else
                {
                    currentText.Append((char)unicode);
                }
            }
            
            //handle rest of the string
            ProcessTextByATP(currentText, processAfterATP, ref processAfterATPIndex, unicodeChars, i);
            m_TextProcessingArray = processAfterATP;
        }

        internal override void PopulateTextProcessingArray()
        {
            base.PopulateTextProcessingArray();
            if (m_enableAdvanceTextProcess)
                ParseTextAndProcessByATP(m_TextProcessingArray);
        }
        
        //most of the code was similar to SetArraySizesWithoutATP except the region of LOOKUP GLYPH
        internal int SetArraySizesWithATP(UnicodeChar[] unicodeChars)
        {
            int spriteCount = 0;

            m_totalCharacterCount = 0;
            m_isUsingBold = false;
            m_isParsingText = false;
            tag_NoParsing = false;
            m_FontStyleInternal = m_fontStyle;
            m_fontStyleStack.Clear();

            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? FontWeight.Bold : m_fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;

            m_materialReferenceStack.SetDefault(new MaterialReference(m_currentMaterialIndex, m_currentFontAsset, null, m_currentMaterial, m_padding));

            m_materialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

            // Set allocations for the text object's TextInfo
            if (m_textInfo == null)
                m_textInfo = new TMP_TextInfo(m_InternalTextProcessingArraySize);
            else if (m_textInfo.characterInfo.Length < m_InternalTextProcessingArraySize)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_InternalTextProcessingArraySize, false);

            m_textElementType = TMP_TextElementType.Character;

            // Handling for Underline special character
            #region Setup Underline Special Character
            /*
            GetUnderlineSpecialCharacter(m_currentFontAsset);
            if (m_Underline.character != null)
            {
                if (m_Underline.fontAsset.GetInstanceID() != m_currentFontAsset.GetInstanceID())
                {
                    if (TMP_Settings.matchMaterialPreset && m_currentMaterial.GetInstanceID() != m_Underline.fontAsset.material.GetInstanceID())
                        m_Underline.material = TMP_MaterialManager.GetFallbackMaterial(m_currentMaterial, m_Underline.fontAsset.material);
                    else
                        m_Underline.material = m_Underline.fontAsset.material;

                    m_Underline.materialIndex = MaterialReference.AddMaterialReference(m_Underline.material, m_Underline.fontAsset, m_materialReferences, m_materialReferenceIndexLookup);
                    m_materialReferences[m_Underline.materialIndex].referenceCount = 0;
                }
            }
            */
            #endregion
            
            // Handling for Ellipsis special character
            #region Setup Ellipsis Special Character
            if (m_overflowMode == TextOverflowModes.Ellipsis)
            {
                GetEllipsisSpecialCharacter(m_currentFontAsset);

                if (m_Ellipsis.character != null)
                {
                    if (m_Ellipsis.fontAsset.GetInstanceID() != m_currentFontAsset.GetInstanceID())
                    {
                        if (TMP_Settings.matchMaterialPreset && m_currentMaterial.GetInstanceID() != m_Ellipsis.fontAsset.material.GetInstanceID())
                            m_Ellipsis.material = TMP_MaterialManager.GetFallbackMaterial(m_currentMaterial, m_Ellipsis.fontAsset.material);
                        else
                            m_Ellipsis.material = m_Ellipsis.fontAsset.material;

                        m_Ellipsis.materialIndex = MaterialReference.AddMaterialReference(m_Ellipsis.material, m_Ellipsis.fontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                        m_materialReferences[m_Ellipsis.materialIndex].referenceCount = 0;
                    }
                }
                else
                {
                    m_overflowMode = TextOverflowModes.Truncate;

                    if (!TMP_Settings.warningsDisabled)
                        Debug.LogWarning("The character used for Ellipsis is not available in font asset [" + m_currentFontAsset.name + "] or any potential fallbacks. Switching Text Overflow mode to Truncate.", this);
                }
            }
            #endregion
            
            // Clear Linked Text object content if we have any.
            if (m_overflowMode == TextOverflowModes.Linked && m_linkedTextComponent != null && !m_isCalculatingPreferredValues)
            {
                TMP_Text linkedComponent = m_linkedTextComponent;

                while (linkedComponent != null)
                {
                    linkedComponent.text = String.Empty;
                    linkedComponent.ClearMesh();
                    linkedComponent.textInfo.Clear();

                    linkedComponent = linkedComponent.linkedTextComponent;
                }
            }
            
            // Parsing XML tags in the text
            for (int i = 0; i < unicodeChars.Length && unicodeChars[i].unicode != 0; i++)
            {
                //Make sure the characterInfo array can hold the next text element.
                if (m_textInfo.characterInfo == null || m_totalCharacterCount >= m_textInfo.characterInfo.Length)
                    TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_totalCharacterCount + 1, true);

                int unicode = unicodeChars[i].unicode;
                int atpIndex = unicodeChars[i].atpDataIndex;

                // PARSE XML TAGS
                #region PARSE XML TAGS
                if (m_isRichText && unicode == 60) // if Char '<'
                {
                    int prev_MaterialIndex = m_currentMaterialIndex;
                    int endTagIndex;

                    // Check if Tag is Valid
                    if (ValidateHtmlTag(unicodeChars, i + 1, out endTagIndex))
                    {
                        int tagStartIndex = unicodeChars[i].stringIndex;
                        i = endTagIndex;

                        if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)
                            m_isUsingBold = true;

                        if (m_textElementType == TMP_TextElementType.Sprite)
                        {
                            m_materialReferences[m_currentMaterialIndex].referenceCount += 1;

                            m_textInfo.characterInfo[m_totalCharacterCount].character = (char)(57344 + m_spriteIndex);
                            m_textInfo.characterInfo[m_totalCharacterCount].spriteIndex = m_spriteIndex;
                            m_textInfo.characterInfo[m_totalCharacterCount].fontAsset = m_currentFontAsset;
                            m_textInfo.characterInfo[m_totalCharacterCount].spriteAsset = m_currentSpriteAsset;
                            m_textInfo.characterInfo[m_totalCharacterCount].materialReferenceIndex = m_currentMaterialIndex;
                            m_textInfo.characterInfo[m_totalCharacterCount].textElement = m_currentSpriteAsset.spriteCharacterTable[m_spriteIndex];
                            m_textInfo.characterInfo[m_totalCharacterCount].elementType = m_textElementType;
                            m_textInfo.characterInfo[m_totalCharacterCount].index = tagStartIndex;
                            m_textInfo.characterInfo[m_totalCharacterCount].stringLength = unicodeChars[i].stringIndex - tagStartIndex + 1;

                            // Restore element type and material index to previous values.
                            m_textElementType = TMP_TextElementType.Character;
                            m_currentMaterialIndex = prev_MaterialIndex;

                            spriteCount += 1;
                            m_totalCharacterCount += 1;
                        }

                        continue;
                    }
                }
                #endregion

                bool isUsingAlternativeTypeface = false;
                bool isUsingFallbackOrAlternativeTypeface = false;

                TMP_FontAsset prev_fontAsset = m_currentFontAsset;
                Material prev_material = m_currentMaterial;
                int prev_materialIndex = m_currentMaterialIndex;

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles
                if (m_textElementType == TMP_TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);

                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)unicode))
                            unicode = char.ToLower((char)unicode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        // Only convert lowercase characters to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);
                    }
                }
                #endregion

                // Lookup the Glyph data for each character and cache it.
                #region LOOKUP GLYPH
                TMP_TextElement character = null;
                if (atpIndex != 0)
                {
                    var atpHandleResult = m_ATPGlyphCache[atpIndex - 1];
                    if (atpHandleResult.glyphIndex != 0)
                    {
                        character = TMP_FontAssetUtilities.GetCharacterByGlyphIndexFromFontAsset(atpHandleResult.glyphIndex, m_currentFontAsset,  false, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }
                    if (character == null)
                    {
                        character = GetTextElement((uint)unicode, m_currentFontAsset, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }
                    else
                    {
                        var tmpMetrics = character.glyph.metrics;
                        tmpMetrics.horizontalAdvance = atpHandleResult.xAdvance / 64.0f;
                        tmpMetrics.horizontalBearingX = atpHandleResult.xOffset / 64.0f;
                        tmpMetrics.horizontalBearingY = atpHandleResult.yOffset / 64.0f;
                        character.atpMetrics = tmpMetrics;
                        character.bidiDir = atpHandleResult.bidiDirection;
                        character.harfbuzzSuccess = true;
                    }
                }
                else
                    character = GetTextElement((uint)unicode, m_currentFontAsset, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

                // Check if Lowercase or Uppercase variant of the character is available.
                /* Not sure this is necessary anyone as it is very unlikely with recursive search through fallback fonts.
                if (glyph == null)
                {
                    if (char.IsLower((char)c))
                    {
                        if (m_currentFontAsset.characterDictionary.TryGetValue(char.ToUpper((char)c), out glyph))
                            c = chars[i] = char.ToUpper((char)c);
                    }
                    else if (char.IsUpper((char)c))
                    {
                        if (m_currentFontAsset.characterDictionary.TryGetValue(char.ToLower((char)c), out glyph))
                            c = chars[i] = char.ToLower((char)c);
                    }
                }*/

                // Special handling for missing character.
                // Replace missing glyph by the Square (9633) glyph or possibly the Space (32) glyph.
                if (character == null)
                {
                    // Save the original unicode character
                    int srcGlyph = unicode;

                    // Try replacing the missing glyph character by TMP Settings Missing Glyph or Square (9633) character.
                    unicode = unicodeChars[i].unicode = TMP_Settings.missingGlyphCharacter == 0 ? 9633 : TMP_Settings.missingGlyphCharacter;

                    // Check for the missing glyph character in the currently assigned font asset and its fallbacks
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_currentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

                    if (character == null)
                    {
                        // Search for the missing glyph character in the TMP Settings Fallback list.
                        if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                            character = TMP_FontAssetUtilities.GetCharacterFromFontAssets((uint)unicode, m_currentFontAsset, TMP_Settings.fallbackFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        // Search for the missing glyph in the TMP Settings Default Font Asset.
                        if (TMP_Settings.defaultFontAsset != null)
                            character = TMP_FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, TMP_Settings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        // Use Space (32) Glyph from the currently assigned font asset.
                        unicode = unicodeChars[i].unicode = 32;
                        character = TMP_FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_currentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        // Use End of Text (0x03) Glyph from the currently assigned font asset.
                        unicode = unicodeChars[i].unicode = 0x03;
                        character = TMP_FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_currentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (!TMP_Settings.warningsDisabled)
                    {
                        string formattedWarning = srcGlyph > 0xFFFF
                            ? string.Format("The character with Unicode value \\U{0:X8} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4} in text object [{3}].", srcGlyph, m_fontAsset.name, character.unicode, this.name)
                            : string.Format("The character with Unicode value \\u{0:X4} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4} in text object [{3}].", srcGlyph, m_fontAsset.name, character.unicode, this.name);

                        Debug.LogWarning(formattedWarning, this);
                    }
                }

                if (character.elementType == TextElementType.Character)
                {
                    if (character.textAsset.instanceID != m_currentFontAsset.instanceID)
                    {
                        isUsingFallbackOrAlternativeTypeface = true;
                        m_currentFontAsset = character.textAsset as TMP_FontAsset;

                    }
                }
                #endregion

                // Save text element data
                m_textInfo.characterInfo[m_totalCharacterCount].elementType = TMP_TextElementType.Character;
                m_textInfo.characterInfo[m_totalCharacterCount].textElement = character;
                m_textInfo.characterInfo[m_totalCharacterCount].isUsingAlternateTypeface = isUsingAlternativeTypeface;
                m_textInfo.characterInfo[m_totalCharacterCount].character = (char)unicode;
                m_textInfo.characterInfo[m_totalCharacterCount].index = unicodeChars[i].stringIndex;
                m_textInfo.characterInfo[m_totalCharacterCount].stringLength = unicodeChars[i].length;
                m_textInfo.characterInfo[m_totalCharacterCount].fontAsset = m_currentFontAsset;

                // Special handling if the character is a sprite.
                if (character.elementType == TextElementType.Sprite)
                {
                    TMP_SpriteAsset spriteAssetRef = character.textAsset as TMP_SpriteAsset;
                    m_currentMaterialIndex = MaterialReference.AddMaterialReference(spriteAssetRef.material, spriteAssetRef, ref m_materialReferences, m_materialReferenceIndexLookup);
                    m_materialReferences[m_currentMaterialIndex].referenceCount += 1;

                    m_textInfo.characterInfo[m_totalCharacterCount].elementType = TMP_TextElementType.Sprite;
                    m_textInfo.characterInfo[m_totalCharacterCount].materialReferenceIndex = m_currentMaterialIndex;
                    m_textInfo.characterInfo[m_totalCharacterCount].spriteAsset = spriteAssetRef;
                    m_textInfo.characterInfo[m_totalCharacterCount].spriteIndex = (int)character.glyphIndex;

                    // Restore element type and material index to previous values.
                    m_textElementType = TMP_TextElementType.Character;
                    m_currentMaterialIndex = prev_materialIndex;

                    spriteCount += 1;
                    m_totalCharacterCount += 1;

                    continue;
                }

                if (isUsingFallbackOrAlternativeTypeface && m_currentFontAsset.instanceID != m_fontAsset.instanceID)
                {
                    // Create Fallback material instance matching current material preset if necessary
                    if (TMP_Settings.matchMaterialPreset)
                        m_currentMaterial = TMP_MaterialManager.GetFallbackMaterial(m_currentMaterial, m_currentFontAsset.material);
                    else
                        m_currentMaterial = m_currentFontAsset.material;

                    m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                }

                // Handle Multi Atlas Texture support
                if (character != null && character.glyph.atlasIndex > 0)
                {
                    m_currentMaterial = TMP_MaterialManager.GetFallbackMaterial(m_currentFontAsset, m_currentMaterial, character.glyph.atlasIndex);

                    m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

                    isUsingFallbackOrAlternativeTypeface = true;
                }

                if (!char.IsWhiteSpace((char)unicode) && unicode != 0x200B)
                {
                    // Limit the mesh of the main text object to 65535 vertices and use sub objects for the overflow.
                    if (m_materialReferences[m_currentMaterialIndex].referenceCount < 16383)
                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    else
                    {
                        m_currentMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_currentMaterial), m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    }
                }

                m_textInfo.characterInfo[m_totalCharacterCount].material = m_currentMaterial;
                m_textInfo.characterInfo[m_totalCharacterCount].materialReferenceIndex = m_currentMaterialIndex;
                m_materialReferences[m_currentMaterialIndex].isFallbackMaterial = isUsingFallbackOrAlternativeTypeface;

                // Restore previous font asset and material if fallback font was used.
                if (isUsingFallbackOrAlternativeTypeface)
                {
                    m_materialReferences[m_currentMaterialIndex].fallbackMaterial = prev_material;
                    m_currentFontAsset = prev_fontAsset;
                    m_currentMaterial = prev_material;
                    m_currentMaterialIndex = prev_materialIndex;
                }

                m_totalCharacterCount += 1;
            }

            // Early return if we are calculating the preferred values.
            if (m_isCalculatingPreferredValues)
            {
                m_isCalculatingPreferredValues = false;
                return m_totalCharacterCount;
            }

            // Save material and sprite count.
            m_textInfo.spriteCount = spriteCount;
            int materialCount = m_textInfo.materialCount = m_materialReferenceIndexLookup.Count;

            // Check if we need to resize the MeshInfo array for handling different materials.
            if (materialCount > m_textInfo.meshInfo.Length)
                TMP_TextInfo.Resize(ref m_textInfo.meshInfo, materialCount, false);

            // Resize SubTextObject array if necessary
            if (materialCount > m_subTextObjects.Length)
                TMP_TextInfo.Resize(ref m_subTextObjects, Mathf.NextPowerOfTwo(materialCount + 1));

            // Resize CharacterInfo[] if allocations are excessive
            if (m_VertexBufferAutoSizeReduction && m_textInfo.characterInfo.Length - m_totalCharacterCount > 256)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, Mathf.Max(m_totalCharacterCount + 1, 256), true);
            
            // Iterate through the material references to set the mesh buffer allocations
            for (int i = 0; i < materialCount; i++)
            {
                // Add new sub text object for each material reference
                if (i > 0)
                {
                    if (m_subTextObjects[i] == null)
                    {
                        m_subTextObjects[i] = TMP_SubMeshUI.AddSubTextObject(this, m_materialReferences[i]);

                        // Not sure this is necessary
                        m_textInfo.meshInfo[i].vertices = null;
                    }
                    //else if (m_subTextObjects[i].gameObject.activeInHierarchy == false)
                    //    m_subTextObjects[i].gameObject.SetActive(true);

                    // Make sure the pivots are synchronized
                    if (m_rectTransform.pivot != m_subTextObjects[i].rectTransform.pivot)
                        m_subTextObjects[i].rectTransform.pivot = m_rectTransform.pivot;

                    // Check if the material has changed.
                    if (m_subTextObjects[i].sharedMaterial == null || m_subTextObjects[i].sharedMaterial.GetInstanceID() != m_materialReferences[i].material.GetInstanceID())
                    {
                        m_subTextObjects[i].sharedMaterial = m_materialReferences[i].material;
                        m_subTextObjects[i].fontAsset = m_materialReferences[i].fontAsset;
                        m_subTextObjects[i].spriteAsset = m_materialReferences[i].spriteAsset;
                    }

                    // Check if we need to use a Fallback Material
                    if (m_materialReferences[i].isFallbackMaterial)
                    {
                        m_subTextObjects[i].fallbackMaterial = m_materialReferences[i].material;
                        m_subTextObjects[i].fallbackSourceMaterial = m_materialReferences[i].fallbackMaterial;
                    }
                }

                int referenceCount = m_materialReferences[i].referenceCount;

                // Check to make sure our buffers allocations can accommodate the required text elements.
                if (m_textInfo.meshInfo[i].vertices == null || m_textInfo.meshInfo[i].vertices.Length < referenceCount * 4)
                {
                    if (m_textInfo.meshInfo[i].vertices == null)
                    {
                        if (i == 0)
                            m_textInfo.meshInfo[i] = new TMP_MeshInfo(m_mesh, referenceCount + 1);
                        else
                            m_textInfo.meshInfo[i] = new TMP_MeshInfo(m_subTextObjects[i].mesh, referenceCount + 1);
                    }
                    else
                        m_textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount + 1));
                }
                else if (m_VertexBufferAutoSizeReduction && referenceCount > 0 && m_textInfo.meshInfo[i].vertices.Length / 4 - referenceCount > 256)
                {
                    // Resize vertex buffers if allocations are excessive.
                    //Debug.Log("Reducing the size of the vertex buffers.");
                    m_textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount + 1));
                }

                // Assign material reference
                m_textInfo.meshInfo[i].material = m_materialReferences[i].material;
            }

            //TMP_MaterialManager.CleanupFallbackMaterials();

            // Clean up unused SubMeshes
            for (int i = materialCount; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                if (i < m_textInfo.meshInfo.Length)
                {
                    m_subTextObjects[i].canvasRenderer.SetMesh(null);

                    // TODO: Figure out a way to handle this without running into Unity's Rebuild loop issue.
                    //m_subTextObjects[i].gameObject.SetActive(false);
                }
            }

            ReBuildPrevLogic();
            
            return m_totalCharacterCount;
        }

        public void ReBuildPrevLogic()
        {
            var logicCharacterInfo = Enumerable.Range(0, m_totalCharacterCount)
                .OrderBy(i => m_textInfo.characterInfo[i].index)
                .ToArray();
            for (int i = 0; i < logicCharacterInfo.Length; i++)
            {
                if (i == 0)
                    m_textInfo.characterInfo[logicCharacterInfo[i]].prevStringIndex =
                        m_textInfo.characterInfo[logicCharacterInfo[i]].index;
                else
                    m_textInfo.characterInfo[logicCharacterInfo[i]].prevStringIndex =
                        m_textInfo.characterInfo[logicCharacterInfo[i - 1]].index;
            }
        }
#endif
    }
}