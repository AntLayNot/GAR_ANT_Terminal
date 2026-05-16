using System.Collections;
using UnityEngine;

public class SpriteSpawnerMover : MonoBehaviour
{
    [Header("Prefab à faire spawn")]
    [SerializeField] private GameObject spritePrefab;

    [Header("Points")]
    [SerializeField] private Transform spawnPointA;
    [SerializeField] private Transform targetPointB;

    [Header("Temps de déplacement")]
    [SerializeField] private float baseTravelTime = 3f;

    [Tooltip("Marge aléatoire autour du temps de base. Exemple : 1 = entre 2s et 4s si baseTravelTime = 3")]
    [SerializeField] private float randomTimeMargin = 1f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.5f;

    private GameObject currentSprite;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnSprite();

            if (currentSprite != null)
            {
                yield return StartCoroutine(MoveSprite());
            }

            if (currentSprite != null)
            {
                Destroy(currentSprite);
            }

            yield return new WaitForSeconds(respawnDelay);
        }
    }

    private void SpawnSprite()
    {
        if (spritePrefab == null || spawnPointA == null)
        {
            Debug.LogWarning("[SpriteSpawnerMover] SpritePrefab ou SpawnPointA manquant.");
            return;
        }

        currentSprite = Instantiate(
            spritePrefab,
            spawnPointA.position,
            spawnPointA.rotation
        );
    }

    private IEnumerator MoveSprite()
    {
        if (currentSprite == null || targetPointB == null)
            yield break;

        Vector3 startPosition = spawnPointA.position;
        Vector3 endPosition = targetPointB.position;

        float minTime = Mathf.Max(0.1f, baseTravelTime - randomTimeMargin);
        float maxTime = baseTravelTime + randomTimeMargin;

        float travelTime = Random.Range(minTime, maxTime);
        float elapsedTime = 0f;

        while (elapsedTime < travelTime)
        {
            if (currentSprite == null)
                yield break;

            elapsedTime += Time.deltaTime;

            float t = elapsedTime / travelTime;

            currentSprite.transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            yield return null;
        }

        if (currentSprite != null)
        {
            currentSprite.transform.position = endPosition;
        }
    }
}