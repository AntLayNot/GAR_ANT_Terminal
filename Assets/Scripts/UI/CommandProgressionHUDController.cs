using UnityEngine;
using TMPro;

public class CommandProgressionHUDController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerCommandProgression2D targetProgression;

    [Header("Auto Find")]
    [SerializeField] private bool autoFindPlayerProgression = true;

    [Header("Diagnostic Mode Text")]
    [SerializeField] private TMP_Text diagnosticText;

    [Header("Objectifs")]
    [SerializeField] private int doubleProjectileKillsRequired = 3;
    [SerializeField] private int damageBonus1KillsRequired = 6;
    [SerializeField] private int rainKillsRequired = 10;
    [SerializeField] private int vitalityDoorsRequired = 3;

    [Header("Style")]
    [SerializeField] private bool useShortRainText = true;

    private void Start()
    {
        if (targetProgression == null && autoFindPlayerProgression)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                targetProgression = player.GetComponent<PlayerCommandProgression2D>();

                if (targetProgression == null)
                    targetProgression = player.GetComponentInParent<PlayerCommandProgression2D>();
            }
        }

        Bind();
        RefreshHUD();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void SetTarget(PlayerCommandProgression2D newTarget)
    {
        if (targetProgression == newTarget)
            return;

        Unbind();

        targetProgression = newTarget;

        Bind();
        RefreshHUD();
    }

    private void Bind()
    {
        if (targetProgression == null)
            return;

        targetProgression.onProgressionChanged.AddListener(RefreshHUD);
    }

    private void Unbind()
    {
        if (targetProgression == null)
            return;

        targetProgression.onProgressionChanged.RemoveListener(RefreshHUD);
    }

    public void RefreshHUD()
    {
        if (targetProgression == null)
        {
            SetMissingTargetHUD();
            return;
        }

        int kills = targetProgression.GetEnemyKills();
        int doors = targetProgression.GetDoorsToggled();

        int projectileCount = targetProgression.GetProjectileCount();
        int damageBonus = targetProgression.GetProjectileDamageBonus();
        int vitalityBonus = targetProgression.GetMaxHealthBonusGiven();

        bool rainUnlocked = targetProgression.IsRainUnlocked();

        UpdateDiagnosticText(
            kills,
            doors,
            projectileCount,
            damageBonus,
            vitalityBonus,
            rainUnlocked
        );
    }

    private void UpdateDiagnosticText(
        int kills,
        int doors,
        int projectileCount,
        int damageBonus,
        int vitalityBonus,
        bool rainUnlocked
    )
    {
        if (diagnosticText == null)
            return;

        string killLine = "KILL  " + FormatProgress(kills, GetNextKillGoal(kills));
        string doorLine = "DOOR  " + FormatProgress(doors, vitalityDoorsRequired);

        string projectileLine = "PROJ  x" + projectileCount;
        string damageLine = "DMG   +" + damageBonus;
        string vitalityLine = "VIT   +" + vitalityBonus;

        string rainState;

        if (rainUnlocked)
            rainState = useShortRainText ? "OK" : "READY";
        else
            rainState = useShortRainText ? "LOCK" : "LOCKED";

        string rainLine = "RAIN  " + rainState;

        diagnosticText.text =
            killLine + "\n" +
            doorLine + "\n" +
            projectileLine + "\n" +
            damageLine + "\n" +
            vitalityLine + "\n" +
            rainLine;
    }

    private string FormatProgress(int current, int required)
    {
        if (required <= 0)
            return current.ToString("00");

        int shownCurrent = Mathf.Min(current, required);

        return shownCurrent.ToString("00") + "/" + required.ToString("00");
    }

    private int GetNextKillGoal(int kills)
    {
        if (kills < doubleProjectileKillsRequired)
            return doubleProjectileKillsRequired;

        if (kills < damageBonus1KillsRequired)
            return damageBonus1KillsRequired;

        if (kills < rainKillsRequired)
            return rainKillsRequired;

        return 0;
    }

    private void SetMissingTargetHUD()
    {
        if (diagnosticText == null)
            return;

        diagnosticText.text =
            "KILL  --/--\n" +
            "DOOR  --/--\n" +
            "PROJ  --\n" +
            "DMG   --\n" +
            "VIT   --\n" +
            "RAIN  --";
    }
}