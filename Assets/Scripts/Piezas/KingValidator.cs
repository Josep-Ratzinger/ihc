using UnityEngine;

public class KingValidator : MonoBehaviour
{
    public bool ValidateKingStep(Vector2Int from, Vector2Int to, SideColor color, ChessBoardState state, out Vector2Int? captureSq)
    {
        captureSq = null;
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);

        if (dx <= 1 && dy <= 1 && (dx + dy) > 0)
        {
            if (!state.IsOccupied(to)) return true;
            if (state.TryGet(to, out var info) && info.color != color)
            {
                captureSq = to;
                return true;
            }
        }
        return false;
    }

    public bool TryCastle(Vector2Int from, Vector2Int to, SideColor color, ChessBoardState state,
                          out Vector2Int rookFrom, out Vector2Int rookTo)
    {
        rookFrom = default; rookTo = default;

        if (from.y != to.y || Mathf.Abs(to.x - from.x) != 2) return false;

        bool shortSide = to.x > from.x;
        int rf = shortSide ? 7 : 0;
        rookFrom = new Vector2Int(rf, from.y);
        rookTo = shortSide ? new Vector2Int(from.x + 1, from.y) : new Vector2Int(from.x - 1, from.y);

        if (!state.TryGet(rookFrom, out var rookInfo) || rookInfo.color != color || rookInfo.type != "Rook")
            return false;

        var rookDriver = rookInfo.root ? rookInfo.root.GetComponent<RookMoveDriver>() : null;
        if (!rookDriver || rookDriver.HasMoved) return false;

        int step = shortSide ? 1 : -1;
        for (int x = from.x + step; x != rf; x += step)
        {
            if (state.IsOccupied(new Vector2Int(x, from.y))) return false;
        }
        return true;
    }
}

