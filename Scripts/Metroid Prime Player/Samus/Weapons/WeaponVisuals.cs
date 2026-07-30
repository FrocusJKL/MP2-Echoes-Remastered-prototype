using System.Collections;
using UnityEngine;

public class WeaponVisuals : MonoBehaviour
{
    [Header("Switch Visuals")]
    [SerializeField] private float transitionDuration = 0.1f; // Qué tan rápido brilla/se apaga 0.1 es perfecto para un destello rápido
    private string shaderProp = "_TransitionProgress"; // El nombre en tu Shader Graph

    [Header("Mesh References")]
    public Renderer chassisRenderer; // Arrastra aquí SK_Canon_Prime2
    public Renderer lightRenderer;   // Arrastra aquí SK_Canon_Light

    [Header("Settings")]
    [SerializeField] private WeaponSetup[] weaponConfigs;

    [System.Serializable]
    public struct WeaponSetup
    {
        public string weaponName;

        [Header("Weapon Materials")]
        public Material chassisMaterial; // Para SK_Canon_Prime2
        public Material lightMaterial;   // Para SK_Canon_Light 


        [Header("Recoil Settings")]
        public float missileKickAmount; //2
        public float kickAmount; //0.5
        public float kickSpeed; //15
        public float returnSpeed; //5
        public float snappiness; //6

        [Header("Recoil Rotation (Muzzle Flip)")]
        public float missileRotationKick; // 1
        public float rotationKick;       // 0.2
        public float rotationSnappiness;  // 20
        public float rotationReturnSpeed; // 40
    }


    
    [Header("Sway (Movement Look)")]
    [SerializeField] private float swayIntensity = 0.5f;
    [SerializeField] private float swaySmooth = 5f;

    [Header("Sway Balance")]
    [SerializeField] private float mouseSensitivityMult = 0.1f; // Reduce el impacto del ratón
    [SerializeField] private float gamepadSensitivityMult = 5f; // Potencia el joystick

    [Header("Procedural Motion")]
    [SerializeField] private float bobAmount = 0.01f;
    [SerializeField] private float bobSpeed = 15f;
    [SerializeField] private float idleAmount = 0.005f;
    [SerializeField] private float idleSpeed = 1f;

    [Header("Physics Feedback")]
    [SerializeField] private float fallLean = 0.01f;
    [SerializeField] private float jumpSmooth = 5f;
    [SerializeField] private float landingImpactForce = 0.1f;

    [Header("Lock-On View")]
    [SerializeField] private Vector3 lockOnOffset = new Vector3(0f, -0.02f, 0.3f);
    [SerializeField] private float lockOnSmooth = 10f;

    [Header("Charge Feedback")]
    [SerializeField] private float shakeIntensity = 0.02f;
    [SerializeField] private float shakeSpeed = 20f;

    [Header("Switch Visuals")]
    [SerializeField] private Light switchFlashLight; // Arrastra la Point Light blanca aquí
    [SerializeField] private float flashDuration = 0.15f;


    [Header("Audio Reference")]
    public AudioSource audioSource;

    [Header("Input Reference")]
    [SerializeField] private PlayerInputReader inputReader;

    [Header("References")]
    [SerializeField] private WeaponCollisionHandler collisionHandler;
    public Animator anim;

    // State Variables
    private Vector3 originPosition;
    private Quaternion originRotation;
    private Quaternion swayRotation;
    private Vector3 currentRecoilOffset;
    private Vector3 currentShakeOffset;
    private Vector3 jumpOffset;
    private Vector3 currentLockOffset;
    private Vector3 recoilTarget;

    private float moveTimer;
    private float idleTimer;
    private float recoilRotationTarget; // El objetivo de rotación (Pitch)
    private float currentRecoilRotation; // La rotación actual aplicada
    private float currentBobIntensity; // Para transiciones suaves
    //private float currentRecoilPosition;
    private bool isCharging;
    private bool isGrounded = true;
    private bool isLocked;
    private bool isMissileBusy;

    public bool IsMissileCanvasOpen => anim != null && anim.GetBool(hashIsMissileOpen);
    public bool CanFireBeams => !isMissileBusy;
    private int pendingWeaponIndex; // Guarda el índice mientras la animación corre

