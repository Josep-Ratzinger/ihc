// RookValidator.cs
using UnityEngine;

public class RookValidator : MonoBehaviour
{
    // Devuelve true si es válido. captureSq = casilla de la pieza enemiga si hay captura; null si no.
    public bool Validate(Vector2Int from, Vector2Int to, SideColor color, ChessBoardState state, out Vector2Int? captureSq)
    {
        captureSq = null;

        // Debe moverse en línea recta
        bool sameFile = from.x == to.x;
        bool sameRank = from.y == to.y;
        if (!sameFile && !sameRank) return false;

        // Dirección y pasos
        int dx = Mathf.Clamp(to.x - from.x, -1, 1);
        int dy = Mathf.Clamp(to.y - from.y, -1, 1);

        // Avanza casilla por casilla verificando bloqueo
        var cur = from;
        while (true)
        {
            cur = new Vector2Int(cur.x + dx, cur.y + dy);
            if (cur == to) break;

            // si hay algo en medio → ilegal
            if (state.IsOccupied(cur)) return false;
        }

        // Destino: si hay pieza propia → ilegal; si hay enemiga → captura
        if (state.TryGet(cur, out var occ))
        {
            if (occ.gate.color == color) return false;
            captureSq = cur;
        }

        return true;
    }
}


