using UnityEngine;

public enum ForwardAxis { Rank, File }   // Rank = eje Y del grid, File = eje X del grid
public enum Sign { Positive, Negative }  // Sentido de avance de BLANCAS

public struct PawnMoveRequest
{
    public Vector2Int from, to;
    public SideColor color;
    public bool hasMoved;
}

public struct PawnMoveResult
{
    public bool valid;
    public bool isCapture;
    public Vector2Int? captureSq;
    public bool isDoubleStep;
}

[DisallowMultipleComponent]
public class PawnValidator : MonoBehaviour
{
    [Header("Orientaci�n del tablero")]
    public ForwardAxis forwardAxis = ForwardAxis.File;   // en tu caso avanzan por X
    public Sign whiteForward = Sign.Negative;            // ajusta seg�n tu giro

    [Header("�ndices iniciales sobre el eje de avance (0..7)")]
    public int whiteStartIndex = 1;
    public int blackStartIndex = 6;

    [Header("Opciones de validaci�n")]
    public bool requireStartIndexForDoubleStep = false;

    [Header("Debug")]
    public bool debugLogs = false;

    // Guarda temporalmente la casilla objetivo del en passant
    private Vector2Int? enPassantTarget = null;
    private SideColor? enPassantColor = null;

    public SoundController soundcontroller;

    public bool Validate(PawnMoveRequest req, ChessBoardState state, out PawnMoveResult res)
    {
        res = new PawnMoveResult();

        int dx = req.to.x - req.from.x;
        int dy = req.to.y - req.from.y;

        int advance = (forwardAxis == ForwardAxis.Rank) ? dy : dx;
        int across = (forwardAxis == ForwardAxis.Rank) ? dx : dy;

        int whiteDir = (whiteForward == Sign.Positive) ? +1 : -1;
        int dir = (req.color == SideColor.White) ? whiteDir : -whiteDir;

        int fromIndex = (forwardAxis == ForwardAxis.Rank) ? req.from.y : req.from.x;
        int startIndex = (req.color == SideColor.White) ? whiteStartIndex : blackStartIndex;

        // --- Validaciones ---
        if (advance == 0 || Mathf.Sign(advance) != Mathf.Sign(dir))
        {
            if (debugLogs) Debug.Log($"Pawn INVALID: retroceso o lateral. advance={advance} dir={dir}");
            return false;
        }

        // 1) Avance recto
        if (across == 0)
        {
            // 1 paso
            if (advance == dir && !state.IsOccupied(req.to))
            {
                res.valid = true;
                ResetEnPassant(); // limpiar anterior

                soundcontroller.Sound_MoverFicha();

                return true;
            }

            // 2 pasos (doble avance)
            bool atStartIndex = (fromIndex == startIndex);
            bool firstMove = !req.hasMoved;
            bool startOk = requireStartIndexForDoubleStep ? (firstMove && atStartIndex) : firstMove;

            if (startOk && advance == 2 * dir)
            {
                Vector2Int mid = (forwardAxis == ForwardAxis.Rank)
                                ? new Vector2Int(req.from.x, req.from.y + dir)
                                : new Vector2Int(req.from.x + dir, req.from.y);

                if (!state.IsOccupied(mid) && !state.IsOccupied(req.to))
                {
                    res.valid = true;
                    res.isDoubleStep = true;

                    // marcar posible en passant
                    enPassantTarget = mid;
                    enPassantColor = req.color;

                    if (debugLogs) Debug.Log($"Pawn OK: doble paso. EnPassantTarget={mid}");

                    soundcontroller.Sound_MoverFicha();
                    return true;
                }
            }

            if (debugLogs) Debug.Log("Pawn INVALID: avance recto no permitido.");
            return false;
        }

        // 2) Captura diagonal (1 transversal y 1 de avance)
        if (Mathf.Abs(across) == 1 && advance == dir)
        {
            // 2a) Captura normal
            if (state.TryGet(req.to, out var target) && target.color != req.color)
            {
                res.valid = true;
                res.isCapture = true;
                res.captureSq = req.to;
                ResetEnPassant();
                if (debugLogs) Debug.Log("Pawn OK: captura normal.");

                soundcontroller.Sound_MoverFicha();
                return true;
            }

            // 2b) Captura al paso
            if (IsCurrentEnPassantTarget(req.to, req.color, out var captureSq))
            {
                res.valid = true;
                res.isCapture = true;
                res.captureSq = captureSq;

                // desactivar en passant tras usarlo
                ResetEnPassant();

                // opcional: podr�as marcar la pieza como "muerta" en tu board manager
                // state.RemovePiece(captureSq);

                if (debugLogs) Debug.Log($"Pawn OK: captura al paso. Pe�n capturado en {captureSq}");

                soundcontroller.Sound_MoverFicha();
                return true;
            }

            if (debugLogs) Debug.Log("Pawn INVALID: diagonal sin rival ni en passant.");
            return false;
        }

        if (debugLogs) Debug.Log("Pawn INVALID: movimiento no reconocido.");
        return false;
    }

    /// <summary>
    /// Comprueba si el destino actual corresponde a una captura al paso v�lida.
    /// </summary>
    private bool IsCurrentEnPassantTarget(Vector2Int dest, SideColor moverColor, out Vector2Int capturedSq)
    {
        capturedSq = Vector2Int.zero;

        if (!enPassantTarget.HasValue || !enPassantColor.HasValue)
            return false;

        // no puede capturar el mismo color
        if (moverColor == enPassantColor.Value)
            return false;

        if (dest != enPassantTarget.Value)
            return false;

        int dir = (moverColor == SideColor.White)
            ? ((whiteForward == Sign.Positive) ? +1 : -1)
            : -((whiteForward == Sign.Positive) ? +1 : -1);

        capturedSq = (forwardAxis == ForwardAxis.Rank)
            ? new Vector2Int(dest.x, dest.y - dir)
            : new Vector2Int(dest.x - dir, dest.y);

        soundcontroller.Sound_MoverFicha();
        return true;
    }

    private void ResetEnPassant()
    {
        enPassantTarget = null;
        enPassantColor = null;
    }
}



