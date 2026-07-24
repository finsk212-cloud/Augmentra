using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    public static float SpeedMultiplier = 1f;

    public float moveSpeed = 3f;
    public int touchDamage = 1;
    public float damageRate = 0.75f;
    public float fallThreshold = -5f;

    private const float BarWidth = 1.0f;
    private const float BarHeight = 0.12f;
    private const float BarHeightAbove = 1.5f;
    private const float BarFillSpeed = 3f;

    private static Material backgroundMaterial;
    private static Material fillMaterial;

    private Transform player;
    private Rigidbody rb;
    private float nextDamageTime;

    private Health health;
    private Transform barRoot;
    private Transform barFill;
    private Transform cameraTransform;
    private float displayedFraction = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        CreateHealthBar();
    }

    private void FixedUpdate()
    {
        if (transform.position.y < fallThreshold)
        {
            Despawn();
            return;
        }

        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        float speed = moveSpeed * SpeedMultiplier;

        rb.linearVelocity = new Vector3(
            direction.x * speed,
            rb.linearVelocity.y,
            direction.z * speed
        );

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.forward = direction;
        }
    }

    private void Update()
    {
        UpdateHealthBar();
    }

    private void CreateHealthBar()
    {
        barRoot = new GameObject("HealthBar").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = new Vector3(0f, BarHeightAbove, 0f);

        Transform background = CreateBarQuad("HealthBarBackground", GetSharedMaterial(ref backgroundMaterial, ParseColor("#1A1A1A")), 0f);
        background.localScale = new Vector3(BarWidth, BarHeight, 1f);

        barFill = CreateBarQuad("HealthBarFill", GetSharedMaterial(ref fillMaterial, ParseColor("#CC2200")), -0.01f);
        barFill.localScale = new Vector3(BarWidth, BarHeight, 1f);
    }

    private Transform CreateBarQuad(string quadName, Material material, float zOffset)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = quadName;

        Collider quadCollider = quad.GetComponent<Collider>();

        if (quadCollider != null)
        {
            Destroy(quadCollider);
        }

        quad.transform.SetParent(barRoot, false);
        quad.transform.localPosition = new Vector3(0f, 0f, zOffset);
        quad.transform.localRotation = Quaternion.identity;

        MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sharedMaterial = material;

        return quad.transform;
    }

    private void UpdateHealthBar()
    {
        if (barRoot == null)
        {
            return;
        }

        if (cameraTransform == null)
        {
            Camera resolved = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

            if (resolved != null)
            {
                cameraTransform = resolved.transform;
            }
        }

        if (cameraTransform != null)
        {
            barRoot.rotation = cameraTransform.rotation;
        }

        float target = 1f;

        if (health != null && health.maxHealth > 0f)
        {
            target = Mathf.Clamp01(health.CurrentHealth / health.maxHealth);
        }

        displayedFraction = Mathf.MoveTowards(displayedFraction, target, BarFillSpeed * Time.deltaTime);

        barFill.localScale = new Vector3(BarWidth * displayedFraction, BarHeight, 1f);
        barFill.localPosition = new Vector3(-BarWidth * 0.5f * (1f - displayedFraction), 0f, barFill.localPosition.z);
    }

    private void Despawn()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.EnemyDied();
        }

        Destroy(gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (Time.time < nextDamageTime) return;

        Health targetHealth = collision.gameObject.GetComponent<Health>();

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(touchDamage);
            nextDamageTime = Time.time + damageRate;
        }
    }

    private static Material GetSharedMaterial(ref Material cache, Color color)
    {
        if (cache == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            cache = new Material(shader);
            cache.color = color;

            if (cache.HasProperty("_BaseColor"))
            {
                cache.SetColor("_BaseColor", color);
            }

            if (cache.HasProperty("_Cull"))
            {
                cache.SetFloat("_Cull", 0f);
            }
        }

        return cache;
    }

    private static Color ParseColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
