using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Ambience : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Configuración de Distancias")]
    public float distanciaMinima = 5f;  // Donde el volumen es 100%
    public float distanciaMaxima = 20f; // Donde el volumen empieza a oírse

    [Header("Configuración de Audio")]
    public AudioClip clipDeSonido;
    [Range(0f, 1f)]
    public float volumenMaximo = 0.5f;
    public float velocidadFade = 2f;    // Qué tan rápido sube/baja el volumen

    private AudioSource audioSource;
    private float volumenObjetivo = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = clipDeSonido;
        audioSource.loop = true;

        // Configuración 3D fija
        audioSource.spatialBlend = 1.0f;
        audioSource.volume = 0;
        audioSource.Play();
    }

    void Update()
    {
        if (jugador == null) return;

        // 1. Calculamos la distancia real entre Samus y la bola de energía
        float distancia = Vector3.Distance(transform.position, jugador.position);

        // 2. Determinamos el volumen objetivo basado en los rangos
        // Si está cerca (distanciaMinima), volumen al máximo.
        // Si está lejos (distanciaMaxima), volumen a cero.
        if (distancia <= distanciaMinima)
        {
            volumenObjetivo = volumenMaximo;
        }
        else if (distancia >= distanciaMaxima)
        {
            volumenObjetivo = 0f;
        }
        else
        {
            // Calculamos una transición suave entre los dos radios
            float interpolacion = 1f - ((distancia - distanciaMinima) / (distanciaMaxima - distanciaMinima));
            volumenObjetivo = interpolacion * volumenMaximo;
        }

        // 3. Aplicamos el volumen con un suavizado (Lerp) para que no sea brusco
        audioSource.volume = Mathf.Lerp(audioSource.volume, volumenObjetivo, Time.deltaTime * velocidadFade);
    }

    // Dibujamos los radios en la escena para que puedas ajustarlos visualmente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaMinima);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaMaxima);
    }
}