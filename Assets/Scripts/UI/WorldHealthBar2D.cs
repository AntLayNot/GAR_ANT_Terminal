using UnityEngine;

[RequireComponent(typeof(Health2D))]
public class WorldHealthBar2D : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Health2D health;

    [Tooltip("Objet parent de toute la barre de vie")]
    [SerializeField] private Transform barRoot;

    [Tooltip("Sprite qui représente la vie")]
    [SerializeField] private Transform fillSprite;

    [Tooltip("Optionnel : sprite de fond")]
    [SerializeField] private SpriteRenderer backgroundSprite;

    [Tooltip("Optionnel : sprite de contour / frame")]
    [SerializeField] private SpriteRenderer frameSprite;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private bool followTarget = true;

    [Header("Comportement")]
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
        if (health == null)
            health = GetComponent<Health2D>();

        if (fillSprite != null)
        {
            initialFillScale = fillSprite.localScale;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.onHPChanged.AddListener(UpdateHealthBar);
            health.onDeath.AddListener(OnDeath);
        }

        RefreshBar();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onHPChanged.RemoveListener(UpdateHealthBar);
            health.onDeath.RemoveListener(OnDeath);
        }
    }

    private void LateUpdate()
    {
        if (followTarget && barRoot != null)
        {
            barRoot.position = transform.position + offset;
        }

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
        if (health == null)
            return;

        UpdateHealthBar(health.currentHP, health.maxHP);
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

        if (hideWhenFull && currentHP >= maxHP)
            shouldShow = false;

        if (hideOnDeath && currentHP <= 0)
            shouldShow = false;

        barRoot.gameObject.SetActive(shouldShow);
    }

    private void OnDeath()
    {
        targetFill = 0f;
        currentFill = 0f;

        ApplyFill(0f);

        if (hideOnDeath && barRoot != null)
            barRoot.gameObject.SetActive(false);
    }
}