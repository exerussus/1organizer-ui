
using System.Collections.Generic;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts
{
    public abstract class OrganizerUI<TModule> : MonoBehaviour
    where TModule : UiModule
    {
        [SerializeField] private bool autoStart;
        [SerializeField] private ShareData shareData;
        [SerializeField] private List<TModule> modules = new();
        [SerializeField] private List<string> enabledModules = new();
        private List<string> _disabledModules = new();
        
        private Dictionary<string, UiModule> _modulesDict;
        private Dictionary<string, List<UiModule>> _groupsDict;
        
        public void Start()
        {
            if (autoStart) Initialize();
        }

        public void Initialize()
        {
            shareData = new ShareData();
            _modulesDict = new();
            SetShareData(shareData);
            
            foreach (var uiModule in modules)
            {
                _modulesDict[uiModule.Name] = uiModule;
                
                if (_groupsDict.ContainsKey(uiModule.Group)) _groupsDict[uiModule.Group].Add(uiModule);
                else _groupsDict.Add(uiModule.Group, new List<UiModule> { uiModule });
                
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
