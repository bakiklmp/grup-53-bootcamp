using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 100;
    }

    [Tooltip("Configure prefabs to be pooled on startup.")]
    public List<PoolConfig> initialPools = new List<PoolConfig>();

    private Dictionary<GameObject, IObjectPool<GameObject>> _poolDictionary = new Dictionary<GameObject, IObjectPool<GameObject>>();
    private Dictionary<GameObject, Transform> _poolContainers = new Dictionary<GameObject, Transform>();
    private Transform _mainPoolContainer; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;


        _mainPoolContainer = new GameObject("POOL MANAGER CONTAINERS").transform;
        _mainPoolContainer.SetParent(this.transform); 

        foreach (var config in initialPools)
        {
            if (config.prefab != null)
            {
                CreatePool(config.prefab, config.initialSize, config.maxSize);
            }
            else
            {
                Debug.LogWarning("PoolConfig has a null prefab. Skipping.");
            }
        }
    }


    public IObjectPool<GameObject> CreatePool(GameObject prefab, int initialSize, int maxSize)
    {
        if (_poolDictionary.ContainsKey(prefab))
        {
            return _poolDictionary[prefab];
        }

        Transform poolSpecificContainer = new GameObject($"{prefab.name}_Pool").transform;
        poolSpecificContainer.SetParent(_mainPoolContainer);
        _poolContainers[prefab] = poolSpecificContainer;

        var pool = new ObjectPool<GameObject>(
            createFunc: () => CreatePooledItem(prefab),
            actionOnGet: (obj) => OnTakeFromPool(obj, prefab),
            actionOnRelease: (obj) => OnReturnedToPool(obj, prefab),
            actionOnDestroy: (obj) => OnDestroyPoolObject(obj),
            collectionCheck: true,  
            defaultCapacity: initialSize,
            maxSize: maxSize
        );

        _poolDictionary[prefab] = pool;


        List<GameObject> preWarmedObjects = new List<GameObject>();
        for (int i = 0; i < initialSize; i++)
        {
            preWarmedObjects.Add(pool.Get());
        }
        foreach (var obj in preWarmedObjects)
        {
            pool.Release(obj);
        }

        Debug.Log($"Pool created for {prefab.name} with initial size {initialSize} and max size {maxSize}.");
        return pool;
    }

    private GameObject CreatePooledItem(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);

        return obj;
    }

    private void OnTakeFromPool(GameObject pooledObject, GameObject prefab)
    {
        PoolableObjectInfo info = pooledObject.GetComponent<PoolableObjectInfo>();
        if (info == null)
        {
            info = pooledObject.AddComponent<PoolableObjectInfo>();
        }
        if (_poolDictionary.TryGetValue(prefab, out var parentPool))
        {
            info.ParentPool = parentPool;
            info.OriginalPrefab = prefab;
        }
        else
        {
            Debug.LogError($"Could not find pool for prefab {prefab.name} when trying to set ParentPool on {pooledObject.name}. This should not happen.");
        }


        pooledObject.transform.SetParent(null);
        pooledObject.SetActive(true);

        IPoolable poolable = pooledObject.GetComponent<IPoolable>();
        poolable?.OnObjectSpawn();
    }

    private void OnReturnedToPool(GameObject pooledObject, GameObject prefab)
    {
        IPoolable poolable = pooledObject.GetComponent<IPoolable>();
        poolable?.OnObjectDespawn();

        pooledObject.SetActive(false);

        if (_poolContainers.TryGetValue(prefab, out Transform container))
        {
            pooledObject.transform.SetParent(container);
        }
        else
        {
            Debug.LogWarning($"No container found for prefab {prefab.name}. Parenting to main pool container.", pooledObject);
            pooledObject.transform.SetParent(_mainPoolContainer);
        }
    }

    private void OnDestroyPoolObject(GameObject pooledObject)
    {
        Destroy(pooledObject);
    }

  
    public GameObject Get(GameObject prefab, Vector3? position = null, Quaternion? rotation = null,
                          bool autoCreatePoolIfMissing = true, int defaultInitialSize = 5, int defaultMaxSize = 20)
    {
        if (prefab == null)
        {
            Debug.LogError("Cannot get object from pool: Prefab is null.");
            return null;
        }

        if (!_poolDictionary.ContainsKey(prefab))
        {
            if (autoCreatePoolIfMissing)
            {
                Debug.LogWarning($"Pool for prefab {prefab.name} not found. Creating one with default settings.");
                CreatePool(prefab, defaultInitialSize, defaultMaxSize);
            }
            else
            {
                Debug.LogError($"Pool for prefab {prefab.name} not found, and autoCreatePoolIfMissing is false.");
                return null;
            }
        }

        GameObject obj = _poolDictionary[prefab].Get();

        if (obj != null)
        {
            if (position.HasValue)
                obj.transform.position = position.Value;
            if (rotation.HasValue)
                obj.transform.rotation = rotation.Value;
        }
        return obj;
    }

 
    public void Release(GameObject instance)
    {
        if (instance == null)
        {
            Debug.LogWarning("Attempted to release a null instance.");
            return;
        }

        PoolableObjectInfo info = instance.GetComponent<PoolableObjectInfo>();
        if (info != null && info.ParentPool != null)
        {
            info.ParentPool.Release(instance);
        }
        else
        {
            Debug.LogWarning($"GameObject {instance.name} cannot be released: No PoolableObjectInfo component found or ParentPool is null. Destroying object instead.", instance);
            Destroy(instance); 
        }
    }





    public void ClearPool(GameObject prefab, bool destroyPool = false)
    {
        if (_poolDictionary.TryGetValue(prefab, out var pool))
        {
            pool.Clear(); 
            if (destroyPool)
            {
                _poolDictionary.Remove(prefab);
                if (_poolContainers.TryGetValue(prefab, out Transform container))
                {
                    Destroy(container.gameObject);
                    _poolContainers.Remove(prefab);
                }
                Debug.Log($"Pool for {prefab.name} cleared and destroyed.");
            }
            else
            {
                Debug.Log($"Pool for {prefab.name} cleared. Inactive objects remain for reuse.");
            }
        }
        else
        {
            Debug.LogWarning($"Cannot clear pool: No pool found for prefab {prefab.name}.");
        }
    }


    public void ClearAllPools(bool destroyPools = false)
    {
        List<GameObject> prefabsToClear = new List<GameObject>(_poolDictionary.Keys);
        foreach (var prefabKey in prefabsToClear)
        {
            ClearPool(prefabKey, destroyPools);
        }
        if (destroyPools)
        {
            Debug.Log("All pools cleared and destroyed.");
        }
        else
        {
            Debug.Log("All pools cleared. Inactive objects remain for reuse.");
        }
    }


}