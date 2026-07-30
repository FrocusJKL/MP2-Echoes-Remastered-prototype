using UnityEngine;

public class WeaponCollisionHandler : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private float detectionDistance = 3f;
    [SerializeField] private float sphereRadius = 0f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Movement Settings")]
    [SerializeField] private Vector3 retractionOffset = new Vector3(0, -0.1f, -0.8f);
    [SerializeField] private Vector3 retractionRotation = new Vector3(2.3f, -38.5f, 0.24f);
    [SerializeField] private float smooth = 3f;

    float currentProximity;

    public Vector3 CollisionOffset { get; private set; }
    public Quaternion CollisionRotation { get; private set; }

    void Start()
    {
        if (raycastOrigin == null)
            raycastOrigin = Camera.main.transform;
        // Ejecutamos el chequeo una vez inmediatamente para 
        // que CollisionOffset no sea Vector3.zero por error al inicio
        CheckCollision();
    }

   
    void LateUpdate()
    {
        CheckCollision();
    }
    void CheckCollision()
    {
        RaycastHit hit;

        bool detected = Physics.SphereCast(
            raycastOrigin.position,
            sphereRadius,
            raycastOrigin.forward,
            out hit,
            detectionDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        float targetProximity = 0f;

        if (detected)
        {
            float raw = 1f - (hit.distance / detectionDistance);
            targetProximity = Mathf.SmoothStep(0f, 1f, raw);
        }

        currentProximity = Mathf.Lerp(
            currentProximity,
            targetProximity,
            Time.deltaTime * smooth
        );

        CollisionOffset = retractionOffset * currentProximity;
        CollisionRotation = Quaternion.Euler(retractionRotation * currentProximity);
    }
}