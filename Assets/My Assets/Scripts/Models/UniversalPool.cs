using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;


/// <summary>
/// Pool can be moved to namespace + static functions to use it in any script.
/// I'm wanna use variant with hard link.
/// </summary>
public class UniversalPool : MonoBehaviour
{
    #region Variables
    [Header("Pool settings:")]
    [SerializeField]
    [Tooltip("Automatically initialize pool after game start?")]
    private bool _autoInitialize = true;

    [SerializeField]
    [Tooltip("How much start copies of objects need have in pool after game start?")]
    private int _prewarmCount = 0;


    [Header("Objects settings:")]
    [SerializeField]
    [Tooltip("Turn off inactive objects?")]
    private bool _turnOffInactive = true;

    [SerializeField]
    [Tooltip("Prefabs hide flags after create")]
    private HideFlags _defaultHideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;


    [System.Serializable]
    public struct PoolRootEntry
    {
        [Tooltip("Category Name")]
        public string key;
        public PoolEntry[] prefabs;
    }

    [System.Serializable]
    public struct PoolEntry
    {
        [Tooltip("Prefab Name (can be empty)")]
        public string key;
        public GameObject prefab;
    }

    [SerializeField]
    [Tooltip("List of prefabs to add to the pool upon start")]
    [Header("Objects tree:")]
    private PoolRootEntry[] _prefabsTree;       // user-friendly prefabs view

    [SerializeField]    // !!! Serialization is required !!! Otherwise, we will not be able to save the array after sorting!
    [HideInInspector]
    private PoolEntry[] _prefabsPool;           // code-friendly prefabs array

    private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, List<GameObject>> _pool = new Dictionary<string, List<GameObject>>();
    private readonly Dictionary<GameObject, string> _activeObjects = new Dictionary<GameObject, string>();

    private bool _wokeUp = false;
    #endregion

    #region UI Editor - SortAndSave
#if UNITY_EDITOR
    /// <summary>
    /// Sort user-friendly prefabs tree, remove empty elements, update system code-friendly prefabs array
    /// </summary>
    [ExecuteInEditMode]
    public void SortAndSaveData()
    {

        // Sort user-friendly prefabs list and remove empty elements
        List<PoolRootEntry> rootEntryToDelete = new List<PoolRootEntry>();
        List<PoolEntry> entryToDelete = new List<PoolEntry>();

        for (int i = 0; i < _prefabsTree.Count(); i++)
        {
            if (_prefabsTree[i].prefabs.Length == 0)  // if rootEntry is empty - removing this category
            {
                Debug.Log("Find empty category! NameKey is [ " + _prefabsTree[i].key + " ] ! </br>Removing...");
                rootEntryToDelete.Add(_prefabsTree[i]);
            }
            else
            {
                entryToDelete.Clear();  // we have multyple entry lists, but single rootEntry list. So we need to clear Only entryToDelete.

                for (int i2 = 0; i2 < _prefabsTree[i].prefabs.Count(); i2++)
                {
                    if (_prefabsTree[i].prefabs[i2].prefab == null)
                    {
                        Debug.Log("Find prefab entry with empty prefab link! Category [ " + _prefabsTree[i].key + " ], prefab key is [ " + _prefabsTree[i].prefabs[i2].key + " ] ! </br>Removing...");
                        entryToDelete.Add(_prefabsTree[i].prefabs[i2]);
                        continue;
                    }

                    if (_prefabsTree[i].prefabs[i2].key == "")
                        _prefabsTree[i].prefabs[i2].key = _prefabsTree[i].prefabs[i2].prefab.name;

                }
                List<PoolEntry> entryListModificed = _prefabsTree[i].prefabs.ToList();

                for (int i2 = 0; i2 < entryToDelete.Count; i2++)
                {
                    entryListModificed.Remove(entryToDelete[i2]);
                }
                _prefabsTree[i].prefabs = entryListModificed.ToArray();
                _prefabsTree[i].prefabs = _prefabsTree[i].prefabs.OrderBy(x => x.key).ToArray();
            }
            List<PoolRootEntry> rootEntryListModificed = _prefabsTree.ToList();

            for (int i2 = 0; i2 < rootEntryToDelete.Count; i2++)
            {
                rootEntryListModificed.Remove(rootEntryToDelete[i2]);
            }
            _prefabsTree = rootEntryListModificed.ToArray();
        }
        _prefabsTree = _prefabsTree.OrderBy(x => x.key).ToArray();

        // fill system-fiendly prefabs list from sorted user-friendly prefabs list
        List<PoolEntry> prefabsFlatArray = new List<PoolEntry>();
        for (int i = 0; i < _prefabsTree.Length; i++)
        {
            for (int i2 = 0; i2 < _prefabsTree[i].prefabs.Length; i2++)
            {
                if (!prefabsFlatArray.Contains(_prefabsTree[i].prefabs[i2]))
                {
                    prefabsFlatArray.Add(_prefabsTree[i].prefabs[i2]);
                }
                else
                {
                    Debug.LogWarning("You have a duplicated version of the prefab with the [ " + _prefabsTree[i].prefabs[i2].prefab.name + " ] name!");
                }
            }
        }
        _prefabsPool = prefabsFlatArray.ToArray();
        Debug.Log(_prefabsPool.Length + " | " + prefabsFlatArray.Count);

        // mark script dirty to set editor know that we updated data and need to save scene
        EditorUtility.SetDirty(gameObject);
    }
#endif
    #endregion

    /// <summary>
    /// Standart Unity Start method
    /// </summary>
    private void Start()
    {
        if (_autoInitialize) Init();
    }

