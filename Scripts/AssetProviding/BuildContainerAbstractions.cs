using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public abstract class BuildContainer : ScriptableObject, IBuildContainer
    {
        internal abstract void AddNew(string id, Object asset);
        public abstract (long id, Object asset)[] GetAssets();
    }
    
    public interface IBuildContainer
    {
        public (long id, Object asset)[] GetAssets();
    }
}