using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PieceTurnGate : MonoBehaviour
{
    public SideColor color = SideColor.White;
    public bool debugLogs = false;

    List<Behaviour> _behavioursToGate;
    List<Collider> _triggerColliders;
    bool _registered;

    static readonly string[] _exclude =
    {
        "PieceTurnGate","TurnManager","MoveEndTurnBySquare","GrabSafeReturn"
    };

    void OnEnable()
    {
        BuildCaches();
        StartCoroutine(EnsureRegistered());  // se registra cuando el TurnManager ya existe
    }

    void OnDisable()
    {
        TurnManager.Instance?.UnregisterGate(this);
        _registered = false;
    }

    IEnumerator EnsureRegistered()
    {
        int tries = 0;
        while (TurnManager.Instance == null && tries++ < 30) yield return null;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterGate(this);
            TurnManager.Instance.ApplyTurn();
            _registered = true;
            if (debugLogs) Debug.Log($"[PieceTurnGate] {name} registrado en TurnManager.");
        }
        else if (debugLogs) Debug.LogWarning($"[PieceTurnGate] {name} no encontró TurnManager.");
    }

    void BuildCaches()
    {
        _behavioursToGate = new List<Behaviour>();
        _triggerColliders = new List<Collider>();

        var allB = GetComponentsInChildren<Behaviour>(true);
        foreach (var b in allB)
        {
            if (!b) continue;
            string n = b.GetType().Name;

            bool excl = false;
            for (int i = 0; i < _exclude.Length; i++) if (n == _exclude[i]) { excl = true; break; }
            if (excl) continue;

            if (n.Contains("Grab") || n.Contains("Grabb") || n.Contains("Interactable"))
                _behavioursToGate.Add(b);
        }

        var allC = GetComponentsInChildren<Collider>(true);
        foreach (var c in allC) if (c && c.isTrigger) _triggerColliders.Add(c);

        if (debugLogs)
            Debug.Log($"[PieceTurnGate] {name} cache: behaviours={_behavioursToGate.Count}, triggers={_triggerColliders.Count}");
    }

    public void SetMovable(bool allow)
    {
        foreach (var c in _triggerColliders) if (c) c.enabled = allow;
        foreach (var b in _behavioursToGate) if (b) b.enabled = allow;

        if (debugLogs)
            Debug.Log($"[PieceTurnGate] {name} -> {(allow ? "ENABLED" : "DISABLED")}");
    }

    [ContextMenu("Log What I'm Gating")]
    void LogWhatImGating()
    {
        BuildCaches();
        Debug.Log($"[PieceTurnGate] {name} (log manual) behaviours={_behavioursToGate.Count}, triggers={_triggerColliders.Count}");
    }
}













