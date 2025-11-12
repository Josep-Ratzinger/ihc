using System.Collections;
using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(PieceTurnGate), typeof(Rigidbody))]
public class PieceMoveDriver : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;
    public MoveCommitter committer;   // llama NotifyPieceMoved internamente (turno)
    public PawnValidator pawnValidator;

    [Header("Snap")]
    public float heightEpsilon = 0.003f;

    // ---- internos
    public PieceTurnGate Gate { get; private set; }
    Rigidbody _rb;
    Collider _col;

    Vector2Int _startSq;
    float _startYaw;
    bool _grabbed;
    bool _hasMoved;                    // primer doble paso

    void Awake()
    {
        Gate = GetComponent<PieceTurnGate>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponentInChildren<Collider>();

        if (!board) board = BoardBounds.Instance ?? FindObjectOfType<BoardBounds>();
        if (!state) state = ChessBoardState.Instance ?? FindObjectOfType<ChessBoardState>();
        if (!committer) committer = FindObjectOfType<MoveCommitter>();
        if (!pawnValidator) pawnValidator = GetComponent<PawnValidator>();
    }

    void Start()
    {
        var sq = board.WorldToSquare(transform.position);
        state.RegisterAt(sq, Gate, "Pawn");
        // no usamos MoveEndTurn..., esto solo inicializa ocupación
    }

    // Vincular a When Select
    public void OnGrabbed()
    {
        // si no es tu turno, no hacemos nada (PieceTurnGate ya lo bloquea)
        if (TurnManager.Instance && Gate.color != TurnManager.Instance.currentTurn) return;

        _grabbed = true;
        _startSq = board.WorldToSquare(transform.position);
        _startYaw = transform.eulerAngles.y;
    }

    // Vincular a When Unselect
    public void OnReleased()
    {
        if (!_grabbed) return;
        _grabbed = false;

        // Casilla destino (tomada directamente del helper del board)
        Vector2Int endSq = board.WorldToSquare(transform.position);

        // Si no cambiaste de casilla → vuelve y NO cambia turno
        if (endSq == _startSq)
        {
            SnapToSquare(_startSq, _startYaw);
            return;
        }

        // Validación de peón
        var req = new PawnMoveRequest
        {
            from = _startSq,
            to = endSq,
            color = Gate.color,
            hasMoved = _hasMoved
        };

        if (!pawnValidator.Validate(req, state, out var result))
        {
            // Movimiento ilegal → volver sin cambiar turno (con delay)
            StartCoroutine(ReturnToStart());
            return;
        }

        // Movimiento válido → commit (actualiza tablero, captura si aplica, marca moved y cambia turno)
        committer.CommitPawnMove(this, _startSq, endSq, result);
        _hasMoved = true; // ya no puede doble paso
    }

    private IEnumerator ReturnToStart()
    {
        // Esperar un frame para permitir que Unity procese el "release" físico
        yield return null;
        SnapToSquare(_startSq, _startYaw);
    }

    // --- utilidades de snap (idéntica idea a GrabSafeReturn para evitar “flotar”)
    public void SnapToSquare(Vector2Int sq) => SnapToSquare(sq, transform.eulerAngles.y);

    public void SnapToSquare(Vector2Int sq, float yawDeg)
    {
        Vector3 c = board.SquareCenterWorld(sq);
        float y = c.y + (_col ? _col.bounds.extents.y : 0.02f) + Mathf.Max(0f, heightEpsilon);

        bool prevKin = _rb.isKinematic;
        _rb.isKinematic = true;                 // congelamos para no rebotar
        transform.SetPositionAndRotation(new Vector3(c.x, y, c.z), Quaternion.Euler(0f, yawDeg, 0f));
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.WakeUp();
        _rb.isKinematic = prevKin;              // restauramos
        state.RegisterAt(sq, Gate, "Pawn");     // ocupa casilla
    }
    // --- compat con MoveCommitter ---
    public void MarkMoved()
    {
        _hasMoved = true;
    }

}














