using UnityEngine;

public class BossWorldHealthBar : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BossRootHealth bossHealth;

    [Header("Références")]
    [Tooltip("Objet parent de toute la barre de vie")]
    [SerializeField] private Transform barRoot;

    [Tooltip("Sprite qui représente la vie")]
    [SerializeField] private Transform fillSprite;

    [Tooltip("Optionnel : sprite de fond")]
    [SerializeField] private SpriteRenderer backgroundSprite;

    [Tooltip("Optionnel : sprite de contour / frame")]
    [SerializeField] private SpriteRenderer frameSprite;

    [Header("Position")]
    [SerializeField] private Transform targetToFollow;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool followTarget = false;

    [Header("Comportement")]
    [SerializeField] private bool hideWhenBossMissing = true;
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private bool hideOnDeath = true;

    [Header("Animation")]
    [SerializeField] private bool smoothBar = true;
    [SerializeField] private float smoothSpeed = 8f;

    private float targetFill = 1f;
    private float currentFill = 1f;

    private Vector3 initialFillScale;

    private void Awake()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<BossRootHealth>();

        if (targetToFollow == null)
            targetToFollow = transform;

        if (fillSprite != null)
        {
            initialFillScale = fillSprite.localScale;
        }
        else
        {
            Debug.LogWarning("[BossWorldHealthBar] Fill Sprite non assigné.", this);
        }
    }

    private void Start()
    {
        RefreshBar();
    }

    private void LateUpdate()
    {
        if (bossHealth == null)
        {
            if (hideWhenBossMissing && barRoot != null)
                barRoot.gameObject.SetActive(false);

            return;
        }

        if (followTarget && barRoot != null && targetToFollow != null)
        {
            barRoot.position = targetToFollow.position + offset;
        }

        UpdateHealthBar(
            bossHealth.CurrentHealth,
            bossHealth.MaxHealth
        );

        if (smoothBar)
        {
            currentFill = Mathf.Lerp(
                currentFill,
                targetFill,
                Time.deltaTime * smoothSpeed
            );

            ApplyFill(currentFill);
        }
    }

    private void RefreshBar()
    {
        if (bossHealth == null)
            return;

        UpdateHealthBar(
            bossHealth.CurrentHealth,
            bossHealth.MaxHealth
        );

        currentFill = targetFill;
        ApplyFill(currentFill);
    }

    private void UpdateHealthBar(int currentHP, int maxHP)
    {
        if (maxHP <= 0)
            return;

        targetFill = Mathf.Clamp01((float)currentHP / maxHP);

        if (!smoothBar)
        {
            currentFill = targetFill;
            ApplyFill(currentFill);
        }

        UpdateVisibility(currentHP, maxHP);
    }

    private void ApplyFill(float fillAmount)
    {
        if (fillSprite == null)
            return;

        float clampedFill = Mathf.Clamp01(fillAmount);

        Vector3 newScale = initialFillScale;
        newScale.x = initialFillScale.x * clampedFill;

        fillSprite.localScale = newScale;
    }

    private void UpdateVisibility(int currentHP, int maxHP)
    {
        if (barRoot == null)
            return;

        bool shouldShow = true;

        if (bossHealth == null && hideWhenBossMissing)
            shouldShow = false;

        if (hideWhenFull && currentHP >= maxHP)
            shouldShow = false;

        if (hideOnDeath && currentHP <= 0)
            shouldShow = false;

        barRoot.gameObject.SetActive(shouldShow);
    }

    public void SetBoss(BossRootHealth newBossHealth)
    {
        bossHealth = newBossHealth;

        if (bossHealth != null && targetToFollow == null)
            targetToFollow = bossHealth.transform;

        RefreshBar();
    }
}