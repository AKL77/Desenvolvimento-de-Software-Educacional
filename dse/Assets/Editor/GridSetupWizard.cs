using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// Run via menu: DSE > Setup Grid (Etapa 1)
// Creates GridLevel_Phase1.asset, GridTile.prefab, and CharacterTriangle.prefab.
public static class GridSetupWizard
{
    [MenuItem("DSE/Setup Grid (Etapa 1)")]
    public static void SetupAll()
    {
        CreateGridLevelAsset();
        CreateGridTilePrefab();
        CreateCharacterTrianglePrefab();
        CreateVictoryPanelPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("DSE Grid Setup concluído! Verifique Assets/Phases/ e Assets/Character & Grid/Prefabs/");
    }

    // ── GridLevel_Phase1.asset ──────────────────────────────────────

    static void CreateGridLevelAsset()
    {
        const string path = "Assets/Phases/GridLevel_Phase1.asset";
        if (AssetDatabase.LoadAssetAtPath<GridLevelData>(path) != null)
        {
            Debug.Log("GridLevel_Phase1.asset já existe, pulando.");
            return;
        }

        var data = ScriptableObject.CreateInstance<GridLevelData>();
        data.width         = 5;
        data.height        = 5;
        data.startPosition = Vector2Int.zero;
        data.startFacing   = FacingDirection.Up;
        data.goalPosition  = new Vector2Int(2, 4);
        data.ResetGrid();

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"Criado: {path}");
    }

    // ── GridTile.prefab ─────────────────────────────────────────────

    static void CreateGridTilePrefab()
    {
        const string dir  = "Assets/Character & Grid/Prefabs";
        const string path = dir + "/GridTile.prefab";

        EnsureDirectory(dir);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("GridTile.prefab já existe, pulando.");
            return;
        }

        // Root
        var root = new GameObject("GridTile");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(100, 100);

        // Background Image
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(root.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Border (outline via child Image slightly smaller and darker)
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(root.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.offsetMin = new Vector2(3, 3);
        borderRect.offsetMax = new Vector2(-3, -3);
        var borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(1f, 1f, 1f, 0.35f);

        // GoalIcon (Text "★" centered, hidden by default)
        var goalObj = new GameObject("GoalIcon");
        goalObj.transform.SetParent(root.transform, false);
        var goalRect = goalObj.AddComponent<RectTransform>();
        goalRect.anchorMin = new Vector2(0.5f, 0.5f);
        goalRect.anchorMax = new Vector2(0.5f, 0.5f);
        goalRect.pivot     = new Vector2(0.5f, 0.5f);
        goalRect.sizeDelta = new Vector2(60, 60);
        var goalText = goalObj.AddComponent<TextMeshProUGUI>();
        goalText.text      = "★";
        goalText.color     = new Color(1f, 0.85f, 0f, 1f);
        goalText.alignment = TextAlignmentOptions.Center;
        goalText.fontSize  = 36;
        goalObj.SetActive(false);

        // GridTileView component
        var view = root.AddComponent<GridTileView>();
        view.background = bgImg;
        view.goalIcon   = goalObj;

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Criado: {path}");
    }

    // ── CharacterTriangle.prefab ────────────────────────────────────

    static void CreateCharacterTrianglePrefab()
    {
        const string dir  = "Assets/Character & Grid/Prefabs";
        const string path = dir + "/CharacterTriangle.prefab";

        EnsureDirectory(dir);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("CharacterTriangle.prefab já existe, pulando.");
            return;
        }

        var root = new GameObject("CharacterTriangle");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(70, 70);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        var tri = root.AddComponent<TriangleGraphic>();
        tri.color = Color.white;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Criado: {path}");
    }

    // ── VictoryPanel.prefab ─────────────────────────────────────────

    static void CreateVictoryPanelPrefab()
    {
        const string dir  = "Assets/UI/Prefabs";
        const string path = dir + "/VictoryPanel.prefab";

        EnsureDirectory(dir);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("VictoryPanel.prefab já existe, pulando.");
            return;
        }

        // Root — full-screen overlay
        var root = new GameObject("VictoryPanel");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        var cg = root.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        // Dark overlay
        var overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(root.transform, false);
        var overlayRect = overlayObj.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        var overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImg.raycastTarget = true;

        // Panel box
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(root.transform, false);
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.35f);
        panelRect.anchorMax = new Vector2(0.7f, 0.65f);
        panelRect.sizeDelta = Vector2.zero;
        var panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        // Title text
        var textObj = new GameObject("TitleText");
        textObj.transform.SetParent(panelObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.55f);
        textRect.anchorMax = new Vector2(1f, 0.9f);
        textRect.sizeDelta = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Fase Concluída!";
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 32;
        tmp.fontStyle = FontStyles.Bold;

        // Restart button
        var btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(panelObj.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.25f, 0.1f);
        btnRect.anchorMax = new Vector2(0.75f, 0.4f);
        btnRect.sizeDelta = Vector2.zero;
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        var btn = btnObj.AddComponent<Button>();

        var btnTextObj = new GameObject("Label");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        var btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text      = "Resetar";
        btnTmp.color     = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.fontSize  = 22;

        // VictoryPanel component
        var vp = root.AddComponent<VictoryPanel>();
        vp.canvasGroup    = cg;
        vp.messageText    = tmp;
        vp.restartButton  = btn;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Criado: {path}");
    }

    // Run via menu: DSE > Configure Phase1 (Straight Line)
    // Sets up Fase 1 as a straight horizontal track on the middle row:
    // start at (0,2) facing Right, goal at (4,2), every tile outside that row is Blocked.
    [MenuItem("DSE/Configure Phase1 (Straight Line)")]
    public static void ConfigurePhase1StraightLine()
    {
        const string path = "Assets/Phases/GridLevel_Phase1.asset";
        var data = AssetDatabase.LoadAssetAtPath<GridLevelData>(path);
        if (data == null)
        {
            Debug.LogError("GridLevel_Phase1.asset não encontrado. Execute 'DSE > Setup Grid (Etapa 1)' primeiro.");
            return;
        }

        const int trackRow = 2;
        data.startPosition = new Vector2Int(0, trackRow);
        data.startFacing   = FacingDirection.Right;
        data.goalPosition  = new Vector2Int(data.width - 1, trackRow);

        data.tiles = new GridTileType[data.width * data.height];
        for (int row = 0; row < data.height; row++)
        {
            for (int col = 0; col < data.width; col++)
            {
                int i = row * data.width + col;
                data.tiles[i] = row != trackRow ? GridTileType.Blocked : GridTileType.Walkable;
            }
        }
        data.tiles[trackRow * data.width + (data.width - 1)] = GridTileType.Goal;

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("Fase 1 configurada como linha reta horizontal na row 2.");
    }

    static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(
                System.IO.Path.GetDirectoryName(path).Replace("\\", "/"),
                System.IO.Path.GetFileName(path)
            );
    }
}
