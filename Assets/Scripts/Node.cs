using System;
using UnityEngine;

public class Node : IHeapItem<Node>
{
    public Node parent;
    
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    public int gCost;
    public int hCost;
    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    private int heapIndex;
    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }

    public Node(bool _walkable ,Vector3 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }

    public int CompareTo(Node other)
    {
        int compare = (fCost.CompareTo(other.fCost));

        //Si el fCost es igual en ambos nodos, comparamos usando el hCost.
        if (compare == 0)
        {
            compare = hCost.CompareTo(other.hCost);
        }

        //Como nos va a interesar el nodo con los menores valores, devolvemos el resultado negativo.
        return -compare;
    }
}
