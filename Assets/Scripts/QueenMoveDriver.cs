using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceTurnGate), typeof(Rigidbody))]
public class QueenMoveDriver : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;
    public MoveCommitter committer;
    public QueenValidator validator;
    public MoveHintsHighlights hints;
    [Header("Snap")]
    public float heightEpsilon = 0.003f;

    public PieceTurnGate Gate { get; private set; }
    Rigidbody _rb; Collider _col;

    Vector2Int _grabStartSq;
    float _grabYaw;
    bool _grabbed;

    void Awake()
    {
        Gate = GetComponent<PieceTurnGate>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponentInChildren<Collider>();

        if (!board) board = BoardBounds.Instance ?? FindObjectOfType<BoardBounds>();
        if (!state) state = ChessBoardState.Instance ?? FindObjectOfType<ChessBoardState>();
        if (!committer) committer = FindObjectOfType<MoveCommitter>();
        if (!validator) validator = GetComponent<QueenValidator>() ?? gameObject.AddComponent<QueenValidator>();

        if (!hints) hints = MoveHintsHighlights.Instance ?? FindObjectOfType<MoveHintsHighlights>();

    }

    void Start()
    {
        var sq = board.WorldToSquare(transform.position);
        state.RegisterAt(sq, Gate, "Queen");
    }

    // InteractableUnityEventWrapper → When Select
    public void OnGrabbed()
    {
        if (TurnManager.Instance && Gate.color != TurnManager.Instance.currentTurn)
        {
            _grabbed = false;
            _grabStartSq = board.WorldToSquare(transform.position);
            _grabYaw = transform.eulerAngles.y;
            return;
        }

        _grabbed = true;
        _grabStartSq = board.WorldToSquare(transform.position);
        _grabYaw = transform.eulerAngles.y;
        hints?.ShowQueenMoves(_grabStartSq, Gate.color);
    }

    // InteractableUnityEventWrapper → When Unselect
    public void OnReleased()
    {
        hints?.ClearAll();
        if (!_grabbed)
        {
            SnapToSquare(_grabStartSq, _grabYaw);
            return;
        }
        _grabbed = false;

        var endSq = board.WorldToSquare(transform.position);

        // Sin cambio → recentrar
        if (endSq == _grabStartSq)
        {
            SnapToSquare(_grabStartSq, _grabYaw);
            return;
        }

        // Validación reina
        if (!validator.Validate(_grabStartSq, endSq, Gate.color, state, out var captureSq))
        {
            StartCoroutine(ReturnToStart());
            return;
        }

        // Válido → commit
        committer.CommitQueenMove(this, _grabStartSq, endSq, captureSq);
    }

    IEnumerator ReturnToStart()
    {
        yield return null; // permite cerrar el frame del release
        SnapToSquare(_grabStartSq, _grabYaw);
    }

    // --- Snap robusto (mismo patrón) ---
    public void SnapToSquare(Vector2Int sq) => SnapToSquare(sq, transform.eulerAngles.y);
    public void SnapToSquare(Vector2Int sq, float yawDeg)
    {
        Vector3 c = board.SquareCenterWorld(sq);
        float y = c.y + (_col ? _col.bounds.extents.y : 0.02f) + Mathf.Max(0f, heightEpsilon);

        bool prevKin = _rb.isKinematic;
        _rb.isKinematic = true;
        transform.SetPositionAndRotation(new Vector3(c.x, y, c.z), Quaternion.Euler(0f, yawDeg, 0f));
        _rb.velocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; _rb.WakeUp();
        _rb.isKinematic = prevKin;

        state.RegisterAt(sq, Gate, "Queen");
    }
}

