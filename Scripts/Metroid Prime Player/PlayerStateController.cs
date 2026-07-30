using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerStateController : MonoBehaviour
{
    //private PlayerInputReader input;
    //private PlayerInput playerInput;

    //[Header("Visuals/GameObjects")]
    //[SerializeField] private GameObject playerModel;      // El GameObject "Samus"
    //[SerializeField] private GameObject morphBallObject;  // El GameObject "MorphBall"

    //[Header("Colliders")]
    //[SerializeField] private CharacterController playerCollider;
    //[SerializeField] private SphereCollider morphBallCollider;

    //[Header("Controllers")]
    //[SerializeField] private MonoBehaviour playerController;    // Script de control de Samus
    //[SerializeField] private MonoBehaviour morphBallController; // MorphBallController

    //[Header("Settings")]
    //[SerializeField] private float unmorphRadius = 0.5f;
    //[SerializeField] private float morphHeightOffset = 1.8f;
    //[SerializeField] private LayerMask groundLayer;
    //[SerializeField] private float groundCheckDistance = 2f;

    //private bool isMorphBall;
    //private Rigidbody rb;
    //private Transform originalParent;

    //void Awake()
    //{
    //    // Buscar componentes en el root
    //    input = GetComponent<PlayerInputReader>();
    //    playerInput = GetComponent<PlayerInput>();

    //    // Verificar que tenemos todas las referencias
    //    ValidateReferences();
    //}

    //void ValidateReferences()
    //{
    //    if (input == null) Debug.LogError("PlayerInputReader no encontrado!");
    //    if (playerInput == null) Debug.LogError("PlayerInput no encontrado!");

    //    // Buscar MorphBallObject si no está asignado
    //    if (morphBallObject == null)
    //        morphBallObject = GameObject.Find("MorphBall");

    //    if (morphBallObject != null)
    //    {
    //        rb = morphBallObject.GetComponent<Rigidbody>();
    //        if (morphBallCollider == null)
    //            morphBallCollider = morphBallObject.GetComponent<SphereCollider>();
    //    }

    //    // Guardar el padre original
    //    originalParent = transform.parent;
    //}

    //void Start()
    //{
    //    isMorphBall = false;
    //    ToggleState(false);
    //}

    //void OnEnable()
    //{
    //    if (input != null)
    //    {
    //        input.MorphBallPressed -= OnMorphBallInput;
    //        input.MorphBallPressed += OnMorphBallInput;
    //    }
    //}

    //void OnDisable()
    //{
    //    if (input != null) input.MorphBallPressed -= OnMorphBallInput;
    //}

    //void OnMorphBallInput()
    //{
    //    Debug.Log($"Input recibido. ¿Es bola?: {isMorphBall}");

    //    if (isMorphBall)
    //    {
    //        if (CanUnmorph())
    //            ToggleState(false);
    //        else
    //            Debug.LogWarning("No puedes salir de Morph Ball aquí (obstrucción)");
    //    }
    //    else
    //    {
    //        ToggleState(true);
    //    }
    //}

    //void ToggleState(bool toMorph)
    //{
    //    isMorphBall = toMorph;
    //    Debug.Log($"Cambiando a: {(toMorph ? "MORPH BALL" : "SAMUS")}");

    //    // Verificar que los objetos existen
    //    if (playerModel == null || morphBallObject == null)
    //    {
    //        Debug.LogError("Faltan referencias a los modelos!");
    //        return;
    //    }

    //    // 1. Activar/desactivar GameObjects
    //    playerModel.SetActive(!isMorphBall);
    //    morphBallObject.SetActive(isMorphBall);

    //    // 2. Cambiar Action Map
    //    if (playerInput != null)
    //    {
    //        string mapName = isMorphBall ? "MorphBall" : "Samus";
    //        playerInput.SwitchCurrentActionMap(mapName);
    //        Debug.Log($"Cambiando a Action Map: {mapName}");
    //    }

    //    // 3. Posicionamiento
    //    if (isMorphBall)
    //    {
    //        // MORPH: Samus -> Bola
    //        Vector3 spawnPos = playerModel.transform.position;

    //        // Ajustar al suelo
    //        if (Physics.Raycast(spawnPos + Vector3.up * groundCheckDistance,
    //            Vector3.down, out RaycastHit hit, groundCheckDistance * 2f, groundLayer))
    //        {
    //            float ballRadius = morphBallCollider != null ? morphBallCollider.radius : 0.5f;
    //            spawnPos = hit.point + Vector3.up * ballRadius;
    //        }

    //        // Posicionar la bola
    //        morphBallObject.transform.position = spawnPos;

    //        // Configurar Rigidbody
    //        if (rb != null)
    //        {
    //            rb.isKinematic = false;
    //            rb.linearVelocity = Vector3.zero;
    //            rb.angularVelocity = Vector3.zero;
    //        }
    //    }
    //    else
    //    {
    //        // UNMORPH: Bola -> Samus
    //        Vector3 spawnPos = morphBallObject.transform.position + Vector3.up * morphHeightOffset;

    //        // Posicionar Samus
    //        playerModel.transform.position = spawnPos;

    //        // Configurar Rigidbody
    //        if (rb != null)
    //        {
    //            rb.isKinematic = true;
    //        }
    //    }

    //    // 4. Activar/desactivar scripts de control
    //    if (playerController) playerController.enabled = !isMorphBall;
    //    if (morphBallController) morphBallController.enabled = isMorphBall;
    //}

    //bool CanUnmorph()
    //{
    //    if (morphBallObject == null) return false;

    //    return !Physics.CheckSphere(morphBallObject.transform.position + Vector3.up * morphHeightOffset,
    //        unmorphRadius, groundLayer);
    //}

    //public bool IsMorphBallActive()
    //{
    //    return isMorphBall;
    //}
}