
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts
{
    [Serializable]
    public abstract class UiModule
    {
        public abstract string Name { get; protected set; }
        public abstract string ResourcePath { get; protected set; }
        public abstract string Group { get; protected set; }
        public IObjectUI UIObject { get; private set; }
        private Transform _parent;

        public virtual void Hide()
        {
            if (UIObject == null) return;
            UIObject.Deactivate();
        }
        
        public virtual void Show(ShareData shareData, Transform transform)
        {
            if (UIObject == null) Load(shareData, transform);
            UIObject?.Activate();
        }
        
        public virtual void UpdateModule()
        {
            UIObject?.UpdateObject();
        }

        public virtual async void Load(ShareData shareData, Transform transform)
        {
           _parent = transform;
           var loadObject = await LoadAndInstantiateAsync(ResourcePath, _parent);
           if (loadObject == null) return;
           UIObject = loadObject.GetComponent<IObjectUI>();
           UIObject.Initialize(shareData);
        }
        
        public static async Task<GameObject> LoadAndInstantiateAsync(string address, Transform parentTransform)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Address is null or empty.");
                return null;
            }
            
            GameObject prefab = await LoadResourceAsync(address);
            
            if (prefab == null)
            {
                Debug.LogError($"Failed to load resource at address: {address}");
                return null;
            }
            
            var instance = Object.Instantiate(prefab, parentTransform);
            
            return instance;
        }
        
        private static async Task<GameObject> LoadResourceAsync(string address)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Debug.LogError($"Failed to load asset with address: {address}");
                return null;
            }
        }
    }
}