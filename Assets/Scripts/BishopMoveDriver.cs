using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceTurnGate), typeof(Rigidbody))]
public class BishopMoveDriver : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;
    public MoveCommitter committer;
    public BishopValidator validator;

    // NUEVO
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
        if (!validator) validator = GetComponent<BishopValidator>() ?? gameObject.AddComponent<BishopValidator>();

        // new 
        if (!hints) hints = MoveHintsHighlights.Instance ?? FindObjectOfType<MoveHintsHighlights>();
    }

    void Start()
    {
        var sq = board.WorldToSquare(transform.position);
        state.RegisterAt(sq, Gate, "Bishop");
    }

    // InteractableUnityEventWrapper → When Select
    /*
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
    }
    */
    public void OnGrabbed()
    {
        _grabStartSq = board.WorldToSquare(transform.position);
        _grabYaw = transform.eulerAngles.y;

        // Si no es tu turno, NO se mueve ni se muestran hints
        if (TurnManager.Instance && Gate.color != TurnManager.Instance.currentTurn)
        {
            _grabbed = false;
            hints?.ClearAll();
            return;
        }

        _grabbed = true;

        // Mostrar posibles movimientos
        hints?.ShowBishopMoves(_grabStartSq, Gate.color);
    }

    // InteractableUnityEventWrapper → When Unselect: al soltar
    public void OnReleased()
    {
        // Siempre limpiamos las luces al soltar
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

        // Validación de alfil
        if (!validator.Validate(_grabStartSq, endSq, Gate.color, state, out var captureSq))
        {
            StartCoroutine(ReturnToStart()); // inválido → volver (sin cambiar turno)
            return;
        }

        // Válido → commit (mueve/captura/turno)
        committer.CommitBishopMove(this, _grabStartSq, endSq, captureSq);
    }

    IEnumerator ReturnToStart()
    {
        yield return null; // deja cerrar el frame del release
        SnapToSquare(_grabStartSq, _grabYaw);
    }

    // --- Snap robusto (igual a otras piezas) ---
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

        state.RegisterAt(sq, Gate, "Bishop");
    }
}

