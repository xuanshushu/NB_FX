using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace W9ParticleShader
{
    internal static class TyflowVATMeshDebug
    {
        private const string MeshAssetPath = "Assets/tyVat/tyVATmesh.fbx";
        private const int SampleCount = 8;

        [MenuItem("Tools/NBShaders/Debug/TyFlow VAT Mesh Channels")]
        private static void DumpTyflowVatMeshChannels()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshAssetPath);
            if (mesh == null)
            {
                Debug.LogError($"TyFlow VAT debug mesh not found: {MeshAssetPath}");
                return;
            }

            Debug.Log($"TyFlow VAT Mesh: {MeshAssetPath}", mesh);
            Debug.Log($"TyFlow VAT Mesh Summary: name={mesh.name}, vertexCount={mesh.vertexCount}, subMeshCount={mesh.subMeshCount}", mesh);

            AppendChannel(mesh, 0);
            AppendChannel(mesh, 1);
            AppendChannel(mesh, 2);
            AppendChannel(mesh, 3);
            AppendChannel(mesh, 4);
            AppendChannel(mesh, 5);
            AppendChannel(mesh, 6);
            AppendChannel(mesh, 7);
        }

        private static void AppendChannel(Mesh mesh, int channel)
        {
            var values = new List<Vector4>();
            mesh.GetUVs(channel, values);

            Debug.Log($"TyFlow VAT UV{channel}: count={values.Count}", mesh);
            if (values.Count == 0)
            {
                return;
            }

            Vector4 min = values[0];
            Vector4 max = values[0];
            int loopCount = Mathf.Min(values.Count, SampleCount);

            for (int i = 0; i < values.Count; i++)
            {
                Vector4 value = values[i];
                min = Vector4.Min(min, value);
                max = Vector4.Max(max, value);
            }

            Debug.Log($"TyFlow VAT UV{channel} Range: min={min} max={max}", mesh);
            for (int i = 0; i < loopCount; i++)
            {
                Debug.Log($"TyFlow VAT UV{channel}[{i}]={values[i]}", mesh);
            }
        }
    }
}
