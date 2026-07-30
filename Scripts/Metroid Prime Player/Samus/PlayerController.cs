using System.Security.Cryptography;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    private CharacterController controller;
    public AudioSource audioSource;
    private WeaponVisuals weaponVisuals;
    private PlayerInputReader input;
    [SerializeField] private LockOnSystem lockOnSystem;

    [Header("Fisicas")]
    public float speed = 8f;
    public float gravity = -20f;
    public float jumpHeight = 3f;

    [Header("Doble Jump")]
    private bool canDoubleJump; // Controla si el segundo salto está disponible
    private bool jumpInputConsumed; // Para evitar que un solo toque active ambos saltos

    [Header("Double Jump Activation")]
    [SerializeField] private float doubleJumpActivationDelay = 0.12f;
    private float jumpStartTime;

    [Header("Dash Cooldown")]
    [SerializeField] private float dashCooldown = 0.18f;
    private float lastDashTime;

    [Header("Jump Audio")]
    public AudioClip normalJumpSound;
    public AudioClip spaceJumpSound;
    [Range(0f, 1f)][SerializeField] private float jumpVolumen = 0.5f;

    [Header("Lock On Combat Movement")]
    [SerializeField] private float sideDashForce = 10f;
    [SerializeField] private float backFlipForce = 4f;
    //[SerializeField] private float forwardDashForce = 4f;
    [SerializeField] private float jumpVerticalForce = 8f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float airDashMultiplier = 1.2f;

    [Header("Settings Fall Impact")]
    public float fallVelocityThreshold = -3.0f;
    public float landingCooldownTime = 0.2f;
    private float landingCooldown = 1f;

    [Header("Impact Camera Dip")]
    [SerializeField] private Transform cameraPivot; // Lo ideal es que sea un objeto padre de la cámara
    [SerializeField] private float dipIntensity = 0.2f; // Qué tanto baja la cámara
    [SerializeField] private float dipSpeed = 10f;


    private Vector3 velocity;
    private Vector3 dashVelocity;

    private float dashTimer;
    private bool isGrounded;
    private bool wasGrounded;

    Vector3 GetTargetDirection()
    {
        if (lockOnSystem == null || !lockOnSystem.IsLocked)
            return transform.forward;

        Vector3 dir = lockOnSystem.CurrentTarget.position - transform.position;
        dir.y = 0;

        return dir.normalized;
    }
    private void PlayJumpSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, jumpVolumen);
        }
    }
    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        if (lockOnSystem == null)
            lockOnSystem = GetComponentInChildren<LockOnSystem>();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        wasGrounded = isGrounded;
        // Capturamos las colisiones del frame anterior
        CollisionFlags flags = controller.collisionFlags;
        isGrounded = controller.isGrounded;
        

        // Referencia dinámica a visuales
        if (weaponVisuals == null || !weaponVisuals.gameObject.activeInHierarchy)
        {
            weaponVisuals = GetComponentInChildren<WeaponVisuals>();
        }
        if (weaponVisuals != null) weaponVisuals.SetGrounded(isGrounded);


        // 1. Detección de impacto (Visual, Animación y Camera Dip)
        if (!wasGrounded && isGrounded && velocity.y < fallVelocityThreshold)
        {
            if (Time.time > landingCooldown)
            {
                // Normalizamos la intensidad (a más velocidad, más baja la cámara)
                float intensity = Mathf.Abs(velocity.y) / 10f;
                intensity = Mathf.Clamp(intensity, 0.5f, 2.0f);

                // Visuales del arma
                if (weaponVisuals != null) weaponVisuals.PlayLandingImpact(intensity);

                // --- CAMERA DIP (NUEVO) ---
                if (cameraPivot != null)
                {
                    StopAllCoroutines(); // Evita que se amontonen movimientos si saltas rápido
                    StartCoroutine(LandingDip(intensity));
                }

                landingCooldown = Time.time + landingCooldownTime;
            }
        }


        // --- DETECCIÓN DE TECHO ---
        // Si chocamos arriba, la velocidad subiendo muere para caer de inmediato
        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0)
        {
            velocity.y = -1f;
        }



        // Lógica de Salto con Sonidos
        if (input.JumpPressed)
        {
            if (!jumpInputConsumed)
            {
                if (isGrounded)
                {
                    // SI HAY LOCK ON Y MOVIMIENTO LATERAL
                    if (lockOnSystem != null && lockOnSystem.IsLocked)
                    {
                        ExecuteDirectionalDash();
                    }
                    else
                    {
                        ExecuteJump(normalJumpSound);
                    }

                    jumpInputConsumed = true;
                }
                else if (canDoubleJump && Time.time > jumpStartTime + doubleJumpActivationDelay)
                {
                    if (lockOnSystem != null && lockOnSystem.IsLocked)
                    {
                        ExecuteDirectionalDash();
                    }
                    else
                    {
                        ExecuteJump(spaceJumpSound);
                    }

                    canDoubleJump = false;
                    jumpInputConsumed = true;
                }
            }
        }
        else
        {
            jumpInputConsumed = false;
        }
        // --- 4. MOVIMIENTO Y GRAVEDAD ---
        Vector3 move = transform.right * input.Move.x + transform.forward * input.Move.y;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -3f;
            canDoubleJump = true;
        }

        velocity.y += gravity * Time.deltaTime;

        

        Vector3 finalMove = move * speed + dashVelocity + Vector3.up * velocity.y;
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
        }
        else
        {
            dashVelocity = Vector3.zero;
        }

        controller.Move(finalMove * Time.deltaTime);

    }
    private void ExecuteSideDash(float direction)
    {
        if (Time.time < lastDashTime + dashCooldown)
            return;

        lastDashTime = Time.time;

        Vector3 targetDir = GetTargetDirection();
        // Vector lateral alrededor del enemigo
        Vector3 sideDir = Vector3.Cross(Vector3.up, targetDir) * direction;

        dashVelocity = sideDir * sideDashForce * airDashMultiplier;

        velocity.y = jumpVerticalForce;
        dashTimer = dashDuration;

        PlayJumpSound(spaceJumpSound);
    }
    private void ExecuteBackflip()
    {
        Vector3 targetDir = GetTargetDirection();

        dashVelocity = -targetDir * backFlipForce;

        velocity.y = jumpVerticalForce;

        dashTimer = dashDuration;

        PlayJumpSound(spaceJumpSound);
    }
    //private void ExecuteForwardDash()
    //{
    //    Vector3 targetDir = GetTargetDirection();

    //    dashVelocity = targetDir * forwardDashForce;

    //    velocity.y = jumpVerticalForce * 0.7f;

    //    dashTimer = dashDuration;

    //    PlayJumpSound(spaceJumpSound);
    //}

    private void ExecuteDirectionalDash()
    {
        if (Time.time < lastDashTime + dashCooldown)
        {
            ExecuteJump(spaceJumpSound);
            return;
        }
        float x = input.Move.x;
        float y = input.Move.y;

        if (x > 0.9f)
            ExecuteSideDash(1);

        else if (x < -0.9f)
            ExecuteSideDash(-1);

        else if (y < -0.9f)
            ExecuteBackflip();

        //else if (y > 0.9f)
        //    ExecuteForwardDash();

        else
            ExecuteJump(spaceJumpSound);
    }

    private IEnumerator LandingDip(float intensityMultiplier)
    {
        Vector3 targetPos = new Vector3(0, -dipIntensity * intensityMultiplier, 0);
        Vector3 startPos = Vector3.zero; // Asumiendo que la posición local inicial es 0
        float t = 0;

        // 1. Bajada (Impacto)
        while (t < 1.0f)
        {
            t += Time.deltaTime * dipSpeed;
            cameraPivot.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        t = 0;
        // 2. Regreso (Recuperación)
        while (t < 1.0f)
        {
            t += Time.deltaTime * (dipSpeed * 0.5f); // El regreso es un poco más lento
            cameraPivot.localPosition = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }

        cameraPivot.localPosition = startPos;
    }

    private void ExecuteJump(AudioClip soundToPlay)
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        jumpStartTime = Time.time;

        if (audioSource != null && soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay, jumpVolumen);
        }
    }

    private void HandleMovement()
    {
        Vector3 move = transform.right * input.Move.x + transform.forward * input.Move.y;
        controller.Move(move * speed * Time.deltaTime);
    }

    private void HandleVisualsAndImpact()
    {
        // 1. Asegurar referencia (por si cambiaste de arma en runtime)
        if (weaponVisuals == null || !weaponVisuals.gameObject.activeInHierarchy)
        {
            weaponVisuals = GetComponentInChildren<WeaponVisuals>();
        }

        if (weaponVisuals != null) weaponVisuals.SetGrounded(isGrounded);

        // 2. DETECCIÓN DE IMPACTO (Lógica corregida)
        // Si en el frame anterior NO estaba en el suelo, pero en este SÍ, y la velocidad era de caída
        if (!wasGrounded && isGrounded)
        {
            // Usamos un pequeño margen para que no salte por simples escalones
            if (velocity.y < fallVelocityThreshold)
            {
                if (Time.time > landingCooldown)
                {
                    // Calculamos la intensidad basada en qué tan rápido caíamos
                    float intensity = Mathf.Abs(velocity.y) / 10f; // Dividir por 10f para normalizar el valor
                    intensity = Mathf.Clamp(intensity, 0.5f, 2f); // Limitar para que no sea exagerado

                    if (weaponVisuals != null)
                    {
                        weaponVisuals.PlayLandingImpact(intensity);
                    }

                    landingCooldown = Time.time + landingCooldownTime;
                }
            }
        }
    }

}

