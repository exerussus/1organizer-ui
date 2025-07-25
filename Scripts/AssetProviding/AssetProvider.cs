using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Source.Scripts.Global.Managers.AssetManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [CreateAssetMenu(menuName = "Exerussus/AssetProviding/AssetProvider", fileName = "AssetProvider")]
    public class AssetProvider : ScriptableObject, IAssetProvider
    {
        /// <summary> Хранит все активные группы, которые будут загружаться при инициализации. </summary>
        [SerializeField] protected List<AssetReferenceT<GroupReferencePack>> groupReferences;
#if UNITY_EDITOR
        /// <summary> Хранит деактивированные группы, которые можно подключить через Inspector. </summary>
        [SerializeField] protected List<AssetReferenceT<GroupReferencePack>> unusedGroupReferences = new();
#endif
        /// <summary> Проинициализированные группы. </summary>
        [SerializeField, ReadOnly] protected List<GroupReferencePack> groups = new();
        
        protected Dictionary<string, IAssetReferencePack> _assetPacks = new();
        //protected Dictionary<string, PanelUiPack> _uiPanelsDict = new();
        protected Dictionary<string, List<IAssetReferencePack>> _typePacks = new();
        protected Dictionary<string, AsyncOperationHandle> _assetPackHandles = new();
        protected Dictionary<string, int> _assetPackReferenceCounts = new();
        protected Dictionary<AssetReference, AsyncOperationHandle> _loadedHandles = new();
        protected Dictionary<AssetReference, AsyncOperationHandle<GameObject>> _loadedPanelHandles = new();
        public List<GroupReferencePack> Groups => groups;
        public bool IsLoaded { get; private set; }
        
        public static AssetProvider Instance { get; private set; }

#if UNITY_EDITOR

        private List<GroupReferencePack> Groups_EDITOR = new();
        private Dictionary<string, AssetReferencePack> _assetPacks_EDITOR = new();
        private Dictionary<string, List<AssetReferencePack>> _assetPacksByAssetType_EDITOR = new();
        private Dictionary<AssetReferencePack, GroupReferencePack> _groupsByAssetRefPack_EDITOR = new();
        
        /// <summary> Вызывать перед использованием TryGetGroupReferencePacks_Editor и TryGetAssetReferencePack_Editor. </summary>
        public void RefreshReferences_Editor()
        {
            Groups_EDITOR.Clear();
            _assetPacks_EDITOR.Clear();
            var tempAllAssetRefList = new List<AssetReferencePack>();
            var tempAssetPacksOfGroupList = new List<AssetReferencePack>();
            
            foreach (var groupRef in groupReferences)
            {
                tempAssetPacksOfGroupList.Clear();
                Groups_EDITOR.Add(groupRef.editorAsset);
                groupRef.editorAsset.SetAssetReferencePacks(tempAssetPacksOfGroupList);
                foreach (var assetRef in tempAssetPacksOfGroupList)
                {
                    _groupsByAssetRefPack_EDITOR[assetRef] = groupRef.editorAsset;
                    tempAllAssetRefList.Add(assetRef);
                }
            }

            foreach (var assetReferencePack in tempAllAssetRefList)
            {
                _assetPacks_EDITOR[assetReferencePack.Id] = assetReferencePack;
                if (!_assetPacksByAssetType_EDITOR.ContainsKey(assetReferencePack.AssetType)) _assetPacksByAssetType_EDITOR[assetReferencePack.AssetType] = new();
                _assetPacksByAssetType_EDITOR[assetReferencePack.AssetType].Add(assetReferencePack);
            }
        }

        /// <summary> Возвращает список AssetReferencePack по указанному asset type. </summary>
        public bool TryGetAllAssetReferencePacksOfAssetType_Editor(string assetType, out List<AssetReferencePack> assetReferencePacks)
        {
            return _assetPacksByAssetType_EDITOR.TryGetValue(assetType, out assetReferencePacks);
        }

        /// <summary> Возвращает AssetReferencePack и GroupReferencePack по id. </summary>
        public bool TryGetFullInfo_Editor(string id, out (AssetReferencePack assetReferencePack, GroupReferencePack groupReferencePack) info)
        {
            if (!_assetPacks_EDITOR.TryGetValue(id, out var assetReferencePack))
            {
                info = default;
                return false;
            }

            info = (assetReferencePack, _groupsByAssetRefPack_EDITOR[assetReferencePack]);
            return true;
        }

        /// <summary> Возвращает AssetReferencePack и GroupReferencePack по id, а так же приводит ассет к типу. </summary>
        /// <returns>found - найден ли ассет, success - приведён ли ассет к типу.</returns>
        public (bool found, bool success) TryGetFullInfo_Editor<T>(string id, out (AssetReferencePack assetReferencePack, GroupReferencePack groupReferencePack, T asset) info)
        {
            if (!_assetPacks_EDITOR.TryGetValue(id, out var assetReferencePack))
            {
                info = default;
                return (false, false);
            }
            
            if (typeof(Sprite) == typeof(T))
            {
                var sprite = GetFirstSpriteFromAssetReference(assetReferencePack.Reference);
                if (sprite is T result)
                {
                    info = (assetReferencePack, _groupsByAssetRefPack_EDITOR[assetReferencePack], result);
                    return (true, sprite != null);
                }
            }

            if (assetReferencePack.Reference.editorAsset is T asset)
            {
                info = (assetReferencePack, _groupsByAssetRefPack_EDITOR[assetReferencePack], asset);
                return (true, true);
                
            }
            
            info = (assetReferencePack, _groupsByAssetRefPack_EDITOR[assetReferencePack], default);
            return (true, false);
        }

        /// <summary> Возвращает лист ассетов с приведением к типу, и лист некорректных референсов, которые не удалось привести к указанному типу. </summary>
        public bool TryGetAllAssetsOfAssetType_Editor<T>(string assetType, 
            out List<(string id, AssetReferencePack assetReferencePack, T asset)> assets,
            out List<(AssetReferencePack assetReferencePack, GroupReferencePack groupReferencePack)> invalidAssets)
        {
            if (!_assetPacksByAssetType_EDITOR.TryGetValue(assetType, out var assetReferencePacks))
            {
                assets = null;
                invalidAssets = null;
                return false;
            }
            
            assets = new();
            invalidAssets = new();
            
            if (typeof(Sprite) == typeof(T))
            {
                foreach (var assetReferencePack in assetReferencePacks)
                {
                    var sprite = GetFirstSpriteFromAssetReference(assetReferencePack.Reference);
                    if (sprite != null && sprite is T t) assets.Add((assetReferencePack.Id, assetReferencePack, t));
                    else invalidAssets.Add((assetReferencePack, _groupsByAssetRefPack_EDITOR[assetReferencePack]));
                }
                return assets.Count > 0;
            }
            
            foreach (var assetReferencePack in assetReferencePacks)
            {
                if (assetReferencePack.Reference.editorAsset is T asset) assets.Add((assetReferencePack.Id, assetReferencePack, asset));
                else invalidAssets.Add((assetReferencePack, _groupsByAssetRefPack_EDITOR[assetReferencePack]));
            }
            
            return assets.Count > 0;
        }
        
        /// <summary> Ищет конкретные group references. Перед использованием требуется вызвать RefreshReferences_Editor для актуализации ассетов. </summary>
        public bool TryGetGroupReferencePacks_Editor<T>(out List<T> foundGroups) where T : GroupReferencePack
        {
            if (Groups_EDITOR.Count == 0) RefreshReferences_Editor();
            
            foundGroups = new();
            
            foreach (var groupReferencePack in Groups_EDITOR)
            {
                if (groupReferencePack is T group) foundGroups.Add(group);
            }
            
            return foundGroups.Count > 0;
        }

        /// <summary> Ищет AssetReferencePack по id. Перед использованием требуется вызвать RefreshReferences_Editor для актуализации ассетов. </summary>
        public bool TryGetAssetReferencePack_Editor(string id, out AssetReferencePack assetReferencePack)
        {
            if (Groups_EDITOR.Count == 0) RefreshReferences_Editor();
            
            return _assetPacks_EDITOR.TryGetValue(id, out assetReferencePack);
        }

        /// <summary> Ищет ассет по id и указанию типа. Перед использованием требуется вызвать RefreshReferences_Editor для актуализации ассетов. </summary>
        public bool TryGetAsset_Editor<T>(string id, out T asset) where T : Object
        {
            if (Groups_EDITOR.Count == 0) RefreshReferences_Editor();
            
            if (_assetPacks_EDITOR.TryGetValue(id, out var assetReferencePack))
            {
                if (typeof(Sprite) == typeof(T))
                {
                    var sprite = GetFirstSpriteFromAssetReference(assetReferencePack.Reference);
                    if (sprite != null && sprite is T t)
                    {
                        asset = t;
                        return true;
                    }
                }
                
                if (assetReferencePack.Reference.editorAsset != null)
                {
                    if (assetReferencePack.Reference.editorAsset is T t)
                    {
                        asset = t;
                        return true;
                    }
                }
            }

            asset = null;
            return false;
        }
        
        private static Sprite GetFirstSpriteFromAssetReference(AssetReference reference)
        {
            if (reference.editorAsset is Texture2D texture)
            {
                // Получаем все саб-объекты по пути
                string path = UnityEditor.AssetDatabase.GetAssetPath(texture);
                Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

                foreach (Object asset in assets)
                {
                    if (asset is Sprite sprite) return sprite;
                }
            }
            return null;
        }
        
#endif
        
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
            IsInitialized = true;
            OnInitialize();
        }

        public bool IsInitialized { get; protected set; }

        public void Initialize()
        {
            _ = InitializeAsync();
        }
        
        public virtual void OnBeforeInitialize() {}
        public virtual void OnInitialize() {}
        
        
#if UNITY_EDITOR
        /// <summary> Используется ТОЛЬКО в Editor </summary>
        [Button("Validate")]
        public virtual void Validate()
        {
            FixRefsAddressableGroups();
            FillUnusedPacks();
        }

        private void FixRefsAddressableGroups()
        {
            var assetRefs = new List<AssetReferencePack>();
            foreach (var groupRef in groupReferences)
            {
                if (groupRef == null) continue;
                AddressableEditorExtensions.EnsureAssetsAreAddressable(groupRef);
                assetRefs.Clear();
                foreach (var assetReferencePack in groupRef.editorAsset.SetAssetReferencePacks(assetRefs))
                {
                    try
                    {
                        AddressableEditorExtensions.EnsureAssetsAreAddressable(assetReferencePack.Reference);
                        AddressableEditorExtensions.SyncReferenceToPackGroup(groupRef.editorAsset, assetReferencePack.Reference);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
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
                    
                    if (instance is DefaultGroupReferencePack defaultGroupReferencePack && !defaultGroupReferencePack.IsValidEditor) continue;
                    
                    unusedGroupReferences.Add(new AssetReferenceT<GroupReferencePack>(guid));
                }
            }
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