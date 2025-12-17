using UnityEngine;

public class NodeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float minDistanceBetweenNodes = 3f;

    void Start()
    {
        SpawnNodes();
    }

    private void SpawnNodes()
    {
        int shopCount = 2;
        int combatCount = 3;
        int restCount = 2;
        int mysteryCount = 3;

        for (int i = 0; i < shopCount; i++)
            SpawnNode<ShopNode>();
        
        for (int i = 0; i < combatCount; i++)
            SpawnNode<CombatNode>();
        
        for (int i = 0; i < restCount; i++)
            SpawnNode<RestNode>();
        
        for (int i = 0; i < mysteryCount; i++)
            SpawnNode<MysteryNode>();
    }

    private void SpawnNode<T>() where T : NodeBase
    {
        Vector3 position = GetRandomPosition();
        
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
        int maxAttempts = 30;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 position = new Vector3(randomCircle.x, 0.75f, randomCircle.y);
            
            if (IsPositionValid(position))
            {
                return position;
            }
        }
        
        Vector2 fallbackCircle = Random.insideUnitCircle * spawnRadius;
        return new Vector3(fallbackCircle.x, 0.75f, fallbackCircle.y);
    }

    private bool IsPositionValid(Vector3 position)
    {
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
