using System;
using System.Threading.Tasks;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.ContentHandlerFeature
{
    /// <summary> Обертка, которая содержит визуальный контент. </summary>
    public interface IContentHandle
    {
        /// <summary> Тип ассета, с которым работает обертка. </summary>
        public string AssetType { get; }
        /// <summary> MeshRenderer, SpriteRenderer </summary>
        public Renderer Renderer { get; protected set; }
        /// <summary> Родительский объект. Всегда должен быть, чтобы проще было унифицировать позиционирование и эффекты. </summary>
        public GameObject Parent { get; protected set; }
        /// <summary> Ссылка на загружаемый ассет. </summary>
        public AssetReferencePack AssetReferencePack { get; protected set; }
        public Action Dispose { get; }

        /// <summary> Ассинхронно загружаем ассет. </summary>
        public virtual async Task LoadAsset(AssetReferencePack referencePack)
        {
            if (referencePack == null)
            {
                Debug.LogError("IContentHandler.LoadAsset | referencePack is null.");
                return;
            }

            AssetReferencePack = referencePack;
            await OnLoadAsset(AssetReferencePack, out var parent, out var renderer);
            Parent = parent;
            Renderer = renderer;
        }
        
        /// <summary> Загружаем ассет в отдельном потоке. </summary>
        public void LoadAsset(AssetReferencePack referencePack, Action callback)
        {
            Task.Run(() =>
            {
                _ = LoadAsset(referencePack).ContinueWith(_ => callback?.Invoke());   
            });
        }

        /// <summary> Выгружаем ассет. </summary>
        public virtual void UnloadAsset()
        {
            OnPreUnloadAsset();
            if (Parent != null) Object.Destroy(Parent);
            OnUnloadAsset();
        }

        protected Task OnLoadAsset(IAssetReferencePack referencePack, out GameObject parent, out Renderer renderer);
        protected void OnUnloadAsset();

        protected virtual void OnPreUnloadAsset() { }
        
        // Возможные методы для манипуляции с контентом
        
        public void Activate();
        public void Deactivate();
        public void MoveTo(Vector2 position);
        
        // И тд
    }
}