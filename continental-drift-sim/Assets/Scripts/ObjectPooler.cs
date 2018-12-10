using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler current;
    public int pooledNodeAmount;
    public int pooledVolcanoAmount;

    private Stack<CrustNode> pooledNodes;
    private Stack<Volcano> pooledVolcanos;

    void Awake()
    {
        current = this;
    }

    void Start()
    {
        pooledNodes = new Stack<CrustNode>(pooledNodeAmount);
        for(int i=0; i<pooledNodeAmount; i++)
        {
            CrustNode node = new CrustNode(0,0);
            pooledNodes.Push(node);
        }

        pooledVolcanos = new Stack<Volcano>(pooledVolcanoAmount);
        for (int i=0; i<pooledVolcanoAmount; i++)
        {
            Volcano volcano = new Volcano(0,0,null);
            pooledVolcanos.Push(volcano);
        }
    }

    public CrustNode GetPooledNode()
    {
        if(pooledNodes.Count > 0)
        {
            return pooledNodes.Pop();
        }
        else
        {
            Debug.Log("Ran out of pooled nodes!");
            Debug.Break();
        }
        return null;
    }

    public Volcano GetPooledVolcano()
    {
        if (pooledVolcanos.Count > 0)
        {
            return pooledVolcanos.Pop();
        }
        else
        {
            Debug.Log("Ran out of pooled nodes!");
            Debug.Break();
        }
        return null;
    }


    public void ReturnNodeToPool(CrustNode n)
    {
        n.CleanObject();
        pooledNodes.Push(n);
    }

    public void ReturnVolcanoToPool(Volcano v)
    {
        v.CleanObject();
        pooledVolcanos.Push(v);
    }
}

public abstract class PoolableObject
{
    public abstract void CleanObject();
}