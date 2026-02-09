using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Preset 2D", fileName = "EnemyPreset2D")]
public class EnemyPreset2D : ScriptableObject
{
    public enum EnemyType { Melee, Shooter, Kamikaze }

    [Header("Type")]
    public EnemyType type = EnemyType.Melee;

    [Header("Movement")]
    public bool patrol = true;
    public float moveSpeed = 2.5f;
    public float chaseSpeed = 3.2f;
    public float stopDistance = 1.1f;

    [Header("Detection")]
    public float detectRange = 8f;
    public float loseRange = 10f;
    public bool requireLineOfSight = false;

    [Header("Combat (Melee)")]
    public float attackRange = 1.2f;
    public float attackCooldown = 0.9f;
    public int damage = 1;
    public Vector2 attackBoxSize = new Vector2(1.2f, 1.0f);
    public Vector2 attackBoxOffset = new Vector2(0.7f, 0f);

    [Header("Combat (Shooter)")]
    public GameObject projectilePrefab;
    public float shootCooldown = 0.8f;
    public float projectileSpeed = 12f;
    public float shootRange = 7f;

    [Tooltip("Distance idéale: si trop proche, le shooter recule.")]
    public float preferredDistance = 4f;

    [Tooltip("Distance mini: si on est en dessous, reculer fort.")]
    public float minDistance = 2f;
}
