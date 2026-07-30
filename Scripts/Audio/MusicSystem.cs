using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioSource))]
public class MusicSystem : MonoBehaviour
{
    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;

    [Header("Ajustes de Tiempo")]
    public float timeToFadeOut = 0.2f; // Corte casi instantáneo
    public float timeToFadeIn = 0.5f;  // Entrada rápida y suave
    public float maxVolume = 0.5f;

    [Header("Precarga de Música (RAM)")]
    public List<AudioClip> cancionesParaPrecargar;

    private Coroutine activeTransition;

    void Awake()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        sourceA = sources[0];
        sourceB = sources[1];

        ConfigureSource(sourceA);
        ConfigureSource(sourceB);
        activeSource = sourceA;

        // Precarga para evitar el lag que tenías antes
        foreach (AudioClip clip in cancionesParaPrecargar)
        {
            if (clip != null) clip.LoadAudioData();
        }
    }

    void ConfigureSource(AudioSource s)
    {
        s.playOnAwake = false;
        s.spatialBlend = 0; // 2D
        s.volume = 0;
        s.loop = true;
        s.priority = 0; // Máxima prioridad para que no se corte con efectos
    }

    public void ChangeMusic(AudioClip newClip)
    {
        if (newClip == null || activeSource.clip == newClip) return;

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(QuickTransition(newClip));
    }

    IEnumerator QuickTransition(AudioClip newClip)
    {
        // 1. Elegimos la fuente libre para que no haya stuttering
        AudioSource nextSource = (activeSource == sourceA) ? sourceB : sourceA;
        nextSource.clip = newClip;
        nextSource.volume = 0;
        nextSource.Play();

        // 2. FADE OUT de la anterior (Muy rápido para que parezca un cambio de zona)
        float startVol = activeSource.volume;
        float tOut = 0;
        while (tOut < timeToFadeOut)
        {
            tOut += Time.deltaTime;
            activeSource.volume = Mathf.Lerp(startVol, 0, tOut / timeToFadeOut);
            yield return null;
        }
        activeSource.Stop();

        // 3. FADE IN de la nueva (Entrada con fuerza)
        float tIn = 0;
        while (tIn < timeToFadeIn)
        {
            tIn += Time.deltaTime;
            nextSource.volume = Mathf.Lerp(0, maxVolume, tIn / timeToFadeIn);
            yield return null;
        }

        nextSource.volume = maxVolume;
        activeSource = nextSource;
        activeTransition = null;
    }
}