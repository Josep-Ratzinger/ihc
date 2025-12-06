using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceTurnGate), typeof(Rigidbody))]
public class KnightMoveDriver : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;
    public MoveCommitter committer;
    public KnightValidator validator;
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
        if (!validator) validator = GetComponent<KnightValidator>() ?? gameObject.AddComponent<KnightValidator>();

        if (!hints) hints = MoveHintsHighlights.Instance ?? FindObjectOfType<MoveHintsHighlights>();

    }

    void Start()
    {
        // Ocupación inicial
        var sq = board.WorldToSquare(transform.position);
        state.RegisterAt(sq, Gate, "Knight");
    }

    // Conectar en InteractableUnityEventWrapper → When Select
    public void OnGrabbed()
    {
        // Turno: PieceTurnGate ya bloquea agarrar; por si acaso:
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
        hints?.ShowKnightMoves(_grabStartSq, Gate.color);
    }

    // Conectar en InteractableUnityEventWrapper → When Unselect
    public void OnReleased()
    {
        hints?.ClearAll();
        if (!_grabbed)
        {
            // No era su turno → volver a donde estaba
            SnapToSquare(_grabStartSq, _grabYaw);
            return;
        }
        _grabbed = false;

        // Destino
        Vector2Int endSq = board.WorldToSquare(transform.position);

        // Sin cambio → recentra
        if (endSq == _grabStartSq)
        {
            SnapToSquare(_grabStartSq, _grabYaw);
            return;
        }

        // Validar caballero
        if (!validator.Validate(_grabStartSq, endSq, Gate.color, state, out var captureSq))
        {
            StartCoroutine(ReturnToStart());   // inválido → regresar, NO cambia turno
            return;
        }

        // Válido → commit (mueve, captura si aplica, registra y cambia turno)
        committer.CommitKnightMove(this, _grabStartSq, endSq, captureSq);
    }

    IEnumerator ReturnToStart()
    {
        yield return null; // deja terminar el frame del release
        SnapToSquare(_grabStartSq, _grabYaw);
    }

    // -------- Snap idéntico a Pawn/Rook (para evitar “flotar”) --------
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

        state.RegisterAt(sq, Gate, "Knight");
    }
}


