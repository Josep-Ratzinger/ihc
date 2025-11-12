using UnityEngine;

public class QueenValidator : MonoBehaviour
{
    // Devuelve true si es legal. Si hay captura: captureSq = 'to'
    public bool Validate(Vector2Int from, Vector2Int to, SideColor mover,
                         ChessBoardState state, out Vector2Int? captureSq)
    {
        captureSq = null;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool straight = (dx == 0) ^ (dy == 0);          // recto (uno cero, el otro no)
        bool diagonal = (dx != 0 && Mathf.Abs(dx) == Mathf.Abs(dy));

        if (!straight && !diagonal) return false;

        int stepx = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int stepy = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        // Camino libre (sin incluir destino)
        for (int i = 1; i < steps; i++)
        {
            var mid = new Vector2Int(from.x + i * stepx, from.y + i * stepy);
            if (state.TryGet(mid, out var _)) return false;
        }

        // Destino
        if (state.TryGet(to, out var occ))
        {
            if (occ.gate && occ.gate.color == mover) return false; // aliado en destino
            captureSq = to; // enemigo
        }

        return true;
    }
}

