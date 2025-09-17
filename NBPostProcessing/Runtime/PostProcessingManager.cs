using System;
using UnityEngine;
// using Sirenix.OdinInspector;

// #if UNITY_EDITOR
//     using Sirenix.OdinInspector.Editor;
// #endif
#if UNITY_EDITOR
using UnityEditor;
#endif

#if CINIMACHINE_3_0
using Unity.Cinemachine;
#endif

namespace NBShader
{


    [ExecuteAlways]
    public class PostProcessingManager : MonoBehaviour
    {
        //单例实现
        private static PostProcessingManager _instance = null;

        public static PostProcessingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // #if UNITY_2022_1_OR_NEWER
                    // _instance = FindFirstObjectByType<PostProcessingManager>();
                    // #else
                    _instance = FindObjectOfType<PostProcessingManager>(); //因为兼容性因素，保留较慢版本
                    // #endif
                    if (_instance == null)
                    {
                        GameObject singletonObj = new GameObject();
                        _instance = singletonObj.AddComponent<PostProcessingManager>();
                        if (Application.isPlaying)
                        {
                            singletonObj.name = "NBPostProcessManager";
                            DontDestroyOnLoad(singletonObj);
                        }
                        else
                        {
                            singletonObj.name = "测试用NB后处理管理器，请美术删除此脚本再上传";
                        }
                    }
                }

                return _instance;
            }
        }


        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                DestroyImmediate(this);
            }
        }

        // [ReadOnly] public Volume volume;
        // private static VolumeProfile profile;
        // [ReadOnly]
        public static Material material
        {
            get { return NBPostProcess.NBPostProcessMaterial; }
        }

        public static NBPostProcessFlags flags = new NBPostProcessFlags();

        // public CinemachineBrain cameraBrain;
        // public CinemachineVirtualCamera currentVirtualCamera;
#if CINIMACHINE_3_0
        public CinemachineCamera currentVirtualCamera;
        private CinemachineBasicMultiChannelPerlin _perlin;
