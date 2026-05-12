using UnityEngine;

public class BossWorldHealthBar : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BossRootHealth bossHealth;

    [Header("Bar")]
    [SerializeField] private Transform fillTransform;
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Settings")]
    [SerializeField] private bool hideWhenBossMissing = true;
    [SerializeField] private bool hideWhenFullLife = false;
    [SerializeField] private bool hideWhenDead = false;
    [SerializeField] private float smoothSpeed = 6f;

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = new Color(0.8f, 0.15f, 0.25f, 1f);
    [SerializeField] private Color midHealthColor = new Color(0.95f, 0.65f, 0.2f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(0.6f, 0.9f, 1f, 1f);

    private float currentDisplay = 1f;

    private void Awake()
    {
        if (fillTransform == null)
            Debug.LogWarning("[BossWorldHealthBar] Fill Transform non assigné.", this);
    }

    private void Start()
    {
        if (bossHealth != null)
            currentDisplay = bossHealth.CurrentHealthNormalized;

        ApplyBar(currentDisplay, true);
        UpdateVisibility(currentDisplay);
    }

    private void Update()
    {
        if (bossHealth == null)
        {
            if (hideWhenBossMissing)
                gameObject.SetActive(false);

            return;
        }

        float target = bossHealth.CurrentHealthNormalized;
        currentDisplay = Mathf.Lerp(currentDisplay, target, Time.deltaTime * smoothSpeed);

        if (Mathf.Abs(currentDisplay - target) < 0.001f)
            currentDisplay = target;

        ApplyBar(currentDisplay, false);
        UpdateVisibility(target);
    }

    private void ApplyBar(float normalized, bool instant)
    {
        normalized = Mathf.Clamp01(normalized);

        if (fillTransform != null)
        {
            Vector3 scale = fillTransform.localScale;
            scale.x = normalized;
            fillTransform.localScale = scale;
        }

        if (fillRenderer != null)
        {
            if (normalized > 0.5f)
            {
                float t = Mathf.InverseLerp(0.5f, 1f, normalized);
                fillRenderer.color = Color.Lerp(midHealthColor, fullHealthColor, t);
            }
            else
            {
                float t = Mathf.InverseLerp(0f, 0.5f, normalized);
                fillRenderer.color = Color.Lerp(lowHealthColor, midHealthColor, t);
            }
        }
    }

    private void UpdateVisibility(float normalized)
    {
        if (hideWhenDead && normalized <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        if (hideWhenFullLife && normalized >= 0.999f)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void SetBoss(BossRootHealth newBossHealth)
    {
        bossHealth = newBossHealth;

        if (bossHealth != null)
        {
            currentDisplay = bossHealth.CurrentHealthNormalized;
            ApplyBar(currentDisplay, true);
            UpdateVisibility(currentDisplay);
        }
    }
}