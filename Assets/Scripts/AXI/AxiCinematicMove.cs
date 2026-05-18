using System.Collections;
using UnityEngine;

public class AxiCinematicMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatingFollow floatingFollow;

    [Header("Cinematic Points")]
    [SerializeField] private Transform cinematicStartPoint;
    [SerializeField] private Transform cinematicExitPoint;

    [Header("Start Placement")]
    [SerializeField] private bool teleportToStartPoint = true;
    [SerializeField] private float moveToStartSpeed = 5f;

    [Header("Exit Movement")]
    [SerializeField] private float exitMoveSpeed = 4f;
    [SerializeField] private float stopDistance = 0.05f;

    [Header("Disappear")]
    [SerializeField] private bool destroyOnExit = false;
    [SerializeField] private bool setInactiveOnExit = true;
    [SerializeField] private float disappearDelay = 0f;

    [Header("Optional Fade")]
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (floatingFollow == null)
            floatingFollow = GetComponent<FloatingFollow>();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void PlaceAtCinematicStart()
    {
        if (cinematicStartPoint == null)
        {
            Debug.LogWarning("[AxiCinematicMove] Aucun Cinematic Start Point assigné.", this);
            return;
        }

        gameObject.SetActive(true);
        ResetAlpha();

        if (floatingFollow != null)
            floatingFollow.enabled = false;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (teleportToStartPoint)
        {
            transform.position = cinematicStartPoint.position;
        }
        else
        {
            currentRoutine = StartCoroutine(MoveToPointRoutine(
                cinematicStartPoint,
                moveToStartSpeed,
                false
            ));
        }
    }

    public void ExitCinematic()
    {
        if (cinematicExitPoint == null)
        {
            Debug.LogWarning("[AxiCinematicMove] Aucun Cinematic Exit Point assigné.", this);
            return;
        }

        if (floatingFollow != null)
            floatingFollow.enabled = false;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(MoveToPointRoutine(
            cinematicExitPoint,
            exitMoveSpeed,
            true
        ));
    }

    private IEnumerator MoveToPointRoutine(Transform point, float speed, bool disappearAtEnd)
    {
        while (Vector2.Distance(transform.position, point.position) > stopDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                point.position,
                speed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        transform.position = point.position;

        if (disappearAtEnd)
            yield return StartCoroutine(DisappearRoutine());
    }

    private IEnumerator DisappearRoutine()
    {
        if (disappearDelay > 0f)
            yield return new WaitForSecondsRealtime(disappearDelay);

        if (fadeOut)
            yield return StartCoroutine(FadeOutRoutine());

        if (destroyOnExit)
            Destroy(gameObject);
        else if (setInactiveOnExit)
            gameObject.SetActive(false);
    }

    private IEnumerator FadeOutRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            yield break;

        float timer = 0f;
        Color[] baseColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                baseColors[i] = spriteRenderers[i].color;
        }

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = fadeDuration <= 0f ? 1f : timer / fadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                    continue;

                Color c = baseColors[i];
                c.a = alpha;
                spriteRenderers[i].color = c;
            }

            yield return null;
        }
    }

    private void ResetAlpha()
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            Color c = spriteRenderers[i].color;
            c.a = 1f;
            spriteRenderers[i].color = c;
        }
    }
}