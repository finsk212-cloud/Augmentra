using UnityEngine;

public class ShieldVisual : MonoBehaviour
{
    [SerializeField] private float bubbleRadius = 1.2f;
    [SerializeField] private Color shieldColor = new Color(0.7f, 0.85f, 1f, 0.35f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Health health;
    private GameObject bubble;
    private MeshRenderer bubbleRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        health = GetComponent<Health>();
        propertyBlock = new MaterialPropertyBlock();
        CreateBubble();
    }

    private void CreateBubble()
    {
        bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bubble.name = "ShieldBubble";
        bubble.transform.SetParent(transform, false);
        bubble.transform.localScale = Vector3.one * bubbleRadius;

        Collider col = bubble.GetComponent<Collider>();

        if (col != null)
        {
            Destroy(col);
        }

        bubbleRenderer = bubble.GetComponent<MeshRenderer>();
        bubbleRenderer.material = CreateTransparentMaterial();
        bubbleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bubbleRenderer.receiveShadows = false;

        bubble.SetActive(false);
    }

    private Material CreateTransparentMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);

        if (shader.name.Contains("Universal Render Pipeline"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        material.SetColor(BaseColorId, shieldColor);
        return material;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.ShieldChanged += HandleShieldChanged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.ShieldChanged -= HandleShieldChanged;
        }
    }

    private void HandleShieldChanged(float current, float maximum)
    {
        if (bubble == null) return;

        bool hasShield = current > 0f;
        bubble.SetActive(hasShield);

        if (!hasShield || maximum <= 0f) return;

        float ratio = Mathf.Clamp01(current / maximum);
        bubble.transform.localScale = Vector3.one * bubbleRadius * (0.7f + ratio * 0.3f);

        Color fadedColor = shieldColor;
        fadedColor.a = shieldColor.a * (0.5f + ratio * 0.5f);
        bubbleRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, fadedColor);
        bubbleRenderer.SetPropertyBlock(propertyBlock);
    }
}
