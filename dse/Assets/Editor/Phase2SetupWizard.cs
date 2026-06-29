using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Run via menu: DSE > Setup Phase 2 (Turns)
// Creates Card_TurnRight.asset, GridLevel_Phase2.asset (6x6 L-shaped track),
// and Phase_2.asset wired with MoveForward + TurnLeft + TurnRight cards.
public static class Phase2SetupWizard
{
    [MenuItem("DSE/Setup Phase 2 (Turns)")]
    public static void SetupPhase2()
    {
        var turnRight = CreateTurnRightCard();
        var levelData = CreateGridLevelPhase2();
        CreatePhase2Asset(turnRight, levelData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fase 2 configurada! Veja Assets/Cards/Card_TurnRight.asset, Assets/Phases/GridLevel_Phase2.asset e Assets/Phases/Phase_2.asset");
    }

    // ── Card_TurnRight.asset ─────────────────────────────────────────

    static CardData CreateTurnRightCard()
    {
        const string path = "Assets/Cards/Card_TurnRight.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CardData>(path);
        if (existing != null)
        {
            Debug.Log("Card_TurnRight.asset já existe, pulando.");
            return existing;
        }

        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardId      = "turn_right";
        card.displayName = "Virar pra direita";
        card.cardType    = CardType.Movement;
        card.cardAction  = CardAction.TurnRight;
        card.cardColor   = new Color(0f, 0.5f, 0.35f, 1f);

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"Criado: {path}");
        return card;
    }

    // ── GridLevel_Phase2.asset (6x6, L-shaped track) ──────────────────

    static GridLevelData CreateGridLevelPhase2()
    {
        const string path = "Assets/Phases/GridLevel_Phase2.asset";
        var existing = AssetDatabase.LoadAssetAtPath<GridLevelData>(path);
        if (existing != null)
        {
            Debug.Log("GridLevel_Phase2.asset já existe, pulando.");
            return existing;
        }

        var data = ScriptableObject.CreateInstance<GridLevelData>();
        data.width  = 6;
        data.height = 6;

        // Track: start at (0,0) facing Right, walk along row 0 to (4,0),
        // turn left (now facing Up), walk up column 4 to the goal at (4,5).
        const int cornerCol = 4;
        var trackCells = new List<Vector2Int>();
        for (int col = 0; col <= cornerCol; col++)
            trackCells.Add(new Vector2Int(col, 0));
        for (int row = 1; row < data.height; row++)
            trackCells.Add(new Vector2Int(cornerCol, row));

        data.tiles = new GridTileType[data.width * data.height];
        for (int i = 0; i < data.tiles.Length; i++)
            data.tiles[i] = GridTileType.Blocked;

        foreach (var cell in trackCells)
            data.tiles[cell.y * data.width + cell.x] = GridTileType.Walkable;

        data.startPosition = new Vector2Int(0, 0);
        data.startFacing   = FacingDirection.Right;
        data.goalPosition  = new Vector2Int(cornerCol, data.height - 1);
        data.tiles[data.goalPosition.y * data.width + data.goalPosition.x] = GridTileType.Goal;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"Criado: {path}");
        return data;
    }

    // ── Phase_2.asset ──────────────────────────────────────────────────

    static void CreatePhase2Asset(CardData turnRight, GridLevelData levelData)
    {
        const string path = "Assets/Phases/Phase_2.asset";
        if (AssetDatabase.LoadAssetAtPath<PhaseData>(path) != null)
        {
            Debug.Log("Phase_2.asset já existe, pulando.");
            return;
        }

        var moveForward = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Card_MoveForward.asset");
        var turnLeft     = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Card_TurnLeft.asset");

        var phase = ScriptableObject.CreateInstance<PhaseData>();
        phase.phaseId      = "phase_2";
        phase.phaseName    = "Fase 2";
        phase.lineCount    = 8;
        phase.availableCards = new List<CardData> { moveForward, turnLeft, turnRight };
        phase.gridLevel    = levelData;

        AssetDatabase.CreateAsset(phase, path);
        Debug.Log($"Criado: {path}");
    }
}
