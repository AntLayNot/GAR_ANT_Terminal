using UnityEngine;

public class SimplePulseEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float endScale = 2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Color color = new Color(0.65f, 0.95f, 1f, 0.9f);

    private float timer;

    private void Awake()
    {
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        timer = 0f;
        transform.localScale = Vector3.one * startScale;

        if (sr != null)
            sr.color = color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float curved = scaleCurve.Evaluate(t);

        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, curved);

        if (sr != null)
        {
            Color c = color;
            c.a = Mathf.Lerp(color.a, 0f, t);
            sr.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}