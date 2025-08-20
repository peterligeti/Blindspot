using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class EnemyLootLogic : MonoBehaviour
{
    [SerializeField] private bool canSpawnHealthPack = true;
    [SerializeField] private float healthPackChance = 0.5f;
    [SerializeField] GameObject healthPackPrefab;
    
    [SerializeField] Transform spawnedLootContainer;
    
    void Start()
    {
        GameObject container = GameObject.Find("spawnedLootContainer");
        if (container != null)
        {
            spawnedLootContainer = container.transform;
        }
        else
        {
            Debug.LogError("No spawnedLootContainer found");
        }
    }
    
    private void OnEnable()
    {
        EnemyHealthLogic.OnOneEnemyKilled += HandleEnemyKill;
    }

    private void OnDisable()
    {
        EnemyHealthLogic.OnOneEnemyKilled -= HandleEnemyKill;
    }

    private void HandleEnemyKill(GameObject deadEnemy)
    {
        if (deadEnemy != gameObject) return; // Only respond to this enemy's death
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); // Last enemy should not give out loot as player cannot collect it
            if (enemies.Length == 1)
            {
                Debug.Log("LAST ENEMY");
                return;
            }

        if (canSpawnHealthPack && Random.value < healthPackChance)
        {
            Instantiate(healthPackPrefab, transform.position, Quaternion.identity, spawnedLootContainer);
            Debug.Log("Health pack spawned!");
        }
    }

}
