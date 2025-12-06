// RookMoveDriver.cs
using System.Collections;                       // <- para IEnumerator / StartCoroutine
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceTurnGate), typeof(Rigidbody))]
public class RookMoveDriver : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;
    public MoveCommitter committer;
    public RookValidator validator;
    public MoveHintsHighlights hints;

    [Header("Ajustes")]
    public float heightEpsilon = 0.003f;

    public PieceTurnGate Gate { get; private set; }
    Rigidbody _rb; Collider _col;

    Vector2Int _grabStartSq;
    float _grabYaw;
    bool _grabbed;

    public bool HasMoved { get; private set; } = false;
    public void NotifyMoved() => HasMoved = true;

    void Awake()
    {
        Gate = GetComponent<PieceTurnGate>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponentInChildren<Collider>();

        if (!board) board = BoardBounds.Instance ?? FindObjectOfType<BoardBounds>();
        if (!state) state = ChessBoardState.Instance ?? FindObjectOfType<ChessBoardState>();
        if (!committer) committer = FindObjectOfType<MoveCommitter>();
        if (!validator) validator = GetComponent<RookValidator>() ?? gameObject.AddComponent<RookValidator>();
        if (!hints) hints = MoveHintsHighlights.Instance ?? FindObjectOfType<MoveHintsHighlights>();

    }

    void Start()
    {
        var sq = board.WorldToSquare(transform.position);
        state.RegisterAt(sq, Gate, "Rook");
    }

    // Conectar en InteractableUnityEventWrapper → When Select
    public void OnGrabbed()
    {

        // si no es su turno, igual memorizamos para re-snap
        _grabStartSq = board.WorldToSquare(transform.position);
        _grabYaw = transform.eulerAngles.y;

        if (TurnManager.Instance && Gate.color != TurnManager.Instance.currentTurn)
        {
            _grabbed = false;
            return;
        }

        _grabbed = true;
        hints?.ShowRookMoves(_grabStartSq, Gate.color);
    }

    // Conectar en InteractableUnityEventWrapper → When Unselect
    public void OnReleased()
    {
        hints?.ClearAll();
        // Si no era su turno → vuelve
        if (!_grabbed)
        {
            StartCoroutine(CoSnap(_grabStartSq, _grabYaw));
            return;
        }
        _grabbed = false;

        // Fuera del tablero → volver
        if (!board.WorldIsOverBoard(transform.position, 0.02f))
        {
            StartCoroutine(CoSnap(_grabStartSq, _grabYaw));
            return;
        }

        // Casilla destino
        Vector2Int endSq = board.WorldToSquare(transform.position);

        // Sin cambio real → recentra y no cuenta
        if (endSq == _grabStartSq)
        {
            StartCoroutine(CoSnap(_grabStartSq, _grabYaw));
            return;
        }

        // Validación de torre
        if (!validator.Validate(_grabStartSq, endSq, Gate.color, state, out var captureSq))
        {
            // Movimiento ilegal → volver (sin cambiar turno)
            StartCoroutine(CoSnap(_grabStartSq, _grabYaw));
            return;
        }

        // Movimiento válido → delega al committer (mueve, captura, registra y cambia turno)
        committer.CommitRookMove(this, _grabStartSq, endSq, captureSq);
    }

    // --- utilidades de snap, igual que PieceMoveDriver ---
    public void SnapToSquare(Vector2Int sq) => StartCoroutine(CoSnap(sq, transform.eulerAngles.y));
    public void SnapToSquare(Vector2Int sq, float yawDeg) => StartCoroutine(CoSnap(sq, yawDeg));

    IEnumerator CoSnap(Vector2Int sq, float yawDeg)
    {
        // esperar a FixedUpdate evita “flotar” tras soltar
        yield return new WaitForFixedUpdate();

        Vector3 c = board.SquareCenterWorld(sq);
        float y = c.y + (_col ? _col.bounds.extents.y : 0.02f) + Mathf.Max(0f, heightEpsilon);

        bool prevKin = _rb.isKinematic;
        _rb.isKinematic = true;
        transform.SetPositionAndRotation(new Vector3(c.x, y, c.z), Quaternion.Euler(0f, yawDeg, 0f));
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.WakeUp();
        _rb.isKinematic = prevKin;

        state.RegisterAt(sq, Gate, "Rook");
    }
}



