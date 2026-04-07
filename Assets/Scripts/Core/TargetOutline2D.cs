using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TargetOutline2D : MonoBehaviour
{
    [Header("Outline Material")]
    public Material outLineShader;

    [Header("Glow")]
    public bool isGlow;

    [Header("Outline")]
    public Color outlineColor = new Color(1f, 0.5f, 0f, 1f);
    [Range(1.01f, 1.2f)]
    public float outlineScale = 1.06f;

    private SpriteRenderer main;
    private SpriteRenderer outline;

    private Material runtimeOutlineMaterial;

    void Awake()
    {
        main = GetComponent<SpriteRenderer>();

        GameObject go = new GameObject("Outline");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * outlineScale;

        outline = go.AddComponent<SpriteRenderer>();

        // Même sprite que l'objet principal
        outline.sprite = main.sprite;
        outline.color = outlineColor;

        // Même layer, mais derrière
        outline.sortingLayerID = main.sortingLayerID;
        outline.sortingOrder = main.sortingOrder - 1;

        // Important : instance du material pour éviter de modifier l'asset global
        if (outLineShader != null)
        {
            runtimeOutlineMaterial = new Material(outLineShader);
            outline.material = runtimeOutlineMaterial;
        }

        outline.enabled = false;
    }

    public void SetOutlined(bool value)
    {
        if (outline == null)
            return;

        if (runtimeOutlineMaterial != null)
        {
            runtimeOutlineMaterial.SetFloat("_isGlow", isGlow ? 1f : 0f);
            runtimeOutlineMaterial.SetColor("_Color", outlineColor);
        }

        outline.enabled = value;
    }

    void LateUpdate()
    {
        if (outline == null || main == null)
            return;

        // garde le sprite synchro
        if (outline.sprite != main.sprite)
            outline.sprite = main.sprite;

        // utile si ton sprite change de flip
        outline.flipX = main.flipX;
        outline.flipY = main.flipY;

        // utile si tu modifies le tri en runtime
        outline.sortingLayerID = main.sortingLayerID;
        outline.sortingOrder = main.sortingOrder - 1;
    }

    private void OnDestroy()
    {
        if (runtimeOutlineMaterial != null)
        {
            Destroy(runtimeOutlineMaterial);
        }
    }
}