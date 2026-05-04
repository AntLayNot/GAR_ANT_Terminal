using UnityEngine;

public class FloatingFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(1.5f, 1f, 0f);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool useLocalRightOfTarget = false;
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = false;

    [Header("Floating")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 2f;
    [SerializeField] private Vector3 floatDirection = Vector3.up;

    [Header("Optional Look")]
    [SerializeField] private bool flipWithTargetScaleX = false;

    private Vector3 baseTargetPosition;

    private void Reset()
    {
        if (target == null && Camera.main != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredOffset = offset;

        if (useLocalRightOfTarget)
        {
            desiredOffset =
                target.right * offset.x +
                target.up * offset.y +
                target.forward * offset.z;
        }
        else if (flipWithTargetScaleX)
        {
            desiredOffset.x *= Mathf.Sign(target.localScale.x);
        }

        baseTargetPosition = target.position + desiredOffset;

        Vector3 floatingOffset = Vector3.zero;

        if (enableFloating)
        {
            Vector3 dir = floatDirection.sqrMagnitude > 0.0001f ? floatDirection.normalized : Vector3.up;
            floatingOffset = dir * Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        }

        Vector3 desiredPosition = baseTargetPosition + floatingOffset;
        Vector3 currentPosition = transform.position;
        Vector3 newPosition = Vector3.Lerp(currentPosition, desiredPosition, followSpeed * Time.deltaTime);

        if (!followX) newPosition.x = currentPosition.x;
        if (!followY) newPosition.y = currentPosition.y;
        if (!followZ) newPosition.z = currentPosition.z;

        transform.position = newPosition;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}