    // Animator Caching
    private int hashIsCharging;
    private int hashIsLocked;
    private int hashIsMissileOpen;
    private int hashFireMissile;
    private int hashFire;
    private int currentWeaponIndex; // Index of currently equipped 
    private void OnEnable()
    {
        ResetVisuals();
    }
    private void Awake()
    {
        originPosition = transform.localPosition;
        originRotation = transform.localRotation;
        swayRotation = originRotation;

        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (inputReader == null) inputReader = GetComponentInParent<PlayerInputReader>();


        // Cachear parámetros para rendimiento óptimo
        hashIsCharging = Animator.StringToHash("isCharging");
        hashIsLocked = Animator.StringToHash("isLocked");
        hashIsMissileOpen = Animator.StringToHash("isMissileOpen");
        hashFireMissile = Animator.StringToHash("fireMissile");
        hashFire = Animator.StringToHash("fire");
        // ESTA LÓGICA ES LA BUENA
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
            if (audioSource == null)
            {
                audioSource = GetComponentInParent<AudioSource>();
            }
        }

    }
    public void ResetVisuals()
    {
        transform.localPosition = originPosition;
        transform.localRotation = originRotation;
        currentRecoilOffset = Vector3.zero;
        currentShakeOffset = Vector3.zero;
        jumpOffset = Vector3.zero;
        isCharging = false;

        if (anim != null) anim.SetBool(hashIsMissileOpen, false);
    }

    //
    // Método para cambiar entre armas, llamado desde 
    public void PrepareWeaponSwitch(int index)
    {
        pendingWeaponIndex = index;
        currentWeaponIndex = index; // <--- AGREGA ESTA LÍNEA AQUÍ
        if (anim != null)
        {
            anim.SetInteger("Weapon", index);
            // Si tienes un trigger para la animación de cambio, actívalo aquí
            // anim.SetTrigger("ChangeWeapon"); 
        }
    }
    public void ExecuteMaterialChange()
    {
        int index = pendingWeaponIndex;
        if (index >= weaponConfigs.Length) return;

        // 1. Cambiamos el color del chasis (Prime 2)
        if (chassisRenderer != null && weaponConfigs[index].chassisMaterial != null)
        {
            chassisRenderer.material = weaponConfigs[index].chassisMaterial;
        }

        // 2. Cambiamos la luz (Slot 0 de SK_Canon_Light)
        if (lightRenderer != null && weaponConfigs[index].lightMaterial != null)
        {
            // Como SK_Canon_Light tiene 2 slots (visto en tu imagen), 
            // accedemos al array para no perder el segundo slot.
            Material[] mats = lightRenderer.materials;
            if (mats.Length > 0)
            {
                mats[0] = weaponConfigs[index].lightMaterial;
                lightRenderer.materials = mats;
            }
        }

        StartCoroutine(FlashRoutine());
    }
    private IEnumerator FlashRoutine()
    {
        if (switchFlashLight != null)
        {
            switchFlashLight.enabled = true;
            yield return new WaitForSeconds(flashDuration);
            switchFlashLight.enabled = false;
        }
    }
    // 1. Se llama al inicio del clip "Switch Weapon Start"
    public void OnTransitionStart()
    {
        StopAllCoroutines(); // Evita que se crucen brillos
        StartCoroutine(FadeShader(0, 1));
    }

    // 2. Se llama en el clip "Switch Weapon" (el punto de máximo brillo)
    public void OnWeaponSwap()
    {
        currentWeaponIndex = pendingWeaponIndex; // <--- ¡AÑADE ESTO!
        // 1. Usamos tu lógica actual para cambiar los materiales
        ApplyWeaponMaterials(pendingWeaponIndex);

        // --- LA SOLUCIÓN AQUÍ ---
        // Inmediatamente después del cambio, forzamos a que AMBOS 
        // renderers estén al 100% de brillo (valor 1).
        UpdateShaderValue(chassisRenderer, 1f);
        UpdateShaderValue(lightRenderer, 1f);
    }

    // 3. Se llama al inicio del clip "Switch Weapon End"
    public void OnTransitionEnd()
    {
        StartCoroutine(FadeShader(1, 0));
    }

    // --- LÓGICA INTERNA ---

    private void ApplyWeaponMaterials(int index)
    {
        if (index < 0 || index >= weaponConfigs.Length) return;

        // Chasis
        if (chassisRenderer != null)
            chassisRenderer.material = weaponConfigs[index].chassisMaterial;

        // Luces (Manejando los 2 slots que mencionaste)
        if (lightRenderer != null)
        {
            Material[] mats = lightRenderer.materials;
            if (mats.Length > 0)
            {
                mats[0] = weaponConfigs[index].lightMaterial;
                lightRenderer.materials = mats;
            }
        }
    }

    private IEnumerator FadeShader(float start, float end)
    {
        float t = 0;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            // Fórmula de interpolación: $$V = \text{Lerp}(start, end, \frac{t}{duration})$$
            float val = Mathf.Lerp(start, end, t / transitionDuration);

            UpdateShaderValue(chassisRenderer, val);
            UpdateShaderValue(lightRenderer, val);
            yield return null;
        }
    }

    private void UpdateShaderValue(Renderer ren, float val)
    {
        if (ren == null) return;
        // Esto asegura que TODOS los materiales del objeto brillen al unísono
        foreach (Material m in ren.materials)
        {
            if (m.HasProperty(shaderProp))
                m.SetFloat(shaderProp, val);
        }
    }
    // IMPORTANTE: Usa 'Object' (con O mayúscula) como parámetro
    public void PlayWeaponSound(Object soundObject)
    {
        AudioClip clip = soundObject as AudioClip;

        // Como ya asignaste el WeaponSystem en el Inspector, esto ya no será NULL
        if (clip != null && audioSource != null)
        {
            // PlayOneShot es perfecto porque permite que los sonidos se solapen 
            // sin cortarse si cambias de arma muy rápido.
            audioSource.PlayOneShot(clip);
        }
    }

    // --- MÉTODOS PÚBLICOS (API del Script) ---

    public void SetCharging(bool state)
    {
        isCharging = state;
        // Comprobamos si el hash existe en este Animator específico
        if (anim != null && HasParameter(hashIsCharging))
            anim.SetBool(hashIsCharging, state);
    }

    public void SetGrounded(bool state) => isGrounded = state;

    public void SetLockState(bool state)
    {
        isLocked = state;
        if (anim != null && HasParameter(hashIsLocked))
            anim.SetBool(hashIsLocked, state);
    }
    private bool HasParameter(int paramHash)
    {
        if (anim == null) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.nameHash == paramHash) return true;
        }
        return false;
    }
    public void PlayRecoil(bool isCharged)
    {
        // 1. Obtenemos la configuración del arma actual
        WeaponSetup config = weaponConfigs[currentWeaponIndex];

        // 2. Usamos los valores ESPECÍFICOS de esta arma
        float amount = isCharged ? config.kickAmount * 2.5f : config.kickAmount;
        recoilTarget -= Vector3.forward * amount;

        // Levantar la punta
        recoilRotationTarget += isCharged ? config.rotationKick * 2f : config.rotationKick;
    }


    public void PlayMissileAnim(bool open)
    {
        if (anim == null) return;
        if (open) anim.SetTrigger(hashFireMissile);
        anim.SetBool(hashIsMissileOpen, open);
    }

    public void SetMissileBusy(int state)
    {
        // 1 = Ocupado (Bloquea disparo), 0 = Libre (Permite disparo)
        isMissileBusy = (state == 1);

        // Sincronizamos con el booleano del Animator para las transiciones visuales
        if (anim != null)
        {
            anim.SetBool(hashIsMissileOpen, isMissileBusy);
        }
    }

    public void PlayMissileRecoil()
    {
        WeaponSetup config = weaponConfigs[currentWeaponIndex];

        // Los misiles tienen un retroceso y levantamiento mucho más pesado
        recoilTarget -= Vector3.forward * config.missileKickAmount;
        recoilRotationTarget += config.missileRotationKick;
    }

    public void PlayLandingImpact(float intensity = 1f)
    {
        jumpOffset = new Vector3(0, -landingImpactForce * intensity, 0);
    }
    public void PlayShootAnim()
    {
        if (anim == null) return;

        // Al resetear primero, nos aseguramos de que cada click sea un disparo limpio
        // y evitamos que las animaciones se pongan en "cola" de espera.
        anim.ResetTrigger(hashFire);
        anim.SetTrigger(hashFire);
    }

    // --- LÓGICA DE ACTUALIZACIÓN ---

    private void Update()
    {
        // Si es el primer frame, forzamos al handler a darnos datos
        if (Time.frameCount < 2 && collisionHandler != null)
        {
            // Forzamos un update manual del handler si es necesario
        }
        HandleOffsets();
        ApplyFinalTransform();

    }

    private void HandleOffsets()
    {
        // Cambiado 'WeaponConfig' por 'WeaponSetup'
        WeaponSetup config = weaponConfigs[currentWeaponIndex];

        // 1. LÓGICA DE RECOIL POSICIONAL
        float currentSpeed = (recoilTarget.magnitude > 0.01f) ? config.kickSpeed : config.returnSpeed;
        recoilTarget = Vector3.Lerp(recoilTarget, Vector3.zero, Time.deltaTime * currentSpeed);
        currentRecoilOffset = Vector3.Lerp(currentRecoilOffset, recoilTarget, Time.deltaTime * config.snappiness);

        // 2. LÓGICA DE RECOIL ROTACIONAL (Levantamiento de punta)
        recoilRotationTarget = Mathf.Lerp(recoilRotationTarget, 0f, Time.deltaTime * config.rotationReturnSpeed);
        currentRecoilRotation = Mathf.Lerp(currentRecoilRotation, recoilRotationTarget, Time.deltaTime * config.rotationSnappiness);

        // 3. OFFSETS DE MOVIMIENTO

        // LOCK-ON
        Vector3 targetLock = isLocked ? lockOnOffset : Vector3.zero;
        currentLockOffset = Vector3.Lerp(currentLockOffset, targetLock, Time.deltaTime * lockOnSmooth);

        // JUMP
        Vector3 targetJump = isGrounded ? Vector3.zero : new Vector3(0, -fallLean, 0);
        jumpOffset = Vector3.Lerp(jumpOffset, targetJump, Time.deltaTime * jumpSmooth);

        // 4. VIBRACIÓN DE CARGA (Shake)
        // Nota: Añade 'shakeIntensity' y 'shakeSpeed' a tu struct WeaponSetup si quieres que varíen por arma
        // Por ahora, si no están en WeaponSetup, usa las variables globales que ya tenías.
        if (isCharging)
        {
            Vector3 targetShake = new Vector3(
                (Random.value - 0.5f) * shakeIntensity,
                (Random.value - 0.5f) * shakeIntensity,
                0);
            currentShakeOffset = Vector3.Lerp(currentShakeOffset, targetShake, Time.deltaTime * shakeSpeed);
        }
        else
        {
            currentShakeOffset = Vector3.Lerp(currentShakeOffset, Vector3.zero, Time.deltaTime * config.returnSpeed);
        }

        ApplySway();
    }

    //private void ApplySway()
    //{
    //    // Cambiado 'WeaponConfig' por 'WeaponSetup'
    //    WeaponSetup config = weaponConfigs[currentWeaponIndex];

    //    if (inputReader == null || isLocked)
    //    {
    //        //transform.localRotation = Quaternion.Slerp(transform.localRotation, originRotation, Time.deltaTime * swaySmooth);
    //        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * swaySmooth);
    //        return;
    //    }

    //    float lookX = inputReader.Look.x;
    //    float lookY = inputReader.Look.y;

    //    float weight = (Mathf.Abs(lookX) + Mathf.Abs(lookY) > 2f) ? mouseSensitivityMult : gamepadSensitivityMult;

    //    // Si 'swayIntensity' no está en el struct, usa la global.
    //    float targetX = -lookX * swayIntensity * weight;
    //    float targetY = lookY * swayIntensity * weight;

    //    targetX = Mathf.Clamp(targetX, -15f, 15f);
    //    targetY = Mathf.Clamp(targetY, -15f, 15f);

    //    Quaternion targetRot = originRotation * Quaternion.Euler(targetY, targetX, 0);
    //    transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * swaySmooth);
    //}
    private void ApplySway()
    {

        WeaponSetup config = weaponConfigs[currentWeaponIndex];

        if (inputReader == null || isLocked)
        {
            swayRotation = Quaternion.Slerp(
                swayRotation,
                originRotation,
                Time.deltaTime * swaySmooth);
            return;
        }

        float lookX = inputReader.Look.x;
        float lookY = inputReader.Look.y;

        float weight = (Mathf.Abs(lookX) + Mathf.Abs(lookY) > 2f) ? mouseSensitivityMult : gamepadSensitivityMult;

        float targetX = -lookX * swayIntensity * weight;
        float targetY = lookY * swayIntensity * weight;

        targetX = Mathf.Clamp(targetX, -15f, 15f);
        targetY = Mathf.Clamp(targetY, -15f, 15f);

        Quaternion targetRot = originRotation * Quaternion.Euler(targetY, targetX, 0);

        swayRotation = Quaternion.Slerp(
            swayRotation,
            targetRot,
            Time.deltaTime * swaySmooth);
    }
    private void ApplyFinalTransform()
    {
        Vector3 finalBob = isGrounded ? CalculateBobbing() : Vector3.zero;
        Vector3 finalIdle = CalculateIdle();

        // 1. Posición final (la que ya tenías perfecta)
        //transform.localPosition = originPosition + currentRecoilOffset + finalBob +
        //                          currentShakeOffset + finalIdle + jumpOffset + currentLockOffset;
        Vector3 collisionOffset = collisionHandler != null? collisionHandler.CollisionOffset: Vector3.zero;

        transform.localPosition = originPosition + currentRecoilOffset + finalBob +
                                  currentShakeOffset + finalIdle + jumpOffset +
                                  currentLockOffset + collisionOffset;
        // 2. Rotación final: Combinamos el Sway (que ya está en el transform) con el levantamiento
        // Usamos el valor negativo para que la punta suba
        Quaternion muzzleFlip = Quaternion.Euler(-currentRecoilRotation, 0, 0);

        // Multiplicamos para añadir el levantamiento sobre la rotación que ya tenga el arma por el Sway

        //transform.localRotation = transform.localRotation * muzzleFlip;

        Quaternion collisionRot = collisionHandler != null? collisionHandler.CollisionRotation: Quaternion.identity;

        transform.localRotation = swayRotation * muzzleFlip * collisionRot;
    }

    private Vector3 CalculateIdle()
    {
        idleTimer += Time.deltaTime * idleSpeed;
        return new Vector3(Mathf.Cos(idleTimer) * idleAmount, Mathf.Sin(idleTimer * 2f) * idleAmount, 0);
    }

    private Vector3 CalculateBobbing()
    {
        // 1. Obtenemos la magnitud del movimiento (de 0 a 1)
        // Si mueves el stick un 20%, moveInput será 0.2
        float moveInput = (inputReader != null) ? inputReader.Move.magnitude : 0f;

        // Aplicamos un pequeño umbral (Deadzone) para que en 0 no se mueva nada
        float targetIntensity = (moveInput > 0.1f) ? moveInput : 0f;

        // 2. Suavizamos la transición de la intensidad para que no sea nerviosa
        // Esto hace que si sueltas el stick de golpe, el arma no se frene en seco
        currentBobIntensity = Mathf.Lerp(currentBobIntensity, targetIntensity, Time.deltaTime * 5f);

        if (currentBobIntensity > 0.01f)
        {
            // 3. El tiempo del Bobbing también puede ser progresivo
            // Así, al caminar despacio, el balanceo es más lento; al correr, es más rápido.
            moveTimer += Time.deltaTime * bobSpeed * currentBobIntensity;

            // 4. La amplitud total depende de la intensidad actual
            float x = Mathf.Cos(moveTimer / 2) * bobAmount * currentBobIntensity;
            float y = Mathf.Sin(moveTimer) * bobAmount * currentBobIntensity;

            return new Vector3(x, y, 0);
        }

        moveTimer = Mathf.Lerp(moveTimer, 0, Time.deltaTime * bobSpeed);
        return Vector3.zero;
    }

    private bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
    // Ejemplo de cómo aplicar el retroceso ahora:
    public void ApplyRecoil()
    {
        // CAMBIO: Debe ser WeaponSetup, que es el nombre de tu struct
        WeaponSetup config = weaponConfigs[currentWeaponIndex];

        // Ahora 'config' ya sabe qué es 'kickAmount'
        recoilTarget -= Vector3.forward * config.kickAmount;
    }
    private void ApplyVibration()
    {
        WeaponSetup config = weaponConfigs[currentWeaponIndex];

        if (isCharging)
        {
            Vector3 targetShake = new Vector3(
                (Random.value - 0.5f) * shakeIntensity,
                (Random.value - 0.5f) * shakeIntensity,
                0);
            currentShakeOffset = Vector3.Lerp(currentShakeOffset, targetShake, Time.deltaTime * shakeSpeed);
        }
        else
        {
            currentShakeOffset = Vector3.Lerp(currentShakeOffset, Vector3.zero, Time.deltaTime * config.returnSpeed);
        }
    }

    private Vector3 ApplyIdle()
    {
        idleTimer += Time.deltaTime * idleSpeed;
        return new Vector3(Mathf.Cos(idleTimer) * idleAmount, Mathf.Sin(idleTimer * 2f) * idleAmount, 0);
    }

    private Vector3 ApplyBobbing()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
            moveTimer += Time.deltaTime * bobSpeed;
            return new Vector3(Mathf.Cos(moveTimer / 2) * bobAmount, Mathf.Sin(moveTimer) * bobAmount, 0);
        }

        moveTimer = Mathf.Lerp(moveTimer, 0, Time.deltaTime * bobSpeed);
        return Vector3.zero;
    }

}


