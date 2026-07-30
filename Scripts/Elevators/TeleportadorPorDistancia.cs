using UnityEngine;

public class TeleportadorPorDistancia : MonoBehaviour
{
    [Header("Referencias de Samus")]
    public Transform jugador;

    [Header("Configuración")]
    public Transform destino;
    public float radioActivacion = 2f;
    public float tiempoEspera = 2.0f; // Tiempo para poder volver a usar un portal

    // Esta variable es estática: si un portal la cambia, afecta a todos los portales
    private static float proximoTeleportDisponible = 0f;

    private CharacterController characterController;

    void Start()
    {
        if (jugador) characterController = jugador.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (jugador == null || destino == null) return;

        // 1. Verificamos si el tiempo actual ya superó el tiempo de espera
        if (Time.time < proximoTeleportDisponible) return;

        // 2. Calculamos la distancia
        float distancia = Vector3.Distance(transform.position, jugador.position);

        // 3. Si Samus está dentro del radio y el cooldown terminó
        if (distancia < radioActivacion)
        {
            RealizarTeletransporte();
        }
    }

    private void RealizarTeletransporte()
    {
        // 4. Establecemos el momento en el que se podrá volver a usar CUALQUIER portal
        proximoTeleportDisponible = Time.time + tiempoEspera;

        if (characterController) characterController.enabled = false;

        jugador.position = destino.position;
        jugador.rotation = destino.rotation;

        if (characterController) characterController.enabled = true;

        Debug.Log("Teletransporte realizado. Cooldown activo.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioActivacion);
    }
}