using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TargetOutline2D : MonoBehaviour
{

    public Material OutLineShader;
    public bool isGlow;
    [Header("Outline")]
    public Color outlineColor = new Color(1f, 0.5f, 0f, 1f); // orange
    [Range(1.01f, 1.2f)]
    public float outlineScale = 1.06f;

    SpriteRenderer main;
    SpriteRenderer outline;

    void Awake()
    {
        main = GetComponent<SpriteRenderer>();

        GameObject go = new GameObject("Outline");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * outlineScale;

        outline = go.AddComponent<SpriteRenderer>();
        outline.sprite = main.sprite;
        outline.color = outlineColor;

        outline.sortingLayerID = main.sortingLayerID;
        outline.sortingOrder = main.sortingOrder - 1;

        outline.enabled = false;
    }

    public void SetOutlined(bool value)
    {
        OutLineShader.SetFloat("_isGlow", isGlow ? 0f : 1f);
        if (outline != null)
            outline.enabled = value;
    }

    void LateUpdate()
    {
        // garde le sprite synchro (animations / changements)
        if (outline.sprite != main.sprite)
            outline.sprite = main.sprite;
    }
}
