using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler current;
    public int pooledNodeAmount;
    public int pooledVolcanoAmount;

    private Stack<Node> pooledNodes;
    private Stack<Volcano> pooledVolcanos;

    void Awake()
    {
        current = this;
    }

    void Start()
    {
        pooledNodes = new Stack<Node>(pooledNodeAmount);
        for(int i=0; i<pooledNodeAmount; i++)
        {
            Node node = new Node(0,0);
            pooledNodes.Push(node);
        }

        //temp
        if (pooledNodes.Peek() != null)
        {
            Debug.Log("success!");
            Debug.Log(pooledNodes.Peek().X);
        }//endtemp

        pooledVolcanos = new Stack<Volcano>(pooledVolcanoAmount);
        for (int i=0; i<pooledVolcanoAmount; i++)
        {
            Volcano volcano = new Volcano(0,0,null);
            pooledVolcanos.Push(volcano);
        }

        //temp
        if (pooledVolcanos.Peek() != null)
        {
            Debug.Log("success!");
            Debug.Log(pooledVolcanos.Peek().X);
        }//endtemp
    }

    public Node GetPooledNode()
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


    public void ReturnNodeToPool(Node n)
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