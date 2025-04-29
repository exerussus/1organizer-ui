#if UNITY_EDITOR
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEditor;

namespace Exerussus._1OrganizerUI.Editor
{
    [InitializeOnLoad]
    public class SettingsCreator
    {
        static SettingsCreator()
        {
            EditorApplication.delayCall += CreateSettings;
        }

        public static void CreateSettings()
        {
            AssetProviderSettings.GetInstanceEditor();
        }
    }
}

#endif