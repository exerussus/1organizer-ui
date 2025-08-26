
#if UNITY_EDITOR

using System.Collections.Generic;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using Source.Scripts.Global.Managers.AssetManagement;
using UnityEditor;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Editor
{
    public static class GroupRefToBuildContainerConverter
    {
        [MenuItem("Assets/Create/Convert to Build Container", true)]
        private static bool ValidateCreateAsset()
        {
            if (Selection.activeObject is not GroupReferencePack pack) return false;
            return true;
        }

        [MenuItem("Assets/Create/Convert to Build Container", false, 10)]
        private static void CreateAsset()
        {
            if (Selection.activeObject is not GroupReferencePack pack) return;
            var container = ScriptableObject.CreateInstance<DefaultBuildContainer>();
            var packs = new List<AssetReferencePack>();
            foreach (var assetReference in pack.SetAssetReferencePacks(packs)) container.AddNew(assetReference.TypeId, assetReference.Reference.editorAsset);
            ProjectWindowUtil.CreateAsset(container, "BuildContainer.asset");
        }
    }
}

#endif