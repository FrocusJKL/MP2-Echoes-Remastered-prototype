using System.Collections;
using UnityEngine;

public class BeamWeapon : Weapon
{
    [Header("Weapon Identity")]
    [SerializeField] private int weaponIndex; // Pon 0 para Power, 1 para Dark, 2 para Light, 3 para Annihilator etc...

    [Header("Projectile Spawn Points")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform chargePoint;

    [Header("Projectiles")]
    [SerializeField] private GameObject normalProjectile; // Arrastra el prefab del disparo normal aquí
    [SerializeField] private GameObject chargedProjectile; // Arrastra el prefab del disparo cargado aquí (puede ser el mismo con más efectos)
    [SerializeField] private GameObject missileProjectile; // Arrastra el prefab del misil aquí (puede ser un proyectil diferente o el mismo con lógica de misil)

    [Header("Charge Visuals")]
    [SerializeField] private GameObject chargeEffectPrefab;
    [SerializeField] private float maxChargeScale = 1.5f; // 2
    [SerializeField] private float minChargeToRelease = 0.2f; //0.2
    [SerializeField] private float chargedFireCooldown = 0.5f; //0.5

    [Header("Timing & Rates")]
    [SerializeField] private float fireRate = 0.2f; //0.1
    [SerializeField] private float chargeTime = 1.2f; //1.2
    [SerializeField] private float startChargeDelay = 0.2f; //0.2
    [SerializeField] private float missileCooldown = 1.5f; //1.5
    [SerializeField] private float missileIdleTime = 4f; //8

    [Header("Charge Shot Settings")]
    [SerializeField] private float postChargeDelay = 0.5f; //0.5 // El tiempo que quieres bloquear
    private bool canFire = true; // Control global de disparo

    [Header("Missile Logic")]
    [SerializeField] private float missileCooldownTime = 0.8f; //0.8 // Tiempo que tarda en cerrar el cañón
    private float lastMissileTime;

    [Header("Heat System")]
    [SerializeField] private ParticleSystem heatVaporParticles; // Arrastra el sistema de partículas aquí
    [SerializeField] private float heatPerNormalShot = 0.1f; // Cuánto calor sube por cada clic normal
    [SerializeField] private float heatPerChargedShot = 0.8f; // El cargado casi llena el calor
    [SerializeField] private float heatDissipationRate = 0.5f; // Cuánto calor pierde por segundo
    [SerializeField] private float heatThresholdToVapor = 0.4f; // A qué nivel de calor empieza a salir vapor

    [Header("Audio Clips")]
    [SerializeField] private AudioClip fireNormalClip;
    [SerializeField] private AudioClip chargeStartClip;
    [SerializeField] private AudioClip chargeLoopClip;
    [SerializeField] private AudioClip fireChargedClip;
    [SerializeField] private AudioClip fireMissileClip;

    [Header("Haptics")]
    [Range(0f, 1f)][SerializeField] private float maxChargeRumble = 0.4f;


    
    private float chargeStartTime;
    private float currentHeatLevel;
    private bool isCharging;
    private bool isVaporActive;

    private PlayerInputReader inputReader;
    private LockOnSystem lockOnSystem;
    private Coroutine chargeCoroutine;
    private GameObject currentChargeEffect;

    private void SetState(WeaponState newState)
    {
        state = newState;
        //Debug.Log("Weapon State: " + state); // Puedes descomentar esta línea para ver los cambios de estado en la consola
    }

    private WeaponState state = WeaponState.Idle;
    public enum WeaponState
    {
        Idle,
        Charging,
        Firing,
        Cooldown,
        Missile
    }

    private bool WeaponBusy()
    {
        return state == WeaponState.Charging
            || state == WeaponState.Cooldown
            || state == WeaponState.Firing;
    }


    private Coroutine missileCoroutine;
    // --- OVERRIDES DE LA CLASE BASE ---

    public override void OnPress()
    {
        // Permitir cancelar misil con disparo
        if (state == WeaponState.Missile)
        {
            InterruptMissileSequence();
            state = (WeaponState.Idle);
        }

        if (WeaponBusy()) return;

        if (visuals != null && !visuals.CanFireBeams) return;

        if (Time.time < LastShotTime + fireRate) return;

        FireNormal();

        if (chargeCoroutine != null)
            StopCoroutine(chargeCoroutine);

        chargeCoroutine = StartCoroutine(ChargeRoutine());
    }

    public override void OnHold()
    {
        // La lógica de escala se maneja en Update para suavidad máxima
    }

    public override void OnRelease()
    {
        if (!isCharging)
        {
            StopChargeSequence();
            return;
        }

        float currentChargeTime = Time.time - chargeStartTime;

        // 1. DISPARO CARGADO (100%)
        if (currentChargeTime >= chargeTime)
        {
            FireCharged();
        }
        // 2. DISPARO SEMICARGADO (Metroid Style)
        // Si soltamos después del delay inicial pero antes del 100%
        else if (currentChargeTime >= minChargeToRelease)
        {
            FireSemiCharged();
        }
        // 3. CANCELACIÓN (Si fue demasiado pronto)
        else
        {
            StopChargeSequence();
        }
    }

    public override void OnMissile()
    {
        if (WeaponBusy()) return;

        if (Time.time < lastMissileTime + missileCooldown) return;

        if (isCharging) StopChargeSequence();

        FireMissileLogic();
    }

    // --- LÓGICA DE ACTUALIZACIÓN ---

    protected override void Awake()
    {
        base.Awake();
        // Buscamos el lector de input en el objeto raíz del jugador
        inputReader = GetComponentInParent<PlayerInputReader>();
        lockOnSystem = GetComponentInParent<LockOnSystem>();
    }

    private void Update()
    {
        if (isCharging)
        {
            UpdateChargeVisuals();
            UpdateChargeHaptics();
        }

        HandleHeatDissipation();
    }

    private void UpdateChargeHaptics()
    {
        if (inputReader == null) return;

        // Calculamos el progreso (0 a 1)
        float t = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);

        // La frecuencia baja (motor izquierdo) da sensación de peso/poder
        // La frecuencia alta (motor derecho) da sensación de electricidad/tensión
        float intensity = t * maxChargeRumble;

        inputReader.SetRumble(intensity * 0.5f, intensity);
    }

