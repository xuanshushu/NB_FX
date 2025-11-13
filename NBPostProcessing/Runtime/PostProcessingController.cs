using UnityEngine;
#if CINIMACHINE_3_0
using Unity.Cinemachine;
#endif
#if UNITY_EDITOR
using UnityEditor.AnimatedValues;
using UnityEditor;
#endif

namespace NBShader
{
    [ExecuteInEditMode]
    public class PostProcessingController : MonoBehaviour
    {
        #if UNITY_EDITOR
        public AnimBool[] AnimBools = new AnimBool[10];
        #endif
        [SerializeField]
        private PostProcessingManager _manager;
        
        public int index
        {
            get
            {
                return _index;
            }
        }
        private int _index;
        public void SetIndex(int controllerIndex)
        {
            _index = controllerIndex;
        }
        
        public Vector2 customScreenCenterPos = new Vector2(0.5f, 0.5f);
        private Vector2 _lastCustomScreenCenterPos = new Vector2(0.5f, 0.5f);
        public bool chromaticAberrationToggle = false;
        public bool caFromDistort = false;
        public float chromaticAberrationIntensity = 0.2f;
        public float chromaticAberrationPos = 0.5f;
        public float chromaticAberrationRange = 0.5f;
        public bool distortSpeedToggle = false;
        public bool distortScreenUVMode = false;
        public Texture2D distortSpeedTexture;
        public float distortTextureMidValue = 1;
        public Vector4 distortSpeedTexSt = new Vector4(30,1,0,0);
        public float distortSpeedIntensity = 1f;
        public float distortSpeedPosition = 0.5f;
        public float distortSpeedRange = 1f;
        public float distortSpeedMoveSpeedX = 0.1f;
        public float distortSpeedMoveSpeed = -0.5f;//因为老的做法是没有X偏移的，只有Y的偏移，不改变量兼容老做法。
        private readonly int _distortSpeedTextureID = Shader.PropertyToID("_SpeedDistortMap");
        private readonly int _distortSpeedTextureStID = Shader.PropertyToID("_SpeedDistortMap_ST");
        public bool radialBlurToggle = false;
        public bool radialBlurFromDistort = false;
        [Range(1,12)]
        public int radialBlurSampleCount = 4;
        public float radialBlurIntensity = 1;
        public float radialBlurPos = 0.5f;
        public float radialBlurRange = 0.5f;
        #if CINIMACHINE_3_0
            public bool cameraShakeToggle = false;
            public CinemachineCamera cinemachineCamera;
            public float cameraShakeIntensity;
        #endif
        
        public bool overlayTextureToggle = false;
        public bool overlayTexturePolarCoordMode = false;
        public Texture2D overlayTexture;
        private readonly int _overlayTextureID = Shader.PropertyToID("_TextureOverlay");
        private readonly int _overlayTextureStID = Shader.PropertyToID("_TextureOverlay_ST");
        private readonly int _textureOverlayAnimProperty = Shader.PropertyToID("_TextureOverlayAnim");
        private readonly int _textureOverlayMaskProperty = Shader.PropertyToID("_TextureOverlayMask");
        private readonly int _textureOverlayMaskStProperty = Shader.PropertyToID("_TextureOverlayMask_ST");
        public Vector4 overlayTextureSt = new Vector4(1, 1, 0, 0);
        public Vector2 overlayTextureAnim = new Vector2(0, 0); 
        public float overlayTextureIntensity = 1f;
        public Texture2D overlayMaskTexture;
        public Vector4 overlayMaskTextureSt;
        
