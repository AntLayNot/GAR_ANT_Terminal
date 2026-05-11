using System.Collections.Generic;
using UnityEngine;

public class TargetOutline2D : MonoBehaviour
{
    [Header("Outline Material")]
    public Material outLineShader;

    [Header("Glow")]
    public bool isGlow = false;

    [Header("Outline")]
    public Color outlineColor = new Color(1f, 0.5f, 0f, 1f);

    [Range(1.0f, 1.2f)]
    public float outlineScale = 1.06f;

    [Tooltip("Inclure aussi les SpriteRenderer des enfants")]
    public bool includeChildren = true;

    private class OutlineEntry
    {
        public SpriteRenderer source;
        public GameObject cloneObject;
        public SpriteRenderer cloneRenderer;
        public Material runtimeMaterial;
        public Vector3 initialLocalPosition;
        public Quaternion initialLocalRotation;
        public Vector3 initialLocalScale;
    }

    private readonly List<OutlineEntry> entries = new();

    void Awake()
    {
        BuildOutlines();
        SetOutlined(false);
    }

    void BuildOutlines()
    {
        ClearOutlines();

        SpriteRenderer[] sources = includeChildren
            ? GetComponentsInChildren<SpriteRenderer>(true)
            : GetComponents<SpriteRenderer>();

        foreach (SpriteRenderer src in sources)
        {
            if (src == null)
                continue;

            // On évite de dupliquer un outline déjà généré
            if (src.gameObject.name.EndsWith("_OutlineClone"))
                continue;

            GameObject clone = new GameObject(src.gameObject.name + "_OutlineClone");
            clone.transform.SetParent(src.transform, false);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one * outlineScale;

            SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();

            cloneSr.sprite = src.sprite;
            cloneSr.color = outlineColor;
            cloneSr.flipX = src.flipX;
            cloneSr.flipY = src.flipY;
            cloneSr.drawMode = src.drawMode;
            cloneSr.size = src.size;
            cloneSr.sortingLayerID = src.sortingLayerID;
            cloneSr.sortingOrder = src.sortingOrder - 1;
            cloneSr.maskInteraction = src.maskInteraction;

            Material runtimeMat = null;
            if (outLineShader != null)
            {
                runtimeMat = new Material(outLineShader);
                ApplyMaterialProperties(runtimeMat);
                cloneSr.material = runtimeMat;
            }

            cloneSr.enabled = false;

            OutlineEntry entry = new OutlineEntry
            {
                source = src,
                cloneObject = clone,
                cloneRenderer = cloneSr,
                runtimeMaterial = runtimeMat,
                initialLocalPosition = clone.transform.localPosition,
                initialLocalRotation = clone.transform.localRotation,
                initialLocalScale = clone.transform.localScale
            };

            entries.Add(entry);
        }
    }

    void ApplyMaterialProperties(Material mat)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", outlineColor);

        if (mat.HasProperty("_OutlineColor"))
            mat.SetColor("_OutlineColor", outlineColor);

        if (mat.HasProperty("_isGlow"))
            mat.SetFloat("_isGlow", isGlow ? 1f : 0f);

        if (mat.HasProperty("_Glow"))
            mat.SetFloat("_Glow", isGlow ? 1f : 0f);
    }

    public void SetOutlined(bool value)
    {
        foreach (var entry in entries)
        {
            if (entry == null || entry.cloneRenderer == null || entry.source == null)
                continue;

            entry.cloneRenderer.color = outlineColor;

            if (entry.runtimeMaterial != null)
                ApplyMaterialProperties(entry.runtimeMaterial);

            entry.cloneRenderer.enabled = value;
        }
    }

    void LateUpdate()
    {
        foreach (var entry in entries)
        {
            if (entry == null || entry.source == null || entry.cloneRenderer == null)
                continue;

            SpriteRenderer src = entry.source;
            SpriteRenderer clone = entry.cloneRenderer;

            if (clone.sprite != src.sprite)
                clone.sprite = src.sprite;

            clone.flipX = src.flipX;
            clone.flipY = src.flipY;
            clone.drawMode = src.drawMode;
            clone.size = src.size;

            clone.sortingLayerID = src.sortingLayerID;
            clone.sortingOrder = src.sortingOrder - 1;

            clone.maskInteraction = src.maskInteraction;

            // On garde bien le clone centré sur sa source
            clone.transform.localPosition = entry.initialLocalPosition;
            clone.transform.localRotation = entry.initialLocalRotation;
            clone.transform.localScale = Vector3.one * outlineScale;

            if (entry.runtimeMaterial != null)
                ApplyMaterialProperties(entry.runtimeMaterial);
        }
    }

    void ClearOutlines()
    {
        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            if (entry.runtimeMaterial != null)
                Destroy(entry.runtimeMaterial);

            if (entry.cloneObject != null)
                Destroy(entry.cloneObject);
        }

        entries.Clear();
    }

    void OnDestroy()
    {
        ClearOutlines();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        foreach (var entry in entries)
        {
            if (entry == null || entry.cloneRenderer == null)
                continue;

            entry.cloneRenderer.color = outlineColor;
            entry.cloneRenderer.transform.localScale = Vector3.one * outlineScale;

            if (entry.runtimeMaterial != null)
                ApplyMaterialProperties(entry.runtimeMaterial);
        }
    }
#endif
}