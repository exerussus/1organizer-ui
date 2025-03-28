using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exerussus._1Extensions.Async;
using Exerussus._1Extensions.SmallFeatures;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.Ui
{
    public abstract class OrganizerUI<TModule> : MonoBehaviour
    where TModule : UiModule, new()
    {
        [SerializeField] protected bool autoStart;
        [SerializeField] protected bool dontDestroyOnLoad;
        [SerializeField] protected GameShare shareData;
        [SerializeField] protected List<TModule> modules = new();
        [SerializeField] protected List<string> enabledModules = new();
        [SerializeField] protected List<string> disabledModules = new();
        [SerializeField] protected Transform _parentTransform;
        
        private readonly HashSet<string> _enabledModules = new();
        private readonly HashSet<string> _disabledModules = new();
        private Dictionary<string, TModule> _modulesDict;
        private Dictionary<string, List<TModule>> _groupsDict;

        public abstract IAssetProvider AssetProvider { get; }
        public abstract JobHandler JobHandler { get; }

        public Transform ParentTransform
        {
            get => _parentTransform;
            set => _parentTransform = value;
        }

        protected virtual GameShare GetGameShare()
        {
            return new GameShare();
        }

        public void Start()
        {
            if (autoStart) _ = Initialize();
        }

        public async Task Initialize()
        {
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            shareData = GetGameShare();
            _modulesDict = new();
            _groupsDict = new();
            shareData.AddSharedObject(typeof(IAssetProvider), AssetProvider);
            shareData.AddSharedObject(AssetProvider.GetType(), AssetProvider);
            SetShareData(shareData);
            
            shareData.AddSharedObject(new OrganizerActions{ Sorting = Sort});

            await TaskUtils.WaitUntilAsync(() => AssetProvider.IsLoaded);

            var allPacks = AssetProvider.GetPacksByType(AssetProviding.AssetConstants.UiPanel);
            if (allPacks.Count > 0)
            {
                foreach (var panelUiPack in allPacks)
                {
                    if (!panelUiPack.TryGetMetaInfo(out PanelUiMetaInfo metaInfo)) continue;
                    
                    var newModule = new TModule();
                    var handle = new UiModule.UiModuleHandle(newModule);
                    handle.panelUiMetaInfo = metaInfo;
                    handle.assetReferencePack = panelUiPack;
                    modules.Add(newModule);
                    _modulesDict[panelUiPack.Id] = newModule;
                    OnPanelUiPackApply(handle, panelUiPack, metaInfo);
                }
            }

            foreach (var uiModule in modules) uiModule.Inject(shareData);
            
            await JobHandler.AddJobAsync(PreInitialize, "OrganizerUI.PreInitialize");
            
            foreach (var uiModule in modules)
            {
                _modulesDict[uiModule.Name] = uiModule;
                
                if (_groupsDict.ContainsKey(uiModule.Group)) _groupsDict[uiModule.Group].Add(uiModule);
                else _groupsDict.Add(uiModule.Group, new List<TModule> { uiModule });
            }

            await JobHandler.AddJobAsync(OnInitialize, "OrganizerUI.OnInitialize");
            
            foreach (var uiModule in modules)
            {
                if (_enabledModules.Contains(uiModule.Name)) _ =  uiModule.ShowAsync(shareData, _parentTransform);
                else _disabledModules.Add(uiModule.Name);
            }
        }

        protected virtual void OnPanelUiPackApply(UiModule.UiModuleHandle moduleHandle, IAssetReferencePack pack, PanelUiMetaInfo metaInfo)
        {
            
        }
        
        private void Sort()
        {
            var sortedModules = modules
                .Where(m => m.LoadedInstance != null) 
                .OrderBy(m => m.Order)                 
                .ToList();
            
            for (int i = 0; i < sortedModules.Count; i++)
            {
                sortedModules[i].LoadedInstance.transform.SetSiblingIndex(i);
            }

            OnSort();
        }
        
        protected virtual void OnSort() {}
        
        private void MoveToEnabledList(string uiName)
        {
            if (_disabledModules.Contains(uiName))
            {
                _disabledModules.Remove(uiName);
                _enabledModules.Add(uiName);
                enabledModules = new List<string>(_enabledModules);
                disabledModules = new List<string>(_disabledModules);
            }
        }

        private void MoveToDisabledList(string uiName)
        {
            if (_enabledModules.Contains(uiName))
            {
                _enabledModules.Remove(uiName);
                _disabledModules.Add(uiName);
                enabledModules = new List<string>(_enabledModules);
                disabledModules = new List<string>(_disabledModules);
            }
        }

        public void ShowModule(string uiName)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                if (_enabledModules.Contains(uiName))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(uiModule.Name);
                uiModule.Show(shareData, _parentTransform);
            }
        }

        public void ShowModule(string uiName, Action onLoad)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                if (_enabledModules.Contains(uiName))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(uiModule.Name);
                uiModule.Show(shareData, _parentTransform, onLoad);
            }
        }

        public void ShowModule(string uiName, Action<GameObject> onLoad)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                if (_enabledModules.Contains(uiName))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(uiModule.Name);
                uiModule.Show(shareData, _parentTransform, onLoad);
            }
        }

        public async Task ShowModuleAsync(string uiName)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                if (_enabledModules.Contains(uiName))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(uiModule.Name);
                await uiModule.ShowAsync(shareData, _parentTransform);
            }
        }

        public void HideModule(string uiName)
        {
            if (_disabledModules.Contains(uiName)) return;
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                MoveToDisabledList(uiModule.Name);
                uiModule.Hide();
            }
        }

        public TModule GetModule(string uiName)
        {
            return _modulesDict[uiName];
        }

        public bool TryGetModule(string uiName, out TModule module)
        {
            if (_modulesDict.TryGetValue(uiName, out TModule resultModule))
            {
                module = resultModule;
                return true;
            }

            module = null;
            return false;
        }

        public void HideGroup(string groupName)
        {
            if (!_groupsDict.TryGetValue(groupName, out var group)) return;
            
            foreach (var uiModule in group)
            {
                if (_disabledModules.Contains(uiModule.Name)) continue;

                MoveToDisabledList(uiModule.Name);
                uiModule.Hide();
            }
        }
        
        public void UnloadModule(string uiName)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                if (_enabledModules.Contains(uiModule.Name))
                {
                    MoveToDisabledList(uiModule.Name);
                    uiModule.Hide();
                }
                uiModule.Unload();
            }
        }
        
        public void UnloadGroup(string groupName)
        {
            if (_groupsDict.TryGetValue(groupName, out var group)) foreach (var uiModule in group)
            {
                if (_enabledModules.Contains(uiModule.Name))
                {
                    MoveToDisabledList(uiModule.Name);
                    uiModule.Hide();
                }
                uiModule.Unload();
            }
        }
        
        public void UpdateModule(string uiName)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule)) uiModule.UpdateModule();
        }

        public void UpdateGroup(string groupName)
        {
            if(_groupsDict.TryGetValue(groupName, out var group)) foreach (var uiModule in group) uiModule.UpdateModule();
        }

        public void ShowGroup(string groupName)
        {
            if(_groupsDict.TryGetValue(groupName, out var group)) foreach (var uiModule in group)
            {
                MoveToEnabledList(uiModule.Name);
                uiModule.Show(shareData, _parentTransform);
            }
        }

        public bool IsEnabledModule(string uiName)
        {
            return _enabledModules.Contains(uiName);
        }

        protected virtual void PreInitialize() {}
        protected virtual void OnInitialize() {}
        protected abstract void SetShareData(GameShare shareData);

    }
    public class OrganizerActions
    {
        public Action Sorting;
    }
}
