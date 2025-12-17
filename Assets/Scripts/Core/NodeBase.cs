using UnityEngine;

public enum NodeType
{
    Shop,
    Combat,
    Rest,
    Mystery
}

public class NodeBase : MonoBehaviour
{
    [SerializeField] protected NodeType nodeType;
    protected bool hasBeenCompleted = false;
    protected Referencer refs;
    protected GameManager gm;

    protected virtual void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        gm = FindFirstObjectByType<GameManager>();
        
        SetNodeColor();
    }

    protected virtual void SetNodeColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            switch (nodeType)
            {
                case NodeType.Shop:
                    renderer.material.color = Color.yellow;
                    break;
                case NodeType.Combat:
                    renderer.material.color = Color.red;
                    break;
                case NodeType.Rest:
                    renderer.material.color = Color.green;
                    break;
                case NodeType.Mystery:
                    renderer.material.color = Color.cyan;
                    break;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenCompleted) return;
        
        if (other.CompareTag("Player"))
        {
            OnNodeTriggered();
        }
    }

    protected virtual void OnNodeTriggered()
    {
        if (refs == null || refs.ui == null) return;
        
        string nodeTitle = GetNodeTitle();
        refs.ui.ShowNodePopup(nodeTitle, this);
    }

    public virtual void OnNodeCompleted()
    {
        hasBeenCompleted = true;
        
        if (gm != null)
        {
            gm.OnNodeCompleted();
        }
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.gray;
        }
    }

    protected virtual string GetNodeTitle()
    {
        switch (nodeType)
        {
            case NodeType.Shop:
                return "Shop Node";
            case NodeType.Combat:
                return "Combat Node";
            case NodeType.Rest:
                return "Rest Node";
            case NodeType.Mystery:
                return "Mystery Node";
            default:
                return "Unknown Node";
        }
    }

    public NodeType GetNodeType()
    {
        return nodeType;
    }
}