    private void UpdateChargeVisuals()
    {
        // [CÓDIGO ANTERIOR] - Ya está bien, el problema no es aquí.
        if (currentChargeEffect == null) return;

        currentChargeEffect.transform.position = chargePoint.position;
        currentChargeEffect.transform.rotation = chargePoint.rotation;

        float t = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
        float currentScale = Mathf.Lerp(0.1f, maxChargeScale, t);

        if (t >= 1.0f)
        {
            currentScale += Mathf.Sin(Time.time * 25f) * 0.05f;
        }

        currentChargeEffect.transform.localScale = Vector3.one * currentScale;
    }

    // --- ACCIONES DE DISPARO ---

    private void FireNormal()
    {

        // 1. Verificamos si el cañón visual está abierto por un misil
        if (visuals != null && visuals.IsMissileCanvasOpen) return;

        // 2. Verificamos si ha pasado suficiente tiempo desde el último misil
        if (Time.time < lastMissileTime + missileCooldownTime) return;
        if (visuals != null && !visuals.CanFireBeams)
        {
            return; // Si entra aquí, el código de abajo jamás se ejecuta
        }
        LastShotTime = Time.time;
        Quaternion shotRotation = firePoint.rotation;

        if (lockOnSystem != null)
        {
            //Transform target = lockOnSystem.GetMagnetismTarget();
            if (lockOnSystem != null &&
                lockOnSystem.GetMagnetismTarget(out Transform target, out float strength))
            {
                Vector3 dir = target.position - firePoint.position;
                Quaternion targetRot = Quaternion.LookRotation(dir);

                shotRotation = Quaternion.Slerp(
                    firePoint.rotation,
                    targetRot,
                    strength
                );
            }
            //if (target != null)
            //{
            //    Vector3 dir = target.position - firePoint.position;

            //    Quaternion targetRot = Quaternion.LookRotation(dir);

            //    shotRotation = Quaternion.Slerp(
            //        firePoint.rotation,
            //        targetRot,
            //        0.6f
            //    );
            //}
        }

        Instantiate(normalProjectile, firePoint.position, shotRotation);

        if (audioSource && fireNormalClip) audioSource.PlayOneShot(fireNormalClip);

        if (visuals != null)
        {
            visuals.PrepareWeaponSwitch(weaponIndex); // <--- Línea vital
            visuals.PlayRecoil(false);
            visuals.PlayShootAnim();
        }
        currentHeatLevel = Mathf.Clamp01(currentHeatLevel + heatPerNormalShot);
    }

