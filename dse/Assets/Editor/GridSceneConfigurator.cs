using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Run via menu: DSE > Configure Scene (MapaMax)
// Wires GridVisualizer, GridCharacter, and VictoryPanel into the active scene.
// Run AFTER "DSE > Setup Grid (Etapa 1)" so all prefabs/assets exist.
public static class GridSceneConfigurator
{
    [MenuItem("DSE/Configure Scene (MapaMax)")]
    public static void ConfigureScene()
    {
        // Load assets created by GridSetupWizard
        var levelData  = AssetDatabase.LoadAssetAtPath<GridLevelData>("Assets/Phases/GridLevel_Phase1.asset");
        var tilePrefab = AssetDatabase.LoadAssetAtPath<GridTileView>("Assets/Character & Grid/Prefabs/GridTile.prefab");
        var triPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Character & Grid/Prefabs/CharacterTriangle.prefab");
        var vpPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/VictoryPanel.prefab");
        var phase1     = AssetDatabase.LoadAssetAtPath<PhaseData>("Assets/Phases/Phase_1.asset");

        if (levelData == null || tilePrefab == null || triPrefab == null || vpPrefab == null)
        {
            Debug.LogError("Assets faltando. Execute 'DSE > Setup Grid (Etapa 1)' primeiro.");
            return;
        }

        // ── Phase_1.asset → link gridLevel ──────────────────────────
        if (phase1 != null && phase1.gridLevel == null)
        {
            phase1.gridLevel = levelData;
            EditorUtility.SetDirty(phase1);
        }

        // ── Find scene objects ───────────────────────────────────────
        var mapPanel = GameObject.Find("MapPanel");
        if (mapPanel == null) { Debug.LogError("MapPanel não encontrado na cena."); return; }

        var canvas = mapPanel.GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("Canvas não encontrado."); return; }

        var seqExecutorGO = GameObject.FindObjectOfType<SequenceExecutor>()?.gameObject;
        if (seqExecutorGO == null) { Debug.LogError("SequenceExecutor não encontrado."); return; }

        // ── GridVisualizer on MapPanel ───────────────────────────────
        var visualizer = mapPanel.GetComponent<GridVisualizer>();
        if (visualizer == null)
            visualizer = mapPanel.AddComponent<GridVisualizer>();

        visualizer.levelData  = levelData;
        visualizer.tilePrefab = tilePrefab;
        EditorUtility.SetDirty(mapPanel);

        // ── GridCharacter child of MapPanel ──────────────────────────
        var charGO = mapPanel.transform.Find("GridCharacter")?.gameObject;
        if (charGO == null)
        {
            charGO = new GameObject("GridCharacter");
            charGO.transform.SetParent(mapPanel.transform, false);
            var rt = charGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(70, 70);
        }

        // Instantiate triangle as child
        var triChild = charGO.transform.Find("Triangle")?.gameObject;
        if (triChild == null)
        {
            triChild = PrefabUtility.InstantiatePrefab(triPrefab, charGO.transform) as GameObject;
            triChild.name = "Triangle";
            var triRect = triChild.GetComponent<RectTransform>();
            triRect.anchorMin = triRect.anchorMax = new Vector2(0.5f, 0.5f);
            triRect.pivot     = new Vector2(0.5f, 0.5f);
            triRect.sizeDelta = new Vector2(70, 70);
            triRect.anchoredPosition = Vector2.zero;
        }

        var gridChar = charGO.GetComponent<GridCharacter>();
        if (gridChar == null)
            gridChar = charGO.AddComponent<GridCharacter>();

        gridChar.levelData        = levelData;
        gridChar.visualizer       = visualizer;
        gridChar.characterRect    = triChild.GetComponent<RectTransform>();
        EditorUtility.SetDirty(charGO);

        // ── SequenceExecutor → point to GridCharacter ────────────────
        var seqExec = seqExecutorGO.GetComponent<SequenceExecutor>();
        seqExec.characterReference = gridChar;
        seqExec.stepDelay          = 0.4f;
        EditorUtility.SetDirty(seqExecutorGO);

        // ── VictoryPanel on Canvas ───────────────────────────────────
        var existingVP = canvas.GetComponentInChildren<VictoryPanel>(true);
        if (existingVP == null)
        {
            var vpGO = PrefabUtility.InstantiatePrefab(vpPrefab, canvas.transform) as GameObject;
            vpGO.name = "VictoryPanel";
            vpGO.SetActive(true);
            EditorUtility.SetDirty(canvas.gameObject);
        }

        // ── Deactivate MockGridCharacter if present ──────────────────
        var mockChar = GameObject.FindObjectOfType<MockGridCharacter>();
        if (mockChar != null)
        {
            mockChar.gameObject.SetActive(false);
            EditorUtility.SetDirty(mockChar.gameObject);
            Debug.Log("MockGridCharacter desativado.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Cena configurada! Salve a cena (Ctrl+S) e pressione Play.");
    }
}
