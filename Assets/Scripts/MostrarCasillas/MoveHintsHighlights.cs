using System.Collections.Generic;
using UnityEngine;

public class MoveHintsHighlights : MonoBehaviour
{
    public static MoveHintsHighlights Instance { get; private set; }

    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;

    [Header("Visual")]
    public GameObject highlightPrefab;   // tu prefab con luz/quad
    public float yOffset = 0.01f;        // altura sobre el tablero

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

    public void ClearAll()
    {
        foreach (var go in _active)
            if (go) Destroy(go);
        _active.Clear();
    }

    /// <summary>
    /// Muestra todas las casillas a las que el alfil podría moverse
    /// desde "from" con el color indicado.
    /// </summary>
    public void ShowBishopMoves(Vector2Int from, SideColor color)
    {
        ClearAll();
        if (!board || !state || !highlightPrefab) return;

        // 4 diagonales
        Vector2Int[] dirs =
        {
            new Vector2Int( 1,  1),
            new Vector2Int( 1, -1),
            new Vector2Int(-1,  1),
            new Vector2Int(-1, -1)
        };

        foreach (var dir in dirs)
        {
            var sq = from;

            while (true)
            {
                sq += dir;

                // fuera del tablero → se acaba esa dirección
                if (!board.IsInsideSquare(sq))
                    break;

                bool isCapture = false;

                // ¿Hay pieza?
                if (state.TryGet(sq, out var info))
                {
                    if (info.color == color)
                    {
                        // pieza aliada → no se puede pisar ni seguir
                        break;
                    }
                    else
                    {
                        // pieza enemiga → se puede capturar, pero ahí termina
                        isCapture = true;
                    }
                }

                SpawnHighlight(sq, isCapture);

                if (isCapture)
                    break; // no seguimos más allá de la pieza capturada
            }
        }
    }

    void SpawnHighlight(Vector2Int sq, bool isCapture)
    {
        var center = board.SquareCenterWorld(sq);
        var pos = center + Vector3.up * yOffset;

        var go = Instantiate(highlightPrefab, pos, Quaternion.identity);
        _active.Add(go);

        // OPCIONAL: colorear diferente si es casilla de captura
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend != null && isCapture)
        {
            if (rend.material.HasProperty("_Color"))
                rend.material.color = Color.red;
        }
    }
}
