using System.Collections;
using UnityEngine;

public class MovingPlatform2D : MonoBehaviour
{
    public enum MoveDirection
    {
        Horizontal,
        Vertical
    }

    [Header("Movement")]
    [SerializeField] private MoveDirection moveDirection = MoveDirection.Horizontal;
    [SerializeField] private float distance = 3f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waitTimeAtEnds = 0.5f;
    [SerializeField] private bool startFromInitialPosition = true;

    [Header("Optional")]
    [SerializeField] private bool useLocalPosition = false;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 targetPosition;
    private bool isWaiting = false;

    private void Start()
    {
        if (useLocalPosition)
            startPosition = transform.localPosition;
        else
            startPosition = transform.position;

        Vector3 offset = GetOffset();
        endPosition = startPosition + offset;

        if (startFromInitialPosition)
            targetPosition = endPosition;
        else
        {
            if (useLocalPosition)
                transform.localPosition = endPosition;
            else
                transform.position = endPosition;

            targetPosition = startPosition;
        }
    }

    private void Update()
    {
        if (isWaiting)
            return;

        MovePlatform();
    }

    private Vector3 GetOffset()
    {
        switch (moveDirection)
        {
            case MoveDirection.Vertical:
                return Vector3.up * distance;

            case MoveDirection.Horizontal:
            default:
                return Vector3.right * distance;
        }
    }

    private void MovePlatform()
    {
        Vector3 currentPosition = useLocalPosition ? transform.localPosition : transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed * Time.deltaTime);

        if (useLocalPosition)
            transform.localPosition = nextPosition;
        else
            transform.position = nextPosition;

        if (Vector3.Distance(nextPosition, targetPosition) <= 0.01f)
        {
            StartCoroutine(WaitAndSwitchTarget());
        }
    }

    private IEnumerator WaitAndSwitchTarget()
    {
        isWaiting = true;

        if (waitTimeAtEnds > 0f)
            yield return new WaitForSeconds(waitTimeAtEnds);

        if (targetPosition == endPosition)
            targetPosition = startPosition;
        else
            targetPosition = endPosition;

        isWaiting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 origin = useLocalPosition && Application.isPlaying ? startPosition : transform.position;
        Vector3 destination = origin;

        switch (moveDirection)
        {
            case MoveDirection.Vertical:
                destination = origin + Vector3.up * distance;
                break;

            case MoveDirection.Horizontal:
                destination = origin + Vector3.right * distance;
                break;
        }

        Gizmos.DrawLine(origin, destination);
        Gizmos.DrawSphere(origin, 0.12f);
        Gizmos.DrawSphere(destination, 0.12f);
    }
}