using System.Collections.Generic;
using UnityEngine;

public class ChessBoardState : MonoBehaviour
{
    public static ChessBoardState Instance;

    [Header("Refs")]
    public BoardBounds board;   // arrástralo

    // Ocupación por casilla
    public struct PieceInfo
    {
        public SideColor color;
        public PieceTurnGate gate;
        public Transform root;
        public string type; // "Pawn" por ahora (sirve luego)
    }

    private Dictionary<Vector2Int, PieceInfo> occ = new();

    // En passant (válido SOLO en el turno inmediatamente siguiente)
    private struct EnPassantInfo
    {
        public bool valid;
        public SideColor moverColor;
        public Vector2Int from, to, targetSquare; // target = casilla "saltada"
        public PieceInfo pawnInfo;                // peón que dio el doble paso
        public int createdTurnIndex;
    }
    private EnPassantInfo enPassant;

    // contador de jugadas (para en passant)
    public int turnIndex = 0;

    void Awake()
    {
        Instance = this;
        if (!board) board = GetComponent<BoardBounds>() ?? FindObjectOfType<BoardBounds>();
    }

    void Start()
    {
        RebuildOccupancyFromScene();
    }

    // --- OCCUPANCY ----------------------------------------------------------
    public void RebuildOccupancyFromScene()
    {
        occ.Clear();
        var gates = FindObjectsOfType<PieceTurnGate>(false); // solo activos
        foreach (var g in gates)
        {
            var sq = board.WorldToSquare(g.transform.position);
            var info = new PieceInfo { color = g.color, gate = g, root = g.transform, type = "Any" };
            occ[sq] = info;
        }
    }

    public bool TryGet(Vector2Int sq, out PieceInfo info) => occ.TryGetValue(sq, out info);
    public bool IsOccupied(Vector2Int sq) => occ.ContainsKey(sq);

    public void RegisterAt(Vector2Int sq, PieceTurnGate gate, string type = "Any")
    {
        // elimina ubicación anterior de ese gate
        Vector2Int? toRemove = null;
        foreach (var kv in occ)
            if (kv.Value.gate == gate) { toRemove = kv.Key; break; }
        if (toRemove.HasValue) occ.Remove(toRemove.Value);

        occ[sq] = new PieceInfo { color = gate.color, gate = gate, root = gate.transform, type = type };
    }

    public void RemoveAt(Vector2Int sq) => occ.Remove(sq);

    public void CaptureAt(Vector2Int sq)
    {
        if (occ.TryGetValue(sq, out var info))
        {
            if (info.root) info.root.gameObject.SetActive(false); // quitar del tablero
            occ.Remove(sq);
        }
    }

    public void CaptureGiven(PieceInfo info)
    {
        if (info.root) info.root.gameObject.SetActive(false);
        // limpiar su entrada si existía
        Vector2Int? key = null;
        foreach (var kv in occ) if (kv.Value.gate == info.gate) { key = kv.Key; break; }
        if (key.HasValue) occ.Remove(key.Value);
    }

    // --- EN PASSANT ---------------------------------------------------------
    public void ClearEnPassant() => enPassant.valid = false;

    public void SetEnPassant(SideColor color, Vector2Int from, Vector2Int to, PieceInfo pawnInfo)
    {
        int dir = (color == SideColor.White) ? 1 : -1;
        var target = new Vector2Int(from.x, from.y + dir); // casilla "saltada"
        enPassant = new EnPassantInfo
        {
            valid = true,
            moverColor = color,
            from = from,
            to = to,
            targetSquare = target,
            pawnInfo = pawnInfo,
            createdTurnIndex = turnIndex
        };
    }

    public bool IsCurrentEnPassantTarget(Vector2Int square, SideColor capturerColor, out PieceInfo capturedPawn)
    {
        // válido SOLO en el turno inmediatamente siguiente
        if (enPassant.valid &&
            turnIndex == enPassant.createdTurnIndex + 1 &&
            square == enPassant.targetSquare &&
            capturerColor != enPassant.moverColor)
        {
            capturedPawn = enPassant.pawnInfo;
            return true;
        }
        capturedPawn = default;
        return false;
    }

    public void OnLegalMoveCommitted(bool pawnDoubleStep, SideColor moverColor, Vector2Int from, Vector2Int to)
    {
        if (pawnDoubleStep)
        {
            if (occ.TryGetValue(to, out var info))
                SetEnPassant(moverColor, from, to, info);
        }
        else
        {
            ClearEnPassant();
        }
        turnIndex++;
    }
}

