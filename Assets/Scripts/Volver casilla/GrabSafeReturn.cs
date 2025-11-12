using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class GrabSafeReturn : MonoBehaviour
{
    [Header("Referencias")]
    public BoardBounds board;                 // arrástralo; si lo dejas vacío lo busca solo

    [Header("Opciones")]
    public bool keepUprightAlways = true;     // siempre de pie (no solo al agarrar)
    public bool freezeRotationXZ = true;      // congela rotación X/Z en el Rigidbody
    public float heightEpsilon = 0.003f;      // 0.002–0.006
    public float insidePadding = 0.015f;      // tolerancia de borde (m)

    // Estado
    private Rigidbody _rb;
    private Collider _col;
    private bool _grabbed;

    // Guardado al iniciar el agarre
    private Vector2Int _savedSquare;
    private float _savedYaw;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponentInChildren<Collider>();
        if (!board) board = BoardBounds.Instance ?? FindObjectOfType<BoardBounds>();
        if (!board) { Debug.LogError("GrabSafeReturn: No se encontró BoardBounds."); enabled = false; return; }

        if (freezeRotationXZ)
        {
            _rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void Start()
    {
        // Asegura inicio asentado en su casilla más cercana
        var sq = board.WorldToSquare(transform.position);
        TeleportToSquareCenter(sq, transform.eulerAngles.y);
        KeepUpright();
        // Valor por defecto por si el primer agarre tarda
        _savedSquare = sq;
        _savedYaw = transform.eulerAngles.y;
    }

    // Conecta esto a When Select
    public void OnGrabbed()
    {
        _grabbed = true;
        _savedSquare = board.WorldToSquare(transform.position);
        _savedYaw = transform.eulerAngles.y;
        KeepUpright();
    }

    // Conecta esto a When Unselect
    public void OnReleased()
    {
        _grabbed = false;
        KeepUpright();

        // Si al soltar está fuera, vuelve
        if (!board.WorldIsOverBoard(transform.position, insidePadding))
            TeleportToSquareCenter(_savedSquare, _savedYaw);
    }

    void Update()
    {
        // 1) Mientras está agarrado: no permitimos salir
        if (_grabbed && !board.WorldIsOverBoard(transform.position, insidePadding))
            TeleportToSquareCenter(_savedSquare, _savedYaw);

        // 2) Si NO está agarrado y (por física) quedó fuera, también lo regresamos
        if (!_grabbed && !board.WorldIsOverBoard(transform.position, insidePadding))
            TeleportToSquareCenter(_savedSquare, _savedYaw);

        if (keepUprightAlways) KeepUpright();
    }

    // --- Utilidades ---
    void TeleportToSquareCenter(Vector2Int sq, float yawDeg)
    {
        Vector3 c = board.SquareCenterWorld(sq);
        float y = c.y + HalfHeight() + Mathf.Max(0f, heightEpsilon);

        bool prevKin = _rb.isKinematic;
        _rb.isKinematic = true; // evita rebotes y “flotar”
        transform.SetPositionAndRotation(new Vector3(c.x, y, c.z), Quaternion.Euler(0f, yawDeg, 0f));
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.WakeUp();
        _rb.isKinematic = prevKin;
    }

    float HalfHeight() => _col ? _col.bounds.extents.y : 0.02f;

    void KeepUpright()
    {
        // fuerza a 0 el pitch/roll; preserva yaw
        var e = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, e.y, 0f);
    }
}





