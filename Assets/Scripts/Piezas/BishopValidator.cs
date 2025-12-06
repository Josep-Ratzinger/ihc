using UnityEngine;

public class BishopValidator : MonoBehaviour
{

    public SoundController soundcontroller;


    // Valida movimiento diagonal del alfil. Devuelve true si es legal.
    // Si hay captura, retorna captureSq = casilla destino.
    public bool Validate(Vector2Int from, Vector2Int to, SideColor mover,
                         ChessBoardState state, out Vector2Int? captureSq)
    {
        captureSq = null;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        // Debe ser diagonal y moverse al menos 1 casilla
        if (dx == 0 || dy == 0 || Mathf.Abs(dx) != Mathf.Abs(dy))
            return false;

        int stepx = dx > 0 ? 1 : -1;
        int stepy = dy > 0 ? 1 : -1;

        // Camino libre (sin contar la casilla destino)
        int steps = Mathf.Abs(dx);
        for (int i = 1; i < steps; i++)
        {
            var mid = new Vector2Int(from.x + i * stepx, from.y + i * stepy);
            if (state.TryGet(mid, out var _)) return false; // bloqueado
        }

        // Destino: libre o enemigo (si aliado -> inv�lido)
        if (state.TryGet(to, out var occ))
        {
            if (occ.gate && occ.gate.color == mover) return false;
            captureSq = to; // enemigo
        }

        soundcontroller.Sound_MoverFicha();

        return true;
    }
}

