using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthHUDController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Health2D targetHealth;
    [SerializeField] private PlayerSaveCharges targetSaveCharges;

    [Header("UI")]
    [SerializeField] private Image healthFlaskImage;
    [SerializeField] private TMP_Text lifeAmountText;
    [SerializeField] private TMP_Text lifeHistoryText;

    [Header("Flask Sprites (du plein vers le vide)")]
    [SerializeField] private Sprite[] healthStates;

    [Header("Options")]
    [SerializeField] private bool autoFindPlayerHealth = true;
    [SerializeField] private bool autoFindPlayerSaveCharges = true;
    [SerializeField] private bool showCriticalMessage = true;
    [SerializeField] private int criticalThreshold = 1;

    [Header("History Display")]
    [SerializeField] private float temporaryMessageDuration = 1.5f;
    [SerializeField] private string savesLabel = "SAVES : ";

    private int lastHP = -1;
    private int lastMaxHP = -1;
    private int currentSaves = 0;

    private Coroutine historyRoutine;

    private void Start()
    {
        if (targetHealth == null && autoFindPlayerHealth)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetHealth = player.GetComponent<Health2D>();
                if (targetHealth == null)
                    targetHealth = player.GetComponentInParent<Health2D>();
            }
        }

        if (targetSaveCharges == null && autoFindPlayerSaveCharges)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetSaveCharges = player.GetComponent<PlayerSaveCharges>();
                if (targetSaveCharges == null)
                    targetSaveCharges = player.GetComponentInParent<PlayerSaveCharges>();
            }
        }

        BindToHealth();
        BindToSaveCharges();
        ForceRefresh();
    }

    private void OnDestroy()
    {
        UnbindFromHealth();
        UnbindFromSaveCharges();
    }

    public void SetTarget(Health2D newTarget)
    {
        if (targetHealth == newTarget)
            return;

        UnbindFromHealth();
        targetHealth = newTarget;
        BindToHealth();
        ForceRefresh();
    }

    public void SetSaveChargesTarget(PlayerSaveCharges newTarget)
    {
        if (targetSaveCharges == newTarget)
            return;

        UnbindFromSaveCharges();
        targetSaveCharges = newTarget;
        BindToSaveCharges();
        ForceRefresh();
    }

    private void BindToHealth()
    {
        if (targetHealth == null)
            return;

        targetHealth.onHPChanged.AddListener(OnHPChanged);
        targetHealth.onDeath.AddListener(OnDeath);
    }

    private void UnbindFromHealth()
    {
        if (targetHealth == null)
            return;

        targetHealth.onHPChanged.RemoveListener(OnHPChanged);
        targetHealth.onDeath.RemoveListener(OnDeath);
    }

    private void BindToSaveCharges()
    {
        if (targetSaveCharges == null)
            return;

        targetSaveCharges.onChargesChanged.AddListener(OnChargesChanged);
    }

    private void UnbindFromSaveCharges()
    {
        if (targetSaveCharges == null)
            return;

        targetSaveCharges.onChargesChanged.RemoveListener(OnChargesChanged);
    }

    private void ForceRefresh()
    {
        if (targetHealth != null)
            OnHPChanged(targetHealth.currentHP, targetHealth.maxHP);

        if (targetSaveCharges != null)
            OnChargesChanged(targetSaveCharges.CurrentCharges);
        else
            ShowDefaultHistory();
    }

    private void OnHPChanged(int current, int max)
    {
        UpdateFlask(current, max);
        UpdateLifeAmountText(current, max);
        UpdateHealthHistory(current, max);

        lastHP = current;
        lastMaxHP = max;
    }

    private void OnChargesChanged(int amount)
    {
        currentSaves = amount;
        ShowTemporaryHistory(savesLabel + currentSaves);
    }

    private void UpdateFlask(int current, int max)
    {
        if (healthFlaskImage == null || healthStates == null || healthStates.Length == 0)
            return;

        float ratio = (max <= 0) ? 0f : (float)current / max;
        int index = Mathf.RoundToInt((1f - ratio) * (healthStates.Length - 1));
        index = Mathf.Clamp(index, 0, healthStates.Length - 1);

        healthFlaskImage.sprite = healthStates[index];
    }

    private void UpdateLifeAmountText(int current, int max)
    {
        if (lifeAmountText == null)
            return;

        lifeAmountText.text = current + " / " + max;
    }

    private void UpdateHealthHistory(int current, int max)
    {
        if (lifeHistoryText == null)
            return;

        if (lastHP < 0 || lastMaxHP < 0)
        {
            ShowDefaultHistory();
            return;
        }

        int delta = current - lastHP;

        if (delta < 0)
        {
            ShowTemporaryHistory(delta + " HP");
        }
        else if (delta > 0)
        {
            ShowTemporaryHistory("+" + delta + " HP");
        }
        else if (showCriticalMessage && current > 0 && current <= criticalThreshold)
        {
            ShowTemporaryHistory("CRITICAL");
        }
        else
        {
            ShowDefaultHistory();
        }
    }

    private void OnDeath()
    {
        if (lifeAmountText != null)
            lifeAmountText.text = "0 / 0";

        ShowTemporaryHistory("LIFE SIGNAL LOST", 2f);

        if (healthFlaskImage != null && healthStates != null && healthStates.Length > 0)
            healthFlaskImage.sprite = healthStates[healthStates.Length - 1];
    }

    private void ShowDefaultHistory()
    {
        if (lifeHistoryText == null)
            return;

        lifeHistoryText.text = savesLabel + currentSaves;
    }

    private void ShowTemporaryHistory(string message)
    {
        ShowTemporaryHistory(message, temporaryMessageDuration);
    }

    private void ShowTemporaryHistory(string message, float duration)
    {
        if (lifeHistoryText == null)
            return;

        if (historyRoutine != null)
            StopCoroutine(historyRoutine);

        historyRoutine = StartCoroutine(TemporaryHistoryRoutine(message, duration));
    }

    private IEnumerator TemporaryHistoryRoutine(string message, float duration)
    {
        lifeHistoryText.text = message;
        yield return new WaitForSeconds(duration);
        ShowDefaultHistory();
        historyRoutine = null;
    }
}