    // --- LÓGICA DE DISPARO CARGADO ---

    private void FireCharged()
    {
        state = WeaponState.Firing;

        if (!canFire) return;
        // En lugar de usar Time.time normal, le sumamos el cooldown especial
        // Esto bloquea el OnPress hasta que pase este tiempo extra
        LastShotTime = Time.time + (chargedFireCooldown - fireRate);

        if (chargedProjectile && firePoint)
            Instantiate(chargedProjectile, firePoint.position, firePoint.rotation);

        StopChargeSequence();

        if (audioSource && fireChargedClip)
            audioSource.PlayOneShot(fireChargedClip);

        if (visuals != null)
        {
            visuals.PrepareWeaponSwitch(weaponIndex); // <--- Línea vital
            visuals.PlayRecoil(true);
            // Opcional: Si tienes una animación de "Humo" o "CoolDown" en el Animator
             visuals.anim.SetTrigger("OnOverheat");
        }
        

        // ... aquí va tu lógica actual para instanciar el disparo, sonido, etc.

        // Iniciamos el bloqueo
        StartCoroutine(PostChargeCooldown());
        currentHeatLevel = Mathf.Clamp01(currentHeatLevel + heatPerChargedShot);
    }
    private IEnumerator PostChargeCooldown()
    {
        state = WeaponState.Cooldown;

        yield return new WaitForSeconds(postChargeDelay);

        state = WeaponState.Idle;
    }
    // --- LIMPIEZA DE SECUENCIA DE CARGA ---


    private void FireSemiCharged()
    {
        LastShotTime = Time.time;

        // Disparamos el proyectil normal (o uno intermedio si prefieres)
        if (normalProjectile && firePoint)
            Instantiate(normalProjectile, firePoint.position, firePoint.rotation);

        // Sonido: Usamos el de disparo normal pero podrías usar uno con más "punch"
        if (audioSource && fireChargedClip)
            audioSource.PlayOneShot(fireChargedClip);

        if (visuals != null) visuals.PlayRecoil(false);

        StopChargeSequence();
    }
    private void StopChargeSequence()
    {
        isCharging = false;
        inputReader?.StopRumble();
        state = WeaponState.Idle;

        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
        }

        if (currentChargeEffect != null) Destroy(currentChargeEffect);
        if (visuals != null) visuals.SetCharging(false);

