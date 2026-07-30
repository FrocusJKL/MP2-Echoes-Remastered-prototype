using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;

public class PlayerInputReader : MonoBehaviour
{
    [Header("References")]
    private WeaponSystem weaponSystem;
    private LockOnSystem lockOnSystem;

    // Propiedades para valores continuos (Sticks / Mouse)
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }


    // Propiedades de estado
    public bool JumpPressed { get; private set; }

    private void Awake()
    {
        // Búsqueda automática de componentes si no están asignados
        if (weaponSystem == null) weaponSystem = GetComponentInChildren<WeaponSystem>();
        if (lockOnSystem == null) lockOnSystem = GetComponentInChildren<LockOnSystem>();
    }
    public void SetRumble(float lowFreq, float highFreq)
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(lowFreq, highFreq);
        }
    }

    public void StopRumble()
    {
        Gamepad.current?.SetMotorSpeeds(0, 0);
    }
    private void OnDisable() => StopRumble();
    // --- MÉTODOS DE MOVIMIENTO ---

    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Look = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) JumpPressed = true;
        else if (context.canceled) JumpPressed = false;
    }

    // --- MÉTODOS DE COMBATE ---

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (weaponSystem == null) return;

        // Llamamos a los métodos del WeaponSystem (Estructura delegada)
        if (context.performed)
            weaponSystem.OnFirePressed();
        else if (context.canceled)
            weaponSystem.OnFireReleased();
    }

    public void OnMissiles(InputAction.CallbackContext context)
    {
        if (context.performed && weaponSystem != null)
        {
            weaponSystem.OnMissilePressed();
        }
    }

    public void OnLockOn(InputAction.CallbackContext context)
    {
        if (lockOnSystem == null) return;

        if (context.performed)
            lockOnSystem.OnLockPressed();
        else if (context.canceled)
            lockOnSystem.OnLockReleased();
    }


    // --- CAMBIO DE ARMAS (D-PAD / TECLADO) ---
    // Usamos SelectWeapon para coincidir con el WeaponSystem profesional

    public void OnWeaponUp(InputAction.CallbackContext context)
    {
        if (context.performed) weaponSystem?.SelectWeapon(0);
    }

    public void OnWeaponRight(InputAction.CallbackContext context)
    {
        if (context.performed) weaponSystem?.SelectWeapon(1);
    }

    public void OnWeaponDown(InputAction.CallbackContext context)
    {
        if (context.performed) weaponSystem?.SelectWeapon(2);
    }

    public void OnWeaponLeft(InputAction.CallbackContext context)
    {
        if (context.performed) weaponSystem?.SelectWeapon(3);
    }


    // Morph Ball 

    
}