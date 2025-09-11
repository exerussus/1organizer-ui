using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Exerussus._1Extensions.Abstractions;
using Exerussus._1Extensions.Async;
using Exerussus._1Extensions.SmallFeatures;
using Exerussus._1Extensions.ThreadGateFeature;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using Sirenix.OdinInspector;
using Exerussus.GameSharing.Runtime;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.Ui
{
    public abstract class OrganizerUI<TModule> : MonoBehaviour, IInitializableAsync
    where TModule : UiModule, new()
    {
        [SerializeField, FoldoutGroup("Settings")] protected bool autoStart;
        [SerializeField, FoldoutGroup("Settings")] protected bool dontDestroyOnLoad;
        [SerializeField, FoldoutGroup("Settings")] protected Transform _parentTransform;
        
        [SerializeField, FoldoutGroup("DEBUG")] protected GameShare shareData;
        [SerializeField, FoldoutGroup("DEBUG")] protected List<TModule> modules = new();

#if UNITY_EDITOR
        [SerializeField, FoldoutGroup("DEBUG")] protected List<string> enabledModules = new();
        [SerializeField, FoldoutGroup("DEBUG")] protected List<string> disabledModules = new();
#endif
        
        
        private readonly HashSet<long> _enabledModules = new();
        private readonly HashSet<long> _disabledModules = new();
        private Dictionary<long, TModule> _modulesDict;
        private Dictionary<string, List<TModule>> _groupsDict;

        public abstract IAssetProvider AssetProvider { get; }

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

        public async UniTask Initialize()
        {
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            shareData = GetGameShare();
            _modulesDict = new();
            _groupsDict = new();
            shareData.AddSharedObject(typeof(IAssetProvider), AssetProvider);
            shareData.AddSharedObject(AssetProvider.GetType(), AssetProvider);
            SetShareData(shareData);
            
            shareData.AddSharedObject(new OrganizerActions{ Sorting = Sort});

            await TaskUtils.WaitUntilCondition(() => AssetProvider.IsLoaded);

            var allPacks = AssetProvider.GetPacksByType(AssetConstants.UiPanelId);
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
                    _modulesDict[panelUiPack.ConvertId()] = newModule;
                    OnPanelUiPackApply(handle, panelUiPack, metaInfo);
                }
            }

            foreach (var uiModule in modules) uiModule.Inject(shareData);
            
            await ThreadGate.CreateJob(PreInitialize).Run().AsUniTask();
            
            foreach (var uiModule in modules)
            {
                _modulesDict[uiModule.Id] = uiModule;
                
                if (_groupsDict.ContainsKey(uiModule.Group)) _groupsDict[uiModule.Group].Add(uiModule);
                else _groupsDict.Add(uiModule.Group, new List<TModule> { uiModule });
            }

            await ThreadGate.CreateJob(OnInitialize).Run().AsUniTask();
            
            foreach (var uiModule in modules)
            {
                if (_enabledModules.Contains(uiModule.ConvertId())) _ = uiModule.ShowAsync(shareData, _parentTransform);
                else _disabledModules.Add(uiModule.ConvertId());
            }
            
            await ThreadGate.CreateJob(OnPostInitialize).Run().AsUniTask();
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
        
        private void MoveToEnabledList(long moduleId)
        {
            if (_disabledModules.Contains(moduleId))
            {
                _disabledModules.Remove(moduleId);
                _enabledModules.Add(moduleId);
                #if UNITY_EDITOR
                enabledModules.Clear();
                disabledModules.Clear();
                foreach (var id in _enabledModules) enabledModules.Add(id.ToStringFromStableId());
                foreach (var id in _disabledModules) disabledModules.Add(id.ToStringFromStableId());
                #endif
            }
        }

        private void MoveToDisabledList(long moduleId)
        {
            if (_enabledModules.Contains(moduleId))
            {
                _enabledModules.Remove(moduleId);
                _disabledModules.Add(moduleId);
                #if UNITY_EDITOR
                enabledModules.Clear();
                disabledModules.Clear();
                foreach (var id in _enabledModules) enabledModules.Add(id.ToStringFromStableId());
                foreach (var id in _disabledModules) disabledModules.Add(id.ToStringFromStableId());
                #endif
            }
        }

        public void ShowModule(long moduleId)
        {
            if (_modulesDict.TryGetValue(moduleId, out var uiModule))
            {
                if (_enabledModules.Contains(moduleId))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(moduleId);
                uiModule.Show(shareData, _parentTransform);
            }
        }

        public void ShowModule(long moduleId, Action onLoad)
        {
            if (_modulesDict.TryGetValue(moduleId, out var uiModule))
            {
                if (_enabledModules.Contains(moduleId))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(moduleId);
                uiModule.Show(shareData, _parentTransform, onLoad);
            }
        }

        public void ShowModule(long moduleId, Action<GameObject> onLoad)
        {
            if (_modulesDict.TryGetValue(moduleId, out var uiModule))
            {
                if (_enabledModules.Contains(moduleId))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(moduleId);
                uiModule.Show(shareData, _parentTransform, onLoad);
            }
        }

        public async UniTask ShowModuleAsync(long moduleId)
        {
            if (_modulesDict.TryGetValue(moduleId, out var uiModule))
            {
                if (_enabledModules.Contains(moduleId))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(moduleId);
                await uiModule.ShowAsync(shareData, _parentTransform);
            }
        }

        public void HideModule(long moduleId)
        {
            if (_disabledModules.Contains(moduleId)) return;
            if (_modulesDict.TryGetValue(moduleId, out var uiModule))
            {
                MoveToDisabledList(moduleId);
                uiModule.Hide();
            }
        }

        public TModule GetModule(long moduleId)
        {
            return _modulesDict[moduleId];
        }

        public bool TryGetModule(long moduleId, out TModule module)
        {
            if (_modulesDict.TryGetValue(moduleId, out TModule resultModule))
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
                if (_disabledModules.Contains(uiModule.Id)) continue;

                MoveToDisabledList(uiModule.Id);
                uiModule.Hide();
            }
        }
        
        public void UnloadModule(long moduleId)
        {
            if (_modulesDict.TryGetValue(moduleId, out var uiModule))
            {
                if (_enabledModules.Contains(moduleId))
                {
                    MoveToDisabledList(moduleId);
                    uiModule.Hide();
                }
                uiModule.Unload();
            }
        }
        
        public void UnloadGroup(string groupName)
        {
            if (_groupsDict.TryGetValue(groupName, out var group)) foreach (var uiModule in group)
            {
                if (_enabledModules.Contains(uiModule.Id))
                {
                    MoveToDisabledList(uiModule.Id);
                    uiModule.Hide();
                }
                uiModule.Unload();
            }
        }
        
        public void UpdateModule(long moduleId)
        {
            if (_modulesDict.TryGetValue(moduleId, out var uiModule)) uiModule.UpdateModule();
        }

        public void UpdateGroup(string groupName)
        {
            if(_groupsDict.TryGetValue(groupName, out var group)) foreach (var uiModule in group) uiModule.UpdateModule();
        }

        public void ShowGroup(string groupName)
        {
            if(_groupsDict.TryGetValue(groupName, out var group)) foreach (var uiModule in group)
            {
                MoveToEnabledList(uiModule.Id);
                uiModule.Show(shareData, _parentTransform);
            }
        }

        public bool IsEnabledModule(long moduleId)
        {
            return _enabledModules.Contains(moduleId);
        }

        protected virtual void PreInitialize() {}
        protected virtual void OnInitialize() {}
        protected virtual void OnPostInitialize() {}
        protected abstract void SetShareData(GameShare shareData);

    }
    public class OrganizerActions
    {
        public Action Sorting;
    }
}
