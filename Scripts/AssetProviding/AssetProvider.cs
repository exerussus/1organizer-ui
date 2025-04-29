using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [CreateAssetMenu(menuName = "Exerussus/AssetProviding/AssetProvider", fileName = "AssetProvider")]
    public class AssetProvider : ScriptableObject, IAssetProvider
    {
#if UNITY_EDITOR
        /// <summary> Хранит все активные группы, которые будут загружаться при инициализации. </summary>
        [SerializeField] public List<GroupReferencePack> groupReferences_EDITOR = new();

        /// <summary> Хранит деактивированные группы, которые можно подключить через Inspector. </summary>
        [SerializeField] public List<GroupReferencePack> unusedGroupReferences_EDITOR = new();

        [SerializeField, FoldoutGroup("DEBUG")]
        protected ExistingGuidGroupPacks existingGuidGroupPacks = new();
#endif
        /// <summary> Хранит все активные группы, которые будут загружаться при инициализации. </summary>
        [SerializeField, ReadOnly] protected List<AssetReferenceT<GroupReferencePack>> groupReferences;

        /// <summary> Проинициализированные группы. </summary>
        [SerializeField, ReadOnly] protected List<GroupReferencePack> groups = new();

#if UNITY_EDITOR
        [SerializeField] private bool autoValidate = false;
#endif

        protected Dictionary<string, IAssetReferencePack> _assetPacks = new();
        protected Dictionary<string, List<IAssetReferencePack>> _typePacks = new();
        protected Dictionary<string, AsyncOperationHandle> _assetPackHandles = new();
        protected Dictionary<string, int> _assetPackReferenceCounts = new();
        protected Dictionary<AssetReference, AsyncOperationHandle> _loadedHandles = new();
        protected Dictionary<AssetReference, AsyncOperationHandle<GameObject>> _loadedPanelHandles = new();
        public List<GroupReferencePack> Groups => groups;
        public bool IsLoaded { get; private set; }

        public static AssetProvider Instance { get; private set; }

        private async Task InitializingAsync()
        {
            Clear();
            Instance = this;
            var handles = new List<AsyncOperationHandle<GroupReferencePack>>();
            var taskArray = new Task<GroupReferencePack>[groupReferences.Count];

            for (var index = 0; index < groupReferences.Count; index++)
            {
                var groupRef = groupReferences[index];
                var handle = groupRef.LoadAssetAsync();
                handles.Add(handle);
                taskArray[index] = handle.Task;
            }

            await Task.WhenAll(taskArray);

            for (var index = 0; index < handles.Count; index++)
            {
                var groupRef = groupReferences[index];
                var handle = handles[index];
                _loadedHandles.Add(groupRef, handle);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var group = handle.Result;
                    if (!groups.Contains(group)) groups.Add(group);
                    else Debug.LogWarning($"Duplicate GroupReference loaded: {group.name}");
                }
                else Debug.LogError($"Failed to load GroupReference: {groupRef.RuntimeKey}");
            }

            var dict = new Dictionary<string, GroupReferencePack>();

            foreach (var group in groups)
            {
                var tempAssetPacksList = new List<AssetReferencePack>();
                group.SetAssetReferencePacks(tempAssetPacksList);

                foreach (var assetReferencePack in tempAssetPacksList)
                {
                    if (!_typePacks.TryGetValue(assetReferencePack.AssetType, out var packList))
                    {
                        packList = new List<IAssetReferencePack>();
                        _typePacks.Add(assetReferencePack.AssetType, packList);
                    }

                    packList.Add(assetReferencePack);
                    if (!_assetPacks.TryAdd(assetReferencePack.Id, assetReferencePack))
                    {
                        Debug.LogError($"Duplicate AssetPack ID detected: {assetReferencePack.Id}", group);
                        Debug.LogError($"Already exist AssetPack >>> ping", dict[assetReferencePack.Id]);
                    }
                    else dict.Add(assetReferencePack.Id, group);
                }
            }

            Debug.Log("AssetManager initialization completed.");
            IsLoaded = true;
        }

        public bool TryGetMetaInfo<T>(string id, out T info) where T : ScriptableObject
        {
            if (TryGetAssetPack(id, out var resultPack))
            {
                return resultPack.TryGetMetaInfo(out info);
            }

            info = null;
            return false;
        }

        public List<IAssetReferencePack> GetPacksByType(string type)
        {
            return _typePacks.TryGetValue(type, out var packs) ? packs : new List<IAssetReferencePack>();
        }

        public IAssetReferencePack GetPack(string id)
        {
            return _assetPacks[id];
        }

        public bool TryGetPack(string id, out IAssetReferencePack assetReferencePack)
        {
            return _assetPacks.TryGetValue(id, out assetReferencePack);
        }

        public async Task<T> LoadAssetPackAsync<T>(string packId) where T : UnityEngine.Object
        {
            if (_assetPackHandles.TryGetValue(packId, out var handle))
            {
                if (_assetPackReferenceCounts.ContainsKey(packId)) _assetPackReferenceCounts[packId]++;
                else _assetPackReferenceCounts[packId] = 1;

                if (handle.Status == AsyncOperationStatus.Succeeded) return handle.Result as T;
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded) return handle.Result as T;
                Debug.LogError($"Failed to load AssetPack with ID: {packId}");
                return null;
            }

            if (!_assetPacks.TryGetValue(packId, out var assetPack))
            {
                Debug.LogError($"AssetPack with ID {packId} not found!");
                return null;
            }

            var newHandle = assetPack.Reference.LoadAssetAsync<T>();
            _assetPackHandles.Add(packId, newHandle);
            _assetPackReferenceCounts[packId] = 1;
            await newHandle.Task;

            if (newHandle.Status == AsyncOperationStatus.Succeeded) return newHandle.Result;
            else
            {
                Debug.LogError($"Failed to load AssetPack with ID: {packId}");
                _assetPackHandles.Remove(packId);
                _assetPackReferenceCounts.Remove(packId);
                return null;
            }
        }

        public async Task<(bool, VfxPack)> TryLoadVfxPackAsync(string packId)
        {
            if (_assetPackHandles.TryGetValue(packId, out var handle))
            {
                if (_assetPackReferenceCounts.ContainsKey(packId)) _assetPackReferenceCounts[packId]++;
                else _assetPackReferenceCounts[packId] = 1;

                if (handle.Status == AsyncOperationStatus.Succeeded) return (true, handle.Result as VfxPack);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded) return (true, handle.Result as VfxPack);

                Debug.LogError($"Failed to load AssetPack with ID: {packId}");
                return (false, null);
            }

            if (!_assetPacks.TryGetValue(packId, out var assetPack))
            {
                Debug.LogError($"AssetPack with ID {packId} not found!");
                return (false, null);
            }

            var newHandle = assetPack.Reference.LoadAssetAsync<VfxPack>();
            _assetPackHandles.Add(packId, newHandle);
            _assetPackReferenceCounts[packId] = 1;
            await newHandle.Task;

            if (newHandle.Status == AsyncOperationStatus.Succeeded)
                return (true, newHandle.Result as VfxPack);

            Debug.LogError($"Failed to load AssetPack with ID: {packId}");
            _assetPackHandles.Remove(packId);
            _assetPackReferenceCounts.Remove(packId);
            return (false, null);
        }

        public async Task<(bool, T)> TryLoadAssetPackContentAsync<T>(string packId, Action<string, LogType> messageCallback) where T : UnityEngine.Object
        {
            if (_assetPackHandles.TryGetValue(packId, out var handle))
            {
                if (_assetPackReferenceCounts.ContainsKey(packId)) _assetPackReferenceCounts[packId]++;
                else _assetPackReferenceCounts[packId] = 1;

                if (handle.Status == AsyncOperationStatus.Succeeded) return (true, handle.Result as T);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded) return (true, handle.Result as T);
                messageCallback.Invoke($"Failed to load AssetPack with ID: {packId}", LogType.Error);
                return (false, null);
            }

            if (!_assetPacks.TryGetValue(packId, out var assetPack))
            {
                messageCallback.Invoke($"AssetPack with ID {packId} not found!", LogType.Error);
                return (false, null);
            }

            messageCallback.Invoke($"Loading new asset pack: {packId}, type: {typeof(T)}", LogType.Log);

            var newHandle = assetPack.Reference.LoadAssetAsync<T>();
            _assetPackHandles.Add(packId, newHandle);
            _assetPackReferenceCounts[packId] = 1;
            await newHandle.Task;

            if (newHandle.Status == AsyncOperationStatus.Succeeded)
            {
                messageCallback.Invoke($"Loading successful: {packId}, type: {typeof(T)}", LogType.Log);
                return (true, newHandle.Result as T);
            }

            messageCallback.Invoke($"Failed to load AssetPack with ID: {packId}", LogType.Error);
            _assetPackHandles.Remove(packId);
            _assetPackReferenceCounts.Remove(packId);
            return (false, null);
        }

        public bool TryGetAssetPack(string packId, out IAssetReferencePack resultPack)
        {
            return _assetPacks.TryGetValue(packId, out resultPack);
        }

        public async Task<(bool, T)> TryLoadAssetPackContentAsync<T>(string packId) where T : UnityEngine.Object
        {
            if (_assetPackHandles.TryGetValue(packId, out var handle))
            {
                if (_assetPackReferenceCounts.ContainsKey(packId)) _assetPackReferenceCounts[packId]++;
                else _assetPackReferenceCounts[packId] = 1;

                if (handle.Status == AsyncOperationStatus.Succeeded) return (true, handle.Result as T);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded) return (true, handle.Result as T);

                Debug.LogError($"Failed to load AssetPack with ID: {packId}");
                return (false, null);
            }

            if (!_assetPacks.TryGetValue(packId, out var assetPack))
            {
                Debug.LogError($"AssetPack with ID {packId} not found!");
                return (false, null);
            }

            Debug.Log($"Loading new asset pack: {packId}, type: {typeof(T)}");

            var newHandle = assetPack.Reference.LoadAssetAsync<T>();
            _assetPackHandles.Add(packId, newHandle);
            _assetPackReferenceCounts[packId] = 1;
            await newHandle.Task;

            if (newHandle.Status == AsyncOperationStatus.Succeeded)
                return (true, newHandle.Result as T);

            Debug.LogError($"Failed to load AssetPack with ID: {packId}");
            _assetPackHandles.Remove(packId);
            _assetPackReferenceCounts.Remove(packId);
            return (false, null);
        }

        public void UnloadAssetPack(string packId)
        {
            if (!_assetPackHandles.TryGetValue(packId, out var handle))
            {
                Debug.LogWarning($"Attempted to unload AssetPack with ID {packId}, but it was not loaded.");
                return;
            }

            if (_assetPackReferenceCounts.TryGetValue(packId, out var count))
            {
                count--;
                if (count <= 0)
                {
                    Addressables.Release(handle);
                    _assetPackHandles.Remove(packId);
                    _assetPackReferenceCounts.Remove(packId);
                }
                else _assetPackReferenceCounts[packId] = count;
            }
            else
            {
                Debug.LogWarning($"Reference count for AssetPack with ID {packId} was not found.");
            }
        }

        protected void Clear()
        {
            if (_loadedHandles is { Count: > 0 })
            {
                foreach (var reference in groupReferences)
                {
                    if (reference != null && _loadedHandles.ContainsKey(reference))
                    {
                        var handle = _loadedHandles[reference];
                        if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                        {
                            reference.ReleaseAsset();
                            _loadedHandles.Remove(reference);
                        }
                    }
                }
            }

            groups.Clear();
            _assetPacks.Clear();
            _typePacks.Clear();
            _assetPackReferenceCounts.Clear();
            _assetPackHandles.Clear();
            //_uiPanelsDict.Clear();
            _loadedHandles.Clear();
            IsLoaded = false;
        }

        public async Task InitializeAsync()
        {
            Instance = this;
            OnBeforeInitialize();
            await InitializingAsync();
            OnInitialize();
        }

        public void Initialize()
        {
            _ = InitializeAsync();
        }

        public virtual void OnBeforeInitialize()
        {
        }

        public virtual void OnInitialize()
        {
        }

        public virtual void OnValidate()
        {
            if (!autoValidate) return;
            ValidateGroups();
        }

