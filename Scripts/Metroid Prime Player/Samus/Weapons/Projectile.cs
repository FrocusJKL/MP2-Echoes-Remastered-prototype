using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 70f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private LayerMask hitMask;

    [Header("Combat Settings")]
    [SerializeField] private float damage = 20f;

    [Header("Visual & Audio Effects")]
    [SerializeField] private GameObject impactEffectPrefab;
    [Tooltip("If true, the impactTexture will be used instead of instantiating a prefab.")]
    [SerializeField] private bool useTextureAsImpact = false;
    [Tooltip("Texture to project onto a quad at the impact point when using texture-based impact.")]
    [SerializeField] private Texture2D impactTexture;
    [Tooltip("Size of the spawned quad when using a texture as impact.")]
    [SerializeField] private float impactTextureSize = 0.5f;
    [Tooltip("How long the spawned texture quad will remain in the world.")]
    [SerializeField] private float impactTextureDuration = 5f;

    [SerializeField] private AudioClip impactClip;
    [Range(0f, 1f)][SerializeField] private float impactVolume = 1f;

    private Vector3 _lastPosition;

    private void Start()
    {
        _lastPosition = transform.position;

        // El proyectil se autodestruye si no golpea nada para limpiar la memoria
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        MoveAndCheckCollision();
    }

    private void MoveAndCheckCollision()
    {
        float distanceThisFrame = speed * Time.deltaTime;
        Vector3 direction = transform.forward;

        // Usamos un Raycast continuo para evitar que el proyectil atraviese objetos (Tunnelling)
        if (Physics.Raycast(_lastPosition, direction, out RaycastHit hit, distanceThisFrame, hitMask))
        {
            HandleImpact(hit);
        }
        else
        {
            // Si no hay impacto, actualizamos posición normalmente
            transform.position += direction * distanceThisFrame;
            _lastPosition = transform.position;
        }
    }

    private void HandleImpact(RaycastHit hit)
    {
        // 1. Aplicar daño si el objeto es "dañable"
        if (hit.collider.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(damage);
        }

        // 2. Efectos Visuales (VFX)
        if (!useTextureAsImpact)
        {
            if (impactEffectPrefab != null)
            {
                // Instanciamos el efecto rotado según la cara de la pared golpeada (hit.normal)
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            if (impactTexture != null)
            {
                // Crear un quad en el punto de impacto y aplicar la textura
                GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.transform.position = hit.point + hit.normal * 0.01f; // pequeño offset para evitar z-fighting
                decal.transform.rotation = Quaternion.LookRotation(hit.normal);
                decal.transform.localScale = Vector3.one * impactTextureSize;

                // Aplicar textura a un material Unlit para que no dependa de la iluminación de la escena
                var renderer = decal.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = impactTexture;
                renderer.material = mat; // renderer.material crea una instancia del material

                // Quitar collider (no necesario para un decal)
                var col = decal.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Destruir el decal después de un tiempo
                Destroy(decal, impactTextureDuration);
            }
        }
        // --- NUEVA LÓGICA PARA LA PUERTA ---
        // Buscamos el script PrimeDoor en el objeto golpeado o en su padre
        PrimeDoor puerta = hit.collider.GetComponentInParent<PrimeDoor>();
        if (puerta != null)
        {
            puerta.RecibirDisparo();
        }
        // 3. Efectos de Sonido (SFX)
        if (impactClip != null)
        {
            AudioSource.PlayClipAtPoint(impactClip, hit.point, impactVolume);
        }

        // 4. Destruir el proyectil inmediatamente al impactar
        Destroy(gameObject);
    }
}