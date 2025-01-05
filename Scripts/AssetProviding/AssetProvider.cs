using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [CreateAssetMenu(menuName = "Exerussus/AssetProviding/AssetProvider", fileName = "AssetProvider")]
    public class AssetProvider : SerializedScriptableObject, IAssetProvider
    {
        [SerializeField] private List<AssetReferenceT<GroupReferencePack>> groupReferences;
#if UNITY_EDITOR
        [SerializeField] private List<AssetReferenceT<GroupReferencePack>> unusedGroupReferences = new();
#endif
        [SerializeField, ReadOnly] private List<GroupReferencePack> groups = new();
        
        private Dictionary<string, IAssetReferencePack> _vfxPacksDict = new();
        private Dictionary<string, IAssetReferencePack> _assetPacks = new();
        private Dictionary<string, List<IAssetReferencePack>> _typePacks = new();
        private Dictionary<string, AsyncOperationHandle> _assetPackHandles = new();
        private Dictionary<string, int> _assetPackReferenceCounts = new();
        private Dictionary<AssetReference, AsyncOperationHandle> _loadedHandles = new();
        public List<GroupReferencePack> Groups => groups;
        public bool IsLoaded { get; private set; }
        
        public async Task InitializeAsync()
        {
            Clear(); 
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
                    if (!groups.Contains(group))
                    {
                        groups.Add(group);
                        Debug.Log($"CyberAssets | Group loaded : {group.name}");
                        foreach (var assetPack in group.AssetPacks) if (assetPack.AssetType == AssetConstants.VfxPack) _vfxPacksDict[assetPack.Id] = assetPack;
                    }
                    else Debug.LogWarning($"Duplicate GroupReference loaded: {group.name}");
                }
                else Debug.LogError($"Failed to load GroupReference: {groupRef.RuntimeKey}");
            }

            foreach (var group in groups)
            {
                foreach (var assetReferencePack in group.AssetPacks)
                {
                    if (!_typePacks.TryGetValue(assetReferencePack.AssetType, out var packList))
                    {
                        packList = new List<IAssetReferencePack>();
                        _typePacks.Add(assetReferencePack.AssetType, packList);
                    }

                    packList.Add(assetReferencePack);
                    if (!_assetPacks.TryAdd(assetReferencePack.Id, assetReferencePack)) Debug.LogWarning($"Duplicate AssetPack ID detected: {assetReferencePack.Id}");
                }
            }

            Debug.Log("AssetManager initialization completed.");
            IsLoaded = true;
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
        
        public async Task<(bool, T)> TryLoadAssetPackAsync<T>(string packId) where T : UnityEngine.Object
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
            _vfxPacksDict.Clear();
            _loadedHandles.Clear();
            IsLoaded = false;
        }
        
        public async void Initialize()
        {
            OnBeforeInitialize();
            await InitializeAsync();
            OnInitialize();
        }
        
        public virtual void OnBeforeInitialize() {}
        public virtual void OnInitialize() {}
        
        
#if UNITY_EDITOR
        /// <summary> Используется ТОЛЬКО в Editor </summary>
        [Button("Validate")]
        public virtual void OnValidate()
        {
            //ChangeIcon();
            FillUnusedPacks();
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
            var allInstances = UnityEditor.AssetDatabase.FindAssets("t:GroupReferencePack");
            var usedGroupReferences = new HashSet<string>(groupReferences
                .Where(gr => gr != null && !string.IsNullOrEmpty(gr.AssetGUID)) // Исключаем null и пустые GUID.
                .Select(gr => gr.AssetGUID));

            unusedGroupReferences.Clear();

            foreach (var guid in allInstances)
            {
                if (!usedGroupReferences.Contains(guid))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogWarning($"Asset with GUID {guid} has an invalid path and will be skipped.");
                        continue;
                    }

                    var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<GroupReferencePack>(path);

                    if (instance == null)
                    {
                        Debug.LogWarning($"Asset at path '{path}' could not be loaded and will be skipped.");
                        continue;
                    }
                    
                    if (!instance.IsValidEditor) continue;
                    
                    unusedGroupReferences.Add(new AssetReferenceT<GroupReferencePack>(guid));
                }
            }

            groupReferences.RemoveAll(gr => gr == null || string.IsNullOrEmpty(gr.AssetGUID) || !UnityEditor.AssetDatabase.GUIDToAssetPath(gr.AssetGUID).EndsWith(".asset"));
            Debug.Log("Unused group references filled and invalid references removed.");
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