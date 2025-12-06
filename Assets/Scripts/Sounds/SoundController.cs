using UnityEngine;

public class SoundController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip comerFicha;
    public AudioClip moverFicha;

    void Awake()
    {
        // Si no tiene AudioSource, lo crea
        if (audioSource == null)
            audioSource = gameObject.GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // --- CONFIGURACIÓN AUTOMÁTICA ---
        audioSource.enabled = true;
        audioSource.playOnAwake = false;

        // Volumen máximo
        audioSource.volume = 1f;

        // Sonido 2D (ideal para pruebas, siempre se escucha)
        audioSource.spatialBlend = 0f;

        // Evitar problemas de distancia (solo importa en 3D)
        audioSource.minDistance = 0.1f;
        audioSource.maxDistance = 20f;
    }

    public void Sound_ComerFicha()
    {
        if (comerFicha != null)
            audioSource.PlayOneShot(comerFicha);
        else
            Debug.LogWarning("No se asignó AudioClip comerFicha.");
    }

    public void Sound_MoverFicha()
    {
        if (moverFicha != null)
            audioSource.PlayOneShot(moverFicha);
        else
            Debug.LogWarning("No se asignó AudioClip moverFicha.");
    }
}
