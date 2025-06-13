using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MapEditor
{
    /// <summary>
    /// シーン・Tilemap操作管理クラス
    /// </summary>
    public class TileMapSceneController
    {
        private const string exportPathPrefix = "Assets/MapEditor/Migration";
        private const string sceneDir = "Assets/MapEditor/Scene";
        private const string templateScenePath = sceneDir + "/MapEditorTemplate.unity";
        private string TileDataManagerPath => $"Assets/MapEditor/Resources/TileDataManager_{SceneManager.GetActiveScene().name}.asset";

        public Camera SceneCamera { get; private set; }
        public SpriteRenderer BackgroundSprite { get; private set; }
        public Tilemap CurrentFieldTilemap { get; private set; }
        public Tilemap CurrentGimmickTilemap { get; private set; }
        public TileDataManager TileDataManager { get; private set; }
        public string ExportPath => $"{exportPathPrefix}/{SceneManager.GetActiveScene().name}";

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize(string groupId)
        {
            LoadOrCreateTileDataManager();
            RefreshSceneObjects();
            SyncTileDataWithScene(groupId);
        }

        /// <summary>
        /// シーン内オブジェクト再取得
        /// </summary>
        public void RefreshSceneObjects()
        {
            var root = GameObject.Find("Root");
            if (root == null) return;

            var cameraObj = root.transform.Find("Camera");
            if (cameraObj != null)
                SceneCamera = cameraObj.GetComponent<Camera>();

            var bgObj = root.transform.Find("MapRoot/background_9s");
            if (bgObj != null)
                BackgroundSprite = bgObj.GetComponent<SpriteRenderer>();

            LoadTilemapsFromScene();
        }

        /// <summary>
        /// TileDataManagerロードまたは新規作成
        /// </summary>
        private void LoadOrCreateTileDataManager()
        {
            TileDataManager = AssetDatabase.LoadAssetAtPath<TileDataManager>(TileDataManagerPath);
            if (TileDataManager == null)
            {
                TileDataManager = ScriptableObject.CreateInstance<TileDataManager>();
                AssetDatabase.CreateAsset(TileDataManager, TileDataManagerPath);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// シーン内Tilemap取得
        /// </summary>
        private void LoadTilemapsFromScene()
        {
            var tilemaps = GameObject.FindObjectsOfType<Tilemap>();
            if (tilemaps != null && tilemaps.Length > 0)
            {
                CurrentFieldTilemap = tilemaps.FirstOrDefault(t => t.gameObject.name == "Field");
                CurrentGimmickTilemap = tilemaps.FirstOrDefault(t => t.gameObject.name == "Gimmick");
            }
        }

        /// <summary>
        /// シーン上のTilemapとTileDataManagerを同期
        /// </summary>
        public void SyncTileDataWithScene(string groupId)
        {
            if (TileDataManager == null) return;

            var sceneTiles = new HashSet<Vector3Int>();
            if (CurrentFieldTilemap != null)
            {
                foreach (var pos in CurrentFieldTilemap.cellBounds.allPositionsWithin)
                {
                    if (CurrentFieldTilemap.HasTile(pos))
                        sceneTiles.Add(pos);
                }
            }

            if (CurrentGimmickTilemap != null)
            {
                foreach (var pos in CurrentGimmickTilemap.cellBounds.allPositionsWithin)
                {
                    if (CurrentGimmickTilemap.HasTile(pos))
                        sceneTiles.Add(pos);
                }
            }

            foreach (var pos in sceneTiles)
            {
                if (!TileDataManager.tilesData.Any(d => d.position == pos))
                {
                    var fieldType = CurrentFieldTilemap.GetFieldTileType(pos);
                    var newData = new TileData
                    {
                        id = TileDataManager.tilesData.Count > 0 ? TileDataManager.tilesData.Max(d => d.id) + 1 : 1,
                        groupId = int.Parse(groupId),
                        position = pos,
                        fieldTileType = fieldType,
                        gimmickTileType = CurrentGimmickTilemap.GetGimmickTileType(pos),
                        isMovable = fieldType.GetDefaultIsMovable()
                    };
                    TileDataManager.tilesData.Add(newData);
                    EditorUtility.SetDirty(TileDataManager);
                }
            }

            TileDataManager.tilesData.RemoveAll(d => !sceneTiles.Contains(d.position));
            EditorUtility.SetDirty(TileDataManager);
        }

        /// <summary>
        /// タイル削除（Tilemap上も削除）
        /// </summary>
        public void RemoveTile(TileData tileData)
        {
            if (CurrentFieldTilemap != null && CurrentFieldTilemap.HasTile(tileData.position))
            {
                Undo.RecordObject(CurrentFieldTilemap, "Remove Tile");
                CurrentFieldTilemap.SetTile(tileData.position, null);
                EditorUtility.SetDirty(CurrentFieldTilemap);
            }
            if (CurrentGimmickTilemap != null && CurrentGimmickTilemap.HasTile(tileData.position))
            {
                Undo.RecordObject(CurrentGimmickTilemap, "Remove Tile");
                CurrentGimmickTilemap.SetTile(tileData.position, null);
                EditorUtility.SetDirty(CurrentGimmickTilemap);
            }

            TileDataManager.tilesData.Remove(tileData);
            EditorUtility.SetDirty(TileDataManager);
        }

        /// <summary>
        /// 新規シーン作成
        /// </summary>
        public void CreateNewScene(string groupId)
        {
            if (!File.Exists(templateScenePath))
            {
                EditorUtility.DisplayDialog("エラー", $"テンプレートシーンが見つかりません: {templateScenePath}", "OK");
                return;
            }

            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
                AssetDatabase.Refresh();
            }

            var index = 1;
            string newScenePath;
            do
            {
                newScenePath = $"{sceneDir}/tilemap_{groupId}_{index}.unity";
                index++;
            } while (File.Exists(newScenePath));

            File.Copy(templateScenePath, newScenePath);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(newScenePath);
            LoadTilemapsFromScene();
            SyncTileDataWithScene(groupId);
        }

        /// <summary>
        /// 既存シーン編集
        /// </summary>
        public void EditScene()
        {
            var path = EditorUtility.OpenFilePanel("シーンを選択", sceneDir, "unity");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    var relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    EditorSceneManager.OpenScene(relativePath);
                    LoadTilemapsFromScene();
                    SyncTileDataWithScene("1");
                }
                else
                {
                    EditorUtility.DisplayDialog("エラー", "Assetsフォルダ内のシーンのみ開けます", "OK");
                }
            }
        }

        /// <summary>
        /// 選択タイルのハイライト描画
        /// </summary>
        public void DrawHandle(TileData selectedTileData)
        {
            Vector3 cellCenter;
            Vector3 cellSize;
            TileBase tile = null;
            if (CurrentFieldTilemap != null && CurrentFieldTilemap.HasTile(selectedTileData.position))
            {
                cellCenter = CurrentFieldTilemap.GetCellCenterWorld(selectedTileData.position);
                cellSize = CurrentFieldTilemap.cellSize;
                tile = CurrentFieldTilemap.GetTile(selectedTileData.position);
            }
            else if (CurrentGimmickTilemap != null && CurrentGimmickTilemap.HasTile(selectedTileData.position))
            {
                cellCenter = CurrentGimmickTilemap.GetCellCenterWorld(selectedTileData.position);
                cellSize = CurrentGimmickTilemap.cellSize;
                tile = CurrentGimmickTilemap.GetTile(selectedTileData.position);
            }
            else
            {
                return;
            }

            // ハンドル表示色の作成（反転色）
            var tileColor = Color.yellow;
            if (tile is Tile t && t.sprite != null && t.sprite.texture != null)
            {
                var tex = t.sprite.texture;
                try
                {
                    var rect = t.sprite.textureRect;
                    var pixels = tex.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                    if (pixels.Length > 0)
                    {
                        float r = 0, g = 0, b = 0;
                        foreach (var p in pixels)
                        {
                            r += p.r;
                            g += p.g;
                            b += p.b;
                        }
                        r /= pixels.Length;
                        g /= pixels.Length;
                        b /= pixels.Length;
                        tileColor = new Color(r, g, b, 1f);
                    }
                }
                catch { }
            }

            var invColor = new Color(1f - tileColor.r, 1f - tileColor.g, 1f - tileColor.b, 1f);
            Handles.color = invColor;

            var half = cellSize * 0.45f;
            var verts = new Vector3[]
            {
                cellCenter + new Vector3(-half.x, -half.y, 0),
                cellCenter + new Vector3(-half.x, half.y, 0),
                cellCenter + new Vector3(half.x, half.y, 0),
                cellCenter + new Vector3(half.x, -half.y, 0),
                cellCenter + new Vector3(-half.x, -half.y, 0),
            };
            Handles.DrawAAPolyLine(10f, verts);
        }
    }

    /// <summary>
    /// エクスポート処理クラス
    /// </summary>
    public class TileMapExporter
    {
        /// <summary>
        /// タイルデータをエクスポート
        /// </summary>
        public void Export(TileDataManager tileDataManager, string exportPath)
        {
            var outputPath = $"{exportPath}.txt";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Summer2025HuntTile.create!(");
                sb.AppendLine("      [");
                for (var i = 0; i < tileDataManager.tilesData.Count; i++)
                {
                    var data = tileDataManager.tilesData[i];
                    var line =
                        $"        {{id: {data.id}, group_id: {data.groupId}, field_tile_type_id: {(int)data.fieldTileType}, gimmick_tile_type_id: {(int)data.gimmickTileType}, gimmick_tile_type_value: {data.gimmickTileMasterId}, is_movable: {(data.isMovable ? 1 : 0)}, position_x: {data.position.x}, position_y: {data.position.y}}}";
                    if (i < tileDataManager.tilesData.Count - 1)
                        line += ",";
                    sb.AppendLine(line);
                }

                sb.AppendLine("      ])");
                sb.AppendLine("Summer2025HuntTile.expire_all_cache");

                var directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                Debug.Log($"タイルマップデータを出力しました: {outputPath}");
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"エクスポート中にエラーが発生しました: {ex.Message}");
            }
        }
    }
}