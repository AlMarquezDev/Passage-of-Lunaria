// MapMusicController.cs
using UnityEngine;

public class MapMusicController : MonoBehaviour
{
    [Tooltip("El clip de música de fondo para este mapa/zona.")]
    public AudioClip mapBackgroundMusic;

    [Tooltip("¿Reproducir la música automáticamente cuando esta escena/zona se carga?")]
    public bool playOnStart = true;

    void Start()
    {
        if (playOnStart)
        {
            if (MusicManager.Instance != null)
            {
                if (mapBackgroundMusic != null)
                {
                    MusicManager.Instance.PlayMapTrack(mapBackgroundMusic);
                }
                else
                {
                    Debug.LogWarning($"MapMusicController en '{gameObject.name}': No se ha asignado 'mapBackgroundMusic'.", this);
                }
            }
            else
            {
                Debug.LogError($"MapMusicController en '{gameObject.name}': MusicManager.Instance no encontrado.", this);
            }
        }
    }
}