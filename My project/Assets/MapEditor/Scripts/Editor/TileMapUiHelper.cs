using UnityEditor;
using UnityEngine;

namespace MapEditor
{
    /// <summary>
    /// UIスタイル・共通UI部品管理クラス
    /// </summary>
    public class TileMapUIHelper
    {
        private GUIStyle sectionStyle;
        private GUIStyle[] sectionStyles;
        private GUIStyle selectedTileStyle;
        private GUIStyle normalTileStyle;

        /// <summary>
        /// スタイル初期化
        /// </summary>
        public void InitializeStyles()
        {
            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.helpBox);
                sectionStyle.margin = new RectOffset(5, 5, 5, 5);
                sectionStyle.padding = new RectOffset(10, 10, 10, 10);
            }
            if (sectionStyles == null)
            {
                sectionStyles = new GUIStyle[4];
                sectionStyles[0] = new GUIStyle(sectionStyle);
                sectionStyles[0].normal.background = CreateColorTexture(new Color(0.8f, 0.9f, 1f, 0.5f));
                sectionStyles[1] = new GUIStyle(sectionStyle);
                sectionStyles[1].normal.background = CreateColorTexture(new Color(0.9f, 1f, 0.8f, 0.5f));
                sectionStyles[2] = new GUIStyle(sectionStyle);
                sectionStyles[2].normal.background = CreateColorTexture(new Color(1f, 0.9f, 0.8f, 0.5f));
                sectionStyles[3] = new GUIStyle(sectionStyle);
                sectionStyles[3].normal.background = CreateColorTexture(new Color(1f, 0.8f, 0.9f, 0.5f));
            }
            if (normalTileStyle == null)
            {
                normalTileStyle = new GUIStyle(EditorStyles.label);
                normalTileStyle.alignment = TextAnchor.MiddleLeft;
                normalTileStyle.fontSize = 13;
                normalTileStyle.fixedHeight = 32;
                normalTileStyle.padding = new RectOffset(8, 8, 0, 0);
            }
            if (selectedTileStyle == null || selectedTileStyle.normal.background == null)
            {
                selectedTileStyle = new GUIStyle(normalTileStyle);
                selectedTileStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.5f, 1f, 0.7f));
                selectedTileStyle.normal.textColor = Color.white;
            }
        }

        /// <summary>
        /// 色付きテクスチャ生成
        /// </summary>
        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 選択モードボタンスタイル取得
        /// </summary>
        public GUIStyle GetSelectModeStyle(bool isPlaceMode)
        {
            var style = new GUIStyle(GUI.skin.button);
            if (!isPlaceMode) style.normal.background = CreateColorTexture(Color.yellow);
            return style;
        }

        /// <summary>
        /// 配置モードボタンスタイル取得
        /// </summary>
        public GUIStyle GetPlaceModeStyle(bool isPlaceMode)
        {
            var style = new GUIStyle(GUI.skin.button);
            if (isPlaceMode) style.normal.background = CreateColorTexture(Color.yellow);
            return style;
        }

        /// <summary>
        /// タイルセル用スタイル取得
        /// </summary>
        public GUIStyle GetTileStyle(bool isSelected)
        {
            return isSelected ? selectedTileStyle : normalTileStyle;
        }
    }
}