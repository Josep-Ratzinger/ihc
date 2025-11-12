using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceTurnGate), typeof(Rigidbody))]
public class KingMoveDriver : MonoBehaviour
{
    [Header("Refs")]
    public BoardBounds board;
    public ChessBoardState state;
    public MoveCommitter committer;
    public KingValidator validator;

    [Header("Ajustes")]
    public float heightEpsilon = 0.003f;

    public PieceTurnGate Gate { get; private set; }
    private Rigidbody rb;
    private Collider col;

    private Vector2Int startSq;
    private float startYaw;
    private bool grabbed;

    public bool HasMoved { get; private set; } = false;

    void Awake()
    {
        Gate = GetComponent<PieceTurnGate>();
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        if (!board) board = BoardBounds.Instance ?? FindObjectOfType<BoardBounds>();
        if (!state) state = ChessBoardState.Instance ?? FindObjectOfType<ChessBoardState>();
        if (!committer) committer = FindObjectOfType<MoveCommitter>();
        if (!validator) validator = GetComponent<KingValidator>() ?? gameObject.AddComponent<KingValidator>();
    }

    void Start()
    {
        var sq = board.WorldToSquare(transform.position);
        state.RegisterAt(sq, Gate, "King");
    }

    public void OnGrabbed()
    {
        if (TurnManager.Instance && (TurnManager.Instance.GameOver || Gate.color != TurnManager.Instance.currentTurn))
        {
            grabbed = false;
            return;
        }

        grabbed = true;
        startSq = board.WorldToSquare(transform.position);
        startYaw = transform.eulerAngles.y;
    }

    public void OnReleased()
    {
        if (!grabbed) { SnapToSquare(startSq, startYaw); return; }
        grabbed = false;

        Vector2Int endSq = board.WorldToSquare(transform.position);
        if (endSq == startSq) { SnapToSquare(startSq, startYaw); return; }

        // Movimiento normal del rey
        if (validator.ValidateKingStep(startSq, endSq, Gate.color, state, out var captureSq))
        {
            committer.CommitKingMove(this, startSq, endSq, captureSq, null, null);
            HasMoved = true;
            return;
        }

        // Intento de enroque
        if (!HasMoved && validator.TryCastle(startSq, endSq, Gate.color, state, out var rookFrom, out var rookTo))
        {
            committer.CommitKingMove(this, startSq, endSq, null, rookFrom, rookTo);
            HasMoved = true;
            return;
        }

        StartCoroutine(ReturnToStart());
    }

    private IEnumerator ReturnToStart()
    {
        yield return null;
        SnapToSquare(startSq, startYaw);
    }

    public void SnapToSquare(Vector2Int sq) => SnapToSquare(sq, transform.eulerAngles.y);

    public void SnapToSquare(Vector2Int sq, float yawDeg)
    {
        Vector3 c = board.SquareCenterWorld(sq);
        float y = c.y + (col ? col.bounds.extents.y : 0.02f) + Mathf.Max(0f, heightEpsilon);

        bool prevKin = rb.isKinematic;
        rb.isKinematic = true;
        transform.SetPositionAndRotation(new Vector3(c.x, y, c.z), Quaternion.Euler(0f, yawDeg, 0f));
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
        rb.isKinematic = prevKin;

        state.RegisterAt(sq, Gate, "King");
    }

    public void NotifyMoved() => HasMoved = true;
}



