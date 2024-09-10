using System.Collections.Generic;
using Plugins.Exerussus._1OrganizerUI.Scripts.Ui;
using UnityEngine;

namespace Plugins.Exerussus._1OrganizerUI.Scripts
{
    public abstract class OrganizerUI<TModule> : MonoBehaviour
    where TModule : UiModule
    {
        [SerializeField] private bool autoStart;
        [SerializeField] private ShareData shareData;
        [SerializeField] private List<TModule> modules = new();
        [SerializeField] private List<string> enabledModules = new();
        private List<string> _disabledModules = new();
        
        private Dictionary<string, TModule> _modulesDict;
        private Dictionary<string, List<TModule>> _groupsDict;
        
        public void Start()
        {
            if (autoStart) Initialize();
        }

        public void Initialize()
        {
            shareData = new ShareData();
            _modulesDict = new();
            _groupsDict = new();
            SetShareData(shareData);
            
            foreach (var uiModule in modules)
            {
                _modulesDict[uiModule.Name] = uiModule;
                
                if (_groupsDict.ContainsKey(uiModule.Group)) _groupsDict[uiModule.Group].Add(uiModule);
                else _groupsDict.Add(uiModule.Group, new List<TModule> { uiModule });
                
                if (enabledModules.Contains(uiModule.Name)) uiModule.Show(shareData, transform);
                else _disabledModules.Add(uiModule.Name);
            }
        }

        public void ShowModule(string uiName)
        {
            if (enabledModules.Contains(uiName)) return;
            
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                uiModule.Show(shareData, transform);
                enabledModules.Add(uiName);
                _disabledModules.Remove(uiName);
            }
        }

        public void HideModule(string uiName)
        {
            if (_disabledModules.Contains(uiName)) return;
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                uiModule.Hide();
                _disabledModules.Add(uiName);
                enabledModules.Remove(uiName);
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
            if (_groupsDict.TryGetValue(groupName, out var group)) foreach (var uiModule in group)
            {
                if (_disabledModules.Contains(uiModule.Name)) continue;
                uiModule.Hide();
                _disabledModules.Add(uiModule.Name);
                enabledModules.Remove(uiModule.Name);
            }
        }
        
        public void UnloadModule(string uiName)
        {
            if (_modulesDict.TryGetValue(uiName, out var uiModule))
            {
                if (enabledModules.Contains(uiModule.Name))
                {
                    uiModule.Hide();
                    enabledModules.Remove(uiModule.Name);
                    _disabledModules.Add(uiModule.Name);
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
                    uiModule.Hide();
                    enabledModules.Remove(uiModule.Name);
                    _disabledModules.Add(uiModule.Name);
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

        protected abstract void SetShareData(ShareData shareData);
    }
}
