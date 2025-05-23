// MusicManager.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;
    private AudioClip currentMapTrack; // Guarda el clip del mapa actual

    [Header("Default Themes (Fallback)")]
    [Tooltip("Tema de batalla por defecto si no se especifica otro.")]
    public AudioClip defaultBattleTheme;
    [Tooltip("Tema de victoria por defecto.")]
    public AudioClip defaultVictoryTheme;
    [Tooltip("Tema de derrota por defecto.")]
    public AudioClip defaultDefeatTheme;

    [Header("Fade Settings")]
    public float fadeDuration = 1.0f;

    private Coroutine musicFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) // Por si se olvida añadirlo
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.loop = true; // La música de fondo generalmente se repite
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Reproduce la música de un mapa específico. Guarda este clip para reanudarlo después del combate.
    /// </summary>
    public void PlayMapTrack(AudioClip mapTrack)
    {
        if (mapTrack == null)
        {
            Debug.LogWarning("[MusicManager] PlayMapTrack llamado con un clip nulo. Deteniendo música.");
            StopMusicWithFade();
            currentMapTrack = null;
            return;
        }

        Debug.Log($"[MusicManager] Solicitando música de mapa: {mapTrack.name}");
        if (audioSource.clip == mapTrack && audioSource.isPlaying)
        {
            Debug.Log($"[MusicManager] El track '{mapTrack.name}' ya se está reproduciendo.");
            return; // Ya se está reproduciendo este track
        }
        currentMapTrack = mapTrack;
        StartFade(mapTrack, true);
    }

    /// <summary>
    /// Reproduce el tema de batalla. Usa el default si no se provee uno específico.
    /// </summary>
    public void PlayBattleTheme(AudioClip specificBattleTrack = null)
    {
        AudioClip trackToPlay = specificBattleTrack ?? defaultBattleTheme;
        if (trackToPlay == null)
        {
            Debug.LogWarning("[MusicManager] No se encontró tema de batalla para reproducir (ni específico ni default).");
            StopMusicWithFade(); // Detener música si no hay tema de batalla
            return;
        }
        Debug.Log($"[MusicManager] Solicitando tema de batalla: {trackToPlay.name}");
        StartFade(trackToPlay, true);
    }

    /// <summary>
    /// Reproduce el tema de victoria.
    /// </summary>
    public void PlayVictoryTheme()
    {
        if (defaultVictoryTheme == null)
        {
            Debug.LogWarning("[MusicManager] No se encontró tema de victoria para reproducir.");
            StopMusicWithFade();
            return;
        }
        Debug.Log($"[MusicManager] Solicitando tema de victoria: {defaultVictoryTheme.name}");
        StartFade(defaultVictoryTheme, true); // O loop = false si prefieres
    }

    /// <summary>
    /// Reproduce el tema de derrota.
    /// </summary>
    public void PlayDefeatTheme()
    {
        if (defaultDefeatTheme == null)
        {
            Debug.LogWarning("[MusicManager] No se encontró tema de derrota para reproducir.");
            StopMusicWithFade();
            return;
        }
        Debug.Log($"[MusicManager] Solicitando tema de derrota: {defaultDefeatTheme.name}");
        StartFade(defaultDefeatTheme, true); // O loop = false
    }

    /// <summary>
    /// Detiene la música actual (ej. victoria/derrota) y vuelve a reproducir la música del mapa actual.
    /// </summary>
    public void ReturnToMapMusic()
    {
        if (currentMapTrack != null)
        {
            Debug.Log($"[MusicManager] Volviendo a la música del mapa: {currentMapTrack.name}");
            StartFade(currentMapTrack, true);
        }
        else
        {
            Debug.LogWarning("[MusicManager] No hay música de mapa guardada para reanudar. Deteniendo música.");
            StopMusicWithFade();
        }
    }

    /// <summary>
    /// Detiene la música actual con un fade out.
    /// </summary>
    public void StopMusicWithFade()
    {
        if (audioSource.isPlaying)
        {
            StartFade(null, false); // null clip para indicar solo fade out y stop
        }
    }

    // Changed return type from IEnumerator to void
    public void StartFade(AudioClip newClip, bool loop, float targetVolume = 1.0f)
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
        musicFadeCoroutine = StartCoroutine(FadeMusicCoroutine(newClip, loop, targetVolume));
        // Removed `return musicFadeCoroutine;` as return type is now void
    }

    private IEnumerator FadeMusicCoroutine(AudioClip newClip, bool loop, float targetVolume)
    {
        float startVolume = audioSource.volume;

        // Fade out actual
        if (audioSource.isPlaying && fadeDuration > 0)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
            audioSource.Stop();
            audioSource.volume = startVolume; // Restaurar para el siguiente fade in
        }
        else if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }


        // Si hay un nuevo clip, reproducirlo con fade in
        if (newClip != null)
        {
            audioSource.clip = newClip;
            audioSource.loop = loop;
            audioSource.volume = 0f; // Empezar desde volumen 0 para fade in
            audioSource.Play();

            if (fadeDuration > 0)
            {
                float timer = 0f;
                while (timer < fadeDuration)
                {
                    audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / fadeDuration);
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }
                audioSource.volume = targetVolume;
            }
            else
            {
                audioSource.volume = targetVolume;
            }
        }
        musicFadeCoroutine = null;
    }

    public AudioClip GetCurrentClip()
    {
        return audioSource.clip;
    }
}