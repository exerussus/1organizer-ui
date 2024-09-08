using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts
{
    [Serializable]
    public class ShareData
    {
        [SerializeField, ReadOnly] private List<string> shared = new ();
        private Dictionary<Type, DataPack> _sharedObjects = new();
        
        public T GetObject<T>()
        {
            var classPack = _sharedObjects[typeof(T)];
            var sharedObject = classPack.Object;
            return (T)sharedObject;
        }
        
        public void GetObject<T>(ref T sharedObject)
        {
            var classPack = _sharedObjects[typeof(T)];
            sharedObject = (T)classPack.Object;
        }

        public void AddObject<T>(Type type, T sharedObject)
        {
            shared.Add(type.Name);
            _sharedObjects[type] = new DataPack(type, sharedObject);
        }

        public void AddObject<T>(T sharedObject)
        {
            var type = sharedObject.GetType();
            shared.Add(type.Name);
            _sharedObjects[type] = new DataPack(type, sharedObject);
        }
    }
    
    [Serializable]
    public class DataPack
    {
        public DataPack(Type type, object sharedObject)
        {
            _object = sharedObject;
            _type = type;
            name = _type.Name;
        }

        [SerializeField] private string name;
        private Type _type;
        private object _object;
        
        public string Name => name; 
        public object Object => _object;
    }
}