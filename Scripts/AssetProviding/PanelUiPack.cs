﻿using Sirenix.OdinInspector;
using UnityEngine;


namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [CreateAssetMenu(fileName = "PanelUiPack", menuName = "Exerussus/AssetProviding/PanelUiPack")]
    public class PanelUiPack : SerializedScriptableObject
    {
        public string id;
        public string group;
        public int order;
    }
}