#if UNITY_EDITOR
        
        /// <summary> Используется ТОЛЬКО в Editor </summary>
        [Button("Validate")]
        public void ValidateGroups()
        {
            //ChangeIcon();
            FillUnusedPacks();
        }

        [Button("Set assets by references")]
        public void SetAssetsByReferences()
        {
            foreach (var groupReferencePack in groupReferences_EDITOR)
            {
                foreach (var assetReferencePack in groupReferencePack.GetAssetReferencePacksEditor())
                {
                    if (assetReferencePack.editorAsset != null) continue;
                    
                    if (Editor.QOL.AddressableQoL.IsAssetAddressableByGuid(assetReferencePack.Reference.AssetGUID))
                    {
                        
                    }
                }
            }
        }
        
        [SerializeField, HideInInspector] private bool m_hasIcon;
        private void ChangeIcon()
        {
            if (m_hasIcon) return;
            
            var resource = Resources.Load<AssetProviderSettings>("AssetProviderSettings");
            var icon = resource.AssetProviderTexture;
            
            if (icon == null) return;

            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (obj == null) return;
            
            UnityEditor.EditorGUIUtility.SetIconForObject(obj, icon);
            UnityEditor.EditorUtility.SetDirty(obj); 
            m_hasIcon = true;
        }
        
        private void FillUnusedPacks()
        {
            existingGuidGroupPacks.Initialize();
            var allInstances = UnityEditor.AssetDatabase.FindAssets("t:GroupReferencePack");
            var groupRefs = new HashSet<GroupReferencePack>(groupReferences_EDITOR);
            var tempGroupRefs = new HashSet<GroupReferencePack>(groupReferences_EDITOR);
            var unusedGroupReferences = new HashSet<GroupReferencePack>(unusedGroupReferences_EDITOR);
            
            foreach (var groupRef in groupRefs)
            {
                if (unusedGroupReferences.Contains(groupRef)) unusedGroupReferences.Remove(groupRef);
            }
            
            foreach (var groupGuid in allInstances)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(groupGuid);
                
                var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<GroupReferencePack>(path);
                
                tempGroupRefs.Remove(instance);
                
                if (!existingGuidGroupPacks.Contains(instance)) existingGuidGroupPacks.Add(groupGuid, path, instance);
                
                if (instance == null)
                {
                    Debug.LogWarning($"Asset at path '{path}' could not be loaded and will be skipped.");
                    continue;
                }
                
                if (groupRefs.Contains(instance)) continue;
                if (unusedGroupReferences.Contains(instance)) continue;

                unusedGroupReferences.Add(instance);
            }

            foreach (var groupReferencePack in tempGroupRefs)
            {
                groupRefs.Remove(groupReferencePack);
            }

            groupReferences_EDITOR = new (groupRefs);
            unusedGroupReferences_EDITOR = new (unusedGroupReferences);

            groupReferences.Clear();
            foreach (var groupReferencePack in groupReferences_EDITOR)
            {
                groupReferences.Add(existingGuidGroupPacks.GetAssetReference(groupReferencePack));
            }
            
            groupReferences.RemoveAll(gr => gr == null || string.IsNullOrEmpty(gr.AssetGUID) || !UnityEditor.AssetDatabase.GUIDToAssetPath(gr.AssetGUID).EndsWith(".asset"));

            foreach (var groupReferencePack in groupReferences_EDITOR)
            {
                var list = groupReferencePack.GetAssetReferencePacksEditor();
                if (list == null) continue;

                foreach (var assetReferencePack in list)
                {
                    assetReferencePack.Validate(groupReferencePack);
                }
            }
            
            Debug.Log("AssetProvider validated.");
        }
        
        [UnityEditor.InitializeOnLoad]
        private class StaticCleaner
        {
            static StaticCleaner()
            {
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }

            private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode || state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    Instance = null;
                    
                    string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:AssetProvider");
                    List<AssetProvider> assets = new List<AssetProvider>();

                    foreach (string guid in assetGuids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetProvider>(path);
                
                        if (asset != null) asset.Clear();
                    }
                }
            }
        }
#endif
    }
}