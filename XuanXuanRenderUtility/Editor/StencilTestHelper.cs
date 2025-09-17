
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
// using Sirenix.OdinInspector;
// using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace stencilTestHelper
{

    [Serializable]
    public class StencilValues
    {
        public int DefaultQueue = 2000;
        [BinaryInt(8,true,2)]
        public int Ref = 0;
        public CompareFunction Comp = CompareFunction.Always;
        public StencilOp Pass = StencilOp.Keep;
        public StencilOp Fail = StencilOp.Keep;
        public StencilOp ZFail = StencilOp.Keep;
        [BinaryInt(8,true,1)]
        public int ReadMask = 255;
        [BinaryInt(8,true,1)]
        public int WriteMask = 255;
    }



    public static class StencilTestHelper
    {
        // private static StencilValuesConfig stencilValuesConfig;
        public class  StencilPropertyNames
        {
            public string stencil = "_Stencil";
            public string stencilComp = "_StencilComp";
            public string stencilOp = "_StencilOp";
            public string stencilWriteMask = "_StencilWriteMask";
            public string stencilReadMask = "_StencilReadMask";
            public string stencilZFail = "_StencilZFail";
            public string stencilFail = "_StencilFail";
            public string stencilKexIndex = "_StencilKeyIndex";

            public StencilPropertyNames()
            {
            }

            public StencilPropertyNames(string stencilName, string stencilCompName, string stencilOpName,
                string stencilWriteMaskName, string stencilReadMaskName, string stencilZFailName,
                string stencilFailName,string stencilKexIndexName)
            {
                if (!string.IsNullOrEmpty(stencilName))
                {
                    stencil = stencilName;
                }
                if (!string.IsNullOrEmpty(stencilCompName))
                {
                    stencilComp = stencilCompName;
                }
                if (!string.IsNullOrEmpty(stencilOpName))
                {
                    stencilOp = stencilOpName;
                }
                if (!string.IsNullOrEmpty(stencilWriteMaskName))
                {
                    stencilWriteMask = stencilWriteMaskName;
                }
                if (!string.IsNullOrEmpty(stencilReadMaskName))
                {
                    stencilReadMask = stencilReadMaskName;
                }
                if (!string.IsNullOrEmpty(stencilZFailName))
                {
                    stencilZFail = stencilZFailName;
                }
                if (!string.IsNullOrEmpty(stencilFailName))
                {
                    stencilFail = stencilFailName;
                }

                if (!string.IsNullOrEmpty(stencilKexIndexName))
                {
                    stencilKexIndex = stencilKexIndexName;
                }
            }
        }


        private static StencilPropertyNames defaultStencilPropertyNames = new StencilPropertyNames();

        // public static void SetMaterialStencil(Material mat,StencilValues stencilValues,out int defaultQueue)
        public static void SetMaterialStencil(Material mat, string stencilConfigKey,StencilValuesConfig stencilValuesConfig, out int defaultQueue,StencilPropertyNames stencilPropertyNames = null)
        {
            if (stencilValuesConfig == null)
            {
                Debug.LogError(mat.name+": 缺少模板预设,设置Stencil失败");
                // stencilValuesConfig =
                //     AssetDatabase.LoadAssetAtPath<StencilValuesConfig>(
                //         "Assets/AddressableAssets/Shader/StencilConfig.asset");
            }
            

            if (stencilPropertyNames == null)
            {
                stencilPropertyNames = defaultStencilPropertyNames;
            }

            StencilValues stencilValues;
            if (stencilValuesConfig.ContainsKey(stencilConfigKey))
            {
                stencilValues = stencilValuesConfig[stencilConfigKey];
                if (!string.IsNullOrEmpty(stencilPropertyNames.stencil))
                {
                    mat.SetFloat(stencilPropertyNames.stencil, stencilValues.Ref);
                }
                if (!string.IsNullOrEmpty(stencilPropertyNames.stencilComp) )
                {
                    mat.SetFloat(stencilPropertyNames.stencilComp, (float)stencilValues.Comp);
                }
                if (!string.IsNullOrEmpty(stencilPropertyNames.stencilOp))
                {
                    mat.SetFloat(stencilPropertyNames.stencilOp, (float)stencilValues.Pass);
                }
                if (!string.IsNullOrEmpty(stencilPropertyNames.stencilWriteMask))
                {
                    mat.SetFloat(stencilPropertyNames.stencilWriteMask, stencilValues.WriteMask);
                }
                if (!string.IsNullOrEmpty(stencilPropertyNames.stencilReadMask))
                {
                    mat.SetFloat(stencilPropertyNames.stencilReadMask, stencilValues.ReadMask);
                }

                if (!string.IsNullOrEmpty(stencilPropertyNames.stencilZFail))
                {
                    mat.SetFloat(stencilPropertyNames.stencilZFail, (float)stencilValues.ZFail);
                }

                if (!string.IsNullOrEmpty(stencilPropertyNames.stencilFail))
                {
                    mat.SetFloat(stencilPropertyNames.stencilFail, (float)stencilValues.Fail);
                }

                if (!string.IsNullOrEmpty(stencilPropertyNames.stencilKexIndex))
                {
                    mat.SetFloat(stencilPropertyNames.stencilKexIndex,stencilValuesConfig.GetKeyIndex(stencilConfigKey));
                }

                defaultQueue = stencilValues.DefaultQueue;
            }
            else
            {
                Debug.LogError("无法设置材质模板参数，因为没有配置模板值", mat);
                defaultQueue = mat.renderQueue;
            }
        }
    }
}
