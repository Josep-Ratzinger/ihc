using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceTurnGate), typeof(Rigidbody), typeof(PieceMoveDriver))]
public class MoveEndTurnByPosition : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;
    public MoveCommitter committer;      // cambia turno internamente
    public PawnValidator pawnValidator;  // valida peón (puedes cambiar por otros validators)

    [Header("Visual")]
    public float heightEpsilon = 0.003f; // evita “flotar” al snap

    PieceTurnGate _gate;
    PieceMoveDriver _driver;
    Rigidbody _rb; Collider _col;

    Vector2Int _startSq;
    float _startYaw;
    bool _grabbed;
    bool _hasMoved; // para doble paso del peón

    void Awake()
    {
        _gate = GetComponent<PieceTurnGate>();
        _driver = GetComponent<PieceMoveDriver>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponentInChildren<Collider>();

        if (!board) board = BoardBounds.Instance ?? FindObjectOfType<BoardBounds>();
        if (!state) state = ChessBoardState.Instance ?? FindObjectOfType<ChessBoardState>();
        if (!committer) committer = FindObjectOfType<MoveCommitter>();
        if (!pawnValidator) pawnValidator = GetComponent<PawnValidator>();
    }

    public void OnGrabbed()
    {
        // Si no es tu turno, igual guardamos desde dónde volverá
        _grabbed = true;
        _startSq = board.WorldToSquare(transform.position);
        _startYaw = transform.eulerAngles.y;
    }

    public void OnReleased()
    {
        if (!_grabbed) return;
        _grabbed = false;

        // Casilla destino
        Vector2Int endSq = board.WorldToSquare(transform.position);

        // 1) Si no cambió de casilla → volver
        if (endSq == _startSq)
        {
            HardSnap(_startSq, _startYaw);
            return;
        }

        // 2) Validar (por ahora peón; mismo patrón para torre/alfil/etc.)
        var req = new PawnMoveRequest { from = _startSq, to = endSq, color = _gate.color, hasMoved = _hasMoved };
        if (!pawnValidator || !pawnValidator.Validate(req, state, out var result))
        {
            // inválido → regresar y NO cambiar turno
            HardSnap(_startSq, _startYaw);
            return;
        }

        // 3) Válido → commit (captura si aplica, ocupa casilla, marca moved y cambia turno)
        committer.CommitPawnMove(_driver, _startSq, endSq, result);
        _hasMoved = true; // ya no permite doble paso
    }

    // Snap robusto (idéntica idea que GrabSafeReturn para no “flotar”)
    void HardSnap(Vector2Int sq, float yaw)
    {
        Vector3 c = board.SquareCenterWorld(sq);
        float y = c.y + (_col ? _col.bounds.extents.y : 0.02f) + Mathf.Max(0f, heightEpsilon);

        bool prevKin = _rb.isKinematic;
        _rb.isKinematic = true;
        transform.SetPositionAndRotation(new Vector3(c.x, y, c.z), Quaternion.Euler(0f, yaw, 0f));
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.WakeUp();
        _rb.isKinematic = prevKin;

        state.RegisterAt(sq, _gate, "Pawn"); // ocupa la casilla a la que volvió
    }
}







