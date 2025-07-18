using System;
using System.Collections.Generic;
using Unity.Collections;

// A custom Min-Heap implementation for native collections, usable in jobs.
public struct NativeMinHeap<T> : IDisposable where T : struct, IComparer<int>
{
    private NativeList<int> items;

    private T comparer;

    public NativeMinHeap(Allocator allocator, T customComparer)
    {
        this.items = new NativeList<int>(allocator);
        this.comparer = customComparer;
    }

    public int Count => items.Length;

    public void Enqueue(int item)
    {
        items.Add(item);
        HeapifyUp(items.Length - 1);
    }

    public int Dequeue()
    {
        if (items.Length == 0)
            throw new InvalidOperationException("Heap is empty.");

        int root = items[0];
        items[0] = items[items.Length - 1];
        items.RemoveAt(items.Length - 1);

        if (items.Length > 0)
            HeapifyDown(0);

        return root;
    }

    private void HeapifyUp(int index)
    {
        if (index == 0) return;
        int parentIndex = (index - 1) / 2;

        if (comparer.Compare(items[index], items[parentIndex]) < 0)
        {
            Swap(index, parentIndex);
            HeapifyUp(parentIndex);
        }
    }

    private void HeapifyDown(int index)
    {
        int leftChildIndex = 2 * index + 1;
        int rightChildIndex = 2 * index + 2;
        int smallest = index;

        if (leftChildIndex < items.Length && comparer.Compare(items[leftChildIndex], items[smallest]) < 0)
        {
            smallest = leftChildIndex;
        }
        if (rightChildIndex < items.Length && comparer.Compare(items[rightChildIndex], items[smallest]) < 0)
        {
            smallest = rightChildIndex;
        }

        if (smallest != index)
        {
            Swap(index, smallest);
            HeapifyDown(smallest);
        }
    }

    private void Swap(int indexA, int indexB)
    {
        int temp = items[indexA];
        items[indexA] = items[indexB];
        items[indexB] = temp;
    }

    public void Dispose()
    {
        items.Dispose();
    }
}