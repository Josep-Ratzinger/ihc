using UnityEngine;

[DisallowMultipleComponent]
public class MoveCommitter : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;

    void Reset()
    {
        board = GetComponent<BoardBounds>();
        state = GetComponent<ChessBoardState>();
    }

    public void CommitPawnMove(PieceMoveDriver driver,
                               Vector2Int from, Vector2Int to,
                               PawnMoveResult result)
    {
        // Captura normal
        if (result.isCapture && result.captureSq.HasValue)
            state.CaptureAt(result.captureSq.Value);

        // Snap a la casilla destino
        driver.SnapToSquare(to);

        // Estado de ocupación
        state.RegisterAt(to, driver.Gate, "Pawn");
        state.RemoveAt(from);

        // Marcar que ya se movió (bloquea dobles pasos posteriores)
        driver.MarkMoved();

        // (en passant se integrará luego aquí)
        state.ClearEnPassant();

        // Cambiar turno
        TurnManager.Instance?.NotifyPieceMoved(driver.Gate.color);
    }
    // dentro de MoveCommitter.cs
    // MoveCommitter.cs (método nuevo o reemplazo del que tengas)
    public void CommitRookMove(RookMoveDriver driver, Vector2Int from, Vector2Int to, Vector2Int? captureSq)
    {
        // Captura (si hay algo) — ojo: el GO está en occ.gate
        if (captureSq.HasValue && state.TryGet(captureSq.Value, out var occ) && occ.gate)
        {
            if (occ.gate.gameObject) Destroy(occ.gate.gameObject);
            // NOTA: sin ClearAt, simplemente sobreescribiremos la casilla de destino abajo.
        }

        // Mover visualmente
        driver.SnapToSquare(to);
        driver.NotifyMoved();
        // Registrar ocupación en destino (sobreescribe si había algo)
        state.RegisterAt(to, driver.Gate, "Rook");

        // (Opcional) si tu RegisterAt NO limpia el origen, deja esto comentado hasta añadir ClearAt:
        // state.ClearAt(from);

        // Cambiar turno
        TurnManager.Instance?.NotifyPieceMoved(driver.Gate.color);
    }

    public void CommitKnightMove(KnightMoveDriver driver, Vector2Int from, Vector2Int to, Vector2Int? captureSq)
    {
        if (captureSq.HasValue && state.TryGet(captureSq.Value, out var occ) && occ.gate)
            if (occ.gate.gameObject) Destroy(occ.gate.gameObject);

        driver.SnapToSquare(to);
        state.RegisterAt(to, driver.Gate, "Knight");

        TurnManager.Instance?.NotifyPieceMoved(driver.Gate.color);
    }
    public void CommitBishopMove(BishopMoveDriver driver, Vector2Int from, Vector2Int to, Vector2Int? captureSq)
    {
        if (captureSq.HasValue && state.TryGet(captureSq.Value, out var occ) && occ.gate)
            if (occ.gate.gameObject) Destroy(occ.gate.gameObject);

        driver.SnapToSquare(to);
        state.RegisterAt(to, driver.Gate, "Bishop");

        TurnManager.Instance?.NotifyPieceMoved(driver.Gate.color);
    }
    public void CommitQueenMove(QueenMoveDriver driver, Vector2Int from, Vector2Int to, Vector2Int? captureSq)
    {
        if (captureSq.HasValue && state.TryGet(captureSq.Value, out var occ) && occ.gate)
            if (occ.gate.gameObject) Destroy(occ.gate.gameObject);

        driver.SnapToSquare(to);
        state.RegisterAt(to, driver.Gate, "Queen");

        TurnManager.Instance?.NotifyPieceMoved(driver.Gate.color);
    }

    public void CommitKingMove(
    KingMoveDriver driver,
    Vector2Int from,
    Vector2Int to,
    Vector2Int? captureSq,
    Vector2Int? rookFrom,
    Vector2Int? rookTo)
    {
        // Captura
        if (captureSq.HasValue && state.TryGet(captureSq.Value, out var captured))
        {
            if (captured.type == "King")
            {
                state.CaptureAt(captureSq.Value);
                driver.SnapToSquare(to);
                state.RegisterAt(to, driver.Gate, "King");
                TurnManager.Instance?.EndGame($"{driver.Gate.color} captured the King");
                return;
            }
            state.CaptureAt(captureSq.Value);
        }

        // Movimiento normal del rey
        driver.SnapToSquare(to);
        state.RemoveAt(from);
        state.RegisterAt(to, driver.Gate, "King");

        // Enroque (si aplica)
        if (rookFrom.HasValue && rookTo.HasValue)
        {
            if (state.TryGet(rookFrom.Value, out var rookInfo)
                && rookInfo.type == "Rook"
                && rookInfo.color == driver.Gate.color)
            {
                var rookDriver = rookInfo.root ? rookInfo.root.GetComponent<RookMoveDriver>() : null;
                if (rookDriver)
                {
                    rookDriver.SnapToSquare(rookTo.Value);
                    rookDriver.NotifyMoved();
                }
                state.RemoveAt(rookFrom.Value);
                state.RegisterAt(rookTo.Value, rookInfo.gate, "Rook");
            }
        }

        TurnManager.Instance?.NotifyPieceMoved(driver.Gate.color);
    }



}

