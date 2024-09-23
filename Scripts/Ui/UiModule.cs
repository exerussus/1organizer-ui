using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.Ui
{
    [Serializable]
    public abstract class UiModule
    {
        public abstract string Name { get; protected set; }
        public abstract string ResourcePath { get; protected set; }
        public abstract string Group { get; protected set; }
        public IObjectUI UIObject { get; private set; }
        private Transform _parent;
        private GameObject _loadedInstance;
        private AsyncOperationHandle<GameObject> _handle;
        public bool IsActivated { get; private set; }

        public virtual void Hide()
        {
            IsActivated = false;
            if (UIObject == null) return;
            UIObject.Deactivate();
        }
        
        public virtual void Show(ShareData shareData, Transform transform)
        {
            if (UIObject == null) Load(shareData, transform);
            
            if (UIObject != null)
            {
                IsActivated = true;
                UIObject.Activate();
            }
        }
        
        public virtual void UpdateModule()
        {
            UIObject?.UpdateObject();
        }

        public virtual async void Load(ShareData shareData, Transform transform)
        {
           _parent = transform;
           var loadResult = await LoadAndInstantiateAsync(ResourcePath, _parent);
           _loadedInstance = loadResult.instance;
           _handle = loadResult.handle;

           if (_loadedInstance == null) return;
           UIObject = _loadedInstance.GetComponent<IObjectUI>();
           UIObject.Initialize(shareData);
           UIObject.Activate();
        }

        public virtual void Unload()
        {
            UIObject = null;
            
            if (_loadedInstance != null)
            {
                Object.Destroy(_loadedInstance);
                _loadedInstance = null;
            }

            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
                _handle = default;
            }
        }
        
        public static async Task<(GameObject instance, AsyncOperationHandle<GameObject> handle)> LoadAndInstantiateAsync(string address, Transform parentTransform)
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
    }
}