#endif

        public static void InitMat()
        {
            flags.SetMaterial(PostProcessingManager.material);
            if (_instance)
            {
                _instance.ResetEffect();
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (_controllerIndexFlags > 0)
            {
                ReRegistEditorUpdate();
            }
            else
            {
                EditorApplication.update += EditorUpdate;
            }
            // 注册编辑器帧更新事件
#endif
            // //TODO 后续版本要找比较准确快的找Volume的方式
            // volume = GameObject.FindObjectOfType<Volume>();
            // if (volume != null)
            // {
            //     profile = volume.profile;
            // }

            // #if UNITY_EDITOR
            //         //仅仅用于测试。
            //         if (currentVirtualCamera == null)
            //         {
            //             currentVirtualCamera = FindFirstObjectByType<CinemachineCamera>();
            //
            //             if (currentVirtualCamera)
            //             {
            //                 if (!currentVirtualCamera.gameObject.TryGetComponent<CinemachineBasicMultiChannelPerlin>(out _perlin))
            //                 {
            //                     _perlin = currentVirtualCamera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
            //                 }
            //             }
            //
            //             if (_perlin)
            //             {
            //                 _perlin.NoiseProfile =
            //                     // ResourceManager.LoadAssetAsync<NoiseSettings>("Assets/AddressableAssets/Shader/CustomPostprocess/3DPostionShake.asset");
            //                     //在runtime下是找不到的。但设计是美术做好然后存引用在预制件里。
            //                     UnityEditor.AssetDatabase.LoadAssetAtPath<NoiseSettings>(
            //                         "Packages/com.r2.render.postprocessing/3DPostionShake.asset");
            //                 _perlin.FrequencyGain = 5f; //做一个自定义
            //                 _perlin.AmplitudeGain = 0f; //一开始先不要震动
            //             }
            //         }
            //
            // #endif


            //重置Flag
            flags = new NBPostProcessFlags(material);
            flags.SetFlagBits(0);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            // 注册编辑器帧更新事件
            EditorApplication.update -= EditorUpdate;
#endif
            flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_NB_POSTPROCESS_ON);
        }


        //     [ShowInInspector]
        // #if UNITY_EDITOR
        //     [BinaryInt(8)]
        // #endif
        private int _controllerIndexFlags = 0;

        public static int laseUpdateControllerIndex;

        private int GetControllerIndex()
        {
            for (int i = 0; i < 31; i++)
            {
                if (((~_controllerIndexFlags) & (1 << i)) != 0)
                {
                    _controllerIndexFlags |= (1 << i);
                    laseUpdateControllerIndex = i;
                    return i;
                }
            }

            return 32;
        }

        private void ReleaseControllerIndex(int index)
        {
            _controllerIndexFlags &= ~(1 << index);
        }

        private int CountBit(int bit)
        {
            int count = 0;
            while (bit > 0)
            {
                bit = bit & (bit - 1);
                count++;
            }

            return count;
        }

        //每次Controller触发Play都会触发Init
        public void InitController(PostProcessingController controller)
        {
#if CINIMACHINE_3_0
            if (controller.cinemachineCamera != null)
            {
                currentVirtualCamera = controller.cinemachineCamera;
                _perlin = currentVirtualCamera.gameObject.GetComponent<CinemachineBasicMultiChannelPerlin>();
            }
#endif

            controller.SetIndex(GetControllerIndex());
        }

        public void EndController(PostProcessingController controller)
        {
#if CINIMACHINE_3_0
            if (currentVirtualCamera == controller.cinemachineCamera)
            {
                currentVirtualCamera = null;
                _perlin = null;
            }
#endif

            ReleaseControllerIndex(controller.index);
            controller.SetIndex(32); //设置32为关闭index
        }

        private void EffectUpdater(Action initEffect, Action updateEffect, Action endEffect, ref bool lastIsEffect,
            int effectToggles)
        {
            if (effectToggles > 0)
            {
                if (!lastIsEffect)
                {
                    initEffect();
                }

                updateEffect();
                lastIsEffect = true;
            }
            else
            {
                if (lastIsEffect)
                {
                    endEffect();
                    lastIsEffect = false;
                }
            }


        }

        private bool isFirstUpdate = true;

        private void ResetEffect()
        {
            if (_lastIsChromaticAberration)
            {
                EndChromaticAberration();
                _lastIsChromaticAberration = false;
            }

            if (_lastIsRadialBlur)
            {
                EndRadialBlur();
                _lastIsRadialBlur = false;
            }

            if (_lastIsDistortSpeed)
            {
                EndDistortSpeed();
                _lastIsDistortSpeed = false;
            }

#if CINIMACHINE_3_0
            if (_lastIsCameraShake)
            {
                EndCameraShake();
                _lastIsCameraShake = false;
            }
#endif

            if (_lastIsOverlayTexture)
            {
                EndOverlayTexture();
                _lastIsOverlayTexture = false;
            }

            if (_lastIsFlash)
            {
                EndFlash();
                _lastIsFlash = false;
            }

            if (_lastIsVignette)
            {
                EndVignette();
                _lastIsVignette = false;
            }
        }

        private void LateUpdate() //晚于所有脚本触发。
        {
            if (isFirstUpdate)
            {
                isFirstUpdate = false;
                return;
            }

            if (!material) return;
            /*
    #if UNITY_EDITOR
            if (flags.GetMaterial() != PostProcessingManager.material)
            {
                flags.SetMaterial(PostProcessingManager.material);
            }
    #endif
            */

            if (_controllerIndexFlags == 0)
            {
                ResetEffect();
                flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_NB_POSTPROCESS_ON);
                return;
            }

            EffectUpdater(InitChromaticAberration, UpdateChromaticAberration, EndChromaticAberration,
                ref _lastIsChromaticAberration, chromaticAberrationToggles);
            EffectUpdater(InitDistortSpeed, UpdateDistortSpeed, EndDistortSpeed, ref _lastIsDistortSpeed,
                distortSpeedToggles);
            EffectUpdater(InitRadialBlur, UpdateRadialBlur, EndRadialBlur, ref _lastIsRadialBlur, radialBlurToggles);

            bool isSetCustomScreenCenterPos =
                (chromaticAberrationToggles | distortSpeedToggles | radialBlurToggles) > 0;

            if (isSetCustomScreenCenterPos)
            {
                material.SetVector(_customScreenCenterProperty,
                    new Vector4(customScreenCenterPos.x, customScreenCenterPos.y, 0, 0));
            }

