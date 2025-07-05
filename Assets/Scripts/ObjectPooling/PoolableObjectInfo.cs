using UnityEngine;
using UnityEngine.Pool;

public class PoolableObjectInfo : MonoBehaviour
{
    public IObjectPool<GameObject> ParentPool { get; set; }
    public GameObject OriginalPrefab { get; set; } 

    public void ReleaseToPool()
    {
        if (ParentPool != null)
        {
            ParentPool.Release(this.gameObject);
        }
        else
        {
            Debug.LogWarning($"PoolableObjectInfo on {gameObject.name} does not have a ParentPool assigned. Object will be destroyed instead.", this.gameObject);
            Destroy(this.gameObject);
        }
    }
}