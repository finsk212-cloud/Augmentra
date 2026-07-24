using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Stats")]
    public float attackDamage = 10f;
    public float abilityPower = 0f;
    public float movementSpeed = 7f;
    public float attackSpeed = 0.25f;

    [Header("Defensive Stats")]
    public float healthRegen = 0f;
    public float maxMana = 100f;
    public float manaRegen = 0f;
    public float armor = 0f;
    public float magicDefense = 0f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 14f;
    public int projectileCount = 1;
    public float projectileSpawnDistance = 1.2f;
    public float projectileSpawnHeight = 0.5f;
    public float projectileSpreadAngle = 12f;
    public float echoChance = 0f;
    public float misfireChance = 0f;

    [Header("Aiming")]
    public Camera playerCamera;

    [Header("Curses / Effects")]
    public bool jinxed = false;
    public bool bloodstoneActive = false;
    public float bloodstoneDamageBonus = 0f;
    public float bloodstoneTimer = 0f;

    private const float BarWidth = 1.4f;
    private const float BarHeight = 0.12f;
    private const float BarHeightAbove = 2.2f;

    private static Material backgroundMaterial;
    private static Material fillMaterial;

    private Rigidbody rb;
    private Vector3 moveInput;
    private float nextFireTime;
    private bool cameraWarningShown;
    private float currentMana;
    private float regenTimer;

    private Health health;
    private Transform barRoot;
    private Transform barFill;
    private Transform cameraTransform;
    private float displayedFraction = 1f;

    public float Mana
    {
        get { return currentMana; }
    }

    public float EffectiveAttackDamage
    {
        get
        {
            if (bloodstoneActive && bloodstoneTimer > 0f)
            {
                return attackDamage + bloodstoneDamageBonus;
            }

            return attackDamage;
        }
    }

    private float EffectiveFireInterval
    {
        get { return attackSpeed; }
    }

    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        currentMana = maxMana;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
        }

        WarnIfCameraMissing();
        CreateHealthBar();
    }

    private void Update()
    {
        UpdateHealthBar();
        UpdateBloodstone();
        UpdateRegen();

        bool shopOpen = ShopManager.Instance != null && ShopManager.Instance.IsShopOpen;
        bool choosing = GameManager.Instance != null && GameManager.Instance.IsChoosingUpgrade;

        if (shopOpen || choosing)
        {
            moveInput = Vector3.zero;
            return;
        }

        HandleMovementInput();

        if (playerCamera == null)
        {
            WarnIfCameraMissing();
            return;
        }

        HandleAiming();
        HandleShooting();
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void AddMaxMana(float amount)
    {
        maxMana += amount;
        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }

    private void UpdateRegen()
    {
        regenTimer += Time.deltaTime;

        if (regenTimer >= 1f)
        {
            regenTimer -= 1f;

            if (health != null && healthRegen > 0f)
            {
                health.Heal(healthRegen);
            }

            if (manaRegen > 0f)
            {
                currentMana = Mathf.Min(maxMana, currentMana + manaRegen);
            }
        }
    }

    private void UpdateBloodstone()
    {
        if (bloodstoneTimer > 0f)
        {
            bloodstoneTimer -= Time.deltaTime;

            if (bloodstoneTimer <= 0f)
            {
                bloodstoneTimer = 0f;
                bloodstoneDamageBonus = 0f;
            }
        }
    }

    private void HandleMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(x, 0f, z).normalized;
    }

    private void Move()
    {
        Vector3 targetVelocity = moveInput * movementSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    private void HandleAiming()
    {
        Vector3 aimPoint = GetMouseWorldPosition();
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.forward = direction.normalized;
        }
    }

    private void HandleShooting()
    {
        if (!Input.GetMouseButton(0)) return;
        if (Time.time < nextFireTime) return;

        Shoot();
        nextFireTime = Time.time + EffectiveFireInterval;
    }

    private void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned on PlayerController.");
            return;
        }

        FireVolley();

        if (echoChance > 0f && Random.value < echoChance)
        {
            FireVolley();
        }
    }

    private void FireVolley()
    {
        int count = Mathf.Max(1, projectileCount);

        for (int i = 0; i < count; i++)
        {
            if (misfireChance > 0f && Random.value < misfireChance) continue;

            float angle = 0f;

            if (count > 1)
            {
                angle = (i - (count - 1) * 0.5f) * projectileSpreadAngle;
            }

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            Vector3 spawnPosition =
                transform.position +
                direction * projectileSpawnDistance +
                Vector3.up * projectileSpawnHeight;

            GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObject.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.Setup(direction, projectileSpeed, EffectiveAttackDamage);
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position + transform.forward;
    }

    private void CreateHealthBar()
    {
        barRoot = new GameObject("HealthBar").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = new Vector3(0f, BarHeightAbove, 0f);

        Transform background = CreateBarQuad("HealthBarBackground", GetSharedMaterial(ref backgroundMaterial, ParseColor("#1A1A1A")), 0f);
        background.localScale = new Vector3(BarWidth, BarHeight, 1f);

        barFill = CreateBarQuad("HealthBarFill", GetSharedMaterial(ref fillMaterial, ParseColor("#22CC22")), -0.01f);
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
            Camera resolved = playerCamera != null ? playerCamera : Camera.main;

            if (resolved == null)
            {
                resolved = Object.FindFirstObjectByType<Camera>();
            }

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

        displayedFraction = target;

        barFill.localScale = new Vector3(BarWidth * displayedFraction, BarHeight, 1f);
        barFill.localPosition = new Vector3(-BarWidth * 0.5f * (1f - displayedFraction), 0f, barFill.localPosition.z);
    }

    private void WarnIfCameraMissing()
    {
        if (playerCamera != null || cameraWarningShown) return;

        Debug.LogWarning(
            "PlayerController could not find a camera. Assign Player Camera in the Inspector " +
            "or tag your camera as MainCamera. Aiming and shooting are disabled.",
            this
        );

        cameraWarningShown = true;
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
