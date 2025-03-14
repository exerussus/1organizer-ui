﻿using System;
using System.Threading.Tasks;
using Exerussus._1Extensions.Abstractions;
using Exerussus._1Extensions.SmallFeatures;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.Ui
{
    [Serializable]
    public abstract class UiModule : IInjectable
    {
        public abstract string Name { get; protected set; }
        public abstract string Group { get; protected set; }
        public abstract int Order { get; protected set; }
        public IObjectUI UIObject { get; private set; }
        private IAssetProvider _assetProvider;
        private Transform _parent;
        private GameObject _loadedInstance;
        private GameShare _mSharedData;
        public GameObject LoadedInstance => _loadedInstance;
        private bool _isLoading;
        private bool _initedByAssetProvider;
        
        public bool IsActivated { get; private set; }

        public void Inject(GameShare gameShare)
        {
            gameShare.GetSharedObject(ref _assetProvider);
            OnInjectSharedData(gameShare);
        }
        
        public virtual void OnInjectSharedData(GameShare gameShare) {}
        
        public virtual void Hide()
        {
            if (UIObject == null) return;
            if (!IsActivated) return; 
            IsActivated = false;
            UIObject.Deactivate();
        }

        public virtual void Show(GameShare shareData, Transform transform)
        {
            if (UIObject == null) _ = Load(shareData, transform);

            if (UIObject != null)
            {
                if (IsActivated)
                {
                    UIObject.UpdateObject();
                    return;
                }

                IsActivated = true;
                UIObject.Activate();
            }
        }

        public virtual void Show(GameShare shareData, Transform transform, Action onLoad)
        {
            if (UIObject == null) Load(shareData, transform, onLoad);

            if (UIObject != null)
            {
                if (IsActivated)
                {
                    UIObject.UpdateObject();
                    return;
                }

                IsActivated = true;
                UIObject.Activate();
            }
        }

        public virtual void Show(GameShare shareData, Transform transform, Action<GameObject> onLoad)
        {
            if (UIObject == null) Load(shareData, transform, onLoad);

            if (UIObject != null)
            {
                if (IsActivated)
                {
                    UIObject.UpdateObject();
                    return;
                }

                IsActivated = true;
                UIObject.Activate();
            }
        }

        public virtual async Task ShowAsync(GameShare shareData, Transform transform)
        {
            if (UIObject == null) await Load(shareData, transform);

            if (UIObject != null)
            {
                if (IsActivated)
                {
                    UIObject.UpdateObject();
                    return;
                }

                IsActivated = true;
                UIObject.Activate();
            }
        }

        public virtual void UpdateModule()
        {
            UIObject?.UpdateObject();
        }

        public virtual async Task Load(GameShare shareData, Transform transform)
        {
            if (_isLoading) return;
            _mSharedData = shareData;
           _parent = transform;
           _isLoading = true;
           
           var (result, asset) = _initedByAssetProvider ? await _assetProvider.TryLoadUiPanelAsync(Name) : await _assetProvider.TryLoadAssetPackAsync<GameObject>(Name);
           if (!result) return;
           
           _loadedInstance = Object.Instantiate(asset, _parent);

           if (_loadedInstance == null) return;
           UIObject = _loadedInstance.GetComponent<IObjectUI>();
           UIObject.Initialize(shareData);
           UIObject.Activate();
           _mSharedData.GetSharedObject<OrganizerActions>().Sorting.Invoke();
           IsActivated = true;
           _isLoading = false;
        }

        public virtual async void Load(GameShare shareData, Transform transform, Action onLoad)
        {
            if (_isLoading) return;
            _mSharedData = shareData;
           _parent = transform;
           _isLoading = true;
           
           var (result, asset) = _initedByAssetProvider ? await _assetProvider.TryLoadUiPanelAsync(Name) : await _assetProvider.TryLoadAssetPackAsync<GameObject>(Name);
           if (!result) return;
           
           _loadedInstance = Object.Instantiate(asset, _parent);

           if (_loadedInstance == null) return;
           UIObject = _loadedInstance.GetComponent<IObjectUI>();
           UIObject.Initialize(shareData);
           UIObject.Activate();
           onLoad?.Invoke();
           _mSharedData.GetSharedObject<OrganizerActions>().Sorting.Invoke();
           IsActivated = true;
           _isLoading = false;
        }

        public virtual async void Load(GameShare shareData, Transform transform, Action<GameObject> onLoad)
        {
            if (_isLoading) return;
            _mSharedData = shareData;
           _parent = transform;
           _isLoading = true;
           
           var (result, asset) = _initedByAssetProvider ? await _assetProvider.TryLoadUiPanelAsync(Name) : await _assetProvider.TryLoadAssetPackAsync<GameObject>(Name);
           if (!result) return;
           
           _loadedInstance = Object.Instantiate(asset, _parent);

           if (_loadedInstance == null) return;
           UIObject = _loadedInstance.GetComponent<IObjectUI>();
           UIObject.Initialize(shareData);
           UIObject.Activate();
           onLoad?.Invoke(_loadedInstance);
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
                if (_initedByAssetProvider) _assetProvider.UnloadUiPanel(Name);
                else _assetProvider.UnloadAssetPack(Name);
            }
        }

        public class UiModuleHandle
        {
            public UiModuleHandle(UiModule module)
            {
                uiModule = module;
            }

            public UiModule uiModule;
            
            public string name
            {
                get => uiModule.Name;
                set => uiModule.Name = value;
            }
            
            public string group
            {
                get => uiModule.Group;
                set => uiModule.Group = value;
            }
            
            public int order
            {
                get => uiModule.Order;
                set => uiModule.Order = value;
            }
            
            public bool initedByAssetProvider
            {
                get => uiModule._initedByAssetProvider;
                set => uiModule._initedByAssetProvider = value;
            }
        }
    }
}