#if CINIMACHINE_3_0
            EffectUpdater(() => { }, UpdateCameraShake, EndCameraShake, ref _lastIsCameraShake, cameraShakeToggles);
#endif
            EffectUpdater(InitOverlayTexture, UpdateOverlayTexture, EndOverlayTexture, ref _lastIsOverlayTexture,
                overlayTextureToggles);
            EffectUpdater(InitFlash, UpdateFlash, EndFlash, ref _lastIsFlash, flashToggles);
            EffectUpdater(InitVignette, UpdateVignette, EndVignette, ref _lastIsVignette, vignetteToggles);

            bool hasEffect =
            (
                chromaticAberrationToggles |
                distortSpeedToggles |
#if CINIMACHINE_3_0
                cameraShakeToggles |
#endif
                radialBlurToggles |
                overlayTextureToggles |
                flashToggles |
                vignetteToggles
            ) > 0;

            if (hasEffect)
            {
                flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_NB_POSTPROCESS_ON);
            }
            else
            {
                flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_NB_POSTPROCESS_ON);
            }
        }


        private readonly int _customScreenCenterProperty = Shader.PropertyToID("_CustomScreenCenter");
        public static Vector2 customScreenCenterPos = new Vector2(0.5f, 0.5f);




        #region 色散

        //色散相关
        // private ChromaticAberration chromaticAberrationComp;
        // private bool preserveChromaticAberrationActive;
        // private float preserveChromaticAberrationIntensity;
        private bool _lastIsChromaticAberration = false;
        private readonly int _chromaticAberrationVecProperty = Shader.PropertyToID("_ChromaticAberrationVec");

        public static int chromaticAberrationToggles = 0;

        public static bool isCaByDistort = false;
        private bool _lastIsCaByDistort = false;

        public static float chromaticAberrationIntensity = 0;
        public static float chromaticAberrationPos = 0;
        public static float chromaticAberrationRange = 0;

        private void InitChromaticAberration()
        {
            // Debug.Log("InitCA");
            flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_CHORATICABERRAT);
        }

        private void UpdateChromaticAberration()
        {
            if (_lastIsCaByDistort != isCaByDistort)
            {
                if (isCaByDistort)
                {
                    flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_CHORATICABERRAT_BY_DISTORT);
                }
                else
                {
                    flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_CHORATICABERRAT_BY_DISTORT);
                }

                _lastIsCaByDistort = isCaByDistort;
            }

            material.SetVector(_chromaticAberrationVecProperty,
                new Vector4(chromaticAberrationIntensity, chromaticAberrationPos, chromaticAberrationRange));
            chromaticAberrationIntensity = 0; //等待下一次update
            chromaticAberrationPos = 0; //等待下一次update
            chromaticAberrationRange = 0; //等待下一次update
        }

        private void EndChromaticAberration()
        {
            // Debug.Log("EndCA");
            flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_CHORATICABERRAT);
        }

        #endregion

        #region 径向速度扭曲

        //径向速度扭曲相关
        private readonly int _distortVecProperty = Shader.PropertyToID("_SpeedDistortVec");
        private readonly int _distortVec2Property = Shader.PropertyToID("_SpeedDistortVec2");
        private bool _lastIsDistortSpeed = false;

        public static bool isDistortScreenUVMode;

        private bool _lastIsDistortScreenUVMode;

        //     [ShowInInspector]
        //     [LabelText("径向扭曲开关")]
        // #if UNITY_EDITOR
        //     [BinaryInt(8)]
        // #endif
        public static int distortSpeedToggles;

        public static Texture2D distortTexture2D;

        public static float distortSpeedIntensity = 0;
        public static float distortSpeedPosition = 0;
        public static float distortSpeedRange = 0;
        public static float distortSpeedMoveSpeedX = 0;
        public static float distortSpeedMoveSpeedY = 0;
        public static float distortTextureMidValue = 0;

        private void InitDistortSpeed()
        {
            flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_DISTORT_SPEED);
        }

        private void UpdateDistortSpeed()
        {
            if (_lastIsDistortScreenUVMode != isDistortScreenUVMode)
            {
                if (isDistortScreenUVMode)
                {
                    flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_POST_DISTORT_SCREEN_UV);
                }
                else
                {
                    flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_POST_DISTORT_SCREEN_UV);
                }

                _lastIsDistortScreenUVMode = isDistortScreenUVMode;
            }

            Vector4 distortVec = new Vector4(distortSpeedIntensity, distortSpeedPosition, distortSpeedRange,
                distortTextureMidValue);
            Vector4 distortVec2 = new Vector4(distortSpeedMoveSpeedX, distortSpeedMoveSpeedY, 0, 0);
            material.SetVector(_distortVecProperty, distortVec);
            material.SetVector(_distortVec2Property, distortVec2);
            distortSpeedIntensity = 0;
            distortSpeedPosition = 0;
            distortSpeedRange = 0;
            distortSpeedMoveSpeedX = 0;
            distortSpeedMoveSpeedY = 0;
        }

        private void EndDistortSpeed()
        {
            flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_DISTORT_SPEED);
        }

        #endregion

        #region 径向模糊

        // [ShowInInspector]
        // [LabelText("径向模糊开关")]
        // #if UNITY_EDITOR
        //     [BinaryInt(8)]
        // #endif
        public static int radialBlurToggles = 0;
        private readonly int _radialBlurVecProperty = Shader.PropertyToID("_RadialBlurVec");
        public static float radialBlurIntensity = 0;
        public static float radialBlurPos = 0;
        public static float radialBlurRange = 0;
        public static int radialBlurSampleCount = 4;
        public static bool isRadialBlurByDistort = false;
        private bool _lastIsRadialBlurByDistort = false;

        private bool _lastIsRadialBlur = false;

        private void InitRadialBlur()
        {
            // Debug.Log("InitRB");
            flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_RADIALBLUR);
        }

        private void UpdateRadialBlur()
        {
            if (_lastIsRadialBlurByDistort != isRadialBlurByDistort)
            {
                if (isRadialBlurByDistort)
                {
                    flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_RADIALBLUR_BY_DISTORT);
                }
                else
                {
                    flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_RADIALBLUR_BY_DISTORT);
                }

                _lastIsRadialBlurByDistort = isRadialBlurByDistort;
            }

            Vector4 radialBlurVec = new Vector4(radialBlurIntensity * 0.1f / radialBlurSampleCount, radialBlurPos,
                radialBlurRange, radialBlurSampleCount);
            material.SetVector(_radialBlurVecProperty, radialBlurVec);
            radialBlurIntensity = 0;
            radialBlurSampleCount = 0;
            radialBlurPos = 0;
            radialBlurRange = 0;
        }

        private void EndRadialBlur()
        {
            // Debug.Log("EndRB");
            flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_RADIALBLUR);
        }

        #endregion

        #region 震屏

