using System.Collections.Generic;
using UnityEngine;

public class PlayerTargeting2D : MonoBehaviour
{
    [Header("Origin")]
    public Transform origin; // le joueur

    [Header("Camera")]
    public Camera cam;
    public float maxDistance = 50f;

    public TargetObject CurrentTarget { get; private set; }
    public readonly List<TargetObject> VisibleTargets = new();

    private TargetObject lastTarget;

    void Awake()
    {
        ResolveCamera();
    }

    void OnEnable()
    {
        ResolveCamera();
    }

    void LateUpdate()
    {
        // Important avec Cinemachine : la caméra finit souvent son mouvement en LateUpdate
        ResolveCamera();

        if (cam == null)
            return;

        UpdateVisibleTargets();
        PickCurrentTarget();
        UpdateOutline();
    }

    void ResolveCamera()
    {
        if (cam == null || !cam.isActiveAndEnabled)
            cam = Camera.main;
    }

    void UpdateVisibleTargets()
    {
        VisibleTargets.Clear();

        if (cam == null)
            return;

        var all = FindObjectsByType<TargetObject>(FindObjectsSortMode.None);

        foreach (var t in all)
        {
            if (t == null)
                continue;

            if (origin != null && t.transform == origin)
                continue;

            if (origin != null && t.transform.IsChildOf(origin))
                continue;

            if (origin != null && origin.IsChildOf(t.transform))
                continue;

            Vector3 p = t.transform.position;

            // Vérifie si la cible est visible dans le viewport de la vraie caméra
            Vector3 vp = cam.WorldToViewportPoint(p);

            bool inFront = vp.z > 0f;
            bool inView = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

            if (!inFront || !inView)
                continue;

            if (maxDistance > 0f)
            {
                Vector3 o = origin != null ? origin.position : transform.position;
                float d = Vector2.Distance(o, p);
                if (d > maxDistance)
                    continue;
            }

            VisibleTargets.Add(t);
        }
    }

    void PickCurrentTarget()
    {
        CurrentTarget = null;

        if (VisibleTargets.Count == 0 || cam == null)
            return;

        // On prend la cible la plus proche du centre écran
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        float best = float.PositiveInfinity;

        foreach (var t in VisibleTargets)
        {
            if (t == null)
                continue;

            Vector3 vp = cam.WorldToViewportPoint(t.transform.position);
            Vector2 v2 = new Vector2(vp.x, vp.y);

            float d = Vector2.Distance(v2, screenCenter);
            if (d < best)
            {
                best = d;
                CurrentTarget = t;
            }
        }
    }

    void UpdateOutline()
    {
        if (lastTarget == CurrentTarget)
            return;

        if (lastTarget != null)
        {
            var oldOutline = lastTarget.GetComponent<TargetOutline2D>();
            if (oldOutline != null)
                oldOutline.SetOutlined(false);
        }

        if (CurrentTarget != null)
        {
            var newOutline = CurrentTarget.GetComponent<TargetOutline2D>();
            if (newOutline != null)
                newOutline.SetOutlined(true);
        }

        lastTarget = CurrentTarget;
    }

    void OnDisable()
    {
        if (lastTarget != null)
        {
            var outline = lastTarget.GetComponent<TargetOutline2D>();
            if (outline != null)
                outline.SetOutlined(false);
        }

        lastTarget = null;
        CurrentTarget = null;
        VisibleTargets.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Transform o = origin != null ? origin : transform;
        if (o == null)
            return;

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

        Camera debugCam = cam != null ? cam : Camera.main;
        if (debugCam != null)
        {
            Gizmos.color = Color.yellow;

            Vector3 bl = debugCam.ViewportToWorldPoint(new Vector3(0f, 0f, debugCam.nearClipPlane));
            Vector3 br = debugCam.ViewportToWorldPoint(new Vector3(1f, 0f, debugCam.nearClipPlane));
            Vector3 tr = debugCam.ViewportToWorldPoint(new Vector3(1f, 1f, debugCam.nearClipPlane));
            Vector3 tl = debugCam.ViewportToWorldPoint(new Vector3(0f, 1f, debugCam.nearClipPlane));

            bl.z = 0f;
            br.z = 0f;
            tr.z = 0f;
            tl.z = 0f;

            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }
    }
}