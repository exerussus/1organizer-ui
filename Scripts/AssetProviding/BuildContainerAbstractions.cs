using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public abstract class BuildContainer : ScriptableObject, IBuildContainer
    {
        public abstract (long id, Object asset)[] GetAssets();
    }
    
    public interface IBuildContainer
    {
        public (long id, Object asset)[] GetAssets();
    }
}