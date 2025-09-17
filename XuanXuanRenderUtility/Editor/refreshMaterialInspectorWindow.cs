
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class RefreshMaterialInspectorWindow : EditorWindow
{
    [MenuItem("ArtTools/刷新选中材质球面板")]
    static void refershSelectMaterialInspector()
    {
        EditorWindow.GetWindow<RefreshMaterialInspectorWindow>("材质球刷新");
    }
    public static bool isRefreshing = false;
    List<Material> materials = new List<Material>();
    int i = 0;
    private void OnGUI()
    {
        if (GUILayout.Button("开始刷新材质面板"))
        {
                materials.Clear();
                foreach (UnityEngine.Object o in Selection.GetFiltered(typeof(Material), SelectionMode.DeepAssets))
                {
                    string path = AssetDatabase.GetAssetPath(o);
                    string fileExtension = Path.GetExtension(path);
                    if (fileExtension == ".mat")
                    {
                        materials.Add((Material)o);
                        
                    }
                }
                i = 0;
        }
    }

    private void OnEnable()
    {
        Debug.Log("刷新材质打开");

        isRefreshing = true;
    }

    private void Update()
    {
        if(materials.Count <= 0) return;
        if (i < materials.Count)
        {
            // isRefreshing = true;
            // SetNormalToUV1(materials[i]);   
            Selection.activeObject = materials[i];
            Debug.Log(materials[i].name,materials[i]);
            EditorUtility.DisplayProgressBar("材质球刷新", $"{i + 1}/{materials.Count}", (i + 1) / (float)materials.Count);
            i++;
            // isRefreshing = false;
        }
        if (i >= materials.Count)
        {
            EditorUtility.ClearProgressBar();
            materials.Clear();
            Debug.Log("材质刷新结束，isRefreshing布尔值 = "+isRefreshing);
           
        }


    }

    void OnDisable()
    {
        Debug.Log("刷新材质关闭");
            isRefreshing = false;
        
    }

    // void SetNormalToUV1(Material mat)
    // {
    //     if (mat.HasProperty("_NormalFrom"))
    //     {
    //         mat.SetInteger("_NormalFrom",1);
    //     }
    // }
    //
    // // [MenuItem("Assets/测试刷新材质", priority = 1)]
    // public static void TestRefeshMaterial()
    // {
    //         W9ParticleShaderFlags flags = new W9ParticleShaderFlags();
    //         int id = Shader.PropertyToID("_Mask2_Toggle");
    //     foreach (UnityEngine.Object o in Selection.GetFiltered(typeof(Material), SelectionMode.DeepAssets))
    //     {
    //         Material mat = o as Material;
    //         if (mat.shader.name == "XuanXuan/Effects/Particle_NiuBi")
    //         {
    //             bool isToggle = mat.GetFloat(id) > 0.5f;
    //             if (isToggle)
    //             {
    //                 mat.DisableKeyword("_MASKMAP2");
    //                 flags.SetMaterial(mat);
    //                 flags.SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP2,index:1);
    //             }
    //             // flags.SetMaterial(mat);
    //         }
    //     }
    //     AssetDatabase.SaveAssets();
    // }
    //
    // [MenuItem("Assets/Effect Tool/刷新选中文件夹内材质Wrap模式", priority = 1)]
    // public static void SetSelectParticleMaterialWrapMode()
    // {
    //     foreach (UnityEngine.Object o in Selection.GetFiltered(typeof(Material), SelectionMode.DeepAssets))
    //     {
    //         Material mat = (Material)o;
    //         // Debug.Log(mat.shader.name);
    //         SetParticleMaterialWrapMode((Material)o);
    //     }
    //     AssetDatabase.SaveAssets();
    // }
    //
    // public static void SetParticleMaterialWrapMode(Material mat)
    // {
    //     
    //     if (mat.shader.name == "XuanXuan/Effects/Particle_NiuBi")
    //     {
    //         W9ParticleShaderFlags flags = new W9ParticleShaderFlags(mat);
    //         SetWrapModeFlag(flags,mat,"_BaseMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_BASEMAP);
    //         SetWrapModeFlag(flags,mat,"_MaskMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP);
    //         SetWrapModeFlag(flags,mat,"_MaskMap2",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP2);
    //         SetWrapModeFlag(flags,mat,"_NoiseMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_NOISEMAP);
    //         SetWrapModeFlag(flags,mat,"_EmissionMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_EMISSIONMAP);
    //         SetWrapModeFlag(flags,mat,"_DissolveMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MAP);
    //         SetWrapModeFlag(flags,mat,"_DissolveMaskMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP);
    //         SetWrapModeFlag(flags,mat,"_DissolveRampMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP);
    //         SetWrapModeFlag(flags,mat,"_ColorBlendMap",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_COLORBLENDMAP);
    //         SetWrapModeFlag(flags,mat,"_VertexOffset_Map",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP);
    //         SetWrapModeFlag(flags,mat,"_ParallaxMapping_Map",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP);
    //     }
    // }
    //
    // public static void SetParticleMaterialMaskStrenth(Material mat)
    // {
    //     if (mat.shader.name == "XuanXuan/Effects/Particle_NiuBi")
    //     {
    //         Vector4 vec = mat.GetVector("_MaskMap3OffsetAnition");
    //         if (vec.z < 1)
    //         {
    //             Vector4 newVec = new Vector4(vec.x, vec.y, 1, vec.w);
    //             mat.SetVector("_MaskMap3OffsetAnition",newVec);
    //         }
    //     }
    // }
    //
    // static void SetWrapModeFlag(W9ParticleShaderFlags flags, Material mat, string matTexPropertyName, int flagBit)
    // {
    //     if (mat.GetTexture(matTexPropertyName) != null)
    //     {
    //         Texture tex = mat.GetTexture(matTexPropertyName);
    //         Debug.Log(tex.name);
    //         if (tex.wrapMode == TextureWrapMode.Clamp)
    //         {
    //             Debug.Log("This is Clamp");
    //             flags.SetFlagBits(flagBit,index:2);
    //         }
    //         else if (tex.wrapMode == TextureWrapMode.Repeat)
    //         {
    //             Debug.Log("This is Repeat");
    //             flags.ClearFlagBits(flagBit,index:2);
    //         }
    //     }
    // }
    
    
    

    // static IEnumerator refreshMaterialInspector(Material[] materials)
    // {
    //     for (int i = 0; i < materials.Length; i++)
    //     {
    //         EditorUtility.DisplayProgressBar("材质球刷新", $"{i + 1}/{materials.Length}", (i + 1) / (float)materials.Length);
    //         Debug.Log(i);
    //         // Selection.activeObject = materials[i];
    //         yield return null;
    //     }
    //     EditorUtility.ClearProgressBar();
    //     // yield break;
    // }
}

    