    /// <summary>
    /// Initialising.
    /// </summary>
    private void Init()
    {
        if (_wokeUp)
        {
            Debug.LogError("<b>Warning!</b> Double call Init function for Universal Pool!");
            return;
        }
        _wokeUp = true;

        if (_prefabsTree.Length == 0)
        {
            Debug.LogError("Prefabs Tree empty! Please, click Sort&Save before use pool system!");
            return;
        }
        for (int i = 0; i < _prefabsTree.Length; i++)
        {
            List<string> prefabsKeysList = new List<string>();
            for (int i2 = 0; i2 < _prefabsTree[i].prefabs.Length; i2++)
            {
                prefabsKeysList.Add(_prefabsTree[i].prefabs[i2].key);
            }
        }

        if (_prefabsPool == null)
        {
            Debug.LogError("Prefabs Pool dont exist! Please, click Sort&Save before use pool system!");
            return;
        }
        for (int i = 0; i < _prefabsPool.Length; i++)
        {
            AddPrefab(_prefabsPool[i].key, _prefabsPool[i].prefab);
        }

        if (_prewarmCount > 0) PrewarmPool();

        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// Preliminary generation of objects into the pool so that you do not have to fill the pool during the game.
    /// </summary>
    private void PrewarmPool()
    {
        if (_prewarmCount == 0) return;

        for (int i = 0; i < _prefabsPool.Length; i++)
        {
            List<GameObject> toReturn = new List<GameObject>();
            for (int i2 = 0; i2 < _prewarmCount; i2++)
            {
                toReturn.Add(GetObject(_prefabsPool[i].key));
            }
            for (int i2 = 0; i2 < toReturn.Count; i2++)
            {
                Return(toReturn[i2]);
            }
        }
    }


    /// <summary>
    /// Add (or replace) a prefab with a key and source object
    /// </summary>
    /// <param name="key">Unique key</param>
    /// <param name="prefab">Source object</param>
    private void AddPrefab(string key, GameObject prefab)
    {
        if (_prefabs.ContainsKey(key)) Debug.LogWarning("Pool already contains [ " + key + " ] key!");
        _prefabs[key] = prefab;
    }

    /// <summary>
    /// Returns a list of cached objects of the desired key
    /// If list not existing - creating it.
    /// </summary>
    /// <param name="key">Required key</param>
    /// <returns></returns>
    private List<GameObject> GetCacheList(string key)
    {
        List<GameObject> list;
        if (!_pool.TryGetValue(key, out list))
        {
            list = new List<GameObject>();
            _pool[key] = list;
        }

        return list;
    }


    #region Get Object / List of category

    /// <summary>
    /// Get an object from the cache, or create new if not found in cache
    /// </summary>
    /// <param name="key">Required key</param>
    /// <returns>Instance of the prefab or null if catched any error</returns>
    public GameObject GetObject(string key)
    {
        List<GameObject> list = GetCacheList(key);
        GameObject pooledObject;

        if (list.Count == 0)
        {
            GameObject prefab;

            if (!_prefabs.TryGetValue(key, out prefab))
            {
                Debug.LogError("Cant find prefab [ " + key + " ] in cache!");
                return null;
            }

            pooledObject = Instantiate(prefab);
            pooledObject.hideFlags = _defaultHideFlags;
        }
        else
        {
            int index = list.Count - 1;
            pooledObject = list[index];
            list.RemoveAt(index);
        }

        ActivateObject(pooledObject, key);

        return pooledObject;
    }

    /// <summary>
    /// Activate the object when asked for it. Also adding it to "Active objects" dictionary.
    /// </summary>
    /// <param name="objectToActivate">Object</param>
    /// <param name="key">Key that will be stored as value in Active Objects list</param>
    private void ActivateObject(GameObject objectToActivate, string key)
    {
        if (objectToActivate == null) Debug.LogError("Object with a [ " + key + " ] key not exist!");

        objectToActivate.SetActive(true);

        if (_activeObjects.ContainsKey(objectToActivate)) Debug.LogWarning("Object with a [ " + key + " ] already existing in active objects dictionary!");
        _activeObjects[objectToActivate] = key;
    }
    #endregion


    #region Return Object

    /// <summary>
    /// Returns object to cache pool
    /// </summary>
    /// <param name="objectToReturn">Object to return</param>
    /// <returns>Return completed or not</returns>
    public bool Return(GameObject objectToReturn)
    {
        string key;
        if (!_activeObjects.TryGetValue(objectToReturn, out key))
        {
            Debug.LogError("Object not finded in Active Objects Dictionary!");
            return false;
        }

        return Return(objectToReturn, key);
    }

    /// <summary>
    /// Returns object to cache list with requred key
    /// </summary>
    /// <param name="objectToReturn">Object to return</param>
    /// <param name="key">Key of list to return</param>
    /// <returns>True if return completed, false if any error</returns>
    public bool Return(GameObject objectToReturn, string key)
    {
        if (objectToReturn == null)
        {
            Debug.LogError("Object to return dosn't exist!");
            return false;
        }

        List<GameObject> list;
        if (!_pool.TryGetValue(key, out list))
        {
            Debug.LogError("List for the requested key doesnt exist!");
            return false;
        }

        list.Add(objectToReturn);
        _activeObjects.Remove(objectToReturn);
        if (_pool.TryGetValue(key, out list))
        {
            list.Remove(objectToReturn);
        }

        if (_turnOffInactive) objectToReturn.SetActive(false);

        return true;
    }
    #endregion

}
