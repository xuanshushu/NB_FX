using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NBShader
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Mesh UI")]
    public class MeshUI : MaskableGraphic
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private List<Material> subMeshMaterials = new List<Material>();

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector4> _tangents = new List<Vector4>();
        private readonly List<Vector2> _uv0S = new List<Vector2>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int[]> _subMeshIndices = new List<int[]>();
        private readonly List<int> _renderSubMeshMap = new List<int>();
        private Mesh _workingMesh;
        private int _renderSubMeshCount;
        private bool _hasLoggedUnreadableMesh;

        public Mesh Mesh
        {
            get => mesh;
            set
            {
                if (mesh == value)
                {
                    return;
                }

                mesh = value;
                _hasLoggedUnreadableMesh = false;
                SetMaterialDirty();
                SetVerticesDirty();
            }
        }

        public List<Material> SubMeshMaterials => subMeshMaterials;

        public override Texture mainTexture
        {
            get
            {
                Material currentMaterial = material;
                if (currentMaterial != null && currentMaterial.mainTexture != null)
                {
                    return currentMaterial.mainTexture;
                }

                return s_WhiteTexture;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetAllDirty();
        }

        protected override void OnDisable()
        {
            canvasRenderer.Clear();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            ReleaseWorkingMesh();
            base.OnDestroy();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        protected override void UpdateGeometry()
        {
            if (canvasRenderer == null)
            {
                return;
            }

            if (!PrepareRenderMesh(out Mesh renderMesh))
            {
                canvasRenderer.Clear();
                return;
            }

            canvasRenderer.SetMesh(renderMesh);
        }

        protected override void UpdateMaterial()
        {
            if (canvasRenderer == null)
            {
                return;
            }

            int materialCount = Mathf.Max(1, _renderSubMeshCount);
            canvasRenderer.materialCount = materialCount;

            for (int i = 0; i < materialCount; i++)
            {
                Material sourceMaterial = GetRenderMaterial(i);
                Material modifiedMaterial = sourceMaterial != null ? GetModifiedMaterial(sourceMaterial) : defaultGraphicMaterial;
                canvasRenderer.SetMaterial(modifiedMaterial, i);
            }

            canvasRenderer.SetTexture(mainTexture);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetMaterialDirty();
            SetVerticesDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            SetVerticesDirty();
        }

        private bool PrepareRenderMesh(out Mesh renderMesh)
        {
            renderMesh = null;
            _renderSubMeshCount = 0;

            if (mesh == null)
            {
                return false;
            }

            if (!mesh.isReadable)
            {
                if (!_hasLoggedUnreadableMesh)
                {
                    Debug.LogWarning($"MeshUI requires a read/write enabled mesh. Bake a readable mesh in the inspector before using '{mesh.name}'.", this);
                    _hasLoggedUnreadableMesh = true;
                }

                return false;
            }

            if (mesh.vertexCount <= 0)
            {
                return false;
            }

            CollectRenderableSubMeshes(mesh, _subMeshIndices, _renderSubMeshMap);
            if (_subMeshIndices.Count == 0)
            {
                return false;
            }

            _renderSubMeshCount = _subMeshIndices.Count;

            if (ShouldUseSourceMesh())
            {
                renderMesh = mesh;
                return true;
            }

            EnsureWorkingMesh();
            BuildWorkingMesh();
            renderMesh = _workingMesh;
            return true;
        }

        private bool ShouldUseSourceMesh()
        {
            if (color != Color.white)
            {
                return false;
            }

            return _renderSubMeshCount == mesh.subMeshCount;
        }

        private void BuildWorkingMesh()
        {
            Vector3[] srcVertices = mesh.vertices;
            int vertexCount = srcVertices.Length;
            PrepareVertexLists(vertexCount);

            Vector2[] srcUv0 = mesh.uv;
            Vector3[] srcNormals = mesh.normals;
            Vector4[] srcTangents = mesh.tangents;
            Color32 vertexColor = color;

            for (int i = 0; i < vertexCount; i++)
            {
                _positions.Add(srcVertices[i]);
                _uv0S.Add(srcUv0 != null && i < srcUv0.Length ? srcUv0[i] : Vector2.zero);
                _normals.Add(srcNormals != null && i < srcNormals.Length ? srcNormals[i] : Vector3.back);
                _tangents.Add(srcTangents != null && i < srcTangents.Length ? srcTangents[i] : new Vector4(1f, 0f, 0f, 1f));
                _colors.Add(vertexColor);
            }

            _workingMesh.Clear();
            _workingMesh.indexFormat = mesh.indexFormat;
            _workingMesh.SetVertices(_positions);
            _workingMesh.SetNormals(_normals);
            _workingMesh.SetTangents(_tangents);
            _workingMesh.SetUVs(0, _uv0S);
            _workingMesh.SetColors(_colors);
            _workingMesh.subMeshCount = _renderSubMeshCount;

            for (int i = 0; i < _renderSubMeshCount; i++)
            {
                _workingMesh.SetTriangles(_subMeshIndices[i], i, false);
            }

            _workingMesh.RecalculateBounds();
        }

        private static void CollectRenderableSubMeshes(Mesh sourceMesh, List<int[]> subMeshIndices, List<int> renderSubMeshMap)
        {
            subMeshIndices.Clear();
            renderSubMeshMap.Clear();

            int subMeshCount = Mathf.Max(1, sourceMesh.subMeshCount);
            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                if (sourceMesh.GetTopology(subMeshIndex) != MeshTopology.Triangles)
                {
                    continue;
                }

                int[] indices = sourceMesh.GetTriangles(subMeshIndex);
                if (indices == null || indices.Length == 0)
                {
                    continue;
                }

                subMeshIndices.Add(indices);
                renderSubMeshMap.Add(subMeshIndex);
            }
        }

        private void PrepareVertexLists(int capacity)
        {
            _positions.Clear();
            _normals.Clear();
            _tangents.Clear();
            _uv0S.Clear();
            _colors.Clear();

            if (_positions.Capacity < capacity)
            {
                _positions.Capacity = capacity;
            }

            if (_normals.Capacity < capacity)
            {
                _normals.Capacity = capacity;
            }

            if (_tangents.Capacity < capacity)
            {
                _tangents.Capacity = capacity;
            }

            if (_uv0S.Capacity < capacity)
            {
                _uv0S.Capacity = capacity;
            }

            if (_colors.Capacity < capacity)
            {
                _colors.Capacity = capacity;
            }
        }

        private void EnsureWorkingMesh()
        {
            if (_workingMesh != null)
            {
                return;
            }

            _workingMesh = new Mesh
            {
                name = "MeshUI Generated Mesh"
            };
            _workingMesh.MarkDynamic();
        }

        private void ReleaseWorkingMesh()
        {
            if (_workingMesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_workingMesh);
            }
            else
            {
                DestroyImmediate(_workingMesh);
            }

            _workingMesh = null;
        }

        private Material GetRenderMaterial(int subMeshIndex)
        {
            int sourceSubMeshIndex = subMeshIndex;
            if (subMeshIndex >= 0 && subMeshIndex < _renderSubMeshMap.Count)
            {
                sourceSubMeshIndex = _renderSubMeshMap[subMeshIndex];
            }

            if (subMeshMaterials != null && sourceSubMeshIndex >= 0 && sourceSubMeshIndex < subMeshMaterials.Count)
            {
                Material subMeshMaterial = subMeshMaterials[sourceSubMeshIndex];
                if (subMeshMaterial != null)
                {
                    return subMeshMaterial;
                }
            }

            return material;
        }
    }
}
