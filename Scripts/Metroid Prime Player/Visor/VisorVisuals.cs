using UnityEngine;

public class VisorVisuals : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerInputReader inputReader;

    [Header("Ajustes de Sway (Balanceo)")]
    public float amount = 0.02f;
    public float maxAmount = 0.05f;
    public float smoothAmount = 6f;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition;

        // Si no lo asignaste en el inspector, lo buscamos
        if (inputReader == null)
            inputReader = GetComponentInParent<PlayerInputReader>();
    }

    void Update()
    {
        // Usamos el Vector2 'Look' de tu InputReader en lugar del sistema viejo
        Vector2 lookInput = inputReader.Look;

        // Calculamos el movimiento (invertimos con el signo menos para el efecto de inercia)
        float moveX = -lookInput.x * amount;
        float moveY = -lookInput.y * amount;

        // Limitamos el movimiento
        moveX = Mathf.Clamp(moveX, -maxAmount, maxAmount);
        moveY = Mathf.Clamp(moveY, -maxAmount, maxAmount);

        Vector3 targetPosition = new Vector3(moveX, moveY, 0) + initialPosition;

        // Aplicamos el suavizado
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothAmount);
    }
}