using UnityEngine;

public class KnightValidator : MonoBehaviour
{
    // Devuelve true si el destino es legal. Si hay captura, retorna captureSq.
    public bool Validate(Vector2Int from, Vector2Int to, SideColor mover, ChessBoardState state, out Vector2Int? captureSq)
    {
        captureSq = null;

        // delta en “L”
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        bool isL = (dx == 1 && dy == 2) || (dx == 2 && dy == 1);
        if (!isL) return false;

        // destino: libre o enemigo (aliado NO)
        if (state.TryGet(to, out var occ))
        {
            if (occ.gate && occ.gate.color == mover) return false;   // aliado en destino
            captureSq = to; // enemigo
        }

        // El caballo ignora intermedios, así que no revisamos camino
        return true;
    }
}

