using System.Collections.Generic;
using UnityEngine;

public class NodeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private float spawnRadius = 30f;  // Increased to accommodate larger spacing
    [SerializeField] private float minDistanceBetweenNodes = 7.5f;  // 5 player sizes (player ~1.5 units)

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        SpawnNodesForWorld(1);
    }

    public void SpawnNodesForWorld(int world)
    {
        spawnedPositions.Clear();
        
        var encounterData = DataCache.GetWorldEncounter(world);
        
        int shopCount = encounterData.ShopNodeCount;
        int combatCount = encounterData.CombatNodeCount;
        int restCount = encounterData.RestNodeCount;
        int eliteCount = encounterData.EliteNodeCount;

        for (int i = 0; i < shopCount; i++)
            SpawnNode<ShopNode>();
        
        for (int i = 0; i < combatCount; i++)
            SpawnNode<CombatNode>();
        
        for (int i = 0; i < restCount; i++)
            SpawnNode<RestNode>();
        
        for (int i = 0; i < eliteCount; i++)
            SpawnNode<MysteryNode>();
            
        Debug.Log($"[NodeSpawner] Spawned nodes for World {world}: {shopCount} shops, {combatCount} combats, {restCount} rests, {eliteCount} elites (mystery)");
    }

    private void SpawnNode<T>() where T : NodeBase
    {
        Vector3 position = GetRandomPosition();
        spawnedPositions.Add(position);
        
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nodeObj.transform.position = position;
        nodeObj.transform.localScale = Vector3.one * 1.5f;
        nodeObj.name = typeof(T).Name;
        
        var collider = nodeObj.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        nodeObj.AddComponent<T>();
    }

    private Vector3 GetRandomPosition()
    {
        int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 position = new Vector3(randomCircle.x, 0.75f, randomCircle.y);
            
            if (IsPositionValid(position))
            {
                return position;
            }
        }
        
        Debug.LogWarning("[NodeSpawner] Could not find valid position after max attempts, using fallback with offset");
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = spawnRadius * 0.9f;
        return new Vector3(Mathf.Cos(angle) * radius, 0.75f, Mathf.Sin(angle) * radius);
    }

    private bool IsPositionValid(Vector3 position)
    {
        foreach (var existingPos in spawnedPositions)
        {
            float distance = Vector3.Distance(position, existingPos);
            if (distance < minDistanceBetweenNodes)
            {
                return false;
            }
        }
        
        Collider[] colliders = Physics.OverlapSphere(position, minDistanceBetweenNodes);
        foreach (var col in colliders)
        {
            if (col.GetComponent<NodeBase>() != null)
            {
                return false;
            }
        }
        return true;
    }
}
