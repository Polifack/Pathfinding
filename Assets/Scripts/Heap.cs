using UnityEngine;
using System.Collections;
using System;

public class Heap<T> where T : IHeapItem<T>
{

    private T[] items;
    private int currentItemCount;
    public int Count
    {
        get
        {
            return currentItemCount;
        }
    }

    public Heap(int maxHeapSize)
    {
        items = new T[maxHeapSize];
    }

    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        PercolateUp(item);
        currentItemCount++;
    }

    public T RemoveFirst()
    {
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        PercolateDown(items[0]);
        return firstItem;
    }

    public void UpdateItem(T item)
    {
        PercolateUp(item);
    }

    public bool Contains(T item)
    {
        return Equals(items[item.HeapIndex], item);
    }

    private void PercolateDown(T item)
    {
        int childIndexLeft = item.HeapIndex * 2 + 1;
        int childIndexRight = item.HeapIndex * 2 + 2;
        int swapIndex = 0;

        while (childIndexLeft < currentItemCount)
        {
            swapIndex = childIndexLeft;

            if (childIndexRight < currentItemCount)
            {
                if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                {
                    swapIndex = childIndexRight;
                }
            }

            if (item.CompareTo(items[swapIndex]) < 0)
            {
                Swap(item, items[swapIndex]);
            }
            else
            {
                return;
            }

            childIndexLeft = item.HeapIndex * 2 + 1;
            childIndexRight = item.HeapIndex * 2 + 2;
            swapIndex = 0;

        }
    }

    private void PercolateUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;
        T parentItem = items[parentIndex];
        while (item.CompareTo(parentItem) > 0)
        {
            Swap(item, parentItem);
            parentIndex = (item.HeapIndex - 1) / 2;
            parentItem = items[parentIndex];
        }
    }

    private void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}

//Interfaz de los elementos de la Heap. Cada elemento debe recordar el indice en el que se encuentra por eficiencia.
public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex
    {
        get;
        set;
    }
}
