using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class GoldPopup : MonoBehaviour
{
    public float lifetime = 1f;
    public float riseHeight = 1f;
    public float heightAbovePlayer = 2f;
    public Color goldColor = new Color(1f, 0.84f, 0.1f);

    private TextMeshPro text;
    private Transform player;
    private Camera cam;
    private float timer;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        text.text = "+1";
        text.color = goldColor;

        cam = Camera.main;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        float t = Mathf.Clamp01(timer / lifetime);
        float rise = Mathf.Sin(t * Mathf.PI) * riseHeight;

        if (player != null)
        {
            transform.position = player.position + Vector3.up * (heightAbovePlayer + rise);
        }

        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }

        Color c = text.color;
        c.a = Mathf.Lerp(1f, 0f, t);
        text.color = c;

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
