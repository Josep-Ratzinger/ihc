using System;                           // NEW
using System.Collections.Generic;
using UnityEngine;

public enum SideColor { White, Black }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public SideColor currentTurn = SideColor.White;

    // --- NEW: “ticket” de turno. Sube en cada cambio real de turno.
    public int TurnTicket { get; private set; } = 0;      // NEW
    public event Action<SideColor, int> OnTurnChanged;     // NEW (opcional)

    readonly List<PieceTurnGate> _gates = new();

    void Awake() { Instance = this; }

    void Start()
    {
        RescanGatesAndApply();   // asegura que TODAS las piezas queden registradas
    }

    public void RegisterGate(PieceTurnGate g)
    {
        if (!_gates.Contains(g)) _gates.Add(g);
    }

    public void UnregisterGate(PieceTurnGate g)
    {
        _gates.Remove(g);
    }

    public void ApplyTurn()
    {
        foreach (var g in _gates)
            if (g) g.SetMovable(g.color == currentTurn);
    }

    public void NotifyPieceMoved(SideColor mover)
    {
        if (mover != currentTurn) return;

        // cambia el turno
        currentTurn = (currentTurn == SideColor.White) ? SideColor.Black : SideColor.White;
        ApplyTurn();

        // --- NEW: incrementa el ticket y dispara evento
        TurnTicket++;                                    // NEW
        OnTurnChanged?.Invoke(currentTurn, TurnTicket);  // NEW (opcional)
    }

    [ContextMenu("Rescan & Apply Turn")]
    public void RescanGatesAndApply()
    {
        _gates.Clear();
        _gates.AddRange(FindObjectsOfType<PieceTurnGate>(true));
        ApplyTurn();
        Debug.Log($"[TurnManager] Registrados: {_gates.Count} gates. Turno: {currentTurn}");
    }

    [ContextMenu("Force Apply Turn")]
    public void ForceApplyTurn() => ApplyTurn();

    // --- Game Over (bloquea todas las piezas) ---
    public bool GameOver { get; private set; } = false;

    public void EndGame(string reason = "King captured")
    {
        GameOver = true;
        foreach (var g in _gates)
            if (g) g.SetMovable(false);
        Debug.Log($"[TurnManager] GAME OVER: {reason}");
    }

}




