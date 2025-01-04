
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [CreateAssetMenu(menuName = "Exerussus/AssetProviding/VfxPack", fileName = "VfxPack")]
    public class VfxPack : SerializedScriptableObject
    {
        [SerializeField, ReadOnly] private bool hasStart;
        [SerializeField, ReadOnly] private bool hasProcess;
        [SerializeField, ReadOnly] private bool hasEnd;
        [SerializeField] private Sprite[] start;
        [SerializeField] private Sprite[] process;
        [SerializeField] private Sprite[] end;

        public bool HasStart => hasStart;
        public bool HasProcess => hasProcess;
        public bool HasEnd => hasEnd;

        public Sprite[] Start => start;
        public Sprite[] Process => process;
        public Sprite[] End => end;

        public virtual string IconPath { get; } = "";

#if UNITY_EDITOR
        private void OnValidate()
        {
            hasStart = start is { Length: > 0 };
            hasProcess = process is { Length: > 0 };
            hasEnd = end is { Length: > 0 };
            ChangeIcon();
        }
        
        [SerializeField, HideInInspector] private bool m_hasIcon;

        private void ChangeIcon()
        {
            if (m_hasIcon) return;
            if (string.IsNullOrEmpty(IconPath)) return;
            
            var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;

            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (obj == null) return;
            
            UnityEditor.EditorGUIUtility.SetIconForObject(obj, icon);
            UnityEditor.EditorUtility.SetDirty(obj); 
            m_hasIcon = true;
        }
#endif
        
    }
}