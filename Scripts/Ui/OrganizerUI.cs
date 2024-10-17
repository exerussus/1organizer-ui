using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.Ui
{
    public abstract class OrganizerUI<TModule> : MonoBehaviour
    where TModule : UiModule
    {
        [SerializeField] private bool autoStart;
        [SerializeField] private bool dontDestroyOnLoad;
        [SerializeField] private ShareData shareData;
        [SerializeField] private List<TModule> modules = new();
        [SerializeField] private List<string> enabledModules = new();
        [SerializeField] private Transform _parentTransform;
        
        private List<string> _disabledModules = new();
        private Dictionary<string, TModule> _modulesDict;
        private Dictionary<string, List<TModule>> _groupsDict;

        public Transform ParentTransform
        {
            get => _parentTransform;
            set => _parentTransform = value;
        }

        public void Start()
        {
            if (autoStart) Initialize();
        }

        public void Initialize()
        {
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            shareData = new ShareData();
            _modulesDict = new();
            _groupsDict = new();
            SetShareData(shareData);
            
            shareData.AddObject(new OrganizerActions{ Sorting = Sort});
            
            PreInitialize();
            
            foreach (var uiModule in modules)
            {
                _modulesDict[uiModule.Name] = uiModule;
                
                if (_groupsDict.ContainsKey(uiModule.Group)) _groupsDict[uiModule.Group].Add(uiModule);
                else _groupsDict.Add(uiModule.Group, new List<TModule> { uiModule });
            }

            OnInitialize();
            
            foreach (var uiModule in modules)
            {
                if (enabledModules.Contains(uiModule.Name)) uiModule.Show(shareData, _parentTransform);
                else _disabledModules.Add(uiModule.Name);
            }
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
        }
        
        private void MoveToEnabledList(string uiName)
        {
            if (_disabledModules.Contains(uiName))
            {
                _disabledModules.Remove(uiName);
                enabledModules.Add(uiName);
            }
        }

        private void MoveToDisabledList(string uiName)
        {
            if (enabledModules.Contains(uiName))
            {
                enabledModules.Remove(uiName);
                _disabledModules.Add(uiName);
            }
        }

        public void ShowModule(string uiName)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                if (enabledModules.Contains(uiName))
                {
                    uiModule.UpdateModule();
                    return;
                }
                
                MoveToEnabledList(uiModule.Name);
                uiModule.Show(shareData, _parentTransform);
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
                if (enabledModules.Contains(uiModule.Name))
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
                if (enabledModules.Contains(uiModule.Name))
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

        protected virtual void PreInitialize() {}
        protected virtual void OnInitialize() {}
        protected abstract void SetShareData(ShareData shareData);

    }
    public class OrganizerActions
    {
        public Action Sorting;
    }
}
