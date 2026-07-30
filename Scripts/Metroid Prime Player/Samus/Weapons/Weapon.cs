using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public abstract class Weapon : MonoBehaviour
{
    [Header("Base Weapon Components")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected WeaponVisuals visuals;

    // Propiedades protegidas: las clases hijas (PowerBeam, IceBeam) pueden verlas, 
    // pero otros scripts externos no pueden modificarlas.
    protected float LastShotTime { get; set; }

    // Indica si el arma está actualmente en su secuencia de disparo o carga
    protected bool IsBusy { get; set; }

    protected virtual void Awake()
    {
        // Validación y auto-asignación para evitar errores de "Missing Reference"
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (visuals == null) visuals = GetComponent<WeaponVisuals>();
    }

    // --- MÉTODOS DE DISPARO PRINCIPAL ---
    // 'abstract' obliga a que cada arma implemente su propia lógica obligatoriamente.
    public abstract void OnPress();
    public abstract void OnHold();
    public abstract void OnRelease();
    protected virtual void OnDisable()
    {
        // Por ahora vacío, pero permite que los hijos lo usen
    }
    // --- MÉTODOS ESPECIALES ---
    // 'virtual' permite que sea opcional. 
    // Si un arma no tiene misiles, simplemente no sobrescribe este método.
    public virtual void OnMissile()
    {
        // Log general opcional (ej. "Este arma no soporta misiles")
    }
    // --- UTILIDADES COMPARTIDAS ---
    // Un método profesional para verificar el tiempo entre disparos (Cadencia)
    protected bool CanFire(float fireRate)
    {
        return Time.time >= LastShotTime + fireRate && !IsBusy;
    }
    public virtual void OnDeselected()
    {
        // Vacío por defecto, las armas hijas lo sobrescribirán si lo necesitan
    }
}