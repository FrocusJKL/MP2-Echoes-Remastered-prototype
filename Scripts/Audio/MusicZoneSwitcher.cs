using UnityEngine;

public class MusicZoneSwitcher : MonoBehaviour
{
    public AudioSource musicaAbajo;
    public AudioSource musicaArriba;

    [Header("Configuración de Samus")]
    public Transform jugador;

    [Header("Ajustes del Fade de Distancia")]
    public float distanciaCentro = 10f;
    public float margenFade = 5f;

    [Header("Fade Inicial (Entrada al Juego)")]
    public float tiempoFadeInicial = 3.0f;
    private float multiplicadorEntrada = 0f;

    void Start()
    {
        // Forzamos volumen 0 al inicio para evitar el susto de las dos canciones
        if (musicaAbajo) musicaAbajo.volume = 0;
        if (musicaArriba) musicaArriba.volume = 0;
    }

    void Update()
    {
        if (jugador == null || musicaAbajo == null || musicaArriba == null) return;

        // 1. Gestionamos el Fade Inicial (de 0 a 1 poco a poco al empezar el juego)
        if (multiplicadorEntrada < 1f)
        {
            multiplicadorEntrada += Time.deltaTime / tiempoFadeInicial;
        }

        // 2. Calculamos la mezcla por distancia (0 = Abajo, 1 = Arriba)
        float distancia = Vector3.Distance(transform.position, jugador.position);
        float mezcla = Mathf.InverseLerp(distanciaCentro + margenFade, distanciaCentro, distancia);

        // 3. Aplicamos los volúmenes multiplicados por el Fade Inicial
        // Usamos 0.5f como volumen máximo para que no aturda
        musicaArriba.volume = mezcla * 0.5f * multiplicadorEntrada;
        musicaAbajo.volume = (1f - mezcla) * 0.5f * multiplicadorEntrada;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaCentro);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaCentro + margenFade);
    }
}