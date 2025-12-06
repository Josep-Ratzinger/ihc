using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class CaptureCounter : MonoBehaviour
{
    public static CaptureCounter Instance { get; private set; }

    [Header("UI")]
    public TMP_Text text;   // arrastra aquí tu Text (TMP) si no se detecta solo

    // Guarda cuántas piezas de cada tipo se han capturado
    private readonly Dictionary<string, int> _counts = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!text) text = GetComponentInChildren<TMP_Text>();
        UpdateLabel();
    }

    /// <summary>
    /// Registrar una nueva captura de una pieza del tipo dado ("Pawn","Queen", etc.)
    /// </summary>
    public void RegisterCapture(string pieceType)
    {
        if (string.IsNullOrEmpty(pieceType))
            pieceType = "Pieza";

        if (!_counts.ContainsKey(pieceType))
            _counts[pieceType] = 0;

        _counts[pieceType]++;

        UpdateLabel();
    }

    void UpdateLabel()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Capturas:");

        foreach (var kv in _counts)
        {
            string nombreEsp = TraducirTipo(kv.Key);
            sb.AppendLine($"{nombreEsp} (x{kv.Value})");
        }

        text.text = sb.ToString();
    }

    string TraducirTipo(string type)
    {
        switch (type)
        {
            case "Pawn":   return "Peón";
            case "Rook":   return "Torre";
            case "Knight": return "Caballo";
            case "Bishop": return "Alfil";
            case "Queen":  return "Dama";
            case "King":   return "Rey";
            default:       return type;
        }
    }
}