        public bool flashToggle = false;
        [Range(0,1)]
        public float flashIntensity = 1f;
        public float flashInvertIntensity = 0f;
        public float flashDeSaturateIntensity = 1f;
        public float flashGradientRange = 0.25f;
        public float flashContrast = 0.5f;
        [ColorUsage(false, true)]
        public Color flashColor = new Color(2f,2f,2f,1f);
        [ColorUsage(false, true)]
        public Color blackFlashColor = new Color(0f,0f,0f,1f);
        private readonly int _flashTextureProperty = Shader.PropertyToID("_FlashTexture");
        public Texture2D flashTexture;
        public Vector4 flashTextureScaleOffset = new Vector4(10f,0.1f,0f,0f);
        public bool flashTexturePolarCoordMode = true;
        public Vector2 flashVec = new Vector2(0, 0.05f);
        public float flashTextureIntensity = 0.5f;
        
        public bool vignetteToggle = false;
        public Color vignetteColor = Color.black;
        private readonly int _vignetteColorID = Shader.PropertyToID("_VignetteColor");
        public float vignetteIntensity = 1;
        public float vignetteRoundness = 1;
        public float vignetteSmothness = 10;
        
       
        public void InitController()
        {
            if (this.isActiveAndEnabled == false)
            {
                return;
            }

            _manager = PostProcessingManager.Instance;
            
            _manager.InitController(this);
            InitAllSettings();
            
        }

        void InitAllSettings()
        {
            SetScreenCenterPos();
            SetToggles();
            SetUVFromDistort();
            SetTexture();
            #if CINIMACHINE_3_0
                InitCinemachineCamera();
            #endif
        }

        void SetScreenCenterPos()
        {
            PostProcessingManager.customScreenCenterPos = customScreenCenterPos;
            _lastCustomScreenCenterPos = customScreenCenterPos;
        }

        void SetUVFromDistort()
        {
            PostProcessingManager.isDistortScreenUVMode = distortScreenUVMode;
            PostProcessingManager.isCaByDistort = caFromDistort;
            PostProcessingManager.isRadialBlurByDistort = radialBlurFromDistort;
        }

