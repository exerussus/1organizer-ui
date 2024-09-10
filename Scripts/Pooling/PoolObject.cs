using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Plugins.Exerussus._1OrganizerUI.Scripts
{
    public class AddressablePoolObject<T> where T : MonoBehaviour
    {
        private T _prefab;
        private const int DefaultObjectCount = 5;
        private readonly Queue<T> _freeObjects = new();
        private readonly HashSet<T> _usedObjects = new();
        private Transform _parent;
        private string _elementName;
        private int _count;
        private AsyncOperationHandle<GameObject> _handle;
        public bool IsReady { get; private set; }
        
        public async Task InitPrefab(string name, string prefabPath, bool dontDestroyOnLoad, int count = DefaultObjectCount)
        {
            _parent = new GameObject { name = $"{name} pool" }.transform;
            
            if (dontDestroyOnLoad) Object.DontDestroyOnLoad(_parent);
            
            var loadResult = await LoadAndInstantiateAsync(prefabPath, _parent);
            
            IsReady = true;
            _prefab = loadResult.instance.GetComponent<T>();
            _handle = loadResult.handle;
            
            _prefab.gameObject.SetActive(false);
            _prefab.name = $"{name} prefab";
            _elementName = $"{name} element";
            
            for (int i = 0; i < count; i++)
            {
                var element = CreateNewObject();
                element.gameObject.SetActive(false);
                _freeObjects.Enqueue(element);
            }
        }

        public virtual void Unload()
        {
            foreach (var element in _freeObjects) Object.Destroy(element.gameObject);
            foreach (var element in _usedObjects) Object.Destroy(element.gameObject);
            
            _prefab = null;
            
            if (_prefab != null)
            {
                Object.Destroy(_prefab.gameObject);
                _prefab = null;
            }

            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
                _handle = default;
            }
        }
        
        private static async Task<(GameObject instance, AsyncOperationHandle<GameObject> handle)> LoadAndInstantiateAsync(string address, Transform parentTransform)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Address is null or empty.");
                return (null, default);
            }
            
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var prefab = handle.Result;
                var instance = Object.Instantiate(prefab, parentTransform);
                return (instance, handle);
            }
            else
            {
                Debug.LogError($"Failed to load asset with address: {address}");
                return (null, default);
            }
        }

        public T GetObject(Vector3 position, Quaternion rotation)
        {
            var pooledObject = _freeObjects.Count > 0 ? _freeObjects.Dequeue() : CreateNewObject();
            pooledObject.transform.SetPositionAndRotation(position, rotation);
            pooledObject.gameObject.SetActive(true);
            return pooledObject;
        }

        public T GetObject(Vector3 position, Vector2 scale, Quaternion rotation)
        {
            var pooledObject = _freeObjects.Count > 0 ? _freeObjects.Dequeue() : CreateNewObject();
            pooledObject.transform.SetPositionAndRotation(position, rotation);
            pooledObject.gameObject.transform.localScale = scale;
            pooledObject.gameObject.SetActive(true);
            return pooledObject;
        }

        public T GetObject(Vector3 position, Vector2 scale)
        {
            var pooledObject = _freeObjects.Count > 0 ? _freeObjects.Dequeue() : CreateNewObject();
            pooledObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            pooledObject.gameObject.transform.localScale = scale;
            pooledObject.gameObject.SetActive(true);
            return pooledObject;
        }

        public T GetObject(Vector3 position, Transform parent, Quaternion rotation)
        {
            var pooledObject = _freeObjects.Count > 0 ? _freeObjects.Dequeue() : CreateNewObject();
            pooledObject.transform.SetParent(parent);
            pooledObject.transform.SetPositionAndRotation(position, rotation);
            pooledObject.gameObject.transform.localScale = Vector3.one;
            pooledObject.gameObject.SetActive(true);
            return pooledObject;
        }

        public void ReleaseObject(T element)
        {
            element.gameObject.SetActive(false);
            _freeObjects.Enqueue(element);
        }

        public void ReleaseObjectAndResetParent(T element)
        {
            element.transform.SetParent(_parent);
            element.gameObject.SetActive(false);
            _freeObjects.Enqueue(element);
        }

        private T CreateNewObject()
        {
            _count++;
            var newObject = Object.Instantiate(_prefab, _parent).GetComponent<T>();
            newObject.name = $"{_elementName} {_count}";
            return newObject;
        }
    }
}
