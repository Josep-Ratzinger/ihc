using System.Collections.Generic;
using UnityEngine;

public class MoveHintsHighlights : MonoBehaviour
{
    public static MoveHintsHighlights Instance { get; private set; }

    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;

    [Header("Visual")]
    [Tooltip("Prefab plano (Quad) que se instanciará sobre la casilla")]
    public GameObject highlightPrefab;
    public float yOffset = 0.01f;

    [Header("Colores")]
    public Color normalColor = Color.green;
    public Color captureColor = Color.red;

    readonly List<GameObject> _active = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!board) board = BoardBounds.Instance ?? FindObjectOfType<BoardBounds>();
        if (!state) state = ChessBoardState.Instance ?? FindObjectOfType<ChessBoardState>();
    }

    // --------- Limpiar ---------
    public void ClearAll()
    {
        foreach (var go in _active)
        {
            if (go) Destroy(go);
        }
        _active.Clear();
    }

    // ================= ALFIL / TORRE / REINA =================

    void ShowSliding(Vector2Int from, SideColor color, Vector2Int[] dirs)
    {
        if (!board || !state || !highlightPrefab) return;

        foreach (var dir in dirs)
        {
            var sq = from;

            while (true)
            {
                sq += dir;

                // fuera del tablero -> parar esta dirección
                if (!board.IsInsideSquare(sq))
                    break;

                bool isCapture = false;

                // ¿hay pieza en esa casilla?
                if (state.TryGet(sq, out var info))
                {
                    if (info.color == color)
                    {
                        // pieza aliada -> no se puede pisar ni seguir
                        break;
                    }
                    else
                    {
                        // pieza enemiga -> se puede capturar, pero ahí termina
                        isCapture = true;
                    }
                }

                SpawnHighlight(sq, isCapture);

                if (isCapture)
                    break; // no seguimos más allá de la pieza capturada
            }
        }
    }

    /// <summary>Movimientos posibles de un alfil.</summary>
    public void ShowBishopMoves(Vector2Int from, SideColor color)
    {
        ClearAll();
        Vector2Int[] dirs =
        {
            new Vector2Int( 1,  1),
            new Vector2Int( 1, -1),
            new Vector2Int(-1,  1),
            new Vector2Int(-1, -1)
        };
        ShowSliding(from, color, dirs);
    }

    /// <summary>Movimientos posibles de una torre.</summary>
    public void ShowRookMoves(Vector2Int from, SideColor color)
    {
        ClearAll();
        Vector2Int[] dirs =
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1)
        };
        ShowSliding(from, color, dirs);
    }

    /// <summary>Movimientos posibles de una reina.</summary>
    public void ShowQueenMoves(Vector2Int from, SideColor color)
    {
        ClearAll();
        Vector2Int[] dirs =
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1),
            new Vector2Int( 1,  1),
            new Vector2Int( 1, -1),
            new Vector2Int(-1,  1),
            new Vector2Int(-1, -1)
        };
        ShowSliding(from, color, dirs);
    }

    // ====================== CABALLO ======================

    public void ShowKnightMoves(Vector2Int from, SideColor color)
    {
        ClearAll();
        if (!board || !state || !highlightPrefab) return;

        Vector2Int[] jumps =
        {
            new Vector2Int( 1,  2),
            new Vector2Int( 2,  1),
            new Vector2Int( 2, -1),
            new Vector2Int( 1, -2),
            new Vector2Int(-1, -2),
            new Vector2Int(-2, -1),
            new Vector2Int(-2,  1),
            new Vector2Int(-1,  2),
        };

        foreach (var d in jumps)
        {
            var sq = from + d;
            if (!board.IsInsideSquare(sq)) continue;

            bool isCapture = false;
            if (state.TryGet(sq, out var info))
            {
                if (info.color == color)
                    continue;   // misma pieza -> no se puede
                isCapture = true;
            }

            SpawnHighlight(sq, isCapture);
        }
    }

    // ======================= REY ========================

    public void ShowKingMoves(Vector2Int from, SideColor color)
    {
        ClearAll();
        if (!board || !state || !highlightPrefab) return;

        Vector2Int[] around =
        {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(-1,  0),                        new Vector2Int(1,  0),
            new Vector2Int(-1,  1), new Vector2Int(0,  1), new Vector2Int(1,  1),
        };

        foreach (var d in around)
        {
            var sq = from + d;
            if (!board.IsInsideSquare(sq)) continue;

            bool isCapture = false;
            if (state.TryGet(sq, out var info))
            {
                if (info.color == color)
                    continue;
                isCapture = true;
            }

            SpawnHighlight(sq, isCapture);
        }

        // (Enroques los podrías agregar luego si quieres)
    }
    // ==================== HINTS PARA PEONES ====================

    public void ShowPawnMoves(Vector2Int from, SideColor color, bool hasMoved, PawnValidator validator)
    {
        ClearAll();
        if (!board || !state || !highlightPrefab || !validator) return;

        // ¿el peón avanza por filas (y) o por columnas (x)?
        bool advanceOnRank = (validator.forwardAxis == ForwardAxis.Rank);

        // Dirección “hacia adelante” para blancas
        int whiteDir = (validator.whiteForward == Sign.Positive) ? +1 : -1;
        // Dirección real según el color de la pieza
        int dir = (color == SideColor.White) ? whiteDir : -whiteDir;

        // Índice inicial (para saber si puede hacer doble paso)
        int fromIndex = advanceOnRank ? from.y : from.x;
        int startIndex = (color == SideColor.White)
            ? validator.whiteStartIndex
            : validator.blackStartIndex;

        // ------- AVANCE 1 CASILLA --------
        Vector2Int step1 = advanceOnRank
            ? new Vector2Int(0, dir)     // avanza en Y
            : new Vector2Int(dir, 0);    // avanza en X

        Vector2Int one = from + step1;

        if (board.IsInsideSquare(one) && !state.IsOccupied(one))
        {
            // movimiento normal (verde)
            SpawnHighlight(one, false);

            // ------- AVANCE DOBLE (2 casillas) --------
            bool firstMove = !hasMoved;
            bool startOk = validator.requireStartIndexForDoubleStep
                ? (firstMove && fromIndex == startIndex)
                : firstMove;

            if (startOk)
            {
                Vector2Int two = from + step1 * 2;
                if (board.IsInsideSquare(two) && !state.IsOccupied(two))
                {
                    SpawnHighlight(two, false);
                }
            }
        }

        // ------- CAPTURAS DIAGONALES (rojo) --------
        Vector2Int diagL, diagR;
        if (advanceOnRank)
        {
            diagL = new Vector2Int(from.x - 1, from.y + dir);
            diagR = new Vector2Int(from.x + 1, from.y + dir);
        }
        else
        {
            diagL = new Vector2Int(from.x + dir, from.y - 1);
            diagR = new Vector2Int(from.x + dir, from.y + 1);
        }

        TrySpawnPawnCapture(diagL, color);
        TrySpawnPawnCapture(diagR, color);
    }

    void TrySpawnPawnCapture(Vector2Int sq, SideColor color)
    {
        if (!board.IsInsideSquare(sq)) return;

        if (state.TryGet(sq, out var info))
        {
            if (info.color != color)
            {
                // casilla con enemigo -> rojo
                SpawnHighlight(sq, true);
            }
        }
    }

    // ================== Instanciar cuadritos ==================

    void SpawnHighlight(Vector2Int sq, bool isCapture)
    {
        if (!board || !highlightPrefab) return;

        var center = board.SquareCenterWorld(sq);
        var pos = center + Vector3.up * yOffset;

        var go = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
        _active.Add(go);

        var rend = go.GetComponentInChildren<Renderer>();
        if (rend != null && rend.material.HasProperty("_Color"))
        {
            rend.material.color = isCapture ? captureColor : normalColor;
        }
    }
}

