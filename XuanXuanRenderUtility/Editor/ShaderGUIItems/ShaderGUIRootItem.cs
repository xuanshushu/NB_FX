using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class ShaderPropertyInfo
    {
        public MaterialProperty Property;
        public string Name;
        public int Index;
    }
    public class ShaderGUIRootItem
    {
        private static int s_RendererCacheVersion;
        private static double s_RendererCacheFirstDirtyTime = -1d;
        private static double s_RendererCacheDirtyTime;
        private const double RendererCacheHierarchyRefreshDelay = 0.35d;
        private const double RendererCacheMaxHierarchyRefreshDelay = 2d;
        private const float DefaultManualLayoutHeight = 1200f;

        public Shader Shader;
        public MaterialEditor MatEditor;
        public List<Material> Mats;
        public Dictionary<string,ShaderPropertyInfo> PropertyInfoDic = new Dictionary<string,ShaderPropertyInfo>();
        public List<ShaderFlagsBase> ShaderFlags;//各个继承类各自初始化
        public bool IsInit = true;
        public Color DefaultBackgroundColor;
        public bool ClearUnusedTextureReferencesRequested { get; private set; }
        public List<Renderer> RenderersUsingThisMaterial = new List<Renderer>();
        public List<ParticleSystemRenderer> ParticleRenderersUsingThisMaterial = new List<ParticleSystemRenderer>();
        public bool IsUsedByParticleSystem { get; private set; }
        private int _rendererCacheVersion = -1;
        private Material _rendererCacheMaterial;
        private readonly List<Material> _rendererMaterialBuffer = new List<Material>();
        private bool _manualLayoutActive;
        private Rect _manualLayoutRect;
        private float _manualLayoutY;
        private float _manualLayoutUsedHeight;
        private float _manualLayoutHeight = DefaultManualLayoutHeight;
    
        static ShaderGUIRootItem()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            double now = EditorApplication.timeSinceStartup;
            s_RendererCacheVersion++;
            if (s_RendererCacheFirstDirtyTime < 0d)
            {
                s_RendererCacheFirstDirtyTime = now;
            }

            s_RendererCacheDirtyTime = now;
        }
        
        public virtual void OnGUI(MaterialEditor editor,MaterialProperty[] props)
        {
            MatEditor = editor;
            DefaultBackgroundColor = GUI.backgroundColor;
            if (IsInit || ShouldRebuildMaterialTargets(editor.targets))
            {
                IsInit = true;
                Mats = new List<Material>();
                foreach (var obj in editor.targets)
                {
                    Mats.Add(obj as Material);
                }
                InitFlags(Mats);
                Shader = Mats[0].shader;
                RefreshRendererCacheIfNeeded(true);
            }
            else
            {
                RefreshRendererCacheIfNeeded(false);
            }
            if (ShouldRebuildPropertyInfo(props))
            {
                PropertyInfoDic.Clear();
                for (int i = 0; i < props.Length; i++)
                {
                    ShaderPropertyInfo propInfo = new ShaderPropertyInfo();
                    propInfo.Property = props[i];
                    propInfo.Name = props[i].name;
                    propInfo.Index = i;
                    PropertyInfoDic.Add(propInfo.Name, propInfo);
                }
            }
            else
            {
                for (int i = 0; i < props.Length; i++)
                {
                    ShaderPropertyInfo propInfo = PropertyInfoDic[props[i].name];
                    propInfo.Property = props[i];
                    propInfo.Index = i;
                }
            }
          
            try
            {
                BeginManualLayout();
                OnChildOnGUI();
            }
            finally
            {
                EndManualLayout();
                ClearUnusedTextureReferencesRequested = false;
            }

            RepaintWhenAnimationModeActive();
            IsInit = false;
        }

        public void RequestClearUnusedTextureReferences()
        {
            ClearUnusedTextureReferencesRequested = true;
        }

        public Rect GetControlRect(float height = -1f)
        {
            float rectHeight = height >= 0f ? height : EditorGUIUtility.singleLineHeight;
            if (!_manualLayoutActive)
            {
                return EditorGUILayout.GetControlRect(false, rectHeight);
            }

            Rect rect = new Rect(_manualLayoutRect.x, _manualLayoutY, _manualLayoutRect.width, rectHeight);
            _manualLayoutY += rectHeight + EditorGUIUtility.standardVerticalSpacing;
            _manualLayoutUsedHeight = Mathf.Max(
                _manualLayoutUsedHeight,
                _manualLayoutY - _manualLayoutRect.y - EditorGUIUtility.standardVerticalSpacing);
            return rect;
        }

        public void Space(float height = -1f)
        {
            float spaceHeight = height >= 0f ? height : EditorGUIUtility.standardVerticalSpacing;
            if (!_manualLayoutActive)
            {
                EditorGUILayout.Space();
                return;
            }

            _manualLayoutY += spaceHeight;
            _manualLayoutUsedHeight = Mathf.Max(_manualLayoutUsedHeight, _manualLayoutY - _manualLayoutRect.y);
        }

        private void BeginManualLayout()
        {
            float height = Mathf.Max(EditorGUIUtility.singleLineHeight, _manualLayoutHeight);
            _manualLayoutRect = EditorGUILayout.GetControlRect(false, height);
            _manualLayoutY = _manualLayoutRect.y;
            _manualLayoutUsedHeight = 0f;
            _manualLayoutActive = true;
        }

        private void EndManualLayout()
        {
            if (!_manualLayoutActive)
            {
                return;
            }

            _manualLayoutActive = false;
            float usedHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, _manualLayoutUsedHeight);
            if (!Mathf.Approximately(_manualLayoutHeight, usedHeight))
            {
                _manualLayoutHeight = usedHeight;
                MatEditor?.Repaint();
            }
        }

        private bool ShouldRebuildPropertyInfo(MaterialProperty[] props)
        {
            if (PropertyInfoDic.Count != props.Length)
            {
                return true;
            }

            for (int i = 0; i < props.Length; i++)
            {
                if (!PropertyInfoDic.ContainsKey(props[i].name))
                {
                    return true;
                }
            }

            return false;
        }

        void RepaintWhenAnimationModeActive()
        {
            if (AnimationMode.InAnimationMode())
            {
                MatEditor.Repaint();
            }
        }

        private bool ShouldRebuildMaterialTargets(UnityEngine.Object[] targets)
        {
            if (Mats == null || targets == null || Mats.Count != targets.Length)
            {
                return true;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (Mats[i] != (targets[i] as Material))
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshRendererCacheIfNeeded(bool force)
        {
            Material material = Mats != null && Mats.Count > 0 ? Mats[0] : null;
            if (!force && _rendererCacheMaterial == material && _rendererCacheVersion == s_RendererCacheVersion)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            bool waitingForQuietHierarchy = now - s_RendererCacheDirtyTime < RendererCacheHierarchyRefreshDelay;
            bool withinMaxDelay = s_RendererCacheFirstDirtyTime >= 0d &&
                                  now - s_RendererCacheFirstDirtyTime < RendererCacheMaxHierarchyRefreshDelay;
            if (!force &&
                _rendererCacheMaterial == material &&
                waitingForQuietHierarchy &&
                withinMaxDelay)
            {
                return;
            }

            CacheRenderersUsingThisMaterial(material);
            _rendererCacheMaterial = material;
            _rendererCacheVersion = s_RendererCacheVersion;
            s_RendererCacheFirstDirtyTime = -1d;
        }

        void CacheRenderersUsingThisMaterial(Material material)
        {
            Renderer[] renderers = UnityObjectFindCompat.FindAll<Renderer>();
            RenderersUsingThisMaterial.Clear();
            ParticleRenderersUsingThisMaterial.Clear();
            IsUsedByParticleSystem = false;
            if (material == null || renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer psr &&
                    (psr.sharedMaterial == material || psr.trailMaterial == material))
                {
                    IsUsedByParticleSystem = true;
                    ParticleRenderersUsingThisMaterial.Add(psr);
                }

                _rendererMaterialBuffer.Clear();
                renderer.GetSharedMaterials(_rendererMaterialBuffer);
                for (int i = 0; i < _rendererMaterialBuffer.Count; i++)
                {
                    if (_rendererMaterialBuffer[i] == material)
                    {
                        RenderersUsingThisMaterial.Add(renderer);
                        break;
                    }
                }
            }

            _rendererMaterialBuffer.Clear();
        }
        
        public virtual void InitFlags(List<Material> mats) { } //各个子类各自实现 

        public virtual void OnChildOnGUI()
        {
            
        }
    }
}
