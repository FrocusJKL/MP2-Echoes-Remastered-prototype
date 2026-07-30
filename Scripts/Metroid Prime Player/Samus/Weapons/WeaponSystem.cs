using System.Collections;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Weapons Inventory")]
    [SerializeField] private Weapon[] weapons;
    private int currentWeaponIndex = 0;

    [Header("Switch Weapon Settings")]
    [SerializeField] private float switchDelay = 1f;
    private bool isSwitching = false;
    [Header("Switch Weapon (Audio)")]
    [SerializeField] private AudioClip soundTransition;
    [Range(0f, 1f)][SerializeField] private float transitionVolume = 0.5f;

    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private WeaponVisuals visuals;
    private LockOnSystem lockSystem;

    private void Awake()
    {
        lockSystem = GetComponentInParent<LockOnSystem>();
    }

    private void Start()
    {
        if (weapons.Length == 0) return;

        // En lugar de desactivar el objeto, asegúrate de que todas 
        // las armas lógicas estén en estado "Stop"
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(true); // Mantenlas vivas
                weapons[i].OnRelease(); // Asegúrate de que no estén disparando
            }
        }

        currentWeaponIndex = 0;
        if (visuals != null) visuals.PrepareWeaponSwitch(0);
    }

    // --- MÉTODOS PARA EL GAMEPAD (Llamados desde PlayerInputReader) ---

    public void OnFirePressed()
    {
        if (CanAction()) weapons[currentWeaponIndex].OnPress();
    }

    public void OnFireReleased()
    {
        if (weapons[currentWeaponIndex] != null) weapons[currentWeaponIndex].OnRelease();
    }

    public void OnMissilePressed()
    {
        if (CanAction()) weapons[currentWeaponIndex].OnMissile();
    }

    private bool CanAction()
    {
        return weapons.Length > 0 && weapons[currentWeaponIndex] != null && !isSwitching;
    }

    // --- LÓGICA DE CAMBIO DE ARMA ---

    public void SelectWeapon(int index)
    {
        if (index == currentWeaponIndex || isSwitching) return;
        StartCoroutine(SwitchRoutine(index));
    }
  

    private IEnumerator SwitchRoutine(int newIndex)
    {
        isSwitching = true;

        // 1. Limpiar el arma actual (detener disparos/carga)
        if (weapons[currentWeaponIndex] != null)
        {
            //weapons[currentWeaponIndex].OnRelease(); 
            weapons[currentWeaponIndex].OnDeselected();
        }

        // 2. Avisar al Visual que inicie la transición
        // Esto pondrá el 'pendingIndex' y activará el Trigger "OnWeaponChange"
        if (visuals != null)
        {
            visuals.PrepareWeaponSwitch(newIndex);
            visuals.anim.SetTrigger("OnWeaponChange");
        }

        // 3. Sonido de inicio (opcional)
        if (audioSource && soundTransition)
            audioSource.PlayOneShot(soundTransition, transitionVolume);

        // 4. ESPERA: Aquí es donde la animación de "Switch" ocurre en el fondo
        // Asegúrate de que este tiempo coincida más o menos con tu animación
        yield return new WaitForSeconds(switchDelay);



        // Si tus scripts de Weapon (balas, daño) están en objetos separados:
        // Pero si solo usas un cañón, esto puede que no lo necesites.
        // weapons[currentWeaponIndex].gameObject.SetActive(true); 

        //isSwitching = true;

        // 5. Cambio de lógica interno
        currentWeaponIndex = newIndex;
        isSwitching = false;
    }

    public Weapon GetActiveWeapon() => weapons[currentWeaponIndex];
}



