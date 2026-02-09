using System.Collections.Generic;
using UnityEngine;

public class PlayerTargeting2D : MonoBehaviour
{
    [Header("Origin")]
    public Transform origin; // le joueur

    [Header("Camera")]
    public Camera cam;
    public float maxDistance = 50f; // optionnel

    public TargetObject CurrentTarget { get; private set; }
    public readonly List<TargetObject> VisibleTargets = new();

    private TargetObject lastTarget;


    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        UpdateVisibleTargets();
        PickCurrentTarget();
        UpdateOutline();
    }

    void UpdateVisibleTargets()
    {
        VisibleTargets.Clear();

        // Rectangle visible de la caméra en world space
        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)); // bottom-left
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, 0)); // top-right

        // On prend tous les TargetObject présents
        var all = FindObjectsByType<TargetObject>(FindObjectsSortMode.None);

        foreach (var t in all)
        {
            if (t == null) continue;

            if (origin != null && t.transform == origin)
                continue;

            // si ton TargetObject du player est sur un parent/enfant, utilise plutôt :
            if (origin != null && t.transform.IsChildOf(origin))
                continue;
            if (origin != null && origin.IsChildOf(t.transform))
                continue;

            Vector3 p = t.transform.position;

            // filtre "dans l'écran"
            bool inView = (p.x >= bl.x && p.x <= tr.x && p.y >= bl.y && p.y <= tr.y);
            if (!inView) continue;

            // filtre distance optionnel
            if (maxDistance > 0f)
            {
                Vector3 o = origin != null ? origin.position : cam.transform.position;
                float d = Vector2.Distance(o, p);
                if (d > maxDistance) continue;
            }

            VisibleTargets.Add(t);
        }
    }

    void UpdateOutline()
    {
        if (lastTarget == CurrentTarget)
            return;

        // Désactiver ancien
        if (lastTarget != null)
        {
            var o = lastTarget.GetComponent<TargetOutline2D>();
            if (o != null)
                o.SetOutlined(false);
        }

        // Activer nouveau
        if (CurrentTarget != null)
        {
            var o = CurrentTarget.GetComponent<TargetOutline2D>();
            if (o != null)
                o.SetOutlined(true);
        }

        lastTarget = CurrentTarget;
    }


    void PickCurrentTarget()
    {
        CurrentTarget = null;
        if (VisibleTargets.Count == 0) return;

        // Choix: target la plus proche du centre écran
        Vector3 center = origin != null ? origin.position : cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));
        float best = float.PositiveInfinity;

        foreach (var t in VisibleTargets)
        {
            float d = Vector2.Distance(center, t.transform.position);
            if (d < best)
            {
                best = d;
                CurrentTarget = t;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform o = origin != null ? origin : transform;
        if (o == null) return;

        // --- Cercle de portée (maxDistance)
        if (maxDistance > 0f)
        {
            Gizmos.color = Color.cyan;

            const int steps = 64;
            float r = maxDistance;
            Vector3 center = o.position;
            center.z = 0f;

            Vector3 prev = center + new Vector3(r, 0f, 0f);
            for (int i = 1; i <= steps; i++)
            {
                float a = (i / (float)steps) * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        // --- Rectangle caméra (debug visibilité)
        if (cam != null)
        {
            Gizmos.color = Color.yellow;

            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
            Vector3 br = cam.ViewportToWorldPoint(new Vector3(1, 0, 0));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
            Vector3 tl = cam.ViewportToWorldPoint(new Vector3(0, 1, 0));

            bl.z = br.z = tr.z = tl.z = 0f;

            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }
    }
}
