using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraLensTargetZone2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Tooltip("Target à suivre pendant cette zone. Si vide, garde le target actuel.")]
    [SerializeField] private Transform newFollowTarget;

    [Tooltip("Target à regarder. Optionnel, surtout utile en cinématique.")]
    [SerializeField] private Transform newLookAtTarget;

    [Header("Lens")]
    [SerializeField] private bool changeOrthographicSize = true;
    [SerializeField] private float targetOrthographicSize = 6f;

    [SerializeField] private bool changeFieldOfView = false;
    [SerializeField] private float targetFieldOfView = 45f;

    [Header("Follow Offset")]
    [Tooltip("Active le changement de hauteur de caméra.")]
    [SerializeField] private bool changeFollowOffsetY = true;

    [Tooltip("Offset Y de la caméra. Valeur positive = caméra plus haute.")]
    [SerializeField] private float targetFollowOffsetY = 1.5f;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Priority")]
    [SerializeField] private bool changePriorityOnEnter = false;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;

    [Header("Restore On Exit")]
    [SerializeField] private bool restoreOnExit = true;
    [SerializeField] private float restoreDuration = 1f;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = false;

    private Transform originalFollowTarget;
    private Transform originalLookAtTarget;

    private float originalOrthographicSize;
    private float originalFieldOfView;
    private float originalFollowOffsetY;
    private int originalPriority;

    private CinemachinePositionComposer positionComposer;

    private Coroutine transitionCoroutine;
    private bool hasTriggered;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (virtualCamera == null)
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();

        CacheOriginalValues();
    }

    private void CacheOriginalValues()
    {
        if (virtualCamera == null)
            return;

        originalFollowTarget = virtualCamera.Follow;
        originalLookAtTarget = virtualCamera.LookAt;

        originalOrthographicSize = virtualCamera.Lens.OrthographicSize;
        originalFieldOfView = virtualCamera.Lens.FieldOfView;
        originalPriority = virtualCamera.Priority;

        if (positionComposer != null)
            originalFollowOffsetY = positionComposer.TargetOffset.y;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        hasTriggered = true;
        ApplyCameraZone();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!restoreOnExit)
            return;

        if (!other.CompareTag(playerTag))
            return;

        RestoreCamera();
    }

    public void ApplyCameraZone()
    {
        if (virtualCamera == null)
        {
            Debug.LogWarning("[CameraLensTargetZone2D] Aucune CinemachineVirtualCamera assignée.");
            return;
        }

        if (newFollowTarget != null)
            virtualCamera.Follow = newFollowTarget;

        if (newLookAtTarget != null)
            virtualCamera.LookAt = newLookAtTarget;

        if (changePriorityOnEnter)
            virtualCamera.Priority = activePriority;

        StartCameraTransition(
            targetOrthographicSize,
            targetFieldOfView,
            targetFollowOffsetY,
            transitionDuration
        );
    }

    public void RestoreCamera()
    {
        if (virtualCamera == null)
            return;

        virtualCamera.Follow = originalFollowTarget;
        virtualCamera.LookAt = originalLookAtTarget;

        if (changePriorityOnEnter)
            virtualCamera.Priority = inactivePriority;

        StartCameraTransition(
            originalOrthographicSize,
            originalFieldOfView,
            originalFollowOffsetY,
            restoreDuration
        );
    }

    private void StartCameraTransition(float targetOrthoSize, float targetFov, float targetOffsetY, float duration)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CameraTransitionRoutine(
            targetOrthoSize,
            targetFov,
            targetOffsetY,
            duration
        ));
    }

    private IEnumerator CameraTransitionRoutine(
        float targetOrthoSize,
        float targetFov,
        float targetOffsetY,
        float duration
    )
    {
        float startOrthoSize = virtualCamera.Lens.OrthographicSize;
        float startFov = virtualCamera.Lens.FieldOfView;

        float startOffsetY = 0f;

        if (positionComposer != null)
            startOffsetY = positionComposer.TargetOffset.y;

        float timer = 0f;

        while (timer < duration)
        {
            if (PauseMenuController.IsPausedGlobal)
            {
                yield return null;
                continue;
            }

            timer += Time.unscaledDeltaTime;

            float t = duration <= 0f ? 1f : timer / duration;
            float curveT = transitionCurve.Evaluate(Mathf.Clamp01(t));

            LensSettings lens = virtualCamera.Lens;

            if (changeOrthographicSize)
            {
                lens.OrthographicSize = Mathf.Lerp(
                    startOrthoSize,
                    targetOrthoSize,
                    curveT
                );
            }

            if (changeFieldOfView)
            {
                lens.FieldOfView = Mathf.Lerp(
                    startFov,
                    targetFov,
                    curveT
                );
            }

            virtualCamera.Lens = lens;

            if (changeFollowOffsetY && positionComposer != null)
            {
                Vector3 offset = positionComposer.TargetOffset;

                offset.y = Mathf.Lerp(
                    startOffsetY,
                    targetOffsetY,
                    curveT
                );

                positionComposer.TargetOffset = offset;
            }

            yield return null;
        }

        LensSettings finalLens = virtualCamera.Lens;

        if (changeOrthographicSize)
            finalLens.OrthographicSize = targetOrthoSize;

        if (changeFieldOfView)
            finalLens.FieldOfView = targetFov;

        virtualCamera.Lens = finalLens;

        if (changeFollowOffsetY && positionComposer != null)
        {
            Vector3 offset = positionComposer.TargetOffset;
            offset.y = targetOffsetY;
            positionComposer.TargetOffset = offset;
        }

        transitionCoroutine = null;
    }
}