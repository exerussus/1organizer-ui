﻿using System;
using System.Threading.Tasks;
using Exerussus._1Extensions.SmallFeatures;
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
        public abstract int Order { get; protected set; }
        public IObjectUI UIObject { get; private set; }
        private Transform _parent;
        private GameObject _loadedInstance;
        private GameShare _mSharedData;
        public GameObject LoadedInstance => _loadedInstance;

        private AsyncOperationHandle<GameObject> _handle;
        private bool _isLoading;
        
        public bool IsActivated { get; private set; }
        
        public virtual void Hide()
        {
            if (UIObject == null) return;
            if (!IsActivated) return; 
            IsActivated = false;
            UIObject.Deactivate();
        }
        
        public virtual void Show(GameShare shareData, Transform transform, Action onLoad = null)
        {
            if (IsActivated) UIObject.UpdateObject();
            
            if (UIObject == null) Load(shareData, transform, onLoad);
            
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

        public virtual async void Load(GameShare shareData, Transform transform, Action onLoad)
        {
            if (_isLoading) return;
            _mSharedData = shareData;
           _parent = transform;
           _isLoading = true;
           var loadResult = await LoadAndInstantiateAsync(ResourcePath, _parent);
           _loadedInstance = loadResult.instance;
           _handle = loadResult.handle;

           if (_loadedInstance == null) return;
           UIObject = _loadedInstance.GetComponent<IObjectUI>();
           UIObject.Initialize(shareData);
           UIObject.Activate();
           onLoad?.Invoke();
           _mSharedData.GetSharedObject<OrganizerActions>().Sorting.Invoke();
           IsActivated = true;
           _isLoading = false;
        }

        public virtual void Unload()
        {
            UIObject = null;
            IsActivated = false;
            
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
