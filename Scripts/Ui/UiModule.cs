using System;
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
        public string Name => _assetReferencePack.Id;
        public string Group { get => _panelUiMetaInfo.group; protected set => _panelUiMetaInfo.group = value; }
        public int Order { get => _panelUiMetaInfo.order; protected set => _panelUiMetaInfo.order = value; }
        public IObjectUI UIObject { get; private set; }
        private PanelUiMetaInfo _panelUiMetaInfo;
        private IAssetReferencePack _assetReferencePack;
        private IAssetProvider _assetProvider;
        private Transform _parent;
        private GameObject _loadedInstance;
        private GameShare _mSharedData;
        public GameObject LoadedInstance => _loadedInstance;
        private bool _isLoading;
        
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
           
           var (result, asset) = await _assetProvider.TryLoadAssetPackContentAsync<GameObject>(Name);
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
           
           var (result, asset) = await _assetProvider.TryLoadAssetPackContentAsync<GameObject>(Name);
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
           
           var (result, asset) = await _assetProvider.TryLoadAssetPackContentAsync<GameObject>(Name);
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
                _assetProvider.UnloadAssetPack(Name);
            }
        }

        public class UiModuleHandle
        {
            public UiModuleHandle(UiModule module)
            {
                uiModule = module;
            }

            public UiModule uiModule;

            public PanelUiMetaInfo panelUiMetaInfo
            {
                get => uiModule._panelUiMetaInfo;
                set => uiModule._panelUiMetaInfo = value;
            }

            public IAssetReferencePack assetReferencePack
            {
                get => uiModule._assetReferencePack;
                set => uiModule._assetReferencePack = value;
            }
        }
    }
}
