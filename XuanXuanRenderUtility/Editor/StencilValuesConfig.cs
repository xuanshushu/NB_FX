using System;
using System.Collections.Generic;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class StencilValuesConfig : ScriptableObject
    {
        // public Dictionary<string, StencilValues> Config = new Dictionary<string, StencilValues>();
        // public StencilValuesConfigDictionary Config = new StencilValuesConfigDictionary();
        [Serializable]
        public class KeyStencilValues
        {
            public string key;
            public StencilValues Values;
        }

        [SerializeField]
        public List<KeyStencilValues> Config = new List<KeyStencilValues>();

        public bool ContainsKey(string key)
        {
            if (GetStencilValues(key) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public StencilValues GetStencilValues(string key)
        {
            foreach (var item in Config)
            {
                if (item.key == key)
                {
                    return item.Values;
                }
            }
            Debug.LogError("StencilValuesConfig: 不存在Key"+key);
            return null;
        }

        public int GetKeyIndex(string key)
        {
            for (int i = 0; i < Config.Count; i++)
            {
                if (Config[i].key == key)
                {
                    return i;
                }
            }
            Debug.LogError("StencilValuesConfig: 不存在Key"+key);
            return -1;
        }

        public string GetKeyByIndex(int index)
        {
            return Config[index].key;
        }
        

        public StencilValues this[string key]
        {
            get
            {
                return GetStencilValues(key);
            }
        }
        
    }


}