#if CINIMACHINE_3_0

        public static int cameraShakeToggles = 0;


        private bool _lastIsCameraShake = false;
        public static float cameraShakeIntensity = 0;

        private void UpdateCameraShake()
        {
            if (_perlin)
            {
                _perlin.AmplitudeGain = cameraShakeIntensity;
            }

            cameraShakeIntensity = 0;
            // #if UNITY_EDITOR
            if (currentVirtualCamera)
            {
                CinemachineCore.SoloCamera = currentVirtualCamera;
            }
            // #endif
            // Debug.Log(_perlin.m_AmplitudeGain);
        }

        private void EndCameraShake()
        {
            if (_perlin)
            {
                _perlin.AmplitudeGain = 0;
            }

            CinemachineCore.SoloCamera = null;
        }
#endif

        #endregion

        #region 肌理叠加

        //肌理图
        //注意，肌理图就是硬切，只有intensity可以做差值。
        //     [ShowInInspector]
        //     [LabelText("肌理开关")]
        // #if UNITY_EDITOR
        //     [BinaryInt(8)]
        // #endif
        public static int overlayTextureToggles = 0;

        private bool _lastIsOverlayTexture = false;

        public static float overlayTextureIntensity = 0;

        private readonly int _overlayTextureProperty = Shader.PropertyToID("_TextureOverlay");
        private readonly int _overlayTextureStProperty = Shader.PropertyToID("_TextureOverlay_ST");
        private readonly int _overlayTextureIntensityProperty = Shader.PropertyToID("_TextureOverlayIntensity");

        private void InitOverlayTexture()
        {
            flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_OVERLAYTEXTURE);
        }

        private void UpdateOverlayTexture()
        {
            material.SetFloat(_overlayTextureIntensityProperty, overlayTextureIntensity);
            overlayTextureIntensity = 0;
        }

        private void EndOverlayTexture()
        {
            flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_OVERLAYTEXTURE);
        }

        #endregion

        #region 黑白闪

        //     [ShowInInspector]
        //     [LabelText("黑白闪开关")]
        // #if UNITY_EDITOR
        //     [BinaryInt(8)]
        // #endif
        public static int flashToggles = 0;

        private bool _lastIsFlash = false;

        public static float flashDesaturateIntensity = 0;

        public static float flashInvertIntensity = 0;

        public static float flashContrast = 0;

        public static Color flashColor = new Color(1, 1, 1, 1);
        public static Color blackFlashColor = new Color(0, 0, 0, 1);

        private readonly int _flashDesaturateProperty = Shader.PropertyToID("_DeSaturateIntensity");
        private readonly int _flashInvertProperty = Shader.PropertyToID("_InvertIntensity");
        private readonly int _flashContrastProperty = Shader.PropertyToID("_Contrast");
        private readonly int _flashColorProperty = Shader.PropertyToID("_FlashColor");
        private readonly int _blackFlashColorProperty = Shader.PropertyToID("_BlackFlashColor");

        private void InitFlash()
        {
            flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_FLASH);
        }

        private void UpdateFlash()
        {
            material.SetFloat(_flashDesaturateProperty, flashDesaturateIntensity);
            material.SetFloat(_flashInvertProperty, flashInvertIntensity);
            material.SetFloat(_flashContrastProperty, flashContrast);
            material.SetColor(_flashColorProperty, flashColor);
            material.SetColor(_blackFlashColorProperty, blackFlashColor);
            flashDesaturateIntensity = 0;
            flashInvertIntensity = 0;
            flashContrast = 0;
            flashColor = Color.white;
        }

        private void EndFlash()
        {
            flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_FLASH);
        }

        #endregion

        #region 暗角

        //     [ShowInInspector]
        //     [LabelText("暗角开关")]
        // #if UNITY_EDITOR
        //     [BinaryInt(8)]
        // #endif
        public static int vignetteToggles = 0;

        private bool _lastIsVignette;

        public static float vignetteIntensity = 0f;
        public static float vignetteRoundness = 0f;
        public static float vignetteSmothness = 0f;

        private readonly int _vignetteVecProperty = Shader.PropertyToID("_VignetteVec");

        private void InitVignette()
        {
            flags.SetFlagBits(NBPostProcessFlags.FLAG_BIT_VIGNETTE);
        }


        private void UpdateVignette()
        {
            Vector4 vignetteVec = new Vector4(vignetteIntensity, vignetteRoundness, vignetteSmothness, 0);
            material.SetVector(_vignetteVecProperty, vignetteVec);
            vignetteIntensity = 0;
            vignetteRoundness = 0;
            vignetteSmothness = 0;
        }

        private void EndVignette()
        {
            flags.ClearFlagBits(NBPostProcessFlags.FLAG_BIT_VIGNETTE);
        }


        #endregion


#if UNITY_EDITOR
        void EditorUpdate()
        {
            if (!Application.isPlaying)
            {
                LateUpdate(); //每帧Update会导致SceneView闪
            }
        }

        public void ReRegistEditorUpdate()
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }
#endif

    }
}


