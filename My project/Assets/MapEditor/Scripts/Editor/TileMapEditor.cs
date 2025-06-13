using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MapEditor
{
    public class TileMapEditor : EditorWindow
    {
        const string exportPathPrefix = "Assets/MapEditor/Migration";
        const string sceneDir = exportPathPrefix + "/Scene";
        const string templateScenePath = sceneDir + "/MapEditorTemplate.unity";
        private string TileDataManagerPath => $"Assets/MapEditor/Resources/TileDataManager_{SceneManager.GetActiveScene().name}.asset";

        // Scene関連のフィールド
        private Camera sceneCamera;
        private SpriteRenderer backgroundSprite;
        private Tilemap currentFieldTilemap;
        private Tilemap currentGimmickTilemap;

        // タイルデータ関連のフィールド
        private TileData selectedTileData = null;
        private string groupId = "1";

        // UI関連のフィールド
        private bool showTileList = false;
        private Vector2 mainScrollPosition;
        private Vector2 scrollPosition;
        private GUIStyle sectionStyle;
        private GUIStyle[] sectionStyles;
        private GUIStyle selectedTileStyle;
        private GUIStyle normalTileStyle;

        private enum FilterType { All, FieldTileType, GimmickTileType }
        private FilterType filterType = FilterType.All;
        private FieldTileType filterFieldType = FieldTileType.NONE;
        private GimmickTileType filterGimmickType = GimmickTileType.NONE;
        private enum SortType { ID, X, Y, FieldTileType, GimmickTileType }

        private bool isPlaceMode = false;
        private SortType sortType = SortType.ID;
        private bool sortAsc = true;
        private Rect selectedTileRect;
        private bool focusTileFromSceneView = false;

        private TileDataManager tileDataManager;

        string ExportPath => $"{exportPathPrefix}/{SceneManager.GetActiveScene().name}";

        [MenuItem("Boys2/Export Tile Map")]
        public static void ShowWindow()
        {
            GetWindow<TileMapEditor>("Tile Map Editor");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
            Initialize();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            Initialize();
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

            LoadTilemapsFromScene();
            SyncTileDataWithScene();
        }

        private void Initialize()
        {
            if(SceneManager.GetActiveScene().name.Contains("MapEditorTemplate"))
            {
                EditorUtility.DisplayDialog("エラー", "テンプレートシーンでは編集できません。実際のマップシーンを開いてください。", "OK");
                return;
            }
            if (!SceneManager.GetActiveScene().name.Contains("tilemap"))
            {
                EditorUtility.DisplayDialog("エラー", "このエディタはtilemapシーンでのみ使用できます。", "OK");
                return;
            }

            LoadOrCreateTileDataManager();
            LoadTilemapsFromScene();
            SyncTileDataWithScene();
            InitializeStyles();
        }

        private void LoadOrCreateTileDataManager()
        {
            tileDataManager = AssetDatabase.LoadAssetAtPath<TileDataManager>(TileDataManagerPath);
            if (tileDataManager == null)
            {
                tileDataManager = ScriptableObject.CreateInstance<TileDataManager>();
                AssetDatabase.CreateAsset(tileDataManager, TileDataManagerPath);
                AssetDatabase.SaveAssets();
            }
        }

        private void LoadTilemapsFromScene()
        {
            var tilemaps = GameObject.FindObjectsOfType<Tilemap>();
            if (tilemaps != null && tilemaps.Length > 0)
            {
                currentFieldTilemap = tilemaps.FirstOrDefault(t => t.gameObject.name == "Field");
                currentGimmickTilemap = tilemaps.FirstOrDefault(t => t.gameObject.name == "Gimmick");
            }
        }

        private void SyncTileDataWithScene()
        {
            if (tileDataManager == null) return;

            var sceneTiles = new HashSet<Vector3Int>();
            if (currentFieldTilemap != null)
            {
                foreach (var pos in currentFieldTilemap.cellBounds.allPositionsWithin)
                {
                    if (currentFieldTilemap.HasTile(pos))
                        sceneTiles.Add(pos);
                }
            }

            if (currentGimmickTilemap != null)
            {
                foreach (var pos in currentGimmickTilemap.cellBounds.allPositionsWithin)
                {
                    if (currentGimmickTilemap.HasTile(pos))
                        sceneTiles.Add(pos);
                }
            }

            foreach (var pos in sceneTiles)
            {
                if (!tileDataManager.tilesData.Any(d => d.position == pos))
                {
                    var fieldType = currentFieldTilemap.GetFieldTileType(pos);
                    var newData = new TileData
                    {
                        id = tileDataManager.tilesData.Count > 0 ? tileDataManager.tilesData.Max(d => d.id) + 1 : 1,
                        groupId = int.Parse(groupId),
                        position = pos,
                        fieldTileType = fieldType,
                        gimmickTileType = currentGimmickTilemap.GetGimmickTileType(pos),
                        isMovable = fieldType.GetDefaultIsMovable()
                    };
                    tileDataManager.tilesData.Add(newData);
                    EditorUtility.SetDirty(tileDataManager);
                }
            }

            tileDataManager.tilesData.RemoveAll(d => !sceneTiles.Contains(d.position));
            EditorUtility.SetDirty(tileDataManager);
        }

        private void OnGUI()
        {
            // SceneViewから選択された直後のみフォーカス
            if (focusTileFromSceneView && Event.current.type == EventType.Layout)
            {
                // 選択タイルが表示範囲外ならスクロール
                float viewHeight = 400f;
                if (selectedTileRect.y < scrollPosition.y)
                {
                    scrollPosition.y = selectedTileRect.y;
                }
                else if (selectedTileRect.yMax > scrollPosition.y + viewHeight)
                {
                    scrollPosition.y = selectedTileRect.yMax - viewHeight;
                }
                focusTileFromSceneView = false;
            }
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            DrawBasicSettings();
            EditorGUILayout.Space(10);
            DrawSceneControls();
            EditorGUILayout.Space(10);
            DrawExportSection();
            EditorGUILayout.Space(10);
            DrawTileEditingSection();
            EditorGUILayout.EndScrollView();
        }

        private void InitializeStyles()
        {
            sectionStyle = new GUIStyle(EditorStyles.helpBox);
            sectionStyle.margin = new RectOffset(5, 5, 5, 5);
            sectionStyle.padding = new RectOffset(10, 10, 10, 10);

            sectionStyles = new GUIStyle[4];
            sectionStyles[0] = new GUIStyle(sectionStyle);
            sectionStyles[0].normal.background = CreateColorTexture(new Color(0.8f, 0.9f, 1f, 0.5f));
            sectionStyles[1] = new GUIStyle(sectionStyle);
            sectionStyles[1].normal.background = CreateColorTexture(new Color(0.9f, 1f, 0.8f, 0.5f));
            sectionStyles[2] = new GUIStyle(sectionStyle);
            sectionStyles[2].normal.background = CreateColorTexture(new Color(1f, 0.9f, 0.8f, 0.5f));
            sectionStyles[3] = new GUIStyle(sectionStyle);
            sectionStyles[3].normal.background = CreateColorTexture(new Color(1f, 0.8f, 0.9f, 0.5f));

            normalTileStyle = new GUIStyle(EditorStyles.label);
            normalTileStyle.alignment = TextAnchor.MiddleLeft;
            normalTileStyle.fontSize = 13;
            normalTileStyle.fixedHeight = 32;
            normalTileStyle.padding = new RectOffset(8, 8, 0, 0);

            selectedTileStyle = new GUIStyle(normalTileStyle);
            selectedTileStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.5f, 1f, 0.7f));
            selectedTileStyle.normal.textColor = Color.white;
        }

        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void DrawBasicSettings()
        {
            GUILayout.Label(new GUIContent("基本設定", "エディタの基本設定を行います"),EditorStyles.boldLabel);
            GUILayout.Label("このエディタは、タイルマップの編集とsummer2025_hunt_tileマスタのエクスポートを行うためのツールです。", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            var selectModeStyle = new GUIStyle(GUI.skin.button);
            var placeModeStyle = new GUIStyle(GUI.skin.button);
            if (isPlaceMode) selectModeStyle.normal.background = CreateColorTexture(Color.yellow);
            else placeModeStyle.normal.background = CreateColorTexture(Color.yellow);
            if (GUILayout.Button("選択モード", selectModeStyle, GUILayout.Width(100)))
                isPlaceMode = false;
            if (GUILayout.Button("配置モード", placeModeStyle, GUILayout.Width(100)))
                isPlaceMode = true;
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("配置モードではUnity標準のTilemapツールでタイルを配置できます。選択モードではタイル情報の編集ができます。", EditorStyles.wordWrappedLabel);

            groupId = EditorGUILayout.TextField("Group ID", groupId);
            GUILayout.Space(5);
            GUILayout.Label("マップ設定", EditorStyles.boldLabel);

            if (sceneCamera != null)
            {
                sceneCamera.orthographicSize = EditorGUILayout.FloatField("Camera倍率変更(プレビュー用)", sceneCamera.orthographicSize);
            }

            if (backgroundSprite != null)
            {
                backgroundSprite.size = EditorGUILayout.Vector2Field("マップサイズ", backgroundSprite.size);
            }
        }

        private void DrawSceneControls()
        {
            GUILayout.Label("シーン操作", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("新規作成", "新しくマップを作製します。新規マップの作成を行うときに押してください")))
            {
                CreateNewScene();
            }

            if (GUILayout.Button(new GUIContent("編集", "すでに存在するマップを編集します。マップを選択して編集を行うときに押してください")))
            {
                EditScene();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTileList()
        {
            //  絞り込み・ソート
            EditorGUILayout.BeginHorizontal();
            filterType = (FilterType)EditorGUILayout.EnumPopup("フィルタ種別", filterType);
            switch (filterType)
            {
                case FilterType.FieldTileType:
                    filterFieldType = (FieldTileType)EditorGUILayout.EnumPopup("FieldType", filterFieldType);
                    break;
                case FilterType.GimmickTileType:
                    filterGimmickType = (GimmickTileType)EditorGUILayout.EnumPopup("GimmickType", filterGimmickType);
                    break;
            }
            sortType = (SortType)EditorGUILayout.EnumPopup("ソート", sortType);
            sortAsc = GUILayout.Toggle(sortAsc, sortAsc ? "昇順" : "降順", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            var list = SortFilter();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox("タイルデータがありません。", MessageType.Info);
            }
            else
            {
                foreach (var tileData in list)
                {
                    DrawTileCell(tileData);
                    if (selectedTileData == tileData)
                    {
                        selectedTileRect = GUILayoutUtility.GetLastRect();
                        DrawSelectedTileGUI();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawTileCell(TileData tileData)
        {
            var tileName = $"[{tileData.id}]{tileData.fieldTileType}_{tileData.gimmickTileType}";
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            var isSelected = tileData == selectedTileData;
            var style = isSelected ? selectedTileStyle : normalTileStyle;
            if (GUILayout.Button($"{tileName} ({tileData.position.x}, {tileData.position.y})", style, GUILayout.Height(32)))
            {
                selectedTileData = tileData;
                SceneView.RepaintAll();

                if (SceneView.lastActiveSceneView != null)
                {
                    Vector3 worldPos;
                    if (currentFieldTilemap != null && currentFieldTilemap.HasTile(tileData.position))
                        worldPos = currentFieldTilemap.CellToWorld(tileData.position) + currentFieldTilemap.cellSize / 2;
                    else
                        worldPos = currentGimmickTilemap.CellToWorld(tileData.position) + currentGimmickTilemap.cellSize / 2;

                    SceneView.lastActiveSceneView.LookAt(worldPos);
                }
            }

            if (GUILayout.Button(new GUIContent("削除", "このタイルデータとTilemap上のタイルを削除します"), GUILayout.Width(60), GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("確認", $"タイルデータ (ID: {tileData.id}) を削除してもよろしいですか？", "はい", "いいえ"))
                {
                    // Tilemap上のタイルも削除
                    if (currentFieldTilemap != null && currentFieldTilemap.HasTile(tileData.position))
                    {
                        Undo.RecordObject(currentFieldTilemap, "Remove Tile");
                        currentFieldTilemap.SetTile(tileData.position, null);
                        EditorUtility.SetDirty(currentFieldTilemap);
                    }
                    if (currentGimmickTilemap != null && currentGimmickTilemap.HasTile(tileData.position))
                    {
                        Undo.RecordObject(currentGimmickTilemap, "Remove Tile");
                        currentGimmickTilemap.SetTile(tileData.position, null);
                        EditorUtility.SetDirty(currentGimmickTilemap);
                    }

                    tileDataManager.tilesData.Remove(tileData);
                    if (selectedTileData == tileData)
                    {
                        selectedTileData = null;
                    }
                    EditorUtility.SetDirty(tileDataManager);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTileEditingSection()
        {
            GUILayout.Label(new GUIContent("タイル編集", "配置ずみのタイルにマスタ情報を設定します"), EditorStyles.boldLabel);

            if (currentFieldTilemap == null && currentGimmickTilemap == null)
            {
                EditorGUILayout.HelpBox("Tilemapが見つかりません。シーンにTilemapがあるか確認してください。", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            showTileList = EditorGUILayout.Foldout(showTileList, "タイルリスト", true);
            if (showTileList)
            {
                DrawTileList();
            }
        }

        private void DrawSelectedTileGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"選択中のタイル: [{selectedTileData.id}][{selectedTileData.gimmickTileType}][{selectedTileData.fieldTileType}][{selectedTileData.position.x},{selectedTileData.position.y}]", EditorStyles.boldLabel);

            if (selectedTileData != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("ID", selectedTileData.id.ToString());
                EditorGUI.BeginDisabledGroup(true);
                selectedTileData.groupId = int.Parse(groupId);
                selectedTileData.fieldTileType =
                    (FieldTileType)EditorGUILayout.EnumPopup("Field Tile", selectedTileData.fieldTileType);
                selectedTileData.gimmickTileType =
                    (GimmickTileType)EditorGUILayout.EnumPopup("Gimmick Tile", selectedTileData.gimmickTileType);
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(selectedTileData.gimmickTileType == GimmickTileType.NONE);
                selectedTileData.gimmickTileTypeValue = EditorGUILayout.IntField(new GUIContent("GimmickValue", "イベントマスに対応した値を指定してください。\n EVENT: summer2025_hunt_event.id"), selectedTileData.gimmickTileTypeValue);
                EditorGUI.EndDisabledGroup();
                selectedTileData.isMovable = EditorGUILayout.Toggle(new GUIContent("移動可能か","キャラクターが通行可能かどうか（FieldTileに対しての設定）"), selectedTileData.isMovable);
                EditorGUILayout.LabelField("位置", selectedTileData.position.ToString());

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(tileDataManager);
                }

                if (GUILayout.Button("選択解除"))
                {
                    selectedTileData = null;
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExportSection()
        {
            GUILayout.Label("エクスポート", EditorStyles.boldLabel);
            GUILayout.Label($"出力先ファイル: {ExportPath}.txt", EditorStyles.miniBoldLabel);

            if (GUILayout.Button(new GUIContent("エクスポート","マイグレーションデータを出力します。\nsummer2025_hunt_tile.create!関数の引数として使用できます。")))
            {
                try
                {
                    Export();
                    EditorUtility.DisplayDialog("エクスポート完了", "タイルマップデータのエクスポートが完了しました。", "OK");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"エクスポート中にエラーが発生しました: {ex.Message}");
                    EditorUtility.DisplayDialog("エラー", $"エクスポート中にエラーが発生しました: {ex.Message}", "OK");
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (isPlaceMode)
            {
                return;
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var mousePosition = e.mousePosition;
                var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                var worldPoint = ray.origin - ray.direction * (ray.origin.z / ray.direction.z);
                var cellPosition = Vector3Int.zero;
                var tileExists = false;

                if (currentFieldTilemap != null)
                {
                    cellPosition = currentFieldTilemap.WorldToCell(worldPoint);
                    tileExists = currentFieldTilemap.HasTile(cellPosition);
                }

                if (!tileExists && currentGimmickTilemap != null)
                {
                    cellPosition = currentGimmickTilemap.WorldToCell(worldPoint);
                    tileExists = currentGimmickTilemap.HasTile(cellPosition);
                }

                if (tileExists)
                {
                    selectedTileData = tileDataManager.tilesData.FirstOrDefault(d => d.position == cellPosition);
                    focusTileFromSceneView = true;
                    Repaint();
                    e.Use();
                }
            }

            // ハイライト
            if (selectedTileData != null)
            {
                Vector3 cellCenter;
                Vector3 cellSize;
                if (currentFieldTilemap != null && currentFieldTilemap.HasTile(selectedTileData.position))
                {
                    cellCenter = currentFieldTilemap.GetCellCenterWorld(selectedTileData.position);
                    cellSize = currentFieldTilemap.cellSize;
                }
                else if (currentGimmickTilemap != null && currentGimmickTilemap.HasTile(selectedTileData.position))
                {
                    cellCenter = currentGimmickTilemap.GetCellCenterWorld(selectedTileData.position);
                    cellSize = currentGimmickTilemap.cellSize;
                }
                else
                {
                    return;
                }

                DrawHandle(cellSize, cellCenter);
            }
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
            SyncTileDataWithScene();
        }

        private void EditScene()
        {
            var path = EditorUtility.OpenFilePanel("シーンを選択", sceneDir, "unity");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    var relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    EditorSceneManager.OpenScene(relativePath);
                    LoadTilemapsFromScene();
                    SyncTileDataWithScene();
                }
                else
                {
                    EditorUtility.DisplayDialog("エラー", "Assetsフォルダ内のシーンのみ開けます", "OK");
                }
            }
        }

        private void Export()
        {
            var outputPath = $"{ExportPath}.txt";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Summer2025HuntTile.create!(");
                sb.AppendLine("      [");
                for (var i = 0; i < tileDataManager.tilesData.Count; i++)
                {
                    var data = tileDataManager.tilesData[i];
                    var line =
                        $"        {{id: {data.id}, group_id: {data.groupId}, field_tile_type_id: {(int)data.fieldTileType}, gimmick_tile_type_id: {(int)data.gimmickTileType}, gimmick_tile_type_value: {data.gimmickTileTypeValue}, is_movable: {(data.isMovable ? 1 : 0)}, position_x: {data.position.x}, position_y: {data.position.y}}}";
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

        private void DrawHandle(Vector3 cellSize, Vector3 cellCenter)
        {
            var tileColor = Color.yellow;
            TileBase tile = null;

            // 選択タイルの取得
            if (currentFieldTilemap != null && currentFieldTilemap.HasTile(selectedTileData.position))
            {
                tile = currentFieldTilemap.GetTile(selectedTileData.position);
            }
            else if (currentGimmickTilemap != null && currentGimmickTilemap.HasTile(selectedTileData.position))
            {
                tile = currentGimmickTilemap.GetTile(selectedTileData.position);
            }

            // タイルのSpriteから平均色を取得
            if (tile is Tile t && t.sprite != null && t.sprite.texture != null)
            {
                var tex = t.sprite.texture;
                try
                {
                    // SpriteのRect範囲のピクセルを取得
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
                catch
                {
                    /* 読み取り不可の場合は無視 */
                }
            }

            // 選択タイルのハイライト
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

        private List<TileData> SortFilter()
        {
            IEnumerable<TileData> list = tileDataManager.tilesData;

            switch (filterType)
            {
                case FilterType.FieldTileType:
                    if (filterFieldType != FieldTileType.NONE)
                        list = list.Where(d => d.fieldTileType == filterFieldType);
                    break;
                case FilterType.GimmickTileType:
                    if (filterGimmickType != GimmickTileType.NONE)
                        list = list.Where(d => d.gimmickTileType == filterGimmickType);
                    break;
            }

            list = sortType switch
            {
                SortType.ID => sortAsc ? list.OrderBy(d => d.id) : list.OrderByDescending(d => d.id),
                SortType.X => sortAsc ? list.OrderBy(d => d.position.x) : list.OrderByDescending(d => d.position.x),
                SortType.Y => sortAsc ? list.OrderBy(d => d.position.y) : list.OrderByDescending(d => d.position.y),
                SortType.FieldTileType => sortAsc ? list.OrderBy(d => d.fieldTileType) : list.OrderByDescending(d => d.fieldTileType),
                SortType.GimmickTileType => sortAsc ? list.OrderBy(d => d.gimmickTileType) : list.OrderByDescending(d => d.gimmickTileType),
                _ => list
            };

            return list.ToList();
        }
    }
}