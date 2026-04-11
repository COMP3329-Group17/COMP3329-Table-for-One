using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using System.Linq;



namespace TMPro
{
    public partial class TMP_InputField
    {
        struct CharToHeightLine
        {
            public int visualIndex;
            public int lineNumber;
        }

        struct RangeToHeightLine
        {
            public int startVisualIndex;
            public int endVisualIndex;
            public int lineNumber;
        }
        
        void RenderHeightLightATP(VertexHelper vbo)
        {
            TMP_TextInfo textInfo = m_TextComponent.textInfo;
            // Logic order
            int startChar = Mathf.Max(0, m_StringPosition);
            int endChar = Mathf.Max(0, m_StringSelectPosition);
            
            // Ensure pos is always less then selPos to make the code simpler
            if (startChar > endChar)
            {
                int temp = startChar;
                startChar = endChar;
                endChar = temp;
            }

            endChar -= 1;
            List<CharToHeightLine> charsToHeightLine = new List<CharToHeightLine>();
            for (int idx = startChar; idx <= endChar; idx++)
            {
                CharToHeightLine visualChar = new CharToHeightLine();
                visualChar.visualIndex = GetCaretPositionFromStringIndex(idx);
                visualChar.lineNumber = textInfo.characterInfo[visualChar.visualIndex].lineNumber;
                charsToHeightLine.Add(visualChar);
            }

            List<RangeToHeightLine> rangesToHeightLine = MergeRenderOrders(charsToHeightLine);
            
            
            UIVertex vert = UIVertex.simpleVert;
            vert.uv0 = Vector2.zero;
            vert.color = selectionColor;

            foreach (var rangeToHeight in rangesToHeightLine)
            {
                TMP_CharacterInfo startCharInfo = textInfo.characterInfo[rangeToHeight.startVisualIndex];
                TMP_CharacterInfo endCharInfo = textInfo.characterInfo[rangeToHeight.endVisualIndex];

                if (m_TextComponent.isRightToLeftText)
                {
                    var tmp = startCharInfo;
                    startCharInfo = endCharInfo;
                    endCharInfo = tmp;
                }
                
                Vector2 startPosition = new Vector2(startCharInfo.origin, textInfo.lineInfo[rangeToHeight.lineNumber].ascender);
                Vector2 endPosition = new Vector2(endCharInfo.xAdvance, textInfo.lineInfo[rangeToHeight.lineNumber].descender);

                if (m_TextComponent.isRightToLeftText)
                {
                    endPosition.x = endCharInfo.topRight.x;
                }

                var startIndex = vbo.currentVertCount;
                vert.position = new Vector3(startPosition.x, endPosition.y, 0.0f);
                vbo.AddVert(vert);

                vert.position = new Vector3(endPosition.x, endPosition.y, 0.0f);
                vbo.AddVert(vert);

                vert.position = new Vector3(endPosition.x, startPosition.y, 0.0f);
                vbo.AddVert(vert);

                vert.position = new Vector3(startPosition.x, startPosition.y, 0.0f);
                vbo.AddVert(vert);

                vbo.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                vbo.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);
            }
        }

        List<RangeToHeightLine> MergeRenderOrders(List<CharToHeightLine> input)
        {
            var result = new List<RangeToHeightLine>();
            var groupedByLine = input.GroupBy(x => x.lineNumber);
            foreach (var lineGroup in groupedByLine)
            {
                int line = lineGroup.Key;
                
                var sortedIndices = lineGroup.Select(x => x.visualIndex).OrderBy(x => x).ToList();
                if (sortedIndices.Count == 0) continue;
                int rangStart = sortedIndices[0];
                int rangEnd = sortedIndices[0];
                for (int i = 1; i < sortedIndices.Count; i++)
                {
                    if (sortedIndices[i] == rangEnd + 1)
                    {
                        rangEnd = sortedIndices[i];
                    }
                    else
                    {
                        RangeToHeightLine r = new RangeToHeightLine();
                        r.lineNumber = line;
                        r.startVisualIndex = rangStart;
                        r.endVisualIndex = rangEnd;
                        result.Add(r);

                        rangStart = rangEnd = sortedIndices[i];
                    }
                }
                result.Add(new RangeToHeightLine(){lineNumber = line, startVisualIndex = rangStart, endVisualIndex = rangEnd});
            }

            return result;
        }

        void DeleteATP()
        {
            if (m_StringPosition > m_StringSelectPosition)
            {
                (m_StringPosition, m_StringSelectPosition) = (m_StringSelectPosition, m_StringPosition);
            }
            m_Text = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
            m_StringSelectPosition = m_StringPosition;
            m_CaretSelectPosition = m_CaretPosition;
        }
    }
}