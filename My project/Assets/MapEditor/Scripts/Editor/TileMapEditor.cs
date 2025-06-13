using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MapEditor
{
    /// <summary>
    /// タイルマップエディタウィンドウ（UI・全体制御）
    /// </summary>
    public class TileMapEditor : EditorWindow
    {
        private TileMapSceneController sceneController;
        private TileMapExporter exporter;
        private TileMapUIHelper uiHelper;

        private TileDataManager tileDataManager => sceneController.TileDataManager;
        private Tilemap currentFieldTilemap => sceneController.CurrentFieldTilemap;
        private Tilemap currentGimmickTilemap => sceneController.CurrentGimmickTilemap;
        private Camera sceneCamera => sceneController.SceneCamera;
        private SpriteRenderer backgroundSprite => sceneController.BackgroundSprite;

        private TileData selectedTileData = null;
        private string groupId = "1";
        private bool showTileList = false;
        private Vector2 mainScrollPosition;
        private Vector2 scrollPosition;
        private Rect selectedTileRect;
        private bool focusTileFromSceneView = false;
        private string warningMessage = null;

        private enum FilterType { All, FieldTileType, GimmickTileType }
        private FilterType filterType = FilterType.All;
        private FieldTileType filterFieldType = FieldTileType.NONE;
        private GimmickTileType filterGimmickType = GimmickTileType.NONE;
        private enum SortType { ID, X, Y, FieldTileType, GimmickTileType }
        private SortType sortType = SortType.ID;
        private bool sortAsc = true;
        private bool isPlaceMode = false;

        /// <summary>
        /// エディタウィンドウを表示
        /// </summary>
        [MenuItem("Boys2/Tile Map Editor")]
        public static void ShowWindow()
        {
            GetWindow<TileMapEditor>("Tile Map Editor");
        }

        /// <summary>
        /// 有効化時の初期化
        /// </summary>
        private void OnEnable()
        {
            sceneController = new TileMapSceneController();
            exporter = new TileMapExporter();
            uiHelper = new TileMapUIHelper();

            SceneView.duringSceneGui += OnSceneGUI;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
            Initialize();
        }

        /// <summary>
        /// 無効化時のイベント解除
        /// </summary>
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        }

        /// <summary>
        /// シーン変更時の初期化
        /// </summary>
        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            Initialize();
        }

        /// <summary>
        /// フォーカス時のTilemap再取得
        /// </summary>
        private void OnFocus()
        {
            sceneController.RefreshSceneObjects();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Initialize()
        {
            warningMessage = null;
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.Contains("MapEditorTemplate"))
            {
                warningMessage = "テンプレートシーンでは編集できません。実際のマップシーンを開いてください。";
                return;
            }
            if (!sceneName.Contains("tilemap"))
            {
                warningMessage = "このエディタはtilemapシーンでのみ使用できます。";
                return;
            }
            sceneController.Initialize(groupId);
        }

        /// <summary>
        /// メインGUI描画
        /// </summary>
        private void OnGUI()
        {
            DrawBasicSettings();
            EditorGUILayout.Space(10);
            DrawSceneControls();

            if (!string.IsNullOrEmpty(warningMessage))
            {
                EditorGUILayout.HelpBox(warningMessage, MessageType.Error);
                return;
            }

            // SceneViewから選択された直後のみフォーカス
            if (focusTileFromSceneView && Event.current.type == EventType.Layout)
            {
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
            uiHelper.InitializeStyles();

            EditorGUILayout.Space(10);
            DrawExportSection();
            EditorGUILayout.Space(10);
            DrawTileEditingSection();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 基本設定UI描画
        /// </summary>
        private void DrawBasicSettings()
        {
            GUILayout.Label(new GUIContent("基本設定", "エディタの基本設定を行います"), EditorStyles.boldLabel);
            GUILayout.Label("このエディタは、タイルマップの編集とsummer2025_hunt_tileマスタのエクスポートを行うためのツールです。", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            var selectModeStyle = uiHelper.GetSelectModeStyle(isPlaceMode);
            var placeModeStyle = uiHelper.GetPlaceModeStyle(isPlaceMode);
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

        /// <summary>
        /// シーン操作UI描画
        /// </summary>
        private void DrawSceneControls()
        {
            GUILayout.Label("シーン操作", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("新規作成", "新しくマップを作製します。新規マップの作成を行うときに押してください")))
            {
                sceneController.CreateNewScene(groupId);
            }

            if (GUILayout.Button(new GUIContent("編集", "すでに存在するマップを編集します。マップを選択して編集を行うときに押してください")))
            {
                sceneController.EditScene();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// タイルリストUI描画
        /// </summary>
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

        /// <summary>
        /// タイルセルUI描画
        /// </summary>
        private void DrawTileCell(TileData tileData)
        {
            var tileName = $"[{tileData.id}]{tileData.fieldTileType}_{tileData.gimmickTileType}";
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            var isSelected = tileData == selectedTileData;
            var style = uiHelper.GetTileStyle(isSelected);
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
                    sceneController.RemoveTile(tileData);
                    if (selectedTileData == tileData)
                    {
                        selectedTileData = null;
                    }
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// タイル編集セクションUI描画
        /// </summary>
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

        /// <summary>
        /// 選択中タイル詳細UI描画
        /// </summary>
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
                selectedTileData.fieldTileType = (FieldTileType)EditorGUILayout.EnumPopup("Field Tile", selectedTileData.fieldTileType);
                selectedTileData.gimmickTileType = (GimmickTileType)EditorGUILayout.EnumPopup("Gimmick Tile", selectedTileData.gimmickTileType);
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(selectedTileData.gimmickTileType == GimmickTileType.NONE);
                selectedTileData.gimmickTileMasterId = EditorGUILayout.IntField(new GUIContent("マスターID", "イベントマスに対応した値を指定してください。\nITEM: summer2025_hunt_item_bonuses.id\nCHARACTER: summer2025_hunt_character.id\nRIDDLE: summer2025_hunt_riddle.id\nHINT: summer2025_hunt_hint.id"), selectedTileData.gimmickTileMasterId);
                EditorGUI.EndDisabledGroup();
                selectedTileData.isMovable = EditorGUILayout.Toggle(new GUIContent("移動可能か", "キャラクターが通行可能かどうか（FieldTileに対しての設定）"), selectedTileData.isMovable);
                EditorGUILayout.LabelField("位置", selectedTileData.position.ToString());

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(tileDataManager);
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("選択解除"))
                {
                    selectedTileData = null;
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// エクスポートセクションUI描画
        /// </summary>
        private void DrawExportSection()
        {
            GUILayout.Label("エクスポート", EditorStyles.boldLabel);
            GUILayout.Label($"出力先ファイル: {sceneController.ExportPath}.txt", EditorStyles.miniBoldLabel);

            if (GUILayout.Button(new GUIContent("エクスポート", "マイグレーションデータを出力します。\nsummer2025_hunt_tile.create!関数の引数として使用できます。")))
            {
                try
                {
                    exporter.Export(tileDataManager, sceneController.ExportPath);
                    EditorUtility.DisplayDialog("エクスポート完了", "タイルマップデータのエクスポートが完了しました。", "OK");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"エクスポート中にエラーが発生しました: {ex.Message}");
                    EditorUtility.DisplayDialog("エラー", $"エクスポート中にエラーが発生しました: {ex.Message}", "OK");
                }
            }
        }

        /// <summary>
        /// SceneView上のタイル選択・ハイライト
        /// </summary>
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
                sceneController.DrawHandle(selectedTileData);
            }
        }

        /// <summary>
        /// タイルリストのフィルタ・ソート
        /// </summary>
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
