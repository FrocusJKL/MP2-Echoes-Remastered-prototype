using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstructionLayer;
    [SerializeField] private float lockRange = 30f;
    [SerializeField] private float lockAngle = 60f;
    [SerializeField] private float rotationSpeed = 10f; // Más rápido para que se sienta responsivo

    [Header("Aim Bullet Assist")]
    [SerializeField] private bool enableBulletMagnetism = true;
    [SerializeField] private float magnetismRange = 40f;
    [SerializeField] private float magnetismAngle = 6f;
    [SerializeField] private float magnetismStrength = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip lockOnClip;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private CameraController cameraController;


    [Header("Debug")]
    [SerializeField] private bool showLockGizmo = true;
    [SerializeField] private Color gizmoLockOnColor = Color.cyan;
    [SerializeField] private Color gizmoAimColor = Color.yellow;




    public bool IsLocked => currentTarget != null;
    private bool isInputHeld = false;
    public bool IsDead => isDead;
    private bool isDead = false; 

    private Quaternion storedCameraRotation;
    private Quaternion storedPlayerRotation;
    public Transform CurrentTarget => currentTarget;
    private Transform currentTarget;
    private WeaponVisuals currentWeaponVisuals;


    public bool GetMagnetismTarget(out Transform target, out float strength)
    {
        target = null;
        strength = 0f;

        if (!enableBulletMagnetism) return false;

        Collider[] enemies = Physics.OverlapSphere(cameraTransform.position, magnetismRange, enemyLayer);

        float smallestAngle = Mathf.Infinity;

        foreach (Collider enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;

            Vector3 dir = enemy.transform.position - cameraTransform.position;
            float angle = Vector3.Angle(cameraTransform.forward, dir);

            if (angle > magnetismAngle) continue;

            if (angle < smallestAngle)
            {
                smallestAngle = angle;
                target = enemy.transform;
            }
        }

        if (target == null) return false;

        // fuerza basada en qué tan centrado está
        float t = 1f - (smallestAngle / magnetismAngle);
        strength = t * magnetismStrength;

        return true;
    }
    private void PlayLockSound()
    {
        if (audioSource != null && lockOnClip != null)
        {
            audioSource.PlayOneShot(lockOnClip);
        }
    }

    private void Awake()
    {
        if (inputReader == null) inputReader = GetComponentInParent<PlayerInputReader>();

        // El LockOnSystem suele estar en la cámara o el jugador, 
        // buscamos las referencias si no están asignadas
        if (playerTransform == null) playerTransform = transform.root;
        if (cameraTransform == null) cameraTransform = Camera.main.transform; 
        if (cameraController == null) cameraController = cameraTransform.GetComponent<CameraController>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    private void Update()
    {
        // 1. Sincronizamos el estado del botón del Gamepad/Mouse
        // Suponiendo que JumpPressed o una nueva propiedad 'LockOnPressed' existe
        // Para este ejemplo, usaremos una lógica de detección de pulsación profesional:

        // NOTA: Para que sea 100% compatible con tu PlayerInputReader, 
        // deberías añadir 'public bool LockOnPressed' en ese script.
        // Por ahora, usemos una validación de seguridad:

        HandleInput();

        if (currentTarget == null)
        {
            Unlock();
            return;
        }

        if (isInputHeld)
        {
            ValidateTarget();
        }
    }
    private void HandleInput()
    {
        // Esta es la forma profesional de leer el input persistente del Gamepad
        // Si tienes un evento OnLockOnPerformed, úsalo, pero para el "Mantener", 
        // lo ideal es leer el valor booleano del Input Action.

        // Simularemos la lectura desde el PlayerInputReader (Asegúrate de tener esta lógica allí)
        // bool lockInput = inputReader.LockOnPressed; 

        // Si prefieres usar el sistema de eventos que ya tienes:
        // El PlayerInputReader debería actualizar una variable booleana cuando se presiona/suelta.
    }

    // Estos métodos deben ser llamados por el PlayerInputReader a través de eventos
    public void OnLockPressed()
    {
        isInputHeld = true;
        TryLock();
    }

    public void OnLockReleased()
    {
        isInputHeld = false;
        Unlock();
    }

    private void ValidateTarget()
    {
        if (currentTarget == null)
        {
            Unlock();
            return;
        }
        // 🔥 NUEVO: detectar si está muerto
        Health health = currentTarget.GetComponent<Health>();
        if (health != null && health.IsDead)
        {
            Unlock();
            return;
        }

        // Si el objetivo se destruye o se aleja demasiado
        if (!currentTarget.gameObject.activeInHierarchy)
        {
            Unlock();
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, currentTarget.position);
        if (distance > lockRange)
        {
            Unlock();
        }

    }

    private void TryLock()
    {
        Collider[] enemies = Physics.OverlapSphere(cameraTransform.position, lockRange, enemyLayer);
        float smallestAngle = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (Collider enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;

            Vector3 directionToEnemy = enemy.transform.position - cameraTransform.position;
            float angle = Vector3.Angle(cameraTransform.forward, directionToEnemy);

            // Filtro de ángulo (Cono de visión)
            if (angle > lockAngle / 2f) continue;

            // Filtro de obstrucción (Raycast)
            float distanceToEnemy = directionToEnemy.magnitude;
            if (Physics.Raycast(cameraTransform.position, directionToEnemy.normalized, out RaycastHit hit, distanceToEnemy, obstructionLayer))
            {
                continue;
            }

            if (angle < smallestAngle)
            {
                smallestAngle = angle;
                bestTarget = enemy.transform;
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            UpdateWeaponVisualsReference();
            if (currentWeaponVisuals != null) currentWeaponVisuals.SetLockState(true);
            PlayLockSound();
        }
    }

    public void Unlock()
    {
        currentTarget = null;

        if (currentWeaponVisuals != null)
            currentWeaponVisuals.SetLockState(false);
    }

    private void UpdateWeaponVisualsReference()
    {
        // Como el arma puede cambiar (WeaponSystem), buscamos los visuales del arma activa
        // Esto evita conflictos cuando cambias de Beam a Misiles
        currentWeaponVisuals = playerTransform.GetComponentInChildren<WeaponVisuals>();
    }

    private void LateUpdate()
    {
        if (currentTarget == null)
        {
            Unlock();
            return;
        }

        if (currentTarget != null)
        {
            RotateTowardsTarget();
        }
        else
        {
            // Mantener la rotación final después del lock
            if (storedCameraRotation != Quaternion.identity)
            {
                cameraTransform.rotation = storedCameraRotation;
                playerTransform.rotation = storedPlayerRotation;

                storedCameraRotation = Quaternion.identity;
            }
        }
    }

    private void RotateTowardsTarget()
    {
        // 1. Rotación del Jugador (Horizontal)
        Vector3 flatDirection = currentTarget.position - playerTransform.position;
        flatDirection.y = 0;
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(flatDirection);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        // 2. Rotación de la Cámara (Vertical)
        Vector3 camDirection = currentTarget.position - cameraTransform.position;
        if (camDirection.sqrMagnitude > 0.001f)
        {
            Quaternion camLookRot = Quaternion.LookRotation(camDirection);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, camLookRot, rotationSpeed * Time.deltaTime);
        }
        

        if (camDirection.sqrMagnitude > 0.001f)
        {
            Quaternion camLookRot = Quaternion.LookRotation(camDirection);

            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, camLookRot, rotationSpeed * Time.deltaTime);

            // SINCRONIZAR con CameraController
            float pitch = cameraTransform.eulerAngles.x;

            if (pitch > 180) pitch -= 360;

            cameraController.SetVerticalRotation(pitch);
        }
    }
    public Transform GetMagnetismTarget()
    {
        if (!enableBulletMagnetism) return null;

        Collider[] enemies = Physics.OverlapSphere(cameraTransform.position, magnetismRange, enemyLayer);

        Transform bestTarget = null;
        float smallestAngle = Mathf.Infinity;

        foreach (Collider enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;

            Vector3 dir = enemy.transform.position - cameraTransform.position;
            float angle = Vector3.Angle(cameraTransform.forward, dir);

            if (angle > magnetismAngle) continue;

            if (angle < smallestAngle)
            {
                smallestAngle = angle;
                bestTarget = enemy.transform;
            }
        }

        return bestTarget;
    }

    private void OnDrawGizmos()
    {
        if (!showLockGizmo || cameraTransform == null) return;

        // 1. Dibujar Rango y Cono de Bloqueo
        Gizmos.color = gizmoLockOnColor;
        Gizmos.DrawWireSphere(cameraTransform.position, lockRange);
        DrawConeGizmo(cameraTransform, lockAngle, lockRange);

        // 2. Dibujar Rango y Cono de Aim Assist
        if (enableBulletMagnetism)
        {
            Gizmos.color = gizmoAimColor;
            Gizmos.DrawWireSphere(cameraTransform.position, magnetismRange);
            DrawConeGizmo(cameraTransform, magnetismAngle * 2f, magnetismRange);
            // Nota: Multipliqué por 2 porque en tu código original usabas el ángulo total, 
            // mientras que arriba usabas la mitad.
        }
    }

    // Método auxiliar para no repetir código y mantenerlo limpio
    private void DrawConeGizmo(Transform cam, float angle, float range)
    {
        Vector3 pos = cam.position;
        Vector3 forward = cam.forward;
        Vector3 up = cam.up; // Usamos el "up" local de la cámara

        // Calculamos las rotaciones usando el eje local de la cámara (up)
        // para que el cono rote correctamente al mirar arriba/abajo
        Quaternion leftRotation = Quaternion.AngleAxis(-angle / 2, up);
        Quaternion rightRotation = Quaternion.AngleAxis(angle / 2, up);

        Vector3 leftRayDirection = leftRotation * forward;
        Vector3 rightRayDirection = rightRotation * forward;
        
        Gizmos.DrawRay(pos, leftRayDirection * range);
        Gizmos.DrawRay(pos, rightRayDirection * range);

    }

}