        private void SetTexture()
        {
            if(PostProcessingManager.material == null) return;
            if (distortSpeedToggle)
            {
                if (distortSpeedTexture != null)
                {
                    PostProcessingManager.material.SetTexture(_distortSpeedTextureID, distortSpeedTexture);
                    PostProcessingManager.distortTextureMidValue = distortTextureMidValue;
                }
                else
                {
                    PostProcessingManager.material.SetTexture(_distortSpeedTextureID, null);
                }
            }

            if (flashToggle&& flashTexture)
            {
                PostProcessingManager.material.SetTexture(_flashTextureProperty, flashTexture);
                if (flashTexturePolarCoordMode)
                {
                    PostProcessingManager.flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_FLASHTEXTURE_POLLARCOORD);
                }
                else
                {
                    PostProcessingManager.flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_FLASHTEXTURE_POLLARCOORD);
                }
            }
            else
            {
                PostProcessingManager.material.SetTexture(_flashTextureProperty, null);
                PostProcessingManager.flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_FLASHTEXTURE_POLLARCOORD);
            }

            if (overlayTextureToggle)
            {
                if (overlayTexturePolarCoordMode)
                {
                    PostProcessingManager.flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_OVERLAYTEXTURE_POLLARCOORD);
                }
                else
                {
                    PostProcessingManager.flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_OVERLAYTEXTURE_POLLARCOORD);
                }
                
                if (overlayTexture)
                {
                    Debug.Log("SetOverlayTexture");
                    PostProcessingManager.material.SetTexture(_overlayTextureID,overlayTexture);
                }
                else
                {
                    Debug.Log("ClearOverlayTexture");
                    PostProcessingManager.material.SetTexture(_overlayTextureID,null);
                }

                if (overlayMaskTexture)
                {
                    Debug.Log("SetOverlayMaskTexture");
                    PostProcessingManager.material.SetTexture(_textureOverlayMaskProperty,overlayMaskTexture);
                    PostProcessingManager.flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_OVERLAYTEXTURE_MASKMAP);
                }
                else
                {
                    Debug.Log("ClearOverlayMaskTexture");
                    PostProcessingManager.flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_OVERLAYTEXTURE_MASKMAP);
                }
            }
            
        }

        private void SetToggles()
        {
            SetBit(ref PostProcessingManager.chromaticAberrationToggles,index,chromaticAberrationToggle);
            SetBit(ref PostProcessingManager.distortSpeedToggles,index,distortSpeedToggle);
            #if CINIMACHINE_3_0
            SetBit(ref PostProcessingManager.cameraShakeToggles,index,cameraShakeToggle);
            #endif
            SetBit(ref PostProcessingManager.overlayTextureToggles,index,overlayTextureToggle);
            SetBit(ref PostProcessingManager.flashToggles,index,flashToggle);
            SetBit(ref PostProcessingManager.radialBlurToggles,index,radialBlurToggle);
            SetBit(ref PostProcessingManager.vignetteToggles,index,vignetteToggle);

    #if UNITY_EDITOR
            SetTexture();
    #endif
            
        }

        private void ClearToggles()
        {
            SetBit(ref PostProcessingManager.chromaticAberrationToggles,index,false);
            SetBit(ref PostProcessingManager.distortSpeedToggles,index,false);
            #if CINIMACHINE_3_0
            SetBit(ref PostProcessingManager.cameraShakeToggles,index,false);
            #endif
            SetBit(ref PostProcessingManager.overlayTextureToggles,index,false);
            SetBit(ref PostProcessingManager.flashToggles,index,false);
            SetBit(ref PostProcessingManager.radialBlurToggles,index,false);
            SetBit(ref PostProcessingManager.vignetteToggles,index,false);
        }

        private bool _lastChormaticAberrationToggle = false;
        private bool _lastDistortSpeedToggle = false;
        #if CINIMACHINE_3_0
        private bool _lastCamerashakeToggle = false;
        #endif
        private bool _lastOverlayTextureToggle = false;
        private bool _lastFlashToggle= false;
        private bool _lastRadialBlurToggle= false;
        private bool _lastVignetteToggle= false;

        bool checkIfToggleChanged(ref bool lastTogggle, bool currentToggle)
        {
            if (lastTogggle != currentToggle)
            {
                lastTogggle = currentToggle;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        

        private static void SetBit(ref int bit, int index,bool bitToggle)
        {
            if (bitToggle)
            {
                bit |= (1 << index);
            }
            else
            {
                bit &= ~(1 << index);
            }
        }

    #if UNITY_EDITOR
            void FindManager()
            {
                UnityEditor.Selection.activeObject = _manager.gameObject;
            }
    #endif

        #if CINIMACHINE_3_0
            #if UNITY_EDITOR
            public void FindVirtualCamera()
            {
                UnityEditor.Selection.activeObject = _manager.currentVirtualCamera;
            }
            #endif

            public void InitCinemachineCamera()
            {
                if (cinemachineCamera)
                {
                    if (!cinemachineCamera.gameObject.TryGetComponent<CinemachineBasicMultiChannelPerlin>(out var _perlin))
                    {
                        _perlin = cinemachineCamera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
                    }

                    #if  UNITY_EDITOR
                        if (_perlin)
                        {
                            if (_perlin.NoiseProfile == null)
                            {
                                _perlin.NoiseProfile =
                                        UnityEditor.AssetDatabase.LoadAssetAtPath<NoiseSettings>(
                                            "Packages/com.xuanxuan.nb.fx/NBPostProcessing/3DPostionShake.asset");
                                _perlin.FrequencyGain = 5f; //做一个自定义
                            }
                            _perlin.AmplitudeGain = 0f; //一开始先不要震动
                        }

                        if (_manager)
                        {
                            _manager.currentVirtualCamera = cinemachineCamera;
                        }
                    #endif
                }
                
            }
        #endif


        private void OnEnable()
        {
            // Debug.Log("InitController");
            InitController();
            #if UNITY_EDITOR
                // 注册编辑器帧更新事件
                EditorApplication.update += ControllerEditorUpdate;
                _manager.ReRegistEditorUpdate();//保证Manager的注册在最后
            #endif
        }

        private void OnDisable()
        {
            // Debug.Log("EndController");
            EndController();
            #if UNITY_EDITOR
                    // 注册编辑器帧更新事件
                    EditorApplication.update -= ControllerEditorUpdate;
            #endif
        }
             
        bool isFirstUpdate = true;
        // Update is called once per frame
        void Update()
        {
            if (isFirstUpdate)
            {
                isFirstUpdate = false;
                return;
            }
            if(!PostProcessingManager.material) return;
    //#if UNITY_EDITOR
            //Odin的ToggleGroup和OnValueChange功能冲突，导致不一定生效。不好调试。所以用手动的方式更新。
            bool isToggleChanged = false;
            isToggleChanged |= checkIfToggleChanged(ref _lastChormaticAberrationToggle, chromaticAberrationToggle);
            isToggleChanged |= checkIfToggleChanged(ref _lastDistortSpeedToggle, distortSpeedToggle);
            #if CINIMACHINE_3_0
            isToggleChanged |= checkIfToggleChanged(ref _lastCamerashakeToggle, cameraShakeToggle);
            #endif
            isToggleChanged |= checkIfToggleChanged(ref _lastOverlayTextureToggle, overlayTextureToggle);
            isToggleChanged |= checkIfToggleChanged(ref _lastFlashToggle, flashToggle);
            isToggleChanged |= checkIfToggleChanged(ref _lastRadialBlurToggle, radialBlurToggle);
            isToggleChanged |= checkIfToggleChanged(ref _lastVignetteToggle, vignetteToggle);
            if (isToggleChanged)
            {
                SetToggles();
            }
    //#endif

            if (customScreenCenterPos != _lastCustomScreenCenterPos)
            {
                SetScreenCenterPos();
            }
            
            
            if (chromaticAberrationToggle)
            {
                PostProcessingManager.chromaticAberrationIntensity =
                    Mathf.Max(PostProcessingManager.chromaticAberrationIntensity, chromaticAberrationIntensity);
                PostProcessingManager.chromaticAberrationPos =
                    Mathf.Max(PostProcessingManager.chromaticAberrationPos, chromaticAberrationPos); 
                PostProcessingManager.chromaticAberrationRange =
                    Mathf.Max(PostProcessingManager.chromaticAberrationRange, chromaticAberrationRange);
            }

            if (distortSpeedToggle)
            {
                if (index == PostProcessingManager.laseUpdateControllerIndex)
                {
                    //只有这里才会更新Scale
                    PostProcessingManager.material.SetVector(_distortSpeedTextureStID, distortSpeedTexSt);
                }

                PostProcessingManager.distortSpeedIntensity =
                    Mathf.Max(PostProcessingManager.distortSpeedIntensity, distortSpeedIntensity);
                PostProcessingManager.distortSpeedPosition =
                    Mathf.Max(PostProcessingManager.distortSpeedPosition, distortSpeedPosition);
                PostProcessingManager.distortSpeedRange =
                    Mathf.Max(PostProcessingManager.distortSpeedRange, distortSpeedRange);
                PostProcessingManager.distortSpeedMoveSpeedX =
                    Mathf.Abs(PostProcessingManager.distortSpeedMoveSpeedX) > Mathf.Abs(distortSpeedMoveSpeedX)
                        ? PostProcessingManager.distortSpeedMoveSpeedX
                        : distortSpeedMoveSpeedX;
                PostProcessingManager.distortSpeedMoveSpeedY =
                    Mathf.Abs(PostProcessingManager.distortSpeedMoveSpeedY) > Mathf.Abs(distortSpeedMoveSpeed)
                        ? PostProcessingManager.distortSpeedMoveSpeedY
                        : distortSpeedMoveSpeed;
            }

            #if CINIMACHINE_3_0
            if (cameraShakeToggle)
            {
                PostProcessingManager.cameraShakeIntensity =
                    Mathf.Max(PostProcessingManager.cameraShakeIntensity, cameraShakeIntensity);

            }
            #endif

            if (overlayTextureToggle)
            {
                if (index == PostProcessingManager.laseUpdateControllerIndex)
                {
                    PostProcessingManager.material.SetVector(_overlayTextureStID, overlayTextureSt);
                    PostProcessingManager.material.SetVector(_textureOverlayMaskStProperty, overlayMaskTextureSt);
                    PostProcessingManager.material.SetVector(_textureOverlayAnimProperty,overlayTextureAnim);
                }

                PostProcessingManager.overlayTextureIntensity = Mathf.Max(PostProcessingManager.overlayTextureIntensity,
                    overlayTextureIntensity);
            }

            if (flashToggle)
            {
                PostProcessingManager.flashDesaturateIntensity =
                    Mathf.Max(PostProcessingManager.flashDesaturateIntensity, flashDeSaturateIntensity);
                PostProcessingManager.flashInvertIntensity =
                    Mathf.Max(PostProcessingManager.flashInvertIntensity, flashInvertIntensity);
                PostProcessingManager.flashIntensity = Mathf.Max(PostProcessingManager.flashIntensity, flashIntensity);
                PostProcessingManager.flashContrast =
                    Mathf.Max(PostProcessingManager.flashContrast, flashContrast);
                PostProcessingManager.flashGradientRange = Mathf.Max(PostProcessingManager.flashGradientRange, flashGradientRange);
                PostProcessingManager.flashVec = flashVec;
                PostProcessingManager.flashTextureIntensity = Mathf.Max(PostProcessingManager.flashTextureIntensity, flashTextureIntensity);
                PostProcessingManager.flashTextureScaleOffset = flashTextureScaleOffset;
                
                PostProcessingManager.flashColor = flashColor;
                PostProcessingManager.blackFlashColor = blackFlashColor;
            }

            if (radialBlurToggle)
            {
                PostProcessingManager.radialBlurIntensity =
                    Mathf.Max(PostProcessingManager.radialBlurIntensity, radialBlurIntensity);
                PostProcessingManager.radialBlurSampleCount = Mathf.Max(PostProcessingManager.radialBlurSampleCount,
                    radialBlurSampleCount);
                PostProcessingManager.radialBlurPos = Mathf.Max(PostProcessingManager.radialBlurPos, radialBlurPos);
                PostProcessingManager.radialBlurRange = Mathf.Max(PostProcessingManager.radialBlurRange, radialBlurRange);
            }

            if (vignetteToggle)
            {
                PostProcessingManager.vignetteIntensity =
                    Mathf.Max(PostProcessingManager.vignetteIntensity, vignetteIntensity);
                PostProcessingManager.vignetteRoundness = Mathf.Max(PostProcessingManager.vignetteRoundness, vignetteRoundness);
                PostProcessingManager.vignetteSmothness = Mathf.Max(PostProcessingManager.vignetteSmothness, vignetteSmothness);
                if (index == PostProcessingManager.laseUpdateControllerIndex)
                {
                    PostProcessingManager.material.SetColor(_vignetteColorID,vignetteColor);
                }
            }


        }


        void EndController()
        {
            ClearToggles();
            _manager.EndController(this);
        }


    #if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/创建NB后处理特效")]
        static void CreatMenu()
        {
            GameObject Effect = new GameObject();
            Effect.name = "NBPostprocessController";
            PostProcessingController controller = Effect.AddComponent<PostProcessingController>();

            UnityEditor.Selection.activeObject = Effect;
        }
        
        void ControllerEditorUpdate()
        {
            if (!Application.isPlaying)
            {
                Update();
            }
        }
    #endif
    }
}
