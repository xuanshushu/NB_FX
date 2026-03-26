using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using NBShader;

namespace NBShaderEditor
{
    [CustomEditor(typeof(MeshUI))]
    [CanEditMultipleObjects]
    public class MeshUIInspector : Editor
    {
        private SerializedProperty _meshProp;
        private SerializedProperty _subMeshMaterialsProp;
        private SerializedProperty _colorProp;
        private SerializedProperty _raycastTargetProp;
        private SerializedProperty _maskableProp;

        private void OnEnable()
        {
            _meshProp = serializedObject.FindProperty("mesh");
            _subMeshMaterialsProp = serializedObject.FindProperty("subMeshMaterials");
            _colorProp = serializedObject.FindProperty("m_Color");
            _raycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
            _maskableProp = serializedObject.FindProperty("m_Maskable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawMeshSection();
            EditorGUILayout.Space();
            DrawSubMeshMaterials();
            EditorGUILayout.Space();
            DrawUGUISection();
            EditorGUILayout.Space();
            DrawMeshInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMeshSection()
        {
            EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_meshProp, new GUIContent("模型"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SyncSlotsToMesh();
                serializedObject.Update();
            }

            DrawBakeMeshSection();
        }

        private void DrawSubMeshMaterials()
        {
            Mesh mesh = _meshProp.objectReferenceValue as Mesh;
            int targetSize = mesh != null ? mesh.subMeshCount : _subMeshMaterialsProp.arraySize;

            EditorGUILayout.LabelField("Mesh Materials", EditorStyles.boldLabel);

            if (mesh != null && _subMeshMaterialsProp.arraySize != targetSize)
            {
                EditorGUILayout.HelpBox($"当前材质槽位数量为 {_subMeshMaterialsProp.arraySize}，Mesh 的 SubMesh 数量为 {targetSize}。更换 Mesh 后会自动同步槽位。", MessageType.Warning);
            }

            for (int i = 0; i < _subMeshMaterialsProp.arraySize; i++)
            {
                SerializedProperty element = _subMeshMaterialsProp.GetArrayElementAtIndex(i);
                string label = $"Sub Mesh {i}";
                if (mesh != null && i < mesh.subMeshCount)
                {
                    label += $" ({mesh.GetTopology(i)})";
                }

                EditorGUILayout.PropertyField(element, new GUIContent(label));
            }
        }

        private void DrawUGUISection()
        {
            EditorGUILayout.LabelField("UGUI", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_colorProp, new GUIContent("颜色"));
            EditorGUILayout.PropertyField(_raycastTargetProp, new GUIContent("射线检测"));
            EditorGUILayout.PropertyField(_maskableProp, new GUIContent("支持 Mask"));
        }

        private void DrawMeshInfo()
        {
            Mesh mesh = _meshProp.objectReferenceValue as Mesh;
            EditorGUILayout.LabelField("信息", EditorStyles.boldLabel);

            if (mesh == null)
            {
                EditorGUILayout.HelpBox("未指定 Mesh。请先指定一个包含三角形拓扑的模型。", MessageType.Warning);
                return;
            }

            int subMeshCount = mesh.subMeshCount;
            int triangleSubMeshCount = GetTriangleSubMeshCount(mesh);
            int vertexCount = mesh.vertexCount;
            int indexCount = 0;

            for (int i = 0; i < subMeshCount; i++)
            {
                if (mesh.GetTopology(i) == MeshTopology.Triangles)
                {
                    indexCount += (int)mesh.GetIndexCount(i);
                }
            }

            string renderMode = GetRenderModeDescription(mesh);
            string info =
                $"可读性: {(mesh.isReadable ? "是" : "否")}\n" +
                $"顶点数: {vertexCount}\n" +
                $"SubMesh 数量: {subMeshCount}\n" +
                $"三角形 SubMesh 数量: {triangleSubMeshCount}\n" +
                $"三角形索引数: {indexCount}\n" +
                $"运行时模式: {renderMode}";

            EditorGUILayout.HelpBox(info, MessageType.Info);

            if (triangleSubMeshCount == 0)
            {
                EditorGUILayout.HelpBox("当前 Mesh 不包含可渲染的三角形 SubMesh。", MessageType.Error);
            }
            else if (triangleSubMeshCount != subMeshCount)
            {
                EditorGUILayout.HelpBox("当前 Mesh 中存在非三角形拓扑的 SubMesh，运行时会生成 Mesh，并且只渲染三角形 SubMesh。", MessageType.Warning);
            }
        }

        private string GetRenderModeDescription(Mesh mesh)
        {
            if (!mesh.isReadable)
            {
                return "当前 Mesh 不可读，无法渲染";
            }

            if (GetTriangleSubMeshCount(mesh) != mesh.subMeshCount)
            {
                return "将生成运行时 Mesh（原因：存在非三角形 SubMesh）";
            }

            Color color = _colorProp.colorValue;
            if (color != Color.white)
            {
                return "将生成运行时 Mesh（原因：需要应用 UGUI 颜色）";
            }

            return "将直接使用原始 Mesh";
        }

        private void DrawBakeMeshSection()
        {
            Mesh mesh = _meshProp.objectReferenceValue as Mesh;
            if (mesh == null || mesh.isReadable)
            {
                return;
            }

            EditorGUILayout.HelpBox("当前 Mesh 没有开启 Read/Write，MeshUI 无法直接通过 CanvasRenderer 渲染。请先烘焙出一个可读的 Mesh。", MessageType.Warning);

            if (GUILayout.Button("烘焙只读Mesh"))
            {
                BakeMeshForTargets(mesh);
            }
        }

        private int GetTriangleSubMeshCount(Mesh mesh)
        {
            int count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (mesh.GetTopology(i) == MeshTopology.Triangles)
                {
                    count++;
                }
            }

            return count;
        }

        private void BakeMeshForTargets(Mesh sourceMesh)
        {
            string defaultDirectory = GetDefaultSaveDirectory(sourceMesh);
            string defaultName = $"{sourceMesh.name}_MeshUI";
            string assetPath = EditorUtility.SaveFilePanelInProject(
                "烘焙 MeshUI Mesh",
                defaultName,
                "asset",
                "请选择烘焙后 Mesh 的保存位置。",
                defaultDirectory);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            Mesh bakedMesh = CreateReadableMeshCopy(sourceMesh, System.IO.Path.GetFileNameWithoutExtension(assetPath));
            if (bakedMesh == null)
            {
                return;
            }

            string uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(bakedMesh, uniqueAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Mesh bakedAsset = AssetDatabase.LoadAssetAtPath<Mesh>(uniqueAssetPath);
            if (bakedAsset == null)
            {
                Debug.LogError($"无法加载已烘焙的 Mesh 资源: '{uniqueAssetPath}'");
                return;
            }

            Undo.RecordObjects(targets, "烘焙 MeshUI Mesh");
            foreach (Object editorTarget in targets)
            {
                MeshUI meshUI = editorTarget as MeshUI;
                if (meshUI == null)
                {
                    continue;
                }

                meshUI.Mesh = bakedAsset;
                EditorUtility.SetDirty(meshUI);
                PrefabUtility.RecordPrefabInstancePropertyModifications(meshUI);
            }

            serializedObject.Update();
            SyncSlotsToMesh();
        }

        private string GetDefaultSaveDirectory(Mesh sourceMesh)
        {
            string sourcePath = AssetDatabase.GetAssetPath(sourceMesh);
            if (string.IsNullOrEmpty(sourcePath))
            {
                return "Assets";
            }

            string directory = System.IO.Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(directory))
            {
                return "Assets";
            }

            return directory.Replace('\\', '/');
        }

        private static Mesh CreateReadableMeshCopy(Mesh sourceMesh, string newMeshName)
        {
            using (Mesh.MeshDataArray meshDataArray = MeshUtility.AcquireReadOnlyMeshData(sourceMesh))
            {
                if (meshDataArray.Length == 0)
                {
                    Debug.LogError($"无法读取 Mesh 数据: {sourceMesh.name}");
                    return null;
                }

                Mesh.MeshData meshData = meshDataArray[0];
                Mesh bakedMesh = new Mesh
                {
                    name = newMeshName,
                    indexFormat = meshData.indexFormat
                };

                int vertexCount = meshData.vertexCount;
                if (vertexCount <= 0)
                {
                    Debug.LogError($"Mesh '{sourceMesh.name}' 不包含任何顶点。");
                    Object.DestroyImmediate(bakedMesh);
                    return null;
                }

                using (NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp))
                {
                    meshData.GetVertices(vertices);
                    bakedMesh.SetVertices(vertices);
                }

                if (meshData.HasVertexAttribute(VertexAttribute.Normal))
                {
                    using (NativeArray<Vector3> normals = new NativeArray<Vector3>(vertexCount, Allocator.Temp))
                    {
                        meshData.GetNormals(normals);
                        bakedMesh.SetNormals(normals);
                    }
                }

                if (meshData.HasVertexAttribute(VertexAttribute.Tangent))
                {
                    using (NativeArray<Vector4> tangents = new NativeArray<Vector4>(vertexCount, Allocator.Temp))
                    {
                        meshData.GetTangents(tangents);
                        bakedMesh.SetTangents(tangents);
                    }
                }

                if (meshData.HasVertexAttribute(VertexAttribute.TexCoord0))
                {
                    using (NativeArray<Vector2> uv0 = new NativeArray<Vector2>(vertexCount, Allocator.Temp))
                    {
                        meshData.GetUVs(0, uv0);
                        bakedMesh.SetUVs(0, uv0);
                    }
                }

                int subMeshCount = sourceMesh.subMeshCount;
                bakedMesh.subMeshCount = subMeshCount;

                bool hasTriangleSubMesh = false;
                for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                {
                    SubMeshDescriptor subMesh = meshData.GetSubMesh(subMeshIndex);
                    if (subMesh.topology == MeshTopology.Triangles && subMesh.indexCount > 0)
                    {
                        hasTriangleSubMesh = true;
                        if (meshData.indexFormat == IndexFormat.UInt16)
                        {
                            using (NativeArray<ushort> indices = new NativeArray<ushort>((int)subMesh.indexCount, Allocator.Temp))
                            {
                                meshData.GetIndices(indices, subMeshIndex, true);
                                bakedMesh.SetIndices(indices, MeshTopology.Triangles, subMeshIndex, false);
                            }
                        }
                        else
                        {
                            using (NativeArray<int> indices = new NativeArray<int>((int)subMesh.indexCount, Allocator.Temp))
                            {
                                meshData.GetIndices(indices, subMeshIndex, true);
                                bakedMesh.SetIndices(indices, MeshTopology.Triangles, subMeshIndex, false);
                            }
                        }
                    }
                    else
                    {
                        bakedMesh.SetIndices(System.Array.Empty<int>(), MeshTopology.Triangles, subMeshIndex, false);
                    }
                }

                if (!hasTriangleSubMesh)
                {
                    Debug.LogError($"Mesh '{sourceMesh.name}' 不包含可烘焙的三角形 SubMesh。");
                    Object.DestroyImmediate(bakedMesh);
                    return null;
                }

                bakedMesh.RecalculateBounds();
                return bakedMesh;
            }
        }

        private void SyncSlotsToMesh()
        {
            foreach (Object editorTarget in targets)
            {
                MeshUI meshUI = editorTarget as MeshUI;
                if (meshUI == null)
                {
                    continue;
                }

                SerializedObject targetObject = new SerializedObject(meshUI);
                SerializedProperty meshProp = targetObject.FindProperty("mesh");
                SerializedProperty materialsProp = targetObject.FindProperty("subMeshMaterials");
                Mesh mesh = meshProp.objectReferenceValue as Mesh;
                int targetSize = mesh != null ? mesh.subMeshCount : 0;
                materialsProp.arraySize = Mathf.Max(0, targetSize);
                targetObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(meshUI);
                PrefabUtility.RecordPrefabInstancePropertyModifications(meshUI);
            }
        }
    }
}
