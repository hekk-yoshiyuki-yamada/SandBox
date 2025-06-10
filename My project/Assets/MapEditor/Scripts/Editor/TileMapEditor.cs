using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class TileMapEditor : EditorWindow
{
    private string groupId = "1";
    const string exportPathPrefix = "Assets/MapEditor";
    const string sceneDir = "Assets/MapEditor/Scene";
    const string templateScenePath = "Assets/MapEditor/Scene/MapEditorTemplate.unity";
    private Camera sceneCamera;
    private SpriteRenderer backgroundSprite;

    string ExportPath => $"{exportPathPrefix}/{SceneManager.GetActiveScene().name}.csv";

    [MenuItem("Boys2/Export Tile Map")]
    public static void ShowWindow()
    {
        GetWindow<TileMapEditor>("Export Tile Map");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Tile Map", EditorStyles.boldLabel);
        GUILayout.Label($"出力先ファイル: {ExportPath}", EditorStyles.boldLabel);
        groupId = EditorGUILayout.TextField("GroupId", groupId);

        GUILayout.Space(10);

        if (GUILayout.Button("新規作成"))
        {
            CreateNewScene();
        }

        if (GUILayout.Button("編集"))
        {
            EditScene();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Export"))
        {
            try
            {
                Export();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"エクスポート中にエラーが発生しました: {ex.Message}");
                EditorUtility.DisplayDialog("エラー", $"エクスポート中にエラーが発生しました: {ex.Message}", "OK");
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("マップ設定", EditorStyles.boldLabel);

        if (sceneCamera != null)
        {
            sceneCamera.orthographicSize = EditorGUILayout.FloatField("Camera倍率変更", sceneCamera.orthographicSize);
        }

        if (backgroundSprite != null)
        {
            backgroundSprite.size = EditorGUILayout.Vector2Field("マップサイズ(縦横想定サイズ＋２してね)", backgroundSprite.size);
        }
    }

    private void OnFocus()
    {
        var root = GameObject.Find("Root");
        if (root == null) return;

        var cameraObj = root.transform.Find("Camera");
        if (cameraObj != null)
            sceneCamera = cameraObj.GetComponent<Camera>();

        var bgObj = root.transform.Find("MapRoot/background_9s");
        if (bgObj != null)
            backgroundSprite = bgObj.GetComponent<SpriteRenderer>();
    }

    private void CreateNewScene()
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

        int index = 1;
        string newScenePath;
        do
        {
            newScenePath = $"{sceneDir}/tilemap_{groupId}_{index}.unity";
            index++;
        } while (File.Exists(newScenePath));

        File.Copy(templateScenePath, newScenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(newScenePath);
    }

    private void EditScene()
    {
        string path = EditorUtility.OpenFilePanel("シーンを選択", sceneDir, "unity");
        if (!string.IsNullOrEmpty(path))
        {
            // Unityプロジェクト内の相対パスに変換
            if (path.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                EditorSceneManager.OpenScene(relativePath);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "Assetsフォルダ内のシーンのみ開けます", "OK");
            }
        }
    }

    private void Export()
    {
        try
        {
            // 既存のエクスポートファイルがあれば削除
            if (File.Exists(ExportPath))
            {
                File.Delete(ExportPath);
                AssetDatabase.Refresh();
            }

            var lastId = 0;

            // Scene内のTilemapをすべて取得
            var tilemaps = GameObject.FindObjectsOfType<Tilemap>();
            if (tilemaps == null || tilemaps.Length == 0)
            {
                throw new System.Exception("Tilemapが見つかりませんでした。");
            }

            var fieldTilemap = tilemaps.FirstOrDefault(t => t.gameObject.name == "Field");
            var gimmickTilemap = tilemaps.FirstOrDefault(t => t.gameObject.name == "Gimmick");

            var sb = new StringBuilder();
            if (!File.Exists(ExportPath))
            {
                sb.AppendLine("id, group_id, field_tile_type_id, gimmick_tile_type_id, gimmick_tile_type_value, position_x, position_y");
            }

            // Field, Gimmick両方のTilemapのバウンディングボックスを取得
            var bounds = new BoundsInt();
            if (fieldTilemap)
                bounds = fieldTilemap.cellBounds;
            if (gimmickTilemap)
                bounds = MergeBounds(bounds, gimmickTilemap.cellBounds);

            var id = lastId + 1;
            for (var x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (var y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var fieldId = "";
                    var gimmickId = "";
                    var gimmickValue = "";

                    if (fieldTilemap != null)
                    {
                        var tile = fieldTilemap.GetTile(pos);
                        if (tile != null)
                            fieldId = tile.name;
                    }
                    if (gimmickTilemap != null)
                    {
                        var tile = gimmickTilemap.GetTile(pos);
                        if (tile != null)
                            gimmickId = tile.name;
                    }

                    // どちらかにTileがある場合のみ出力
                    if (string.IsNullOrEmpty(fieldId) && string.IsNullOrEmpty(gimmickId)) continue;

                    sb.AppendLine($"{id}, {groupId}, {fieldId}, {gimmickId}, {gimmickValue}, {x}, {y}");
                    id++;
                }
            }

            File.AppendAllText(ExportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export", "TileMapのエクスポートが完了しました", "OK");
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"エクスポート処理中にエラーが発生しました: {ex.Message}");
        }
    }

    private BoundsInt MergeBounds(BoundsInt a, BoundsInt b)
    {
        if (a.size == Vector3Int.zero) return b;
        if (b.size == Vector3Int.zero) return a;

        int xMin = Mathf.Min(a.xMin, b.xMin);
        int yMin = Mathf.Min(a.yMin, b.yMin);
        int zMin = Mathf.Min(a.zMin, b.zMin);

        int xMax = Mathf.Max(a.xMax, b.xMax);
        int yMax = Mathf.Max(a.yMax, b.yMax);
        int zMax = Mathf.Max(a.zMax, b.zMax);

        return new BoundsInt(
            xMin, yMin, zMin,
            xMax - xMin,
            yMax - yMin,
            zMax - zMin
        );
    }
}