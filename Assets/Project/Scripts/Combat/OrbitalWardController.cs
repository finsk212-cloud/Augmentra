using UnityEngine;

public class OrbitalWardController : MonoBehaviour
{
    public int orbCount = 2;
    public float orbitRadius = 2.5f;
    public float orbitSpeed = 90f;
    public float orbDamage = 5f;
    public GameObject orbPrefab;

    private Transform[] orbs;
    private float currentAngle;

    private void Start()
    {
        orbs = new Transform[orbCount];

        for (int i = 0; i < orbCount; i++)
        {
            GameObject orb = orbPrefab != null
                ? Instantiate(orbPrefab, transform)
                : CreateFallbackOrb();

            OrbitalOrb orbScript = orb.GetComponent<OrbitalOrb>();

            if (orbScript == null)
            {
                orbScript = orb.AddComponent<OrbitalOrb>();
            }

            orbScript.damage = orbDamage;

            orbs[i] = orb.transform;
        }
    }

    private GameObject CreateFallbackOrb()
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "OrbitalOrb";
        orb.transform.SetParent(transform);
        orb.transform.localScale = Vector3.one * 0.4f;

        Collider col = orb.GetComponent<Collider>();
        col.isTrigger = true;

        Rigidbody rb = orb.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        return orb;
    }

    private void Update()
    {
        if (orbs == null) return;

        currentAngle += orbitSpeed * Time.deltaTime;
        float step = 360f / Mathf.Max(1, orbs.Length);

        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null) continue;

            float angle = (currentAngle + step * i) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
            orbs[i].position = transform.position + offset;
        }
    }
}
