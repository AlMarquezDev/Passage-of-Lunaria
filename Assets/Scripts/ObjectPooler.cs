using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        [Tooltip("Identificador único para esta piscina (ej. 'GhostTrail', 'HitVFX').")]
        public string tag;
        [Tooltip("El Prefab que esta piscina contendrá.")]
        public GameObject prefab;
        [Tooltip("Número inicial de objetos a crear en la piscina.")]
        public int size;
    }

    #region Singleton
    public static ObjectPooler Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    [Tooltip("Lista de todas las piscinas de objetos gestionadas.")]
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, GameObject> prefabDictionary;
    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>();
        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"[ObjectPooler] El Prefab para la piscina con tag '{pool.tag}' no está asignado.", this);
                continue;
            }
            if (string.IsNullOrEmpty(pool.tag))
            {
                Debug.LogError($"[ObjectPooler] Una piscina con prefab '{pool.prefab.name}' no tiene tag asignado.", this);
                continue;
            }
            if (poolDictionary.ContainsKey(pool.tag))
            {
                Debug.LogWarning($"[ObjectPooler] Ya existe una piscina con el tag '{pool.tag}'. Ignorando duplicado.", this);
                continue;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false); obj.transform.SetParent(this.transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
            prefabDictionary.Add(pool.tag, pool.prefab);
        }
        Debug.Log($"[ObjectPooler] Inicializado con {poolDictionary.Count} piscinas.");
    }

    public GameObject GetPooledObject(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPooler] Piscina con tag '{tag}' no existe.", this);
            return null;
        }

        if (poolDictionary[tag].Count > 0)
        {
            GameObject objectToSpawn = poolDictionary[tag].Dequeue();
            return objectToSpawn;
        }
        else
        {
            if (prefabDictionary.TryGetValue(tag, out GameObject prefabToSpawn))
            {
                Debug.LogWarning($"[ObjectPooler] Piscina '{tag}' vacía, instanciando uno nuevo (considera aumentar tamaño inicial).", this);
                GameObject obj = Instantiate(prefabToSpawn);
                obj.transform.SetParent(this.transform); return obj;
            }
            else
            {
                Debug.LogError($"[ObjectPooler] Piscina '{tag}' vacía y no se pudo encontrar prefab para instanciar.", this);
                return null;
            }
        }
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (objectToReturn == null) return;
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPooler] Intento de devolver objeto a piscina inexistente '{tag}'. Objeto: {objectToReturn.name}. Destruyendo objeto.", objectToReturn);
            Destroy(objectToReturn); return;
        }

        objectToReturn.SetActive(false);

        poolDictionary[tag].Enqueue(objectToReturn);
    }
}