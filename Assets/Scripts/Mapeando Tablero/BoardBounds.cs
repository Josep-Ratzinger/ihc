using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BoardBounds : MonoBehaviour
{
    public static BoardBounds Instance;

    [Header("Grid")]
    public int files = 8;   // columnas (eje X)
    public int ranks = 8;   // filas (eje Z)

    [Header("Anchors (recomendado)")]
    [Tooltip("Centro de la casilla A1 mirando desde las blancas")]
    public Transform a1Corner;
    [Tooltip("Centro de la casilla H8 (opuesto diagonal)")]
    public Transform h8Corner;

    [Header("Fallback sin anclas")]
    [Tooltip("Recorte del borde/marco en metros si no usas anclas")]
    public Vector2 innerPadding = Vector2.zero; // X = izquierda/derecha, Y = abajo/arriba (Z)

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gridColor = Color.green;

    // Internos
    MeshRenderer _mr;
    Bounds _meshBounds;
    float _minX, _maxX, _minZ, _maxZ, _surfaceY;
    Vector3 _topMin;         // (minX, surfaceY, minZ)
    Vector2 _cellSizeXZ;     // tamaño de una casilla en X y Z

    void Awake()
    {
        Instance = this;
        _mr = GetComponent<MeshRenderer>();
        Recalc();
    }

    // Si mueves/escala el tablero en runtime, puedes llamar a Recalc() manualmente.
    void Recalc()
    {
        _meshBounds = _mr.bounds;

        // La altura de contacto SIEMPRE viene del mesh del tablero (evita 'flotar')
        _surfaceY = _meshBounds.max.y;

        // Área XZ del tablero (sin marco): usa anclas si están, sino padding
        if (a1Corner != null && h8Corner != null)
        {
            _minX = Mathf.Min(a1Corner.position.x, h8Corner.position.x);
            _maxX = Mathf.Max(a1Corner.position.x, h8Corner.position.x);
            _minZ = Mathf.Min(a1Corner.position.z, h8Corner.position.z);
            _maxZ = Mathf.Max(a1Corner.position.z, h8Corner.position.z);
        }
        else
        {
            _minX = _meshBounds.min.x + innerPadding.x;
            _maxX = _meshBounds.max.x - innerPadding.x;
            _minZ = _meshBounds.min.z + innerPadding.y;
            _maxZ = _meshBounds.max.z - innerPadding.y;
        }

        _topMin = new Vector3(_minX, _surfaceY, _minZ);
        _cellSizeXZ = new Vector2((_maxX - _minX) / files, (_maxZ - _minZ) / ranks);
    }

    public Bounds WorldBounds => _meshBounds;

    // ¿El punto (x,z) está sobre el rectángulo jugable (sin marco)?
    public bool WorldIsOverBoard(Vector3 world, float padding = 0f)
    {
        return world.x >= (_minX - padding) && world.x <= (_maxX + padding) &&
               world.z >= (_minZ - padding) && world.z <= (_maxZ + padding);
    }

    // Conversión mundo <-> casilla
    public Vector2Int WorldToSquare(Vector3 world)
    {
        int f = Mathf.FloorToInt((world.x - _topMin.x) / _cellSizeXZ.x);
        int r = Mathf.FloorToInt((world.z - _topMin.z) / _cellSizeXZ.y);
        return new Vector2Int(Mathf.Clamp(f, 0, files - 1), Mathf.Clamp(r, 0, ranks - 1));
    }

    public Vector3 SquareCenterWorld(Vector2Int sq)
    {
        float x = _topMin.x + (sq.x + 0.5f) * _cellSizeXZ.x;
        float z = _topMin.z + (sq.y + 0.5f) * _cellSizeXZ.y;
        return new Vector3(x, _surfaceY, z);
    }

    public bool IsInsideSquare(Vector2Int sq) =>
        sq.x >= 0 && sq.x < files && sq.y >= 0 && sq.y < ranks;

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        var mr = GetComponent<MeshRenderer>();
        if (!mr) return;

        // Recalcular para vista en editor
        _mr = mr; Recalc();

        Gizmos.color = gridColor;
        for (int f = 0; f < files; f++)
            for (int r = 0; r < ranks; r++)
            {
                var c = SquareCenterWorld(new Vector2Int(f, r));
                var size = new Vector3(_cellSizeXZ.x, 0.002f, _cellSizeXZ.y);
                Gizmos.DrawWireCube(new Vector3(c.x, c.y + 0.001f, c.z), size);
            }
    }
}
