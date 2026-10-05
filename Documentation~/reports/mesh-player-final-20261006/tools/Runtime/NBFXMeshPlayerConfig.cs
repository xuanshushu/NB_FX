using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NBFX.PlayerValidation
{
    [Serializable]
    public sealed class PlayerCase
    {
        public string identity,route;
        public bool orthographic;
        public Material currentOn,graphOn,currentControl,graphControl,currentStrength0,graphStrength0;
    }
    [Serializable]
    public sealed class PlayerFlipbookCase
    {
        public string identity;
        public bool orthographic;
        public Material current,graph;
    }
    // Serialized scene reference retains shaders/material states in an actual
    // BuildPlayer. No AssetDatabase/ShaderGraph importer exists in this assembly.
    public sealed class NBFXMeshPlayerConfig : ScriptableObject
    {
        public UniversalRenderPipelineAsset pipeline;
        public Shader currentShader,graphShader,uberShader,colorBlitShader;
        public Material backgroundMaterial,transparentBackgroundMaterial,uberRetentionMaterial,colorBlitRetentionMaterial;
        public Texture2D overlay;
        public Material nbPostReadbackMaterial;
        public string buildIdentity,sourceLockSHA256,sourceReceiptJSON;
        public PlayerCase[] cases;
        public PlayerFlipbookCase[] automaticFlipbookCases;
    }
}