        if (audioSource)
        {
            audioSource.loop = false;
            // En lugar de Stop() inmediato, dejamos que el sonido de disparo 
            // (PlayOneShot) cubra la interrupción del loop.
            audioSource.clip = null;
        }
    }

    private IEnumerator PulseRumble(float duration, float low, float high)
    {
        inputReader?.SetRumble(low, high);
        yield return new WaitForSeconds(duration);
        if (!isCharging) inputReader?.StopRumble();
    }

    private void FireMissileLogic()
    {
        lastMissileTime = Time.time;
        state = WeaponState.Missile;

        // Disparo físico
        Instantiate(missileProjectile, firePoint.position, firePoint.rotation);

        if (audioSource && fireMissileClip) audioSource.PlayOneShot(fireMissileClip);
        if (visuals != null)
        {
            visuals.PlayMissileAnim(true);
            visuals.PlayMissileRecoil();
        }

        // Manejo de la secuencia de apertura del cañón
        if (missileCoroutine != null) StopCoroutine(missileCoroutine);
        missileCoroutine = StartCoroutine(MissileSequenceRoutine());
    }

    // --- RUTINAS ---

    private IEnumerator ChargeRoutine()
    {
        yield return new WaitForSeconds(startChargeDelay);

        isCharging = true;
        chargeStartTime = Time.time;
        state = WeaponState.Charging;

        // --- Configuración Visual (Sin parpadeo) ---
        if (chargeEffectPrefab && chargePoint)
        {
            if (currentChargeEffect != null) Destroy(currentChargeEffect);
            currentChargeEffect = Instantiate(chargeEffectPrefab, chargePoint);
            currentChargeEffect.transform.localPosition = Vector3.zero;
            currentChargeEffect.transform.localRotation = Quaternion.identity;
            currentChargeEffect.transform.localScale = Vector3.zero;
        }

        if (visuals != null) visuals.SetCharging(true);

        // --- LÓGICA DE AUDIO PROFESIONAL (Crossfade) ---
        if (audioSource && chargeStartClip && chargeLoopClip)
        {
            // 1. Reproducir el inicio
            audioSource.clip = chargeStartClip;
            audioSource.loop = false;
            audioSource.Play();

            // 2. Esperar casi hasta el final del clip (restando un pequeño margen de transición)
            float transitionTime = 0.1f; // 100ms de fundido cruzado
            float waitTime = chargeStartClip.length - transitionTime;

            float timer = 0;
            while (timer < waitTime)
            {
                timer += Time.deltaTime;
                if (!isCharging) yield break;
                yield return null;
            }

            // 3. TRANSICIÓN SUAVE AL LOOP
            // En lugar de Stop() y Play(), simplemente cambiamos el clip y activamos loop
            // Unity intentará mantener el buffer si el clip es compatible.
            if (isCharging)
            {
                // Pequeño truco: Bajamos el volumen un poco para suavizar el "salto" si las ondas no coinciden
                float originalVol = audioSource.volume;

                audioSource.clip = chargeLoopClip;
                audioSource.loop = true;
                audioSource.Play();

                // Opcional: Fade in rápido del loop para evitar el "pop"
                float fadeTimer = 0;
                while (fadeTimer < transitionTime)
                {
                    fadeTimer += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(originalVol * 0.8f, originalVol, fadeTimer / transitionTime);
                    yield return null;
                }
                audioSource.volume = originalVol;
            }
        }
    }
    private IEnumerator MissileSequenceRoutine()
    {
        //isMissileSequenceActive = true;

        if (visuals != null)
            visuals.SetMissileBusy(1);

        // tiempo que el misil permanece abierto
        yield return new WaitForSeconds(missileIdleTime);

        if (visuals != null)
            visuals.PlayMissileAnim(false);

        yield return new WaitForSeconds(0.3f);

        //isMissileSequenceActive = false;
        state = WeaponState.Idle;
        missileCoroutine = null;
    }

    private void InterruptMissileSequence()
    {
        if (missileCoroutine != null)
            StopCoroutine(missileCoroutine);

        if (visuals != null)
            visuals.PlayMissileAnim(false);

        //isMissileSequenceActive = false;
        state = WeaponState.Idle;
        missileCoroutine = null;
    }
    protected override void OnDisable()
    {
        base.OnDisable();

        // Llamamos a la limpieza general
        // Esto garantiza que si el script se apaga por CUALQUIER motivo, 
        // el arma se limpie sola.
        OnDeselected();
    }
    // Método público para que el WeaponSystem pueda apagar el humo al cambiar de arma
    public override void OnDeselected()
    {
        state = WeaponState.Idle;

        // 1. Limpieza de efectos de calor
        if (heatVaporParticles != null)
        {
            heatVaporParticles.Stop();
            heatVaporParticles.Clear();
        }
        currentHeatLevel = 0;
        isVaporActive = false;

        // 2. Limpieza de secuencias activas
        StopChargeSequence();
        InterruptMissileSequence();

        if (visuals != null)
            visuals.SetMissileBusy(0);
        // --- 3. RESETEO DE SEGURIDAD (AQUÍ ESTÁ EL TRUCO) ---
        // Forzamos que el arma vuelva a su estado neutral al guardarla
        //isMissileSequenceActive = false;
        canFire = true;
    }

    private void HandleHeatDissipation()
    {
        if (currentHeatLevel > 0)
        {
            // El calor baja progresivamente
            currentHeatLevel = Mathf.MoveTowards(currentHeatLevel, 0f, Time.deltaTime * heatDissipationRate);

            // Activamos o desactivamos el vapor según el nivel de calor
            if (currentHeatLevel > heatThresholdToVapor && !isVaporActive)
            {
                ActivateVapor(true);
            }
            else if (currentHeatLevel <= heatThresholdToVapor && isVaporActive)
            {
                ActivateVapor(false);
            }
        }
    }
    private void ActivateVapor(bool state)
    {
        if (heatVaporParticles == null) return;

        if (state)
        {
            // Usamos .Play() en lugar de SetActive para que el vapor desaparezca suavemente
            heatVaporParticles.Play();
        }
        else
        {
            heatVaporParticles.Stop();
        }
        isVaporActive = state;
    }

    
}





