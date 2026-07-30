using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] private float mouseHorizontalSensitivity = 10f;
    [SerializeField] private float mouseVerticalSensitivity = 7f;

    [Header("Gamepad Sensitivity")]
    [SerializeField] private float gamepadHorizontalSensitivity = 200f;
    [SerializeField] private float gamepadVerticalSensitivity = 100f;

    [Header("Vertical Clamp")]
    [SerializeField] private float verticalClamp = 70f;

    [Header("Vertical Edge Slowdown")]
    [SerializeField] private float edgeSlowPower = 1f;
    [SerializeField] private float minVerticalSpeed = 0.5f;

    [Header("References")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private LockOnSystem lockOnSystem;

    private PlayerInputReader input;
    private float xRotation = 0f;

    private void Awake()
    {
        input = GetComponentInParent<PlayerInputReader>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (input == null) return;
        if (lockOnSystem != null && lockOnSystem.IsLocked) return;

        Vector2 lookInput = input.Look;
        float lookX, lookY;

        // Detectamos si el último input vino del Gamepad o Mouse de forma sencilla
        bool isGamepad = Gamepad.current != null &&
                         (Gamepad.current.rightStick.ReadValue().magnitude > 0.1f);

        if (isGamepad)
        {
            lookX = lookInput.x * gamepadHorizontalSensitivity * Time.deltaTime;
            lookY = lookInput.y * gamepadVerticalSensitivity * Time.deltaTime;
        }
        else
        {
            // Al haber escalado el Mouse en el Input Action Asset (paso 1), 
            // ya no es una locura de velocidad.
            lookX = lookInput.x * mouseHorizontalSensitivity * Time.deltaTime;
            lookY = lookInput.y * mouseVerticalSensitivity * Time.deltaTime;
        }
        // --- Edge Slowdown inteligente ---

        float edgeFactor = 1f;

        // Detectar si vamos hacia el límite
        bool movingTowardLimit = Mathf.Sign(xRotation) != Mathf.Sign(lookY);

        if (movingTowardLimit)
        {
            float normalizedDistance = Mathf.Abs(xRotation) / verticalClamp;

            float distanceFactor = 1f - normalizedDistance;

            distanceFactor = Mathf.Pow(distanceFactor, edgeSlowPower);

            edgeFactor = Mathf.Max(distanceFactor, minVerticalSpeed);
        }
        

        // Aplicar solo si vamos hacia el límite
        lookY *= edgeFactor;

        // Rotación vertical
        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal
        playerBody.Rotate(Vector3.up * lookX);
    }
    public void SetVerticalRotation(float newRotation)
    {
        xRotation = Mathf.Clamp(newRotation, -verticalClamp, verticalClamp);
    }
    public float GetVerticalRotation()
    {
        return xRotation;
    }
}