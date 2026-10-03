Shader "Hidden/Codex/ShaderDebug/NormalDrawTimeH1P12a9c54027c0142ac8a86eedb83c79d0d"
//Shader "Effects/NBShader开发中"
{
    Properties
    {
    	_BigBlockModeSettingFoldOut("_BigBlockModeSettingFoldOut",Float) = 0
    	_BaseOptionBigBlockItemFoldOut("BaseOptionBigBlockItem",Float) = 0
    	_MainTexBigBlockItemFoldOut("_MainTexBigBlockItemFoldOut",Float) = 0
    	_LightBigBlockItemFoldOut("_LightBigBlockItemFoldOut",Float) = 0
    	_FeatureBigBlockItemFoldOut("_FeatureBigBlockItemFoldOut",Float) = 0
        _TABigBlockItemFoldOut("_TABigBlockItemFoldOut",Float) = 0
        [HideInInspector] _NBShaderFeatureTier("NBShader Feature Tier", Float) = 3
        _BaseColorAdjustmentFoldOut("_BaseColorAdjustmentFoldOut",Float) = 0
        _HueShiftFoldOut("_HueShiftFoldOut",Float) = 0
        _SaturabilityFoldOut("_SaturabilityFoldOut",Float) = 0
        _ContrastFoldOut("_ContrastFoldOut",Float) = 0
        _BaseMapColorRefineFoldOut("_BaseMapColorRefineFoldOut",Float) = 0
        _BaseBackColorFoldOut("_BaseBackColorFoldOut",Float) = 0
        _DistanceFadeFoldOut("_DistanceFadeFoldOut",Float) = 0
        _SoftParticlesFoldOut("_SoftParticlesFoldOut",Float) = 0
        _BaseMapFoldOut("_BaseMapFoldOut",Float) = 0
        _MainTexUVModeFoldOut("_MainTexUVModeFoldOut",Float) = 0
        _GlobalTwirlFoldOut("_GlobalTwirlFoldOut",Float) = 0
        _GlobalPolarFoldOut("_GlobalPolarFoldOut",Float) = 0
        _BumpToggleFoldOut("_BumpToggleFoldOut",Float) = 0
        _BumpTexFoldOut("_BumpTexFoldOut",Float) = 0
        _BumpUVModeFoldOut("_BumpUVModeFoldOut",Float) = 0
        _MatCapFoldOut("_MatCapFoldOut",Float) = 0
        _MaskBlockFoldOut("_MaskBlockFoldOut",Float) = 0
        _MaskRefineFoldOut("_MaskRefineFoldOut",Float) = 0
        _MaskMapFoldOut("_MaskMapFoldOut",Float) = 0
        _MaskUVModeFoldOut("_MaskUVModeFoldOut",Float) = 0
        _MaskRotationFoldOut("_MaskRotationFoldOut",Float) = 0
        _Mask2BlockFoldOut("_Mask2BlockFoldOut",Float) = 0
        _Mask2UVModeFoldOut("_Mask2UVModeFoldOut",Float) = 0
        _Mask3BlockFoldOut("_Mask3BlockFoldOut",Float) = 0
        _Mask3UVModeFoldOut("_Mask3UVModeFoldOut",Float) = 0
        _NoiseBlockFoldOut("_NoiseBlockFoldOut",Float) = 0
        _ScreenDistortAlphaFoldOut("_ScreenDistortAlphaFoldOut",Float) = 0
        _NoiseMapFoldOut("_NoiseMapFoldOut",Float) = 0
        _NoiseUVModeFoldOut("_NoiseUVModeFoldOut",Float) = 0
        _NoiseMaskBlockFoldOut("_NoiseMaskBlockFoldOut",Float) = 0
        _NoiseMaskUVModeFoldOut("_NoiseMaskUVModeFoldOut",Float) = 0
        _ChromaticAberrationFoldOut("_ChromaticAberrationFoldOut",Float) = 0
        _EmissionBlockFoldOut("_EmissionBlockFoldOut",Float) = 0
        _EmissionUVModeFoldOut("_EmissionUVModeFoldOut",Float) = 0
        _ColorBlendBlockFoldOut("_ColorBlendBlockFoldOut",Float) = 0
        _ColorBlendUVModeFoldOut("_ColorBlendUVModeFoldOut",Float) = 0
        _RampColorBlockFoldOut("_RampColorBlockFoldOut",Float) = 0
        _RampColorUVModeFoldOut("_RampColorUVModeFoldOut",Float) = 0
        _DissolveBlockFoldOut("_DissolveBlockFoldOut",Float) = 0
        _DissolveMapFoldOut("_DissolveMapFoldOut",Float) = 0
        _DissolveUVModeFoldOut("_DissolveUVModeFoldOut",Float) = 0
        _DissolveLineFoldOut("_DissolveLineFoldOut",Float) = 0
        _DissolveRampFoldOut("_DissolveRampFoldOut",Float) = 0
        _DissolveRampUVModeFoldOut("_DissolveRampUVModeFoldOut",Float) = 0
        _DissolveMaskFoldOut("_DissolveMaskFoldOut",Float) = 0
        _DissolveMaskUVModeFoldOut("_DissolveMaskUVModeFoldOut",Float) = 0
        _ProgramNoiseBlockFoldOut("_ProgramNoiseBlockFoldOut",Float) = 0
        _ProgramNoiseUVModeFoldOut("_ProgramNoiseUVModeFoldOut",Float) = 0
        _ProgramNoiseSimpleFoldOut("_ProgramNoiseSimpleFoldOut",Float) = 0
        _ProgramNoiseVoronoiFoldOut("_ProgramNoiseVoronoiFoldOut",Float) = 0
        _SharedUVBlockFoldOut("_SharedUVBlockFoldOut",Float) = 0
        _SharedUVModeFoldOut("_SharedUVModeFoldOut",Float) = 0
        _FresnelBlockFoldOut("_FresnelBlockFoldOut",Float) = 0
        _VertexOffsetBlockFoldOut("_VertexOffsetBlockFoldOut",Float) = 0
        _VertexOffsetUVModeFoldOut("_VertexOffsetUVModeFoldOut",Float) = 0
        _VertexOffsetMaskBlockFoldOut("_VertexOffsetMaskBlockFoldOut",Float) = 0
        _VertexOffsetMaskUVModeFoldOut("_VertexOffsetMaskUVModeFoldOut",Float) = 0
        _DepthOutlineBlockFoldOut("_DepthOutlineBlockFoldOut",Float) = 0
        _ParallaxBlockFoldOut("_ParallaxBlockFoldOut",Float) = 0
        _ParallaxUVModeFoldOut("_ParallaxUVModeFoldOut",Float) = 0
        _PortalBlockFoldOut("_PortalBlockFoldOut",Float) = 0
        _VATBlockFoldOut("_VATBlockFoldOut",Float) = 0
        _ZOffsetBlockFoldOut("_ZOffsetBlockFoldOut",Float) = 0
        _OverrideZBlockFoldOut("_OverrideZBlockFoldOut",Float) = 0
        _CustomStencilTestFoldOut("_CustomStencilTestFoldOut",Float) = 0
        _ShaderKeywordFoldOut("_ShaderKeywordFoldOut",Float) = 0
    	_MeshSourceMode("Mesh来源模式",Float) = 0
        _UIEffect_Toggle("UI模式_Toggle",Float) = 0
        _DistortionBothDirection_Toggle("__DistortionBothDirection_Toggle",Float) = 0
        _DistanceFade_Toggle("_DistanceFade_Toggle",Float) = 0
        _ChangeSaturability_Toggle("__ChangeSaturability_Toggle",Float) = 0
        _Mask_Toggle("__Mask_Toggle",Float) = 0
        _Mask_RotationToggle("__Mask_RotationToggle",Float) = 0
        _Mask2_Toggle("__Mask2_Toggle",Float) = 0
        _Mask3_Toggle("__Mask3_Toggle",Float) = 0
        _BaseBackColor_Toggle("__BaseBackColor_Toggle",Float) = 0
        _UseUV1_Toggle("__UseUV1_Toggle",Float) = 0
        _TransparentMode("_TransparentMode",Float) = 1
        _ForceZWriteToggle("_ForceZWriteToggle",Float) = 0
        _VAT_Toggle("VAT顶点动画图",Float) = 0
        _VATMode("VAT模式",Float) = 0
        [NoScaleOffset]_VATTex("VAT texture", 2D) = "black" {}
        _ImportScale("ImportScale", Float) = 0.01
        [HideInInspector] _TyFlowVATSubMode("TyFlow VAT Sub Mode", Float) = 0
        _DeformingSkin("Deforming skin", Float) = 0
        _SkinBoneCount("Skin bone count", Float) = 2
        _RGBAEncoded("RGBA encoded", Float) = 1
        _RGBAHalf("RGBA half", Float) = 1
        _LinearToGamma("Gamma correction", Float) = 1
        _VATIncludesNormals("VAT includes normals", Float) = 0
        [HideInInspector] _HoudiniVATSubMode("Houdini VAT Sub Mode", Float) = 0
        [HideInInspector] _B_autoPlayback("Auto Playback", Float) = 1
        [HideInInspector] _gameTimeAtFirstFrame("Game Time at First Frame", Float) = 0
        [HideInInspector] _playbackSpeed("Playback Speed", Float) = 1
        [HideInInspector] _houdiniFPS("Houdini FPS", Float) = 24
        [HideInInspector] _displayFrame("Display Frame", Float) = 0
        [HideInInspector] _B_interpolate("Interframe Interpolation", Float) = 1
        [HideInInspector] _animateFirstFrame("Animate First Frame", Float) = 0
        [HideInInspector] _frameCount("Frame Count", Float) = 1
        [HideInInspector] _boundMinX("Bound Min X", Float) = -1
        [HideInInspector] _boundMinY("Bound Min Y", Float) = -1
        [HideInInspector] _boundMinZ("Bound Min Z", Float) = -1
        [HideInInspector] _boundMaxX("Bound Max X", Float) = 1
        [HideInInspector] _boundMaxY("Bound Max Y", Float) = 1
        [HideInInspector] _boundMaxZ("Bound Max Z", Float) = 1
        [HideInInspector] _globalPscaleMul("Global Piece Scale Multiplier", Float) = 1
        [HideInInspector] _B_pscaleAreInPosA("Piece Scales in Position Alpha", Float) = 1
        [HideInInspector] _widthBaseScale("Width Base Scale", Float) = 0.2
        [HideInInspector] _heightBaseScale("Height Base Scale", Float) = 0.2
        [HideInInspector] _B_hideOverlappingOrigin("Hide Overlapping Origin", Float) = 1
        [HideInInspector] _originRadius("Origin Effective Radius", Float) = 0.02
        [HideInInspector] _B_CAN_SPIN("Particles Can Spin", Float) = 0
        [HideInInspector] _B_spinFromHeading("Compute Spin from Heading", Float) = 0
        [HideInInspector] _spinPhase("Particle Spin Phase", Float) = 0
        [HideInInspector] _scaleByVelAmount("Scale by Velocity Amount", Float) = 1
        [HideInInspector] _particleTexUScale("Particle Texture U Scale", Float) = 1
        [HideInInspector] _particleTexVScale("Particle Texture V Scale", Float) = 1
        [HideInInspector] _B_LOAD_POS_TWO_TEX("Positions Require Two Textures", Float) = 0
        [HideInInspector] _B_UNLOAD_ROT_TEX("Use Compressed Normals", Float) = 0
        [HideInInspector] _B_LOAD_COL_TEX("Load Color Texture", Float) = 0
        [HideInInspector] _B_LOAD_LOOKUP_TABLE("Load Lookup Table", Float) = 0
        [HideInInspector][NoScaleOffset] _posTexture("Houdini Position Texture", 2D) = "black" {}
        [HideInInspector][NoScaleOffset] _posTexture2("Houdini Position Texture 2", 2D) = "black" {}
        [HideInInspector][NoScaleOffset] _rotTexture("Houdini Rotation Texture", 2D) = "black" {}
        [HideInInspector][NoScaleOffset] _colTexture("Houdini Color Texture", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _lookupTable("Houdini Lookup Table", 2D) = "white" {}
        _AffectsShadows("Affects shadows", Float) = 0
        _TransparentShadowDitherToggle("半透明Dither阴影", Float) = 0
        _Frame("Frame", Float) = 0
        _Frames("Frames", Float) = 10
        _FrameInterpolation("Frame interpolation", Float) = 1
        _Loop("Loop", Float) = 1
        _InterpolateLoop("Interpolate loop", Float) = 1
        _Autoplay("Autoplay", Float) = 0
        _AutoplaySpeed("AutoplaySpeed", Float) = 1
        
        _Dissolve_Toggle("__Dissolve_Toggle",Float) = 0
        _DissolveMask_Toggle("__DissolveMask_Toggle",Float) = 0
        _Dissolve_useRampMap_Toggle("__Dissolve_useRampMap_Toggle",Float) = 0
        _ProgramNoise_Toggle("_ProgramNoise_Toggle",Float) = 0
        _ProgramNoise_Simple_Toggle("_ProgramNoise_Simple_Toggle",Float) = 0
        _ProgramNoise_Rotate("_ProgramNoise_Rotate",Float) = 0
        _ProgramNoise_Voronoi_Toggle("_ProgramNoise_Voronoi_Toggle",Float) = 0
        _ProgramNoiseBaseBlendOpacity("两种程序噪波混合强度",Float) = 0
	    

        _FresnelMode("__FresnelMode",Float) = 0
        _InvertFresnel_Toggle("__InvertFresnel_Toggle",Float) = 0
        _HueShift_Toggle("__HueShift_Toggle",Float) = 0
        _ColorAdjustmentOnlyAffectMainTex("颜色调整仅影响主贴图开关",Float) = 0
        _ColorMultiAlpha("颜色乘以透明度",Float) = 0
        _BackFaceColor_Toggle("_BackFaceColor_Toggle",Float) = 0
        _BackFirstPassToggle("_BackFirstPassToggle",Float) = 0
        
        _PolarCordinateOnlySpecialFunciton_Toggle("极坐标仅对特殊功能生效_Toggle",Float) = 0
        
        _CustomData1X_MainTexOffsetX_Toggle("_CustomData1X_MainTexOffsetX_Toggle",Float) = 0
        _CustomData1Y_MainTexOffsetY_Toggle("_CustomData1Y_MainTexOffsetY_Toggle",Float) = 0
        _CustomData1Z_Dissolve_Toggle("_CustomData1Z_Dissolve_Toggle",Float) = 0
        _CustomData1W_HueShift_Toggle("_CustomData1W_HueShift_Toggle",Float) = 0
        _CustomData2X_MaskMapOffsetX_Toggle("_CustomData2X_MaskMapOffsetX_Toggle",Float) = 0
        _CustomData2Y_MaskMapOffsetY_Toggle("_CustomData2Y_MaskMapOffsetY_Toggle",Float) = 0
        _CustomData2Z_FresnelOffset_Toggle("_CustomData2Z_FresnelOffset_Toggle",Float) = 0
        _CustomData2W_Toggle("_CustomData2W_Toggle",Float) = 0
        
//        [PerRendererData] [MainTexture] _MainTex("Sprite Texture-ignore", 2D) = "white" {}
        [PerRendererData] _MainTex ("Sprite Texture-ignore", 2D) = "white" {}
        _Color("颜色贴图叠加", Color) = (1,1,1,1)
        _UI_MainTex_ST("UI模式主贴图 xy:UV缩放 zw:UV偏移",vector) = (1,1,0,0)
        _MainTex_Reverse_ST("MainTex_Reverse_ST-ignore",Vector) = (1,1,0,0)
        
        _BaseMap ("主贴图 xy:UV缩放 zw:UV偏移", 2D) = "white" { }
        _BaseMapMaskMapOffset ("xy主贴图偏移速度", vector) = (0, 0, 0, 0)

        _BaseMapUVRotation ("主贴图旋转", Range(0, 360)) = 0
        _BaseMapUVRotationSpeed ("主贴图旋转速度",Float) = 0
        [HDR]_BaseColor ("主贴图颜色_hdr", Color) = (1, 1, 1, 1)//HDR颜色不需要做Gamma Linear转换，Unity默认用Linear颜色
//        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)//HDR颜色不需要做Gamma Linear转换，Unity默认用Linear颜色
        [HDR]_BaseBackColor ("背面颜色_hdr", Color) = (1, 1, 1, 1)//HDR颜色不需要做Gamma Linear转换，Unity默认用Linear颜色
        _BaseColorIntensityForTimeline("整体颜色强度", Range(0,10)) = 1 //独立出来是以为Timeline  K颜色的时候会很奇怪的影响色相
        _Saturability("饱和度", range(0,1)) = 0
        _Contrast_Toggle("__Contrast_Toggle",Float) = 0
        _Contrast("对比度", Float) = 1
        _ContrastMidColor ("对比度中值颜色", Color) = (0.5, 0.5, 0.5, 1)//HDR颜色不需要做Gamma Linear转换，Unity默认用Linear颜色
        _HueShift("色相",Range(0,1)) = 0
		_BaseMapColorRefine_Toggle ("__BaseMapColorRefine_Toggle",Float) = 0
        _BaseMapColorRefine("主贴图颜色Refine",Vector) = (1,1,2,0.5)
        _AlphaAll("整体透明度",Range(0,1)) = 1
        _IgnoreVetexColor_Toggle("_IgnoreVetexColor_Toggle",Float) = 0
        _MainTexPNoiseBlendOpacity("主贴图噪波图混合强度",Float) = 1
    	
    	_SpecialUVChannelMode("特殊UV通道选择",Float) = 0
    	
    	_CylinderUVRotate("圆柱UV旋转",Vector) = (0,0,90,0)
    	_CylinderUVPosOffset("圆柱UV位置偏移",Vector) = (0,0,0,0)
    	_CylinderMatrix0("圆柱偏移矩阵0",Vector) = (0,0,0,0)
    	_CylinderMatrix1("圆柱偏移矩阵1",Vector) = (0,0,0,0)
    	_CylinderMatrix2("圆柱偏移矩阵2",Vector) = (0,0,0,0)
    	_CylinderMatrix3("圆柱偏移矩阵3",Vector) = (0,0,0,0)
        
        _Cutoff ("裁剪位置", float) = 0.5
    	
    	//--------------光照部分-------------
    	_FxLightMode("灯光模式",Float) = 0
    	_BumpMapToggle("法线贴图开关",Float) = 0
    	_BumpMapMaskMode("法线贴图多通道模式",Float) = 0
    	_BumpScale("Scale", Float) = 1.0
        _BumpTex("Normal Map", 2D) = "bump" {}
    	_MaterialInfo("x:金属度,y:光滑度",Vector) = (1,1,0,0)
    	_BlinnPhongSpecularToggle("BlinnPhong高光开关",Float) = 0
    	[HDR]_SpecularColor("BlinnPhong高光颜色",Color) = (1,1,1,1)
	    //-----------SixWayLight----------
    	_RigRTBk("六路正方向图(P)",2D) = "white"{}
    	_RigLBtF("六路反方向图(N)",2D) = "white"{}
    	_SixWayColorAbsorptionToggle("六路光颜色吸收开关",Float) = 0
    	_SixWayInfo("x:六路吸收强度",Vector) = (0.5,0,0,0)
    	_SixWayEmissionRamp("六路自发光Ramp",2D) = "white"{}
    	[HDR]_SixWayEmissionColor("六路自发光颜色",Color) = (1,0.5,0,1)
    	
    	//-----MatCap------
    	_MatCapToggle("MatCap开关",Float) = 0
    	_MatCapTex("MatCap图",2D) = "white"{}
    	[HDR]_MatCapColor("MatCap颜色",Color) = (1,1,1,1)
    	_MatCapInfo("x:MatCap叠加和相乘过渡",Vector) = (1,0,0,0)
//    	_MatCapBlendMode("MatCap叠加模式",Float) = 0

        //时间缩放影响开关----------
        [HideInInspector] _TimeMode("__TimeMode",float) = 0.0
    	
    	_StencilWithoutPlayerToggle("剔除主角色开关",Float) = 0.0
 
        // MaskMap-----------
    	_MaskRefineToggle("遮罩整体调整开关",Float) = 0
    	_MaskRefineVec("遮罩整体调整：x:Pow,y:相乘,z:相加",Vector) = (1,1,0,0)
        _MaskMap ("遮罩贴图 xy:UV缩放 zw:UV偏移", 2D) = "white" { }
        _MaskMap2 ("遮罩2贴图 xy:UV缩放 zw:UV偏移", 2D) = "white"{}
        _MaskMap3 ("遮罩3贴图 xy:UV缩放 zw:UV偏移", 2D) = "white"{}
        _MaskMapOffsetAnition("xy:遮罩偏移速度，zw:遮罩2偏移速度", vector) = (0,0,0,0)
        _MaskMap3OffsetAnition("xy:遮罩3偏移速度", vector) = (0,0,0,0)
        _MaskMapUVRotation ("遮罩旋转", Range(0, 360)) = 0.0
        _MaskDistortion_intensity ("遮罩扭曲强度", float) = 0.0
        _MaskMapRotationSpeed("遮罩旋转速度", float) = 0.0
        _MaskMapVec("x整体遮罩强度,y遮罩2旋转,z遮罩3旋转",Vector) = (1,0,0,0)
    	
    	_MaskMapGradientToggle("遮罩渐变模式",Float) = 0
    	_MaskMapGradientCount("颜色映射数量",Integer) = 2
    	_MaskMapGradientFloat0("x:MaskAlpha0,y:Pos0,z:MaskAlpha1,w:Pos1",Vector) = (0,0,1,1)
    	_MaskMapGradientFloat1("x:MaskAlpha2,y:Pos2,z:MaskAlpha3,w:Pos3",Vector) = (1,0,1,1)
    	_MaskMapGradientFloat2("x:MaskAlpha4,y:Pos4,z:MaskAlpha5,w:Pos5",Vector) = (1,0,1,1)
    	_MaskMap2GradientToggle("遮罩2渐变模式",Float) = 0
    	_MaskMap2GradientCount("颜色映射数量",Integer) = 2
    	_MaskMap2GradientFloat0("x:Mask2Alpha0,y:Pos0,z:Mask2Alpha1,w:Pos1",Vector) = (0,0,1,1)
    	_MaskMap2GradientFloat1("x:Mask2Alpha2,y:Pos2,z:Mask2Alpha3,w:Pos3",Vector) = (1,0,1,1)
    	_MaskMap2GradientFloat2("x:Mask2Alpha4,y:Pos4,z:Mask2Alpha5,w:Pos5",Vector) = (1,0,1,1)
    	_MaskMap3GradientToggle("遮罩3渐变模式",Float) = 0
    	_MaskMap3GradientCount("颜色映射数量",Integer) = 2
	    _MaskMap3GradientFloat0("x:Mask3Alpha0,y:Pos0,z:Mask3Alpha1,w:Pos1",Vector) = (0,0,1,1)
    	_MaskMap3GradientFloat1("x:Mask3Alpha2,y:Pos2,z:Mask3Alpha3,w:Pos3",Vector) = (1,0,1,1)
    	_MaskMap3GradientFloat2("x:Mask3Alpha4,y:Pos4,z:Mask3Alpha5,w:Pos5",Vector) = (1,0,1,1)
        _MaskPNoiseBlendOpacity("遮罩噪波图混合强度",Float) = 1
        
        // 擦除----------------
        //[Header(ChaChu(Anima For CustomData.z).......)]
        //[KeywordEnum(NoChange,XianXing, JingXiang, Self)] _ch ("ChaChu mode", Float) = 0
        [HideInInspector] _Chachu ("__Chachu_ignore", Float) = 0.0
        _EdgeFade ("EdgeFade_ignore", Range(0, 1)) = 0.05
        _XianXingCH_UVRota ("XianXingCH_UVRota_ignore", float) = 0
        _jingxiangCH_dire ("Direction_ignore", Range(0, 1)) = 0
        
        // 漩涡 -------------------
        //[Toggle(_JIZUOBIAO)] _N121 ("JIZUOBIAO?", float) = 0
        [HideInInspector] _UTwirlEnabled ("__UTwirlEnabled", Float) = 0.0
        _TWParameter ("xy:旋转扭曲中心", vector) = (0.5, 0.5, 0, 0)
        _TWStrength ("旋转扭曲强度", float) = 1
        
        
        // 极坐标 -------------------
        //[Toggle(_JIZUOBIAO)] _N121 ("JIZUOBIAO?", float) = 0
        [HideInInspector]_PolarCoordinatesEnabled ("__PolarCoordinatesEnabled", Float) = 0.0
        _PCCenter ("xy:极坐标中心 z:极坐标强度", vector) = (0.5, 0.5, 1, 0)//位置坐标用的前两个分量，z分量给强度。
        
        // 噪波 --------------
        //[Toggle(_NOISEMAP)]_N ("NOISEMAP?", float) =0
        [HideInInspector] _ScreenDistortModeToggle("_ScreenDistortModeToggle",Float) = 0
        [HideInInspector] _DisableMainPassToggle("_ScreenDistortModeToggle",Float) = 0
        [HideInInspector] _DistortMode("_DistortMode",Float) = 0
        [HideInInspector] _noisemapEnabled ("__noisemapEnabled", Float) = 0.0
        [HideInInspector] _noiseMaskMap_Toggle ("__noiseMaskMap_Toggle", Float) = 0.0
    	_RefractionIOR("折射率",Float) = 1.5
        _NoiseMap ("扭曲贴图 xy:UV缩放 zw:UV偏移", 2D) = "white" { }
        _NoiseMaskMap ("扭曲遮罩贴图 xy:UV缩放 zw:UV偏移", 2D) = "white" { }
        _NoiseMapUVRotation("扭曲旋转",Range(0,360)) = 0
        _NoiseOffset ("xy:扭曲偏移速度 ", vector) = (0, 0, 0, 0)//w分量为本地uv坐标到世界坐标的变化
    	_TexDistortion_intensity ("主贴图扭曲强度", float) = 0.5
    	_NoiseIntensity("整体扭曲强度",Float) = 1
    	_ScreenDistortIntensity("屏幕扭曲强度",Float) = 1
        _DistortPNoiseBlendOpacity("扭曲噪波图混合强度",Float) = 1
    	
    	_ScreenDistortAlphaRefineToggle("屏幕扭曲Alpha调整开关",Float) = 0
    	_ScreenDistortAlphaPow("屏幕扭曲AlphaPow",Float) = 1
    	_ScreenDistortAlphaMulti("屏幕扭曲Alpha相乘",Float) = 1
    	_ScreenDistortAlphaAdd("屏幕扭曲Alpha相加",Float) = 0
        _DistortionDirection ("扭曲方向xy, 色散强度z", vector) = (1,1,0,0)
        _Distortion_Choraticaberrat_Toggle("扭曲色散开关_Toggle",Float) = 0 
        _Distortion_Choraticaberrat_WithNoise_Toggle("色散受扭曲影响_Toggle",Float) = 1
	    
        // 叠加贴图 1（旧流光属性名保留） ----------
        //[Header(LiuGuang(Anima For CustomData.w).......)]
        //[Toggle(_EMISSION)]_N1 ("EMISSION?", float) = 0
        [HideInInspector] _EmissionEnabled ("__EmissionEnabled", Float) = 0.0
        _EmissionBlendMode("叠加贴图1混合方式", Float) = 0
        _EmissionMap ("流光贴图 xy:UV缩放 zw:UV偏移", 2D) = "white" { }
        _EmissionMapUVRotation ("流光贴图旋转", Range(0, 360)) = 0
        _Emi_Distortion_intensity ("流光贴图扭转强度", float) = 0
        _EmissionMapUVOffset ("xy:流光贴图偏移速度", vector) = (0, 0, 0, 0)
        _EmissionSelfAlphaWeight ("__EmissionSelfAlphaWeight_ignore", float) = 0
        _uvRapSoft ("LiuuvRapSoft-ignore", Range(0, 1)) = 0
        [HDR]_EmissionMapColor ("流光贴图颜色_hdr", Color) = (1, 1, 1, 1)
        _EmissionMapColorIntensity("流光颜色强度", float) = 1
        _EmissionAlphaMultiplyMode("叠加贴图1 Alpha作用", Float) = 0
        _EmissionAlphaIntensity("叠加贴图1 Alpha强度", Range(0, 1)) = 1
    	
        //叠加贴图 2（旧颜色渐变属性名保留）--------
    	_ColorBlendMap_Toggle("__ColorBlendMap_Toggle",Float) = 0
        _ColorBlendMode("叠加贴图2混合方式", Float) = 1
        _ColorBlendMap("颜色渐变贴图 xy:UV缩放 zw:UV偏移",2D) = "white"{}
        [HDR]_ColorBlendColor("颜色渐变叠加_hdr",Color) = (1,1,1,1)
        _ColorBlendColorIntensity("叠加贴图2颜色强度", Float) = 1
        _ColorBlendMapOffset("xy:颜色渐变贴图偏移动画",Vector) = (0,0,0,0)
		_ColorBlendAlphaMultiplyMode("叠加贴图2 Alpha作用",Float) = 0
    	_ColorBlendVec("x:颜色渐变扰动强度z:Alpha强度w:旋转",Vector) = (0,0,1,0)
    	
    	//颜色映射Ramp
    	_RampColorToggle("颜色映射开关",Float) = 0
    	_RampColorSourceMode("Ramp来源模式",Float) = 0
    	_RampColorBlendMode("Ramp颜色混合模式",Float) = 0
    	_RampColorMap("颜色映射黑白图",2D) = "white"{}
    	_RampColor0("rgb:RampColor0,a:pos",Color) = (0,0,0,0)
    	_RampColor1("rgb:RampColor1,a:pos",Color) = (1,0,0,1)
    	_RampColor2("rgb:RampColor2,a:pos",Color) = (1,1,1,1)
    	_RampColor3("rgb:RampColor3,a:pos",Color) = (1,1,1,1)
    	_RampColor4("rgb:RampColor4,a:pos",Color) = (1,1,1,1)
    	_RampColor5("rgb:RampColor5,a:pos",Color) = (1,1,1,1)
    	_RampColorAlpha0("x:RampColorAlpha0,y:Pos0,z:RampColorAlpha1,w:Pos1",Vector) = (1,0,1,1)
    	_RampColorAlpha1("x:RampColorAlpha2,y:Pos2,z:RampColorAlpha3,w:Pos3",Vector) = (1,0,1,1)
    	_RampColorAlpha2("x:RampColorAlpha4,y:Pos4,z:RampColorAlpha5,w:Pos5",Vector) = (1,0,1,1)
    	_RampColorCount("颜色映射数量",Integer) = 131074
    	[HDR]_RampColorBlendColor("颜色映射叠加颜色_hdr",Color) = (1,1,1,1)
	    _RampColorMapOffset("xy:颜色映射贴图偏移动画,w:旋转",Vector) = (0,0,0,0)

        
        // Rongjie ------------------
        // [Header(RongJie(Anima For CustomData.y).......)]
        // [Toggle(_DISSOLVE)]_RJ ("RONGJIE?", float) = 0
        _Dissolve ("x:溶解强度 y:溶解值Pow z:过程溶解强度 w:溶解硬软度", vector) = (0.5, 1, 1, 0.1)
        _DissolveMap("溶解贴图 xy:UV缩放 zw:UV偏移",2D) = "grey"{}
        _DissolveMaskMap("局部溶解蒙版 xy:UV缩放 zw:UV偏移",2D) = "white"{}
        _DissolveMaskMode("溶解遮罩模式", Float) = 0
        _DissolveOffsetRotateDistort("xy:溶解贴图偏移速度 z:溶解贴图旋转",Vector) = (0,0,0,0)
        [HDR]_DissolveLineColor("溶解描边颜色_hdr",Color) = (1,0,0,1)
        _DissolveVoronoi_Vec("xy:噪波1缩放,zw:噪波2缩放",Vector) = (1,1,2,2)
        _DissolveVoronoi_Vec2("x:噪波1和噪波2混合系数(圆尖),y:噪波整体和溶解贴图混合系数,z:噪波1速度,w:噪波2速度",Vector) = (1,1,2,2)
        _DissolveVoronoi_Vec3("xy:噪波1偏移速度,zw:噪波2偏移速度",Vector) = (0,0,0,0)
    	_DissolveVoronoi_Vec4("xy:噪波1偏移,zw:噪波2偏移",Vector) = (0,0,0,0)
        _Dissolve_Vec2("x:Ramp位置偏移,y:Ramp范围",Vector) = (0.2,0.1,0,0)
        _DissolveRampMap("溶解Ramp图",2D) = "white"{}
    	_DissolveRampColorBlendMode("溶解Ramp图混合模式",Float) = 0
        [HDR]_DissolveRampColor("溶解Ramp颜色_hdr",Color) = (1,1,1,1)
    	_DissolveLineMaskToggle("溶解描边开关",Float) = 0
	    
    	_DissolveRampSourceMode("溶解Ramp来源模式",Float) = 0
    	_DissolveRampColor0("rgb:DissolveRampColor0,a:pos",Color) = (1,0,0,0)
    	_DissolveRampColor1("rgb:DissolveRampColor1,a:pos",Color) = (0,0,0,1)
    	_DissolveRampColor2("rgb:DissolveRampColor2,a:pos",Color) = (1,1,1,1)
    	_DissolveRampColor3("rgb:DissolveRampColor3,a:pos",Color) = (1,1,1,1)
    	_DissolveRampColor4("rgb:DissolveRampColor4,a:pos",Color) = (1,1,1,1)
    	_DissolveRampColor5("rgb:DissolveRampColor5,a:pos",Color) = (1,1,1,1)
    	_DissolveRampAlpha0("x:DissolveRampAlpha0,y:Pos0,z:DissolveRampAlpha1,w:Pos1",Vector) = (1,0,1,1)
    	_DissolveRampAlpha1("x:DissolveRampAlpha2,y:Pos2,z:DissolveRampAlpha3,w:Pos3",Vector) = (1,0,1,1)
    	_DissolveRampAlpha2("x:DissolveRampAlpha4,y:Pos4,z:DissolveRampAlpha5,w:Pos5",Vector) = (1,0,1,1)
    	_DissolveRampCount("溶解Ramp映射数量",Integer) = 131074
        _DissolvePNoiseBlendOpacity("溶解噪波图混合强度",Float) = 1

        _SharedUVToggle("公共UV开关",Float) = 0
        _SharedUV_ST("公共UVTillingOffset",Vector) = (1,1,0,0)
        _SharedUV_Vec("xy:公共UV偏移速度,z:旋转,w:旋转速度",Vector) = (0,0,0,0)
	    

        _CustomData1X ("ignore", float) = 0
        _CustomData1Y ("ignore", float) = 0
        //因为希望本Shader兼容CanvasRender，由于Canvas渲染只会传递TEXCOORD通道的xy分量（zw分量忽略）的特性,所以强制让CustomedData只传递xy分量。
        _CustomData2X ("ignore", float) = 0
        
        // -------------------------------------
        // Particle specific   属于粒子特殊的属性
        [ToggleOff] _CustomData ("__CustomData_Toggle", Float) = 0.0   //Toggleoff 和 Toggle 的区别
        [HideInInspector] _BaseMap_AnimationSheetBlend_ST ("AnimationSheetHelper next frame ST", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AnimationSheetHelperBlendIntensity ("AnimationSheetHelper blend weight", Float) = 0
        [ToggleOff] _FlipbookBlending ("__flipbookblending_Toggle", Float) = 0.0   //Toggleoff 和 Toggle 的区别
//        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
//        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        //[Toggle] _Fading ("Fading?     =Default(1,2,0,0)", Float) = 0.0
        _CameraNearFadeDistance ("Camera Near Fade-ignore", Float) = 1.0
        _CameraFarFadeDistance ("Camera Far Fade-ignore", Float) = 2.0
        //临时
        _fogintensity ("雾影响强度", Range(0, 1)) = 1
        // -------------------------------------
        // Hidden properties - Generic  通用的隐藏属性
    	_AdditiveToPreMultiplyAlphaLerp("相加到预乘混合",Float) = 0.0
        [HideInInspector] _Blend ("__mode-ignore", Float) = 0.0
        [HideInInspector] _AlphaClip ("__clip-ignore", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src-ignore", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst-ignore", Float) = 0.0
        [HideInInspector] _Cull ("__cull-ignore", Float) = 2.0
        [HideInInspector] _ZTest ("__ztest-ignore", Float) = 4.0 //默认值LEqual
        [HideInInspector] _ZWrite("__ZWrite-ignore", Float) = 0 //默认值LEqual
        // [HideInInspector] _ZTestt ("__ztestt", Float) = 4.0//雨轩：注释掉了。。。这是个什么鬼。。。
	    
        _CustomStencilTest ("__CustomStencilTest-ignore", Float) = 0
    	_StencilKeyIndex("__StencilKeyIndex-ignore",Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("__StencilComp-ignore", Float) = 8
        _Stencil("Stencil ID-ignore", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]_StencilOp("Stencil Operation-ignore", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]_StencilFail("Stencil Fail-ignore", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]_StencilZFail("Stencil ZFail-ignore", Float) = 0
        _StencilWriteMask ("Stencil Write Mask-ignore", Float) = 255
        _StencilReadMask ("Stencil Read Mask-ignore", Float) = 255
        _ColorMask("Color Mask-ignore", Float) = 15

        //[HideInInspector] _ZTestO ("__ztest0", Float) = 1.0
        _FresnelFadeDistance ("菲涅尔透明乘数", float) = 1
        _FresnelUnit("菲涅尔通用", Vector) = (0,0.5,1,0.5)
        
        _DepthOutline_Toggle("深度描边",Float) = 0
        [HDR]_DepthOutline_Color("深度描边颜色_hdr",Color) = (1,1,1,1)
        _DepthOutline_Vec("菲涅尔深度描边参数",Vector) = (0,0.5,0,0)
    	_FresnelColorAffectByAlpha("菲涅尔颜色受Alpha影响",Float) = 1
//        _DepthOutline_withoutFresnel_Toggle("深度描边关闭菲涅尔",Float) = 0
//        _FresnelUnit2("菲涅尔通用2", Vector) = (1,1,0,0)
        
        _DepthDecal_Toggle("深度贴花",Float) = 0
        
        _VertexOffset_Toggle("顶点偏移",Float) = 0
        _VertexOffset_Map("顶点偏移贴图",2D) = "white"{}
        _VertexOffset_Vec("xy:顶点偏移动画z:顶点偏移强度",Vector) = (0,0,1,0)
        // Keep the serialized Float name: 0 = custom, 1 = normal, 2 = vertex RGB, 3 = texture RGB.
        _VertexOffset_NormalDir_Toggle("顶点偏移方向模式",Float) = 0
        _VertexOffset_DirectionSpace("顶点偏移RGB方向空间",Float) = 0
        _VertexOffset_StartFromZero("顶点偏移从零开始开关",Float) = 0
        _VertexOffset_CustomDir("顶点偏移自定义方向",Vector) = (1,1,1,0)
    	
    	_VertexOffset_Mask_Toggle("顶点偏移遮罩开关",Float) = 0
    	_VertexOffset_MaskMap("顶点偏移遮罩贴图",2D) = "white"{}
        _VertexOffset_MaskMap_Vec("xy:顶点偏移遮罩动画z:顶点偏移遮罩强度",Vector) = (0,0,1,0)
    	
        
        _ParallaxMapping_Toggle("视差",Float) = 0
        _ParallaxMapping_Map("视差贴图",2D) = "white"{}
        _ParallaxMapping_Intensity("视差强度",Float) = 0.05
    	_ParallaxMapping_Vec("遮蔽视差层数 x:最小值,y:最大值",Vector) = (5,30,0,0)
        
        
        // Particle specific   粒子特殊的隐藏属性
        [HideInInspector] _ColorMode ("_ColorMode", Float) = 0.0
        [HideInInspector] _BaseColorAddSubDiff ("_ColorMode", Vector) = (0, 0, 0, 0)
        [HideInInspector] _SoftParticlesEnabled ("__softparticlesenabled", Float) = 0.0
        [HideInInspector] _CameraFadingEnabled ("__camerafadingenabled", Float) = 0.0
        [HideInInspector] _SoftParticleFadeParams ("xy:软粒子远近裁剪面", Vector) = (0, 0.5, 0, 0)
        [HideInInspector] _CameraFadeParams ("__camerafadeparams_ignore", Vector) = (0, 0, 0, 0)
        [HideInInspector] _IntersectEnabled("__IntersectEnabled_ignore",Float) = 0.0
        [HideInInspector] _IntersectRadius("__IntersectRadius_ignore",Float) = 0.3
        [HideInInspector] _IntersectColor("__IntersectColor_ignore",Color) = (1,1,1,1)
        
        
        // Editmode props  编辑模式下的PropFlags？
        [HideInInspector] _QueueBias ("Queue偏移_QueueBias", Float) =0
        
        // ObsoleteProperties   弃用的属性？？？？
        [HideInInspector] _FlipbookMode ("flipbook mode", Float) = 0
        [HideInInspector] _Mode ("mode", Float) = 0
        // [HideInInspector] _Color ("color", Color) = (1, 1, 1, 1)
        
        [HideInInspector]_fresnelEnabled ("__fresnelEnabled", Float) = 0.0
        [NoScaleOffset]_FresnelHDRITex("__FresnelHDRITex_ignore",Cube) = "white"{} 
        [HDR]_FresnelColor ("菲涅尔颜色_hdr", COLOR) = (1, 1, 1, 1)
        _FresnelRotation("菲涅尔方向偏移",vector) = (0,0,0,0.5)
        _FresnelInOutSlider ("direction-ignore", Range(0, 1)) = 1
        _FrePower ("FrePower-ignore", Range(0,1)) = 0.5
        _FresnelSelfAlphaWeight("__FresnelSelfAlphaWeight-ignore",float) = 0
        //用于程序控制透明属性
        [HideInInspector] _ColorA ("ColorA-ignore", Color) = (1,1,1,1)
        
        _Portal_Toggle("模板视差开关",Float) = 0
        _Portal_MaskToggle("模板视差蒙版开关",Float) = 0

        //基于深度的a通道控制
        [HideInInspector] _Fade("xy:近距离透明过度范围", Vector) = (2,4,0,0)

        [HideInInspector] _InspectorData("__InspectorData-ignore",vector) = (1,1,0,0)
    
        [Header(ZOffset)]
        _ZOffset_Toggle("深度偏移_Toggle",Float) = 0
        _offsetFactor("深度偏移Sacle-ignore", range(-2000,2000)) = 0
        _offsetUnits("深度偏移单位距离-ignore", range(-2000,2000)) = 0
        _OverrideZ_Toggle("Override Z Toggle-ignore",Float) = 0
        _OverrideZValue("Override Z Value-ignore",Float) = 1000
    	
    	_WorldSpaceUVModeSelector("_WordSpaceUVModeSelector",Float) = 1
    	_ObjectSpaceUVModeSelector("_ObjectSpaceUVModeSelector",Float) = 1
        
        [HideInInspector] _W9ParticleShaderFlags("_W9ParticleShaderFlags", Integer) = 0
        [HideInInspector] _W9ParticleShaderFlags1("_W9ParticleShaderFlags1", Integer) = 1
        [HideInInspector] _W9ParticleShaderWrapFlags("_W9ParticleShaderWrapFlags", Integer) = 0
        [HideInInspector] _NBShaderForceNoMipFlags("_NBShaderForceNoMipFlags", Integer) = 0
        [HideInInspector] _W9ParticleShaderWrapFlags2("_W9ParticleShaderWrapFlags2", Integer) = 0
        [HideInInspector] _W9ParticleCustomDataFlag0("_W9ParticleCustomDataFlag0", Integer) = 0
        [HideInInspector] _W9ParticleCustomDataFlag1("_W9ParticleCustomDataFlag1", Integer) = 0
        [HideInInspector] _W9ParticleCustomDataFlag2("_W9ParticleCustomDataFlag2", Integer) = 0
        [HideInInspector] _W9ParticleCustomDataFlag3("_W9ParticleCustomDataFlag3", Integer) = 0
        [HideInInspector] _UVModeFlag0("_UVModeFlag0", Integer) = 0
        [HideInInspector] _UVModeFlagType0("_UVModeFlagType0", Integer) = 0
        [HideInInspector] _NBShaderGUIFoldToggle("_NBShaderGUIFoldToggle", Integer) = 3//前2个开关默认打开
        [HideInInspector] _NBShaderGUIFoldToggle1("_NBShaderGUIFoldToggle1", Integer) = 255//这边默认全开
        [HideInInspector] _NBShaderGUIFoldToggle2("_NBShaderGUIFoldToggle2", Integer) = 255//这边默认全开
        [HideInInspector] _W9ParticleShaderColorChannelFlag("_W9ParticleShaderColorChannelFlag", Integer) = 3//默认主贴图开A通道
        [HideInInspector] _W9ParticleShaderPNoiseBlendFlag("_W9ParticleShaderPNoiseBlendFlag", Integer) = 0
	    
    	
    	SaturabilityRangeVec("_Saturability",Vector) = (0,1,0,0)
    	TexDistortionintensityRangeVec("_TexDistortion_intensity",Vector) = (-1,1,0,0)
    	MaskDistortionIntensityRangeVec("_MaskDistortion_intensity",Vector) = (-2,2,0,0)
    	EmiDistortionIntensityRangeVec("_TexDistortion_intensity",Vector) = (-1,1,0,0)
    	BumpScaleRangeVec("_BumpScale",Vector) = (-1,1,0,0)
    	DissolveXRangeVec("_Dissolve.x",Vector) = (-1,2,0,0)
    	Dissolve2XRangeVec("_Dissolve_Vec2.x",Vector) = (0,1,0,0)
    	Dissolve2YRangeVec("_Dissolve_Vec2.y",Vector) = (0,1,0,0)
    	AlphaAllRangeVec("_AlphaAll",Vector) = (0,1,0,0)
    	_NoiseIntensityRangeVec("_NoiseIntensity",Vector) = (0,1,0,0)
    	_ScreenDistortIntensityRangeVec("_ScreenDistortIntensity",Vector) = (0,1,0,0)
    	_ParallaxMapping_IntensityRangeVec("_ParallaxMappingRangeVec",Vector) = (0,0.1,0,0)
//        _offsetUnits("深度偏移单位距离-ignore", range(-2000,2000)) = 0
        _NB_Debug_Mask("_NB_Debug_Mask",Float) = 0
        _NB_Debug_PNoise("_NB_Debug_PNoise",Float) = 0
        _NB_Debug_Dissolve("_NB_Debug_Dissolve",Float) = 0
        _NB_Debug_Distort("_NB_Debug_Dissolve",Float) = 0
        _NB_Debug_Fresnel("_NB_Debug_Fresnel",Float) = 0
        _NB_Debug_VertexOffset("_NB_Debug_VertexOffset",Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Sphere" "CanUseSpriteAtlas"="True" }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            Fail [_StencilFail]
            ZFail [_StencilZFail]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        //BlendOp[_BlendOp]   //考虑注释~
        Blend[_SrcBlend][_DstBlend]
        ZWrite [_ZWrite]//粒子不写入深度缓冲
        ZTest[_ZTest]

        ColorMask [_ColorMask]
        
        // ------------------------------------------------------------------
        //  预渲染反面Pass，基本理念。先剔除正面棉片，渲染反面面片（一般在后），然后常规Pass再渲染正面（一般在前）
        //  这样可以应对大部分的特效半透明渲染的次序问题。
        //  URP下的多Pass渲染，需要使用特定的LightModeTag：
        //  https://zhuanlan.zhihu.com/p/469589277#:~:text=%E6%B3%A8%E6%84%8F%E7%AC%AC%E4%BA%8C%E4%B8%AA%E5%85%89%E7%85%A7pass%E7%9A%84%E5%85%89%E7%85%A7tag%E4%B8%BA%20%22LightMode%22%20%3D%20%22SRPDefaultUnlit%22%EF%BC%8C%E8%BF%99%E6%98%AF%E5%9B%A0%E4%B8%BA%E5%BF%85%E9%A1%BB%E8%AE%BE%E7%BD%AE%E4%B8%BA%E7%89%B9%E5%AE%9A%E7%9A%84%E5%87%A0%E4%B8%AALightMode%20%E6%89%8D%E8%83%BD%E6%AD%A3%E7%A1%AE%E8%A2%AB%E6%B8%B2%E6%9F%93%E3%80%82%E5%85%B7%E4%BD%93%E5%93%AA%E5%87%A0%E4%B8%AA%20LightMode%20%E8%A7%81URP%E5%8C%85%E9%87%8C%E7%9A%84%20Runtime/Passes%20%E9%87%8C%E7%9A%84%20DrawObjectsPass.cs%E8%84%9A%E6%9C%AC%EF%BC%8C%E9%87%8C%E9%9D%A2%E6%9C%89%E5%A6%82%E4%B8%8B%E4%BB%A3%E7%A0%81%EF%BC%9A
        //  开关Pass，同样也需要用到LightTag
        //  https://blog.csdn.net/shaoy1234567/article/details/106494878
        
        Pass
        {
             Tags
            {
                "LightMode" = "SRPDefaultUnlit" "Queue"="Opaque"
            }
            offset [_offsetFactor], [_offsetUnits]
            Name "SRPDefaultUnlit"
            Cull Front
            
            HLSLPROGRAM
            #define PARTICLE
           #if defined ( SHADER_API_GLES)||defined(SHADER_API_GLES3)
            #pragma target 3.0
            #else
            #pragma target 4.5
            #endif
            
            
            #pragma exclude_renderers d3d11_9x
            #pragma exclude_renderers d3d9
            
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDebugPragmas.hlsl"
            
            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local _ _SCREEN_DISTORT_MODE
            #pragma shader_feature_local _ _DISTORT_REFRACTION
            #pragma shader_feature_local _ _MASKMAP_ON
            #pragma shader_feature_local _MASKMAP2_ON
            #pragma shader_feature_local _MASKMAP3_ON
            // #pragma shader_feature_local _MASKMAP
            // #pragma shader_feature_local _MASKMAP2
            //#pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISE_MASKMAP
            #pragma shader_feature_local_fragment _CHROMATIC_ABERRATION
            //#pragma shader_feature_local _EMISSION   //流光
            #pragma shader_feature_local _EMISSION
            //#pragma shader_feature_local _ _DISSOLVE    //溶解
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _DISSOLVE_MASK
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP_MAP
            #pragma shader_feature_local _PROGRAM_NOISE
            #pragma shader_feature_local _PROGRAM_NOISE_SIMPLE
            #pragma shader_feature_local _PROGRAM_NOISE_VORONOI
            #pragma  shader_feature_local  _COLORMAPBLEND//颜色渐变
            #pragma  shader_feature_local  _COLOR_RAMP//颜色映射
            #pragma shader_feature_local_fragment _COLOR_RAMP_MAP
            #pragma shader_feature_local _SHARED_UV//公共UV
            #pragma shader_feature_local _ _VAT _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _ _VAT_HOUDINI _VAT_TYFLOW
            #pragma shader_feature_local_vertex _ _HOUDINI_VAT_SOFTBODY _HOUDINI_VAT_RIGIDBODY _HOUDINI_VAT_DYNAMIC_REMESH _HOUDINI_VAT_PARTICLE_SPRITE
            #pragma shader_feature_local_vertex _ _TYFLOW_VAT_ABSOLUTE _TYFLOW_VAT_RELATIVE _TYFLOW_VAT_SKIN_R _TYFLOW_VAT_SKIN_PR _TYFLOW_VAT_SKIN_PRSAVE _TYFLOW_VAT_SKIN_PRSXYZ

            //将光照和UI混用，达到节省Keywords的目的。
            #pragma multi_compile _ UNITY_UI_CLIP_RECT _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS//UI 2D遮罩
            // #pragma multi_compile _ _UIPARTICLE_ON//用于UIParticle组件动态更改参数//暂时注释掉，觉得没什么意义
            #pragma multi_compile _ SOFT_UI_FRAME EVALUATE_SH_MIXED EVALUATE_SH_VERTEX//用于UI软蒙版
            
            // #pragma shader_feature_local _PARCUSTOMDATA_ON

            //用于特效层关键字
            // #pragma shader_feature_local  _UIEFFECT_ON
            #pragma shader_feature_local _CUSTOM_LOCAL_TRANSFORM
            #pragma shader_feature_local_vertex _VERTEX_OFFSET
            #pragma shader_feature_local_vertex _VERTEX_OFFSET_MASKMAP
            
            #pragma shader_feature_local_fragment _FRESNEL
           
            
            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON     //设置alpah Add 。。组合
            #pragma shader_feature_local _ALPHATEST_ON
            //#pragma shader_feature_local _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON  //粒子颜色和材质颜色的混合运算  暂时先不要了
            #pragma shader_feature_local _SOFTPARTICLES_ON
            // #pragma shader_feature_local _OCCLUDEOPACITY_ON
            // #pragma shader_feature_local _ _SATURABILITY_ON

            //UnscaleTime用于接收项目传的公开不受缩放影响的Time值
            #pragma shader_feature_local _UNSCALETIME
            //scriptableTime用于程序每帧传值
            #pragma shader_feature_local _SCRIPTABLETIME
            //#pragma shader_feature_local _DISTORTION_ON

            #pragma shader_feature_local _DEPTH_DECAL
            #pragma shader_feature_local _DEPTH_OUTLINE
            #pragma shader_feature_local _DISTANCE_FADE
            #pragma shader_feature_local _PARALLAX_MAPPING
            #pragma shader_feature_local_fragment _OVERRIDE_Z

            #pragma shader_feature_local _STENCIL_WITHOUT_PLAYER

            //LIGHTING
			#pragma shader_feature_local _FX_LIGHT_MODE_UNLIT _FX_LIGHT_MODE_BLINN_PHONG _FX_LIGHT_MODE_HALF_LAMBERT _FX_LIGHT_MODE_PBR _FX_LIGHT_MODE_SIX_WAY 
            #pragma shader_feature_local _ _NORMALMAP
            #pragma shader_feature_local _ _MATCAP
            #pragma shader_feature_local _ _SPECULAR_COLOR
            #pragma shader_feature_local _ VFX_SIX_WAY_ABSORPTION
            #pragma shader_feature_local _ NB_DEBUG_MASK NB_DEBUG_PNOISE NB_DEBUG_DISSOLVE NB_DEBUG_DISTORT NB_DEBUG_FRESNEL NB_DEBUG_VERTEX_OFFSET
            
            
            // -------------------------------------
            // Unity defined keywords
            // 之后进行优化时再说。
            #pragma multi_compile_fog
            // #define FOG_EXP2 1 
            

            // #if defined(_SOFTPARTICLES_ON)
            //     #define NEED_EYE_DEPTH
            // #endif

            #define PARTICLE_BACKFACE_PASS
                    
            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit
            
            // #include "UnityCG.cginc"
            // #include "AutoLight.cginc"
            // #include "UnityUI.cginc"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            
			#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

            #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl"
            
            ENDHLSL
            
        }
        
        // ------------------------------------------------------------------
        //  Forward pass.
        Pass
        {
            Name "UniversalForward"
            Tags
        {
              "LightMode" = "UniversalForward" 
        } //Queue设置是希望特效渲染在场景透明物体前面
            offset [_offsetFactor], [_offsetUnits]
            Cull[_Cull]

            HLSLPROGRAM
            #define PARTICLE
            //20240228 target3.0 顶点着色器限制16个输出。提高版本
           #if defined ( SHADER_API_GLES)||defined(SHADER_API_GLES3)
            #pragma target 3.0
            #else
            #pragma target 4.5
            #endif
            
            // -------------------------------------
            // Material Keywords
 
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDebugPragmas.hlsl"
            
            // #pragma shader_feature_local _ _SCREEN_DISTORT_MODE
            #pragma shader_feature_local _ _DISTORT_REFRACTION
            #pragma shader_feature_local _ _MASKMAP_ON
            #pragma shader_feature_local _MASKMAP2_ON
            #pragma shader_feature_local _MASKMAP3_ON
            // #pragma shader_feature_local _MASKMAP
            // #pragma shader_feature_local _MASKMAP2
            //#pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISE_MASKMAP
            #pragma shader_feature_local_fragment _CHROMATIC_ABERRATION
            //#pragma shader_feature_local _EMISSION   //流光
            #pragma shader_feature_local _EMISSION
            //#pragma shader_feature_local _ _DISSOLVE    //溶解
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _DISSOLVE_MASK
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP_MAP
            #pragma shader_feature_local _PROGRAM_NOISE
            #pragma shader_feature_local _PROGRAM_NOISE_SIMPLE
            #pragma shader_feature_local _PROGRAM_NOISE_VORONOI
            #pragma shader_feature_local  _COLORMAPBLEND//颜色渐变
            #pragma  shader_feature_local  _COLOR_RAMP//颜色映射
            #pragma shader_feature_local_fragment _COLOR_RAMP_MAP
            #pragma shader_feature_local _SHARED_UV//公共UV
            #pragma shader_feature_local _ _VAT _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _ _VAT_HOUDINI _VAT_TYFLOW
            #pragma shader_feature_local_vertex _ _HOUDINI_VAT_SOFTBODY _HOUDINI_VAT_RIGIDBODY _HOUDINI_VAT_DYNAMIC_REMESH _HOUDINI_VAT_PARTICLE_SPRITE
            #pragma shader_feature_local_vertex _ _TYFLOW_VAT_ABSOLUTE _TYFLOW_VAT_RELATIVE _TYFLOW_VAT_SKIN_R _TYFLOW_VAT_SKIN_PR _TYFLOW_VAT_SKIN_PRSAVE _TYFLOW_VAT_SKIN_PRSXYZ
            
            
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS //UI 2D遮罩
            // #pragma shader_feature_local _PARCUSTOMDATA_ON

            //用于特效层关键字
            // #pragma shader_feature_local  _UIEFFECT_ON
            #pragma shader_feature_local _CUSTOM_LOCAL_TRANSFORM
            #pragma shader_feature_local_vertex _VERTEX_OFFSET
            #pragma shader_feature_local_vertex _VERTEX_OFFSET_MASKMAP
            
            #pragma shader_feature_local_fragment _FRESNEL

            
            // #pragma multi_compile _ _UIPARTICLE_ON//用于UIParticle组件动态更改参数//暂时注释掉，觉得没什么意义
            #pragma multi_compile _ SOFT_UI_FRAME EVALUATE_SH_MIXED EVALUATE_SH_VERTEX//用于UI软蒙版
            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON     //设置alpah Add 。。组合
            #pragma shader_feature_local _ALPHATEST_ON
            //#pragma shader_feature_local _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON  //粒子颜色和材质颜色的混合运算  暂时先不要了
            #pragma shader_feature_local _SOFTPARTICLES_ON
            // #pragma shader_feature_local _OCCLUDEOPACITY_ON
            // #pragma shader_feature_local _ _SATURABILITY_ON

            //UnscaleTime用于接收项目传的公开不受缩放影响的Time值
            #pragma shader_feature_local _UNSCALETIME
            //scriptableTime用于程序每帧传值
            #pragma shader_feature_local _SCRIPTABLETIME
            //#pragma shader_feature_local _DISTORTION_ON

            #pragma shader_feature_local _DEPTH_DECAL
            #pragma shader_feature_local _DEPTH_OUTLINE
            #pragma shader_feature_local _DISTANCE_FADE
            #pragma shader_feature_local _PARALLAX_MAPPING
            #pragma shader_feature_local_fragment _OVERRIDE_Z

            #pragma shader_feature_local _STENCIL_WITHOUT_PLAYER

            //LIGHTING
			#pragma shader_feature_local _FX_LIGHT_MODE_UNLIT _FX_LIGHT_MODE_BLINN_PHONG _FX_LIGHT_MODE_HALF_LAMBERT _FX_LIGHT_MODE_PBR _FX_LIGHT_MODE_SIX_WAY 
            #pragma shader_feature_local _ _NORMALMAP
            #pragma shader_feature_local _ _MATCAP
            #pragma shader_feature_local _ _SPECULAR_COLOR
            #pragma shader_feature_local _ VFX_SIX_WAY_ABSORPTION
            #pragma shader_feature_local _ NB_DEBUG_MASK NB_DEBUG_PNOISE NB_DEBUG_DISSOLVE NB_DEBUG_DISTORT NB_DEBUG_FRESNEL NB_DEBUG_VERTEX_OFFSET
            
            // -------------------------------------
            // Unity defined keywords
            // 之后进行优化时再说。
            #pragma multi_compile_fog
            // #define FOG_EXP2 1 
            
            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit
            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The DeclareDepthTexture.hlsl file contains utilities for sampling the Camera
            // depth texture.
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            
            #include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"
            #ifndef NBSHADER_FORWARD_PASS
    #define NBSHADER_FORWARD_PASS
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderInput.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderOverrideDepthV1.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDistortionV1.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderEnvironmentV2.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderMaskV3.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDissolveV3.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDepthDecalV1.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/VAT.hlsl"
    #include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/SixWaySmokeLit.hlsl"

    #if defined(NB_DEPTH_ONLY_PASS) || defined(NB_SHADOW_CASTER_PASS)
        #define NB_DEPTH_SHADOW_PASS
    #endif

    #if defined(NB_SHADOW_CASTER_PASS)
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        float3 _LightDirection;
        float3 _LightPosition;
    #endif


    ///////////////////////////////////////////////////////////////////////////////
    //                  Vertex and Fragment functions                            //

    struct ParticleFragmentOutput
    {
        half4 color : SV_Target;
        #if defined(_OVERRIDE_Z)
            float depth : SV_Depth;
        #endif
    };

    #if defined(_OVERRIDE_Z)
        float OverrideZToDeviceDepth()
        {
            return NBFX_OverrideZDeviceDepthV1(_OverrideZValue,_ProjectionParams,_ZBufferParams,unity_OrthoParams.w);
        }
    #endif

    ParticleFragmentOutput MakeParticleFragmentOutput(half4 color)
    {
        ParticleFragmentOutput output;
        output.color = color;
        #if defined(_OVERRIDE_Z)
            output.depth = OverrideZToDeviceDepth();
        #endif
        return output;
    }

    VaryingsParticle vertParticleUnlit(AttributesParticle input)
    {
        VaryingsParticle output = (VaryingsParticle)0;

        output.VaryingsP_Custom1 = input.Custom1;
        output.VaryingsP_Custom2 = input.Custom2;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        time = _Time.y;

        float4 positionOS = input.vertex;
        float3 normalOS = input.normalOS;
        #if defined(_PARALLAX_MAPPING) || defined(_NORMALMAP) || defined(_FX_LIGHT_MODE_SIX_WAY)
            float4 tangentOS = input.tangentOS;
        #endif
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            positionOS.xyz = TransformWorldToObject_NB(input.vertex.xyz);
            normalOS = TransformWorldToObjectNormal_NB(input.normalOS);
            #if defined(_PARALLAX_MAPPING) || defined(_NORMALMAP) || defined(_FX_LIGHT_MODE_SIX_WAY)
                tangentOS.xyz = TransformWorldToObjectDir_NB(input.tangentOS.xyz);
            #endif
        #endif
        ApplyVAT(input, positionOS, normalOS);
        // position ws is used to compute eye depth in vertFading
        output.positionWS.xyz = TransformObjectToWorld_NB(positionOS.xyz);
        output.positionOS.xyz = positionOS;

        output.clipPos = TransformObjectToHClip_NB(positionOS.xyz);

        float unityFogFactor = ComputeFogFactor(output.clipPos.z);

        output.positionWS.w = unityFogFactor;

        output.color = TryLinearize(input.color);

        // output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS.xyz);
        output.normalWSAndAnimBlend.xyz = TransformObjectToWorldNormal_NB(normalOS.xyz);

        #if defined(_PARALLAX_MAPPING)||defined(_NORMALMAP)||defined(_FX_LIGHT_MODE_SIX_WAY)
            real sign = tangentOS.w * GetCustomLocalOddNegativeScale();
            half3 tangentWS = TransformObjectToWorldDir_NB(tangentOS.xyz);
            #if (defined(_PARALLAX_MAPPING) || defined(_NORMALMAP)) && !defined(_FX_LIGHT_MODE_SIX_WAY)
                output.tangentWS = half4(tangentWS,sign);
            #endif
        #endif


        #ifndef _FX_LIGHT_MODE_UNLIT
            #ifdef _FX_LIGHT_MODE_SIX_WAY
                float3 bitangent = sign * cross(output.normalWSAndAnimBlend.xyz, tangentWS);
                half3 bakeDiffuseLighting0;
                half3 bakeDiffuseLighting1;
                half3 bakeDiffuseLighting2;
                half3 backBakeDiffuseLighting0;
                half3 backBakeDiffuseLighting1;
                half3 backBakeDiffuseLighting2;
                GetSixWayBakeDiffuseLight(output.normalWSAndAnimBlend.xyz,tangentWS,bitangent,
                    bakeDiffuseLighting0,bakeDiffuseLighting1,bakeDiffuseLighting2,
                    backBakeDiffuseLighting0,backBakeDiffuseLighting1,backBakeDiffuseLighting2);

                output.bakeDiffuseLighting0 = half4(bakeDiffuseLighting0,tangentWS.x);
                output.bakeDiffuseLighting1 = half4(bakeDiffuseLighting1,tangentWS.y);
                output.bakeDiffuseLighting2 = half4(bakeDiffuseLighting2,tangentWS.z);
                output.backBakeDiffuseLighting0 = half4(backBakeDiffuseLighting0,sign);
                output.backBakeDiffuseLighting1 = half4(backBakeDiffuseLighting1,0);
                output.backBakeDiffuseLighting2 = half4(backBakeDiffuseLighting2,0);

            #else
                OUTPUT_SH(output.normalWSAndAnimBlend.xyz, output.vertexSH);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.vertexLight = VertexLighting(output.positionWS.xyz, output.normalWSAndAnimBlend.xyz);
                #endif
            #endif
        #endif



        // UNITY_FLATTEN
        // Fresnel enable is keyword-driven by _FRESNEL.
        // {
        //     output.fresnelViewDir = output.viewDirWS;
        // }

        output.texcoord.xy = input.texcoords.xy;

        //顶点处理的原则：
        //Twirl和极坐标,贴花处理，在片段着色器层处理UV。
        //BaseMap，遮罩Mask，Noise，高光（自发光） 和极坐标处理相关。
        BaseUVs baseUVs;
        if(!isProcessUVInFrag())
        {

            float2 specialUVInTexcoord3 = 0;
            //如果同时在粒子系统里开启序列帧融帧和特殊UV通道模式。
            #if _FLIPBOOKBLENDING_ON
                if(CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM) & (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_USE_TEXCOORD1)|CheckLocalFlags1(FLAG_BIT_PARTICLE_1_USE_TEXCOORD2)))
                {
                    specialUVInTexcoord3 = input.texcoordBlend.yz;
                    output.texcoord2AndSpecialUV.zw = specialUVInTexcoord3;
                }
            #endif
            ParticleUVs particleUVs = (ParticleUVs)0;
            float2 screenUV = output.clipPos.xy/output.clipPos.w;
            screenUV = screenUV*0.5+0.5;
            baseUVs = ProcessBaseUVs(input.texcoords,specialUVInTexcoord3,output.VaryingsP_Custom1,output.VaryingsP_Custom2,output.positionOS.xyz,output.positionWS,screenUV);
            ParticleProcessUV(particleUVs,input.texcoords,output.VaryingsP_Custom1,output.VaryingsP_Custom2,baseUVs);
            output.texcoord2AndSpecialUV.xy = particleUVs.animBlendUV;
            output.texcoord2AndSpecialUV.zw= particleUVs.specUV;
            output.texcoord.xy = particleUVs.mainTexUV;
            output.texcoord.zw = particleUVs.maskMapUV;

            #if defined(_MASKMAP2_ON) || defined(_MASKMAP3_ON)
                output.texcoordMaskMap2.xy = particleUVs.maskMap2UV;
                output.texcoordMaskMap2.zw = particleUVs.maskMap3UV;
            #endif
            #if defined (_NORMALMAP) || defined(_COLOR_RAMP)
                output.bumpTexAndColorRampMapTexcoord.xy = particleUVs.bumpTexUV;
                output.bumpTexAndColorRampMapTexcoord.zw = particleUVs.colorRampMapUV;

            #endif

            #if defined (_EMISSION)   || defined(_COLORMAPBLEND)
                output.emissionColorBlendTexcoord.xy = particleUVs.emissionUV;
                output.emissionColorBlendTexcoord.zw = particleUVs.colorBlendUV;
            #endif

            #ifdef _NOISEMAP
                output.noisemapTexcoord.xy = particleUVs.noiseMapUV;
                output.noisemapTexcoord.zw = particleUVs.noiseMaskMapUV;
            #endif
            #if defined(_DISSOLVE)
                output.dissolveTexcoord.xy = particleUVs.dissolve_uv;
                output.dissolveTexcoord.zw = particleUVs.dissolve_mask_uv;
            #endif

            #ifdef _PROGRAM_NOISE_ACTIVE
                output.dissolveNoiseTexcoord.xy = particleUVs.dissolve_noise1_UV;
                output.dissolveNoiseTexcoord.zw = particleUVs.dissolve_noise2_UV;
            #endif
        }
        else
        {
            output.texcoord = input.texcoords;
            #if _FLIPBOOKBLENDING_ON
            if(CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM) & CheckLocalFlags1(FLAG_BIT_PARTICLE_1_USE_TEXCOORD2))
            {
                output.texcoord2AndSpecialUV.zw = input.texcoordBlend.yz;
            }
            #endif
        }
        #ifdef _FLIPBOOKBLENDING_ON
            //粒子帧融合的情况，兼容一下。
            output.normalWSAndAnimBlend.w = input.texcoordBlend.x;
        #endif

        #if defined(_VERTEX_OFFSET)
        {
            BaseUVs baseUVsForVertexOffset;
            if(!isProcessUVInFrag())
            {
                baseUVsForVertexOffset = baseUVs;
            }
            else
            {
                float2 screenUV = output.clipPos.xy/output.clipPos.w;
                screenUV = screenUV*0.5+0.5;
                baseUVsForVertexOffset = ProcessBaseUVs(input.texcoords,0,output.VaryingsP_Custom1,output.VaryingsP_Custom2,positionOS,output.positionWS,screenUV);
            }

            //因为极坐标和旋转会强制到Frag计算，所以顶点在这边特殊处理一遍。

            _VertexOffset_Map_ST.z += GetCustomData(_W9ParticleCustomDataFlag1,FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_X,0,input.Custom1,input.Custom2);
            _VertexOffset_Map_ST.w += GetCustomData(_W9ParticleCustomDataFlag1,FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_Y,0,input.Custom1,input.Custom2);
            _VertexOffset_Vec.z = GetCustomData(_W9ParticleCustomDataFlag1,FLAGBIT_POS_1_CUSTOMDATA_VERTEXOFFSET_INTENSITY,_VertexOffset_Vec.z,input.Custom1,input.Custom2);

            #if defined(_VERTEX_OFFSET_MASKMAP)
            _VertexOffset_MaskMap_ST.z += GetCustomData(_W9ParticleCustomDataFlag3,FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_X,0,input.Custom1,input.Custom2);
            _VertexOffset_MaskMap_ST.w += GetCustomData(_W9ParticleCustomDataFlag3,FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_Y,0,input.Custom1,input.Custom2);
            #endif

            float2 vertexOffsetUVs = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP,baseUVsForVertexOffset);
            float2 vertexOffsetMaskUVs = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP,baseUVsForVertexOffset);

            half3 vertexOffsetOS = 0;
            positionOS.xyz = VetexOffset(positionOS,vertexOffsetUVs,vertexOffsetMaskUVs,normalOS,input.color.rgb,vertexOffsetOS);
            #ifdef NB_DEBUG_VERTEX_OFFSET
            half3 vertexOffsetWS = TransformObjectToWorldDir_NB(vertexOffsetOS,false);
            output.color = half4(abs(vertexOffsetWS),1);
            #endif
            output.positionWS.xyz = TransformObjectToWorld_NB(positionOS.xyz);

            //再算一遍
            output.positionWS.xyz = TransformObjectToWorld_NB(positionOS.xyz);
            output.positionOS.xyz = positionOS;
            output.clipPos = TransformObjectToHClip_NB(positionOS.xyz);
        }
        #endif

        #if defined(NB_SHADOW_CASTER_PASS)
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - output.positionWS.xyz);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

            output.clipPos = TransformWorldToHClip(ApplyShadowBias(output.positionWS.xyz, output.normalWSAndAnimBlend.xyz, lightDirectionWS));

            #if UNITY_REVERSED_Z
                output.clipPos.z = min(output.clipPos.z, UNITY_NEAR_CLIP_VALUE);
            #else
                output.clipPos.z = max(output.clipPos.z, UNITY_NEAR_CLIP_VALUE);
            #endif
        #endif

        #if !defined(NB_DEPTH_SHADOW_PASS)
        UNITY_BRANCH
        if(needEyeDepth())
        {
            float4 ndc = output.clipPos*0.5f;
            output.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
            output.positionNDC.zw = output.clipPos.zw;
        }
        #endif

        return output;
    }




    ///////////////////////Fragment functions  ////////////////////////

    ParticleFragmentOutput fragParticleUnlit(VaryingsParticle input, half facing : VFACE)
    {

        #if !defined(NB_DEPTH_SHADOW_PASS)
        float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS.xyz);
        input.normalWSAndAnimBlend.xyz = facing > 0 ? input.normalWSAndAnimBlend.xyz : -input.normalWSAndAnimBlend.xyz;
        #endif


        UNITY_SETUP_INSTANCE_ID(input);
        #if !defined(NB_DEPTH_SHADOW_PASS)
        #ifdef NB_DEBUG_VERTEX_OFFSET
        return MakeParticleFragmentOutput(input.color);
        #endif
        #endif

        time = _Time.y;

        float2 screenUV = input.clipPos.xy / _ScaledScreenParams.xy;

        #if !defined(NB_DEPTH_SHADOW_PASS)
        real sceneZBufferDepth = 0;
        real sceneZ = 0;

        UNITY_BRANCH
        if(needSceneDepth())
        {
            #if UNITY_REVERSED_Z
            sceneZBufferDepth = SampleSceneDepth(screenUV);
            #else
            // Adjust z to match NDC for OpenGL
            sceneZBufferDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
            #endif
            sceneZ = (unity_OrthoParams.w == 0) ? LinearEyeDepth(sceneZBufferDepth, _ZBufferParams) : LinearDepthToEyeDepth(sceneZBufferDepth);//场景当前深度
        }

        real thisZ = 0;


		half3 positionVS = 0;
		if(needPositionVS())
		{
			positionVS = TransformWorldToView(input.positionWS);
		}

        if(needEyeDepth())
        {
			thisZ = (unity_OrthoParams.w == 0) ? LinearEyeDepth(input.positionNDC.z / input.positionNDC.w, _ZBufferParams) : -positionVS.z;//场景当前深度
        }


        #ifdef _DEPTH_DECAL
            float3 fragWorldPos = ComputeWorldSpacePosition(screenUV, sceneZBufferDepth, UNITY_MATRIX_I_VP);
            float3 fragobjectPos = TransformWorldToObject_NB(fragWorldPos);

            NBFX_DepthDecalProjectionV1 depthDecal = NBFX_ResolveDepthDecalV1(fragobjectPos);
            half decalAlpha = depthDecal.alpha;
            float2 decalUV = depthDecal.uv;
        #endif
        #endif

        float4 uv = input.texcoord;
        #if !defined(NB_DEPTH_SHADOW_PASS)
        #ifdef _DEPTH_DECAL
            uv.xy = decalUV;
        #endif
        #endif

        float2 MainTex_UV;
        float3 blendUv;
        blendUv.xy = input.texcoord2AndSpecialUV.xy;
        blendUv.z = NBFX_ResolveFlipbookWeightV1(
            input.normalWSAndAnimBlend.w,
            _AnimationSheetHelperBlendIntensity,
            CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER));
        float2 MaskMapuv;
        float2 MaskMapuv2;
        float2 MaskMapuv3;
        float2 noiseMap_uv;
        float2 noiseMaskMap_uv;
        float2 colorBlendMap_uv;
        float2 emission_uv;
        float2 dissolve_uv;
        float2 dissolve_mask_uv;
        float4 dissolve_noise_uv;
        float2 BumpTex_uv;
        float2 colorRamp_uv;

        //如果同时在粒子系统里开启序列帧融帧和特殊UV通道模式。

        if(isProcessUVInFrag())
        {
            float2 specialUVInTexcoord3 = 0;
            #if _FLIPBOOKBLENDING_ON
            if(CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM) & (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_USE_TEXCOORD2)))
            {
                specialUVInTexcoord3 = input.texcoord2AndSpecialUV.zw;
            }

            #endif
            ParticleUVs particleUVs = (ParticleUVs)0;
            BaseUVs baseUVs = ProcessBaseUVs(uv,specialUVInTexcoord3,input.VaryingsP_Custom1,input.VaryingsP_Custom2,input.positionOS.xyz,input.positionWS.xyz,screenUV);
            ParticleProcessUV(particleUVs,uv,input.VaryingsP_Custom1,input.VaryingsP_Custom2,baseUVs);
            MainTex_UV.xy = particleUVs.mainTexUV;
            blendUv.xy = particleUVs.animBlendUV;
            MaskMapuv = particleUVs.maskMapUV;
            MaskMapuv2 = particleUVs.maskMap2UV;
            MaskMapuv3 = particleUVs.maskMap3UV;
            emission_uv = particleUVs.emissionUV;
            dissolve_uv = particleUVs.dissolve_uv;
            dissolve_mask_uv = particleUVs.dissolve_mask_uv;
            colorBlendMap_uv = particleUVs.colorBlendUV;
            noiseMap_uv = particleUVs.noiseMapUV;
            noiseMaskMap_uv = particleUVs.noiseMaskMapUV;
            dissolve_noise_uv = float4(particleUVs.dissolve_noise1_UV,particleUVs.dissolve_noise2_UV);
            BumpTex_uv = particleUVs.bumpTexUV;
            colorRamp_uv = particleUVs.colorRampMapUV;
        }
        else
        {
            MainTex_UV = input.texcoord.xy;
            MaskMapuv = input.texcoord.zw;
            #if defined(_MASKMAP2_ON) || defined(_MASKMAP3_ON)
                MaskMapuv2 = input.texcoordMaskMap2.xy;
                MaskMapuv3 = input.texcoordMaskMap2.zw;
            #endif

            #if defined (_NORMALMAP)||defined(_COLOR_RAMP)
            BumpTex_uv = input.bumpTexAndColorRampMapTexcoord.xy;
            colorRamp_uv = input.bumpTexAndColorRampMapTexcoord.zw;
            #endif

            #ifdef _NOISEMAP
                noiseMap_uv = input.noisemapTexcoord.xy;
                noiseMaskMap_uv = input.noisemapTexcoord.zw;
            #endif

            #if defined (_EMISSION)   || defined(_COLORMAPBLEND)
                emission_uv = input.emissionColorBlendTexcoord.xy;
                colorBlendMap_uv = input.emissionColorBlendTexcoord.zw;
            #endif

            #ifdef _DISSOLVE
                dissolve_uv = input.dissolveTexcoord.xy;
                dissolve_mask_uv = input.dissolveTexcoord.zw;
            #endif
            #ifdef _PROGRAM_NOISE_ACTIVE
                dissolve_noise_uv = input.dissolveNoiseTexcoord;
            #endif
        }


        half programNoise;
        #ifdef _PROGRAM_NOISE_ACTIVE
        half programSimpleNoise;
        half programVoronoiNoise;

        #if defined(_PROGRAM_NOISE_SIMPLE)
        {
            programSimpleNoise = SimplexNoise(dissolve_noise_uv.xy,_Time.y*_DissolveVoronoi_Vec2.z);
            programNoise = programSimpleNoise;
        }
        #endif

        #if defined(_PROGRAM_NOISE_VORONOI)
        {
            half cell;
            Unity_Voronoi_float(dissolve_noise_uv.zw,_Time.y*_DissolveVoronoi_Vec2.w,1,programVoronoiNoise,cell);
            programNoise = programVoronoiNoise ;
        }
        #endif

        #if defined(_PROGRAM_NOISE_SIMPLE) && defined(_PROGRAM_NOISE_VORONOI)
        {
            programNoise =  BlendPNoise(_W9ParticleShaderPNoiseBlendFlag,FLAG_BIT_PNOISE_BLEND_POS_0_BASE_BLEND,programSimpleNoise,programVoronoiNoise,_ProgramNoiseBaseBlendOpacity);
            // Unity_Blend_HardLight_half(programSimpleNoise,programVoronoiNoise,_DissolveVoronoi_Vec2.x,programNoise);
        }
        #endif

        // half dissolveSample = dissolveValue;
        // Unity_Blend_HardLight_half(overlayVoroni,dissolveSample,_DissolveVoronoi_Vec2.y,dissolveValue);
        #ifdef NB_DEBUG_PNOISE
        return MakeParticleFragmentOutput(half4(programNoise.xxx,1));
        #endif

        #endif

        half2 originUV = MainTex_UV;

        //预先处理好法线贴图部分
        half metallic = 1;
        half smoothness = 1;
        half3 normalTS = half3(0, 0, 1);//TODO
        half3x3 tangentToWorld = (half3x3)0;
        #if defined(_PARALLAX_MAPPING) || defined(_NORMALMAP) || defined(_FX_LIGHT_MODE_SIX_WAY)
            half4 tangentWS = 0;
            #if defined(_FX_LIGHT_MODE_SIX_WAY)
                tangentWS = half4(input.bakeDiffuseLighting0.w,input.bakeDiffuseLighting1.w,input.bakeDiffuseLighting2.w,input.backBakeDiffuseLighting0.w);
            #else
                tangentWS = input.tangentWS;
            #endif
        #endif
        #if defined(_PARALLAX_MAPPING) || defined(_NORMALMAP)
            float sgn = tangentWS.w;      // should be either +1 or -1
            float3 bitangent = sgn * cross(input.normalWSAndAnimBlend.xyz, tangentWS.xyz);
            tangentToWorld = half3x3(tangentWS.xyz, bitangent.xyz, input.normalWSAndAnimBlend.xyz);
        #endif
        #ifdef _PARALLAX_MAPPING
            float3 tangentViewDir = NBTransformWorldToTangentDir(viewDirWS, tangentToWorld, true);
            bool forceParallaxLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_PARALLAXMAPPINGMAP);
            MainTex_UV.xy = ParallaxOcclusionMapping(MainTex_UV, tangentViewDir, forceParallaxLod0);
        #endif
        #ifdef _NORMALMAP
            bool forceBumpLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_BUMPTEX);
            half4 normalMapSample = SampleTexture2DWithWrapFlags(_BumpTex,BumpTex_uv,FLAG_BIT_WRAPMODE_BUMPTEX,forceBumpLod0);
 return MakeParticleFragmentOutput(normalMapSample);

            half metallicWeight, smoothnessWeight;
            half3 mappedNormalWS;
            NBFX_DecodeNormalMapV2(normalMapSample, _BumpScale,
                CheckLocalFlags(FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE), tangentToWorld,
                normalTS, mappedNormalWS, metallicWeight, smoothnessWeight);
            metallic *= metallicWeight;
            smoothness *= smoothnessWeight;
            input.normalWSAndAnimBlend.xyz = mappedNormalWS;
        #endif

        half2 cum_noise = 0;
        // half2 cum_noise_xy = half2(0.5,0.5);
        half3 screenDistort_Noise = half3(0,0,1);//xy:noise,z:mask;
        half noiseMask = 1;
        #if defined(_NOISEMAP)
            #ifdef _DISTORT_REFRACTION
                half3 refracVec = CustomRefract(-viewDirWS,input.normalWSAndAnimBlend.xyz,1/_RefractionIOR);
                refracVec = TransformWorldToHClipDir(refracVec);
                cum_noise = refracVec.xy;
                // distortFlow = refracVec.xy * props._NormalRefractionBias;
            #else
                bool forceNoiseLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_NOISEMAP);
                half4 noiseSample = SampleNoise(_NoiseOffset, _NoiseMap, noiseMap_uv, input.positionWS.xyz, forceNoiseLod0);
                NBFX_DecodeTextureNoiseV1(noiseSample,
                    CheckLocalFlags(FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON),
                    cum_noise, noiseMask);
                _DistortionDirection.x += GetCustomData(_W9ParticleCustomDataFlag2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X,0,input.VaryingsP_Custom1,input.VaryingsP_Custom2);
                _DistortionDirection.y += GetCustomData(_W9ParticleCustomDataFlag2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y,0,input.VaryingsP_Custom1,input.VaryingsP_Custom2);
            #endif
            #if defined(_NOISE_MASKMAP)
            {
                bool forceNoiseMaskLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_NOISE_MASKMAP);
                half4 noiseMaskSample = SampleTexture2DWithWrapFlags(_NoiseMaskMap,noiseMaskMap_uv,FLAG_BIT_WRAPMODE_NOISE_MASKMAP,forceNoiseMaskLod0);
                noiseMask *= GetColorChannel(noiseMaskSample,FLAG_BIT_COLOR_CHANNEL_POS_0_NOISE_MASK);
            }
            #endif
            _NoiseIntensity = GetCustomData(_W9ParticleCustomDataFlag1,FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY,_NoiseIntensity,input.VaryingsP_Custom1,input.VaryingsP_Custom2);

            cum_noise = cum_noise *_DistortionDirection.xy*_NoiseIntensity;

            #ifdef _PROGRAM_NOISE_ACTIVE
                cum_noise =  BlendPNoise(_W9ParticleShaderPNoiseBlendFlag,FLAG_BIT_PNOISE_BLEND_POS_0_DISTORT,cum_noise,programNoise,_DistortPNoiseBlendOpacity);
            #endif



            #if defined(_SCREEN_DISTORT_MODE)
                screenDistort_Noise.xy = cum_noise;
                screenDistort_Noise.z = noiseMask;
            #endif
            // #if defined(_SCREEN_DISTORT_MODE)
            //     cum_noise_xy = cum_noise  * _ScreenDistortIntensity;
            // #endif

            cum_noise *= noiseMask;

            #ifdef NB_DEBUG_DISTORT
                return MakeParticleFragmentOutput(half4(cum_noise,0,1));
            #endif
        #endif

        // SampleAlbedo--------------------
        half4 albedo = 0;

        #if !defined(NB_DEPTH_SHADOW_PASS)
        UNITY_FLATTEN
        if(CheckLocalFlags(FLAG_BIT_PARTICLE_BACKCOLOR))
        {
            _BaseColor = facing > 0 ? _BaseColor : _BaseBackColor;
        }
        #endif


        Texture2D baseMap = _BaseMap;



        float2 mainTexNoise =  cum_noise * _TexDistortion_intensity;

        MainTex_UV.xy += mainTexNoise;//主贴图纹理扭曲
        blendUv.xy += mainTexNoise;

        bool forceBaseMapLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_BASEMAP);


        UNITY_BRANCH
        if (CheckLocalFlags(FLAG_BIT_PARTICLE_UIEFFECT_ON) & !CheckLocalFlags1(FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE))
        {
            albedo = BlendTexture(_MainTex, MainTex_UV, blendUv, forceBaseMapLod0) * _Color;
        }
        else
        {
            #if defined(_CHROMATIC_ABERRATION)
            #if !defined(NB_DEPTH_SHADOW_PASS)
            _DistortionDirection.z = GetCustomData(_W9ParticleCustomDataFlag0,FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY,_DistortionDirection.z,input.VaryingsP_Custom1,input.VaryingsP_Custom2);
            _DistortionDirection.z *= 0.1;
            albedo = DistortionChoraticaberrat(baseMap,originUV,MainTex_UV,_DistortionDirection.z,FLAG_BIT_WRAPMODE_BASEMAP,forceBaseMapLod0);
            #else
            albedo = BlendTexture(baseMap, MainTex_UV, blendUv,FLAG_BIT_WRAPMODE_BASEMAP,forceBaseMapLod0);
            #endif
            #else
            albedo = BlendTexture(baseMap, MainTex_UV, blendUv,FLAG_BIT_WRAPMODE_BASEMAP,forceBaseMapLod0);
            #endif
        }

        // #ifndef _CAMERA_OPAQUE_DISTORT_PASS

        NBFX_BaseColorInputV1 baseColorInput = (NBFX_BaseColorInputV1)0;
        baseColorInput.sampledAlbedo = albedo;
        baseColorInput.selectedAlpha = GetColorChannel(albedo, FLAG_BIT_COLOR_CHANNEL_POS_0_MAINTEX_ALPHA);
        baseColorInput.effectiveBaseColor = _BaseColor;
        baseColorInput.timelineIntensity = _BaseColorIntensityForTimeline;
        #if !defined(NB_DEPTH_SHADOW_PASS)
        baseColorInput.applyTimelineIntensity = true;
        #endif
        albedo = NBFX_ComposeBaseColorV1(baseColorInput);
            // #endif


        // #endif

        half alpha = albedo.a;
        half3 result = albedo.rgb;
        #if !defined(NB_DEPTH_SHADOW_PASS)
        UNITY_BRANCH
        if (CheckLocalFlags(FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX))
        {
            ColorAdjustment(result,alpha,input.VaryingsP_Custom1,input.VaryingsP_Custom2);
        }
        #endif

        #ifdef _FX_LIGHT_MODE_SIX_WAY
            bool forceRigRTBkLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_RIG_RTBK);
            bool forceRigLBtFLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_RIG_LBTF);
            float4 rigRTBkSample  = BlendTexture(_RigRTBk, MainTex_UV, blendUv,FLAG_BIT_WRAPMODE_BASEMAP,forceRigRTBkLod0);
            float4 rigLBtFSample  = BlendTexture(_RigLBtF, MainTex_UV, blendUv,FLAG_BIT_WRAPMODE_BASEMAP,forceRigLBtFLod0);
        #endif


        //光照模式
        #ifndef _FX_LIGHT_MODE_UNLIT

            InputData inputData;
            InitializeInputData(input, tangentToWorld,viewDirWS, inputData);
            metallic *= _MaterialInfo.x;
            half3 specular = 0;
            smoothness *= _MaterialInfo.y;
            half occlusion = 1;
            half3 pbrEmission = 0;
           // return half4(inputData.bakedGI,1);
            #if defined (_FX_LIGHT_MODE_BLINN_PHONG) || defined(_FX_LIGHT_MODE_HALF_LAMBERT)
                half4 specularGloss = _SpecularColor;
                #ifdef _FX_LIGHT_MODE_BLINN_PHONG
                    half4 blinnPhong = UniversalFragmentBlinnPhong(inputData,result.rgb, specularGloss, smoothness, pbrEmission, alpha,normalTS);
                #else //_FX_LIGHT_MODE_HALF_LAMBERT
                    half4 blinnPhong = UniversalFragmentHalfLambert(inputData,result.rgb, specularGloss, smoothness, pbrEmission, alpha,normalTS);
                #endif
                result = blinnPhong.rgb;
                alpha = blinnPhong.a;
            #elif _FX_LIGHT_MODE_PBR
                half4 pbr = UniversalFragmentPBR(inputData,result.rgb,  metallic,  specular, smoothness,  occlusion,  pbrEmission, alpha);
                result = pbr.rgb;
                alpha = pbr.a;
            #elif _FX_LIGHT_MODE_SIX_WAY
                BSDFData bsdfData = (BSDFData)0;
                bsdfData.absorptionRange = GetAbsorptionRange(_SixWayInfo.x);
                bsdfData.diffuseColor = albedo;
                bsdfData.normalWS = inputData.normalWS;
                bsdfData.tangentWS = tangentWS;
                bsdfData.rigRTBk = rigRTBkSample.xyz * INV_PI;//AccordingTo SixWayForwardPass
                bsdfData.rigLBtF = rigLBtFSample.xyz * INV_PI;//AccordingTo SixWayForwardPass
                bsdfData.bakeDiffuseLighting0 = input.bakeDiffuseLighting0.xyz;
                bsdfData.bakeDiffuseLighting1 = input.bakeDiffuseLighting1.xyz;
                bsdfData.bakeDiffuseLighting2 = input.bakeDiffuseLighting2.xyz;
                bsdfData.backBakeDiffuseLighting0 = input.backBakeDiffuseLighting0.xyz;
                bsdfData.backBakeDiffuseLighting1 = input.backBakeDiffuseLighting1.xyz;
                bsdfData.backBakeDiffuseLighting2 = input.backBakeDiffuseLighting2.xyz;
                bsdfData.emissionInput = rigLBtFSample.a;
                bool forceSixWayEmissionRampLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_SIX_WAY_EMISSION_RAMP);
                GetSixWayEmission(bsdfData,_SixWayEmissionRamp,_SixWayEmissionColor,CheckLocalFlags1(FLAG_BIT_PARTICLE_1_SIXWAY_RAMPMAP),forceSixWayEmissionRampLod0);//Init Emission
                bsdfData.alpha = rigRTBkSample.a * _BaseColor.a;

                ModifyBakedDiffuseLighting(bsdfData,inputData.bakedGI);

                half4 sixWay = UniversalFragmentSixWay(inputData,bsdfData);


            // half3 dir = _MainLightPosition.xyz;
            // dir = TransformToLocalFrame(dir, bsdfData);
            // return half4(dir,1);

                result = sixWay.rgb;
                alpha = sixWay.a;

            #endif

            #ifdef _ADDITIONAL_LIGHTS_VERTEX
            result.rgb *= input.vertexLight;
            #endif

            // input.normalWSAndAnimBlend.xyz = inputData.normalWS;
        #endif


        #ifdef _MATCAP
        // URP
        half3 normalVS = mul(input.normalWSAndAnimBlend.xyz, (float3x3)UNITY_MATRIX_I_V); // 逆转置矩阵
        //half3 positionVS = TransformWorldToView(input.positionWS);


        float2 matCapUV = NBFX_MatCapUVV2(positionVS, normalVS);
        bool forceMatCapLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_MATCAP);
        half3 matCapSample = SampleTexture2D(_MatCapTex,matCapUV,sampler_linear_clamp,forceMatCapLod0);
        result = NBFX_CompositeMatCapV2(result, matCapSample, _MatCapColor, _MatCapInfo.x);
        #endif



        //叠加贴图 1（保留旧流光的执行位置）
        #if defined(_EMISSION)
            #ifdef _NOISEMAP
                emission_uv += cum_noise * _Emi_Distortion_intensity;
            #endif
            bool forceEmissionLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_EMISSIONMAP);
            half4 emission = SampleTexture2DWithWrapFlags(_EmissionMap,emission_uv,FLAG_BIT_WRAPMODE_EMISSIONMAP,forceEmissionLod0);
            NBFX_ApplyColorOverlayV1(
                result,
                alpha,
                emission,
                _EmissionMapColor,
                _EmissionMapColorIntensity,
                _EmissionAlphaIntensity,
                CheckLocalFlags(FLAG_BIT_PARTICLE_COLOR_OVERLAY_1_MULTIPLY),
                CheckLocalFlags1(FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY));
        #endif


        #if defined(_COLOR_RAMP)
            half rampValue = 0;
            #if defined(_COLOR_RAMP_MAP)
            bool forceRampColorLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_RAMP_COLOR_MAP);
            half4 RampColorSample = SampleTexture2DWithWrapFlags(_RampColorMap,colorRamp_uv,FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP,forceRampColorLod0);
            rampValue = GetColorChannel(RampColorSample,FLAG_BIT_COLOR_CHANNEL_POS_0_RAMP_COLOR_MAP);
            #else
                const int rampColorWrapMode = CheckLocalWrapFlags(FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP);
                if (rampColorWrapMode == 0 || rampColorWrapMode == 2)
                {
                    rampValue = frac(colorRamp_uv.x);
                }
                else
                {
                    rampValue = saturate(colorRamp_uv.x);
                }
            #endif

            int colorRampColorCount = _RampColorCount & 0xFFFF;

            int colorRampAlphaCount = _RampColorCount >> 16;

            half4 rampColor;
            rampColor.rgb = SamplePackedGradientColor(_RampColor0, _RampColor1, _RampColor2, _RampColor3, _RampColor4, _RampColor5, colorRampColorCount, rampValue);
            rampColor.a = SamplePackedGradientAlpha(_RampColorAlpha0, _RampColorAlpha1, _RampColorAlpha2, colorRampAlphaCount, rampValue);

            NBFX_ApplyColorRampV1(result, alpha, rampColor,
                _RampColorBlendColor,
                CheckLocalFlags(FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD));
        #endif



        //溶解部分
        #if defined(_DISSOLVE)
            #ifdef _NOISEMAP
                dissolve_uv += cum_noise * _DissolveOffsetRotateDistort.w;

                #if defined(_DISSOLVE_MASK)
                dissolve_mask_uv += cum_noise * _DissolveOffsetRotateDistort.w;
                #endif
            #endif

            bool forceDissolveLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_DISSOLVE_MAP);
            half4 dissolveMapSample  = SampleTexture2DWithWrapFlags(_DissolveMap,dissolve_uv,FLAG_BIT_WRAPMODE_DISSOLVE_MAP,forceDissolveLod0);

            half dissolveValue = GetColorChannel(dissolveMapSample,FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MAP);

            #ifdef _PROGRAM_NOISE_ACTIVE
                dissolveValue =  BlendPNoise(_W9ParticleShaderPNoiseBlendFlag,FLAG_BIT_PNOISE_BLEND_POS_0_DISSOLVE,dissolveValue,programNoise,_DissolvePNoiseBlendOpacity);
            #endif


            half dissolveMaskValue = 0;
            half dissolveMaskStrength = 0;
            #if defined(_DISSOLVE_MASK)
            {
                bool forceDissolveMaskLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_DISSOLVE_MASKMAP);
                half4 dissolveMaskSample = SampleTexture2DWithWrapFlags(_DissolveMaskMap,dissolve_mask_uv,FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP,forceDissolveMaskLod0);
                dissolveMaskValue = GetColorChannel(dissolveMaskSample,FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MASK_MAP);
                dissolveMaskStrength = _Dissolve.z + GetCustomData(_W9ParticleCustomDataFlag1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY,0,input.VaryingsP_Custom1,input.VaryingsP_Custom2);

            }
            #endif

            NBFX_DissolvePrepareInputV3 dissolvePrepareInput = (NBFX_DissolvePrepareInputV3)0;
            dissolvePrepareInput.decodedAndNoiseBlendedValue = dissolveValue;
            dissolvePrepareInput.exponent = _Dissolve.y;
            #if defined(_DISSOLVE_MASK)
                dissolvePrepareInput.hasMask = true;
            #endif
            dissolvePrepareInput.decodedMaskValue = dissolveMaskValue;
            dissolvePrepareInput.maskStrength = dissolveMaskStrength;
            dissolvePrepareInput.maskMode = _DissolveMaskMode;
            NBFX_DissolvePreparedV3 dissolvePrepared = NBFX_PrepareDissolveV3(dissolvePrepareInput);
            dissolveValue = dissolvePrepared.valueForDebugAndSoftStep;

            #ifdef NB_DEBUG_DISSOLVE      //后续Test类的关键字要找机会排除
                return MakeParticleFragmentOutput(half4(dissolveValue.rrr,1));
            #endif
            half dissolveStrenth = _Dissolve.x + GetCustomData(_W9ParticleCustomDataFlag0,FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY,0,input.VaryingsP_Custom1,input.VaryingsP_Custom2);
            NBFX_DissolveResolveInputV3 dissolveResolveInput = (NBFX_DissolveResolveInputV3)0;
            dissolveResolveInput.prepared = dissolvePrepared;
            dissolveResolveInput.threshold = dissolveStrenth;
            dissolveResolveInput.softWidth = _Dissolve.w;
            #if defined(_DISSOLVE_MASK)
                dissolveResolveInput.hasMask = true;
            #endif
            dissolveResolveInput.maskStrength = dissolveMaskStrength;
            dissolveResolveInput.maskMode = _DissolveMaskMode;
            NBFX_DissolveResolvedV3 dissolveResolved = NBFX_ResolveDissolveV3(dissolveResolveInput);
            half dissolveValueBeforeSoftStep = dissolveResolved.valueBeforeSoftStep;
            dissolveValue = dissolveResolved.coverage;
            alpha  *= dissolveValue;
            #if !defined(NB_DEPTH_SHADOW_PASS)
            #if defined(_DISSOLVE_RAMP)
            {
                // half rampRange = (dissolveValueBeforeSoftStep - _Dissolve_Vec2.x)*_Dissolve_Vec2.y;
                half rampRange = dissolveValueBeforeSoftStep;
                rampRange = rampRange * _DissolveRampMap_ST.x +_DissolveRampMap_ST.z;

                half4 rampSample ;
                #if defined(_DISSOLVE_RAMP_MAP)
                bool forceDissolveRampLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_DISSOLVE_RAMPMAP);
                rampSample = SampleTexture2DWithWrapFlags(_DissolveRampMap,half2(rampRange,0.5),FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP,forceDissolveRampLod0);
                #else
                    int dissolveRampColorCount = _DissolveRampCount & 0xFFFF;

                    int dissolveRampAlphaCount = _DissolveRampCount >> 16;

                    const int rampWrapMode = CheckLocalWrapFlags(FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP);
                    if (rampWrapMode == 0 || rampWrapMode == 2)
                    {
                        rampRange = frac(rampRange);
                    }
                    else
                    {
                        rampRange = saturate(rampRange);
                    }


                    rampSample.rgb = SamplePackedGradientColor(_DissolveRampColor0, _DissolveRampColor1, _DissolveRampColor2, _DissolveRampColor3, _DissolveRampColor4, _DissolveRampColor5, dissolveRampColorCount, rampRange);
                    rampSample.a = SamplePackedGradientAlpha(_DissolveRampAlpha0, _DissolveRampAlpha1, _DissolveRampAlpha2, dissolveRampAlphaCount, rampRange);
                #endif

                NBFX_ApplyDissolveRampV1(result, rampSample,
                    _DissolveRampColor,
                    CheckLocalFlags1(FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY));
            }
            #endif



            if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK))
            {
                half lineMask = dissolveValueBeforeSoftStep;//SmoothStep要优化
                lineMask = saturate(NB_Remap01(lineMask,_Dissolve_Vec2.x-_Dissolve_Vec2.y,_Dissolve_Vec2 + _Dissolve_Vec2.y));
                lineMask = 1- lineMask;

                result = lerp(result,_DissolveLineColor.rgb,lineMask*_DissolveLineColor.a);
            }
            #endif
            //


        #endif

        //叠加贴图 2（保留旧颜色渐变的执行位置）
        #ifdef _COLORMAPBLEND
            #if defined(_NOISEMAP)
                colorBlendMap_uv += cum_noise * _ColorBlendVec.x; //加入扭曲效果
            #endif
            bool forceColorBlendLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_COLORBLENDMAP);
            half4 colorBlend = SampleTexture2DWithWrapFlags(_ColorBlendMap,colorBlendMap_uv,FLAG_BIT_WRAPMODE_COLORBLENDMAP,forceColorBlendLod0);
            NBFX_ApplyColorOverlayV1(
                result,
                alpha,
                colorBlend,
                _ColorBlendColor,
                _ColorBlendColorIntensity,
                _ColorBlendVec.z,
                !CheckLocalFlags1(FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_2_ADD),
                CheckLocalFlags(FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE));
        #endif

                //遮罩部分
        #if defined(_MASKMAP_ON)

            #if defined(_NOISEMAP)
                MaskMapuv += cum_noise * _MaskDistortion_intensity; //加入扭曲效果
            #endif

            half mask1 = 1;
            if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT))
            {
                const int maskMapWrapMode = CheckLocalWrapFlags(FLAG_BIT_WRAPMODE_MASKMAP);
                half maskMapTimeValue;
                if (maskMapWrapMode == 0 || maskMapWrapMode == 2)
                {
                    maskMapTimeValue = frac(MaskMapuv.x);
                }
                else
                {
                    maskMapTimeValue = saturate(MaskMapuv.x);
                }

                int maskMapAlphaCount = _MaskMapGradientCount;
                mask1 = SamplePackedGradientAlpha(_MaskMapGradientFloat0, _MaskMapGradientFloat1, _MaskMapGradientFloat2, maskMapAlphaCount, maskMapTimeValue);
            }
            else
            {
                bool forceMaskLod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_MASKMAP);
                half4 maskmap1Sample = SampleTexture2DWithWrapFlags(_MaskMap, MaskMapuv,FLAG_BIT_WRAPMODE_MASKMAP,forceMaskLod0);
                mask1 = GetColorChannel(maskmap1Sample,FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1);

            }

            #if defined(_MASKMAP2_ON)
            {
                half mask2 = 1;
                if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT))
                {
                    const int maskMap2WrapMode = CheckLocalWrapFlags(FLAG_BIT_WRAPMODE_MASKMAP2);
                    half maskMap2TimeValue;
                    if (maskMap2WrapMode == 0 || maskMap2WrapMode == 3)
                    {
                        maskMap2TimeValue = frac(MaskMapuv2.y);
                    }
                    else
                    {
                        maskMap2TimeValue = saturate(MaskMapuv2.y);
                    }

                    int maskMap2AlphaCount = _MaskMap2GradientCount;
                    mask2 = SamplePackedGradientAlpha(_MaskMap2GradientFloat0, _MaskMap2GradientFloat1, _MaskMap2GradientFloat2, maskMap2AlphaCount, maskMap2TimeValue);
                }
                else
                {
                    bool forceMask2Lod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_MASKMAP2);
                    half4 maskMap2Sample = SampleTexture2DWithWrapFlags(_MaskMap2, MaskMapuv2,FLAG_BIT_WRAPMODE_MASKMAP2,forceMask2Lod0);
                    mask2 = GetColorChannel(maskMap2Sample,FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2);
                }
                mask1 *= mask2;
            }
            #endif

            #if defined(_MASKMAP3_ON)
            {
                half mask3 = 1;
                if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT))
                {
                    const int maskMap3WrapMode = CheckLocalWrapFlags(FLAG_BIT_WRAPMODE_MASKMAP3);
                    half maskMap3TimeValue;
                    if (maskMap3WrapMode == 0 || maskMap3WrapMode == 2)
                    {
                        maskMap3TimeValue = frac(MaskMapuv3.x);
                    }
                    else
                    {
                        maskMap3TimeValue = saturate(MaskMapuv3.x);
                    }

                    int maskMap3AlphaCount = _MaskMap3GradientCount;
                    mask3 = SamplePackedGradientAlpha(_MaskMap3GradientFloat0, _MaskMap3GradientFloat1, _MaskMap3GradientFloat2, maskMap3AlphaCount, maskMap3TimeValue);
                }
                else
                {
                    bool forceMask3Lod0 = CheckForceNoMipFlags(FLAG_BIT_FORCE_NO_MIP_MASKMAP3);
                    half4 maskMap3Sample = SampleTexture2DWithWrapFlags(_MaskMap3, MaskMapuv3,FLAG_BIT_WRAPMODE_MASKMAP3,forceMask3Lod0);
                    mask3 = GetColorChannel(maskMap3Sample,FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3);
                }
                mask1 *= mask3;
            }
            #endif

            #ifdef _PROGRAM_NOISE_ACTIVE
                mask1 =  BlendPNoise(_W9ParticleShaderPNoiseBlendFlag,FLAG_BIT_PNOISE_BLEND_POS_0_MASK,mask1,programNoise,_MaskPNoiseBlendOpacity);
            #endif


            NBFX_MaskCoverageInputV3 maskCoverageInput = (NBFX_MaskCoverageInputV3)0;
            maskCoverageInput.combinedMaskAfterNoise = mask1;
            maskCoverageInput.refine = CheckLocalFlags1(FLAG_BIT_PARTICLE_1_MASK_REFINE);
            maskCoverageInput.refinePowMulAdd = _MaskRefineVec.xyz;
            maskCoverageInput.overallStrength = _MaskMapVec.x;
            mask1 = NBFX_ResolveMaskCoverageV3(maskCoverageInput);

            #ifdef NB_DEBUG_MASK
                return MakeParticleFragmentOutput(half4(mask1.rrr,1));
            #endif


            alpha *= mask1;  //mask边缘
        #endif

        //菲涅

        #if !defined(NB_DEPTH_SHADOW_PASS)
        #if defined(_FRESNEL)
        {
            half fresnelValue = 0;
            if(!ignoreFresnel())
            {
                _FresnelUnit.x += GetCustomData(_W9ParticleCustomDataFlag0,FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET,0,input.VaryingsP_Custom1,input.VaryingsP_Custom2);;
                fresnelValue = NBFX_EvaluateFresnelV1(viewDirWS,
                    input.normalWSAndAnimBlend.xyz, _FresnelRotation.rgb,
                    _FresnelUnit,
                    CheckLocalFlags(FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON));
            }

            #ifdef NB_DEBUG_FRESNEL
                return MakeParticleFragmentOutput(half4(fresnelValue.rrr*_FresnelUnit.z,1));
            #endif


            NBFX_ApplyFresnelV1(result, alpha, fresnelValue,
                _FresnelUnit.z, _FresnelColor,
                CheckLocalFlags(FLAG_BIT_PARTICLE_FRESNEL_FADE_ON),
                CheckLocalFlags(FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA));

        }
        #endif

        #if defined(_DEPTH_OUTLINE)
        {
            NBFX_ApplyDepthOutlineV1(result, alpha, _DepthOutline_Color,
                _DepthOutline_Vec.xy, sceneZ, thisZ);
        }
        #endif







        //可以看https://www.cyanilux.com/tutorials/depth/
        // float4 projectedPosition = input.positionNDC;
        // float thisZ1 = LinearEyeDepth(projectedPosition.z / projectedPosition.w, _ZBufferParams);


        #if defined(_DISTANCE_FADE)
        {
            half fade = DepthFactor(thisZ, _Fade.x, _Fade.y);
            alpha *= fade;
        }
        #endif


        #if defined(_SOFTPARTICLES_ON)

        half softAlpha = NBFX_SoftParticlesV1(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, sceneZ,thisZ);
        alpha *= softAlpha;

        #endif
        #endif







        //和粒子颜色信息运算。雨轩：乘顶点色。
        if(!CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR))
        {
            #if !defined(NB_DEPTH_SHADOW_PASS)
            result *= input.color.rgb;
            #endif
            alpha *= input.color.a;
        }
        // 程序额外的颜色
        #if !defined(NB_DEPTH_SHADOW_PASS)
        result *= _ColorA.rgb;
        #endif
        alpha *= _ColorA.a;
        // // alpha *= _ColorA * 0.8;


        #if !defined(NB_DEPTH_SHADOW_PASS)
        #ifdef _DEPTH_DECAL
        alpha *= decalAlpha;
        #endif

        half3 beforeFogResult = result;
        result = MixFog(result,input.positionWS.w);
        result = lerp(beforeFogResult, result, _fogintensity);

        UNITY_BRANCH
        if (!CheckLocalFlags(FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX))
        {
           ColorAdjustment(result,alpha,input.VaryingsP_Custom1,input.VaryingsP_Custom2);
        }

        UNITY_FLATTEN
        if(CheckLocalFlags(FLAG_BIT_PARTICLE_LINEARTOGAMMA_ON))
        {
            result.rgb = LinearToGammaSpace(result.rgb);
        }
        #endif


        alpha *= _AlphaAll;
        alpha = saturate(alpha);

        #if defined(NB_DEPTH_ONLY_PASS)
            #ifdef _ALPHATEST_ON
                clip(alpha - _Cutoff);
            #endif
            return MakeParticleFragmentOutput(half4(input.clipPos.z, 0, 0, 0));
        #elif defined(NB_SHADOW_CASTER_PASS)
            #ifdef _ALPHATEST_ON
                clip(alpha - _Cutoff);
            #endif

            if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_TRANSPARENT_MODE))
            {
                if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_TRANSPARENT_SHADOW_DITHER))
                {
                    clip(NBFX_ShadowDitherMaskClipV1(input.clipPos, alpha));
                }
                else
                {
                    clip(alpha - 0.5);
                }
            }

            return MakeParticleFragmentOutput(0);
        #endif

        half alphaStrength = alpha;
        #if defined  (_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
            #ifdef _ALPHAPREMULTIPLY_ON
                NBFX_ApplyBlendOutputV1(result, alpha, true, _AdditiveToPreMultiplyAlphaLerp);
            #else
                NBFX_ApplyBlendOutputV1(result, alpha, false, 1.0h);
            #endif
        #endif


        #ifdef _SCREEN_DISTORT_MODE

        NBFX_DistortionInputV1 distortionInput = (NBFX_DistortionInputV1)0;
        distortionInput.signedNoise = screenDistort_Noise.xy;
        distortionInput.noiseMask = screenDistort_Noise.z;
        distortionInput.alphaBeforePremultiply = alphaStrength;
        distortionInput.refineAlpha = CheckLocalFlags1(FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE);
        distortionInput.alphaPow = _ScreenDistortAlphaPow;
        distortionInput.alphaMultiplier = _ScreenDistortAlphaMulti;
        distortionInput.alphaAdd = _ScreenDistortAlphaAdd;
        distortionInput.intensity = _ScreenDistortIntensity;
        NBFX_DistortionPayloadV1 distortion = NBFX_BuildDistortionPayloadV1(distortionInput);
        #ifdef _DEFERRED_DISTORT_PASS
            result = half3(distortion.signedRG,1.0);
            alpha = distortion.coverage * distortion.intensity;
        #endif

        #ifdef _CAMERA_OPAQUE_DISTORT_PASS
            float2 screenDistortUV = screenUV;
            screenDistortUV = screenDistortUV + distortion.signedRG * distortion.coverage * distortion.intensity;
            half4 screenTexDistortSample = SampleTexture2DWithWrapFlags(_CameraOpaqueTexture,screenDistortUV,FLAG_BIT_WRAPMODE_BASEMAP,false);
            result = screenTexDistortSample.xyz;
            alpha = 1;
            // alpha = screenTexDistortSample.a;
        #endif

        #endif




        half4 color = half4(result, alpha);



        #ifdef _ALPHATEST_ON
        clip(color.a - _Cutoff);

        #endif

        color = min(color,1000);


        return MakeParticleFragmentOutput(color);
    }

#endif

            
            
            ENDHLSL
            
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            Blend One Zero
            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #define PARTICLE
            #define _FX_LIGHT_MODE_UNLIT
            #define NB_DEPTH_ONLY_PASS

            #if defined ( SHADER_API_GLES)||defined(SHADER_API_GLES3)
            #pragma target 3.0
            #else
            #pragma target 4.5
            #endif

            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit

            #pragma shader_feature_local _ _MASKMAP_ON
            #pragma shader_feature_local _MASKMAP2_ON
            #pragma shader_feature_local _MASKMAP3_ON
            #pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISE_MASKMAP
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _DISSOLVE_MASK
            #pragma shader_feature_local _PROGRAM_NOISE
            #pragma shader_feature_local _PROGRAM_NOISE_SIMPLE
            #pragma shader_feature_local _PROGRAM_NOISE_VORONOI
            #pragma shader_feature_local _SHARED_UV
            #pragma shader_feature_local _ _VAT _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _ _VAT_HOUDINI _VAT_TYFLOW
            #pragma shader_feature_local_vertex _ _HOUDINI_VAT_SOFTBODY _HOUDINI_VAT_RIGIDBODY _HOUDINI_VAT_DYNAMIC_REMESH _HOUDINI_VAT_PARTICLE_SPRITE
            #pragma shader_feature_local_vertex _ _TYFLOW_VAT_ABSOLUTE _TYFLOW_VAT_RELATIVE _TYFLOW_VAT_SKIN_R _TYFLOW_VAT_SKIN_PR _TYFLOW_VAT_SKIN_PRSAVE _TYFLOW_VAT_SKIN_PRSXYZ
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _CUSTOM_LOCAL_TRANSFORM
            #pragma shader_feature_local_vertex _VERTEX_OFFSET
            #pragma shader_feature_local_vertex _VERTEX_OFFSET_MASKMAP
            #pragma shader_feature_local _UNSCALETIME
            #pragma shader_feature_local _SCRIPTABLETIME

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"
            #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            Blend One Zero
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #define PARTICLE
            #define _FX_LIGHT_MODE_UNLIT
            #define NB_SHADOW_CASTER_PASS

            #if defined ( SHADER_API_GLES)||defined(SHADER_API_GLES3)
            #pragma target 3.0
            #else
            #pragma target 4.5
            #endif

            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit

            #pragma shader_feature_local _ _MASKMAP_ON
            #pragma shader_feature_local _MASKMAP2_ON
            #pragma shader_feature_local _MASKMAP3_ON
            #pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISE_MASKMAP
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _DISSOLVE_MASK
            #pragma shader_feature_local _PROGRAM_NOISE
            #pragma shader_feature_local _PROGRAM_NOISE_SIMPLE
            #pragma shader_feature_local _PROGRAM_NOISE_VORONOI
            #pragma shader_feature_local _SHARED_UV
            #pragma shader_feature_local _ _VAT _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _ _VAT_HOUDINI _VAT_TYFLOW
            #pragma shader_feature_local_vertex _ _HOUDINI_VAT_SOFTBODY _HOUDINI_VAT_RIGIDBODY _HOUDINI_VAT_DYNAMIC_REMESH _HOUDINI_VAT_PARTICLE_SPRITE
            #pragma shader_feature_local_vertex _ _TYFLOW_VAT_ABSOLUTE _TYFLOW_VAT_RELATIVE _TYFLOW_VAT_SKIN_R _TYFLOW_VAT_SKIN_PR _TYFLOW_VAT_SKIN_PRSAVE _TYFLOW_VAT_SKIN_PRSXYZ
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _CUSTOM_LOCAL_TRANSFORM
            #pragma shader_feature_local_vertex _VERTEX_OFFSET
            #pragma shader_feature_local_vertex _VERTEX_OFFSET_MASKMAP
            #pragma shader_feature_local _UNSCALETIME
            #pragma shader_feature_local _SCRIPTABLETIME

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"
            #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl"

            ENDHLSL
        }

 // ------------------------------------------------------------------
        //  Distort pass.
        Pass
        {
            Name "NBCameraOpaqueDistortPass"
            Tags{
              "LightMode" = "NBCameraOpaqueDistortPass" 
			} //Queue设置是希望特效渲染在场景透明物体前面
            Blend SrcAlpha OneMinusSrcAlpha
            offset [_offsetFactor], [_offsetUnits]
            Cull[_Cull]

            HLSLPROGRAM
            #define PARTICLE
            //20240228 target3.0 顶点着色器限制16个输出。提高版本
           #if defined ( SHADER_API_GLES)||defined(SHADER_API_GLES3)
            #pragma target 3.0
            #else
            #pragma target 4.5
            #endif
            
            // -------------------------------------
            // Material Keywords
 
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDebugPragmas.hlsl"

            #define _SCREEN_DISTORT_MODE
            #define _CAMERA_OPAQUE_DISTORT_PASS
            #pragma shader_feature_local _ _DISTORT_REFRACTION
            #pragma shader_feature_local _ _MASKMAP_ON
            #pragma shader_feature_local _MASKMAP2_ON
            #pragma shader_feature_local _MASKMAP3_ON
            // #pragma shader_feature_local _MASKMAP
            // #pragma shader_feature_local _MASKMAP2
            //#pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISE_MASKMAP
            #pragma shader_feature_local_fragment _CHROMATIC_ABERRATION
            //#pragma shader_feature_local _EMISSION   //流光
            // #pragma shader_feature_local _EMISSION
            //#pragma shader_feature_local _ _DISSOLVE    //溶解
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _DISSOLVE_MASK
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP_MAP
            #pragma shader_feature_local _PROGRAM_NOISE
            #pragma shader_feature_local _PROGRAM_NOISE_SIMPLE
            #pragma shader_feature_local _PROGRAM_NOISE_VORONOI
            // #pragma shader_feature_local  _COLORMAPBLEND//颜色渐变
            // #pragma  shader_feature_local  _COLOR_RAMP//颜色映射
            #pragma shader_feature_local _SHARED_UV//公共UV
            
            
            
            // #pragma multi_compile_local _ UNITY_UI_CLIP_RECT _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS //UI 2D遮罩
            // #pragma shader_feature_local _PARCUSTOMDATA_ON

            //用于特效层关键字
            // #pragma shader_feature_local  _UIEFFECT_ON
            #pragma shader_feature_local _CUSTOM_LOCAL_TRANSFORM
            #pragma shader_feature_local_vertex _VERTEX_OFFSET
            #pragma shader_feature_local_vertex _VERTEX_OFFSET_MASKMAP
            
            #pragma shader_feature_local_fragment _FRESNEL

            
            // #pragma multi_compile _ _UIPARTICLE_ON//用于UIParticle组件动态更改参数//暂时注释掉，觉得没什么意义
            #pragma multi_compile _ SOFT_UI_FRAME EVALUATE_SH_MIXED EVALUATE_SH_VERTEX//用于UI软蒙版
            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON     //设置alpah Add 。。组合
            #pragma shader_feature_local _ALPHATEST_ON
            //#pragma shader_feature_local _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON  //粒子颜色和材质颜色的混合运算  暂时先不要了
            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _SOFTPARTICLES_ON
            // #pragma shader_feature_local _OCCLUDEOPACITY_ON
            // #pragma shader_feature_local _ _SATURABILITY_ON

            //UnscaleTime用于接收项目传的公开不受缩放影响的Time值
            #pragma shader_feature_local _UNSCALETIME
            //scriptableTime用于程序每帧传值
            #pragma shader_feature_local _SCRIPTABLETIME
            //#pragma shader_feature_local _DISTORTION_ON

            #pragma shader_feature_local _DEPTH_DECAL
            #pragma shader_feature_local _DEPTH_OUTLINE
            #pragma shader_feature_local _DISTANCE_FADE
            // #pragma shader_feature_local _PARALLAX_MAPPING

            #pragma shader_feature_local _STENCIL_WITHOUT_PLAYER

            //LIGHTING
			// #pragma shader_feature_local _FX_LIGHT_MODE_UNLIT _FX_LIGHT_MODE_BLINN_PHONG _FX_LIGHT_MODE_HALF_LAMBERT _FX_LIGHT_MODE_PBR _FX_LIGHT_MODE_SIX_WAY 
            #pragma shader_feature_local _FX_LIGHT_MODE_UNLIT
            #pragma shader_feature_local _ _NORMALMAP
            // #pragma shader_feature_local _ _MATCAP
            // #pragma shader_feature_local _ _SPECULAR_COLOR
            // #pragma shader_feature_local _ VFX_SIX_WAY_ABSORPTION
            
            // -------------------------------------
            // Unity defined keywords
            // 之后进行优化时再说。
            #pragma multi_compile_fog
            // #define FOG_EXP2 1 
            
            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit
            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The DeclareDepthTexture.hlsl file contains utilities for sampling the Camera
            // depth texture.
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            
			#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"
            #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl"
            
            
            ENDHLSL
            
        }

// ------------------------------------------------------------------
        //  Distort pass.
        Pass
        {
            Name "NBDeferredDistortPass"
            Tags{
              "LightMode" = "NBDeferredDistortPass" 
			} //Queue设置是希望特效渲染在场景透明物体前面
            Blend SrcAlpha OneMinusSrcAlpha
            offset [_offsetFactor], [_offsetUnits]
            Cull[_Cull]

            HLSLPROGRAM
            #define PARTICLE
            //20240228 target3.0 顶点着色器限制16个输出。提高版本
           #if defined ( SHADER_API_GLES)||defined(SHADER_API_GLES3)
            #pragma target 3.0
            #else
            #pragma target 4.5
            #endif
            
            // -------------------------------------
            // Material Keywords
 
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDebugPragmas.hlsl"

			#define _SCREEN_DISTORT_MODE
            #define _DEFERRED_DISTORT_PASS
            #pragma shader_feature_local _ _DISTORT_REFRACTION
            
            #pragma shader_feature_local _ _MASKMAP_ON
            #pragma shader_feature_local _MASKMAP2_ON
            #pragma shader_feature_local _MASKMAP3_ON
            // #pragma shader_feature_local _MASKMAP
            // #pragma shader_feature_local _MASKMAP2
            //#pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISE_MASKMAP
            #pragma shader_feature_local_fragment _CHROMATIC_ABERRATION
            //#pragma shader_feature_local _EMISSION   //流光
            // #pragma shader_feature_local _EMISSION
            //#pragma shader_feature_local _ _DISSOLVE    //溶解
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _DISSOLVE_MASK
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP_MAP
            #pragma shader_feature_local _PROGRAM_NOISE
            #pragma shader_feature_local _PROGRAM_NOISE_SIMPLE
            #pragma shader_feature_local _PROGRAM_NOISE_VORONOI
            // #pragma shader_feature_local  _COLORMAPBLEND//颜色渐变
            // #pragma  shader_feature_local  _COLOR_RAMP//颜色映射
            #pragma shader_feature_local _SHARED_UV//公共UV
            
            
            
            // #pragma multi_compile_local _ UNITY_UI_CLIP_RECT _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS //UI 2D遮罩
            // #pragma shader_feature_local _PARCUSTOMDATA_ON

            //用于特效层关键字
            // #pragma shader_feature_local  _UIEFFECT_ON
            #pragma shader_feature_local _CUSTOM_LOCAL_TRANSFORM
            #pragma shader_feature_local_vertex _VERTEX_OFFSET
            #pragma shader_feature_local_vertex _VERTEX_OFFSET_MASKMAP
            
            #pragma shader_feature_local_fragment _FRESNEL

            
            // #pragma multi_compile _ _UIPARTICLE_ON//用于UIParticle组件动态更改参数//暂时注释掉，觉得没什么意义
            #pragma multi_compile _ SOFT_UI_FRAME EVALUATE_SH_MIXED EVALUATE_SH_VERTEX//用于UI软蒙版
            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON     //设置alpah Add 。。组合
            #pragma shader_feature_local _ALPHATEST_ON
            //#pragma shader_feature_local _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON  //粒子颜色和材质颜色的混合运算  暂时先不要了
            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _SOFTPARTICLES_ON
            // #pragma shader_feature_local _OCCLUDEOPACITY_ON
            // #pragma shader_feature_local _ _SATURABILITY_ON

            //UnscaleTime用于接收项目传的公开不受缩放影响的Time值
            #pragma shader_feature_local _UNSCALETIME
            //scriptableTime用于程序每帧传值
            #pragma shader_feature_local _SCRIPTABLETIME
            //#pragma shader_feature_local _DISTORTION_ON

            #pragma shader_feature_local _DEPTH_DECAL
            #pragma shader_feature_local _DEPTH_OUTLINE
            #pragma shader_feature_local _DISTANCE_FADE
            // #pragma shader_feature_local _PARALLAX_MAPPING

            #pragma shader_feature_local _STENCIL_WITHOUT_PLAYER

            //LIGHTING
			// #pragma shader_feature_local _FX_LIGHT_MODE_UNLIT _FX_LIGHT_MODE_BLINN_PHONG _FX_LIGHT_MODE_HALF_LAMBERT _FX_LIGHT_MODE_PBR _FX_LIGHT_MODE_SIX_WAY 
            #pragma shader_feature_local _FX_LIGHT_MODE_UNLIT
            #pragma shader_feature_local _ _NORMALMAP
            // #pragma shader_feature_local _ _MATCAP
            // #pragma shader_feature_local _ _SPECULAR_COLOR
            // #pragma shader_feature_local _ VFX_SIX_WAY_ABSORPTION
            
            // -------------------------------------
            // Unity defined keywords
            // 之后进行优化时再说。
            #pragma multi_compile_fog
            // #define FOG_EXP2 1 
            
            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit
            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The DeclareDepthTexture.hlsl file contains utilities for sampling the Camera
            // depth texture.
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            
			#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"
            #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl"
            
            
            ENDHLSL
            
        }

     // ------------------------------------------------------------------
        //  Forward pass.
        Pass
        {
            Name "Universal2D"
            Tags
        {
              "LightMode" = "Universal2D" 
        } //Queue设置是希望特效渲染在场景透明物体前面
            offset [_offsetFactor], [_offsetUnits]
            Cull[_Cull]

            HLSLPROGRAM
            #define PARTICLE
            //20240228 target3.0 顶点着色器限制16个输出。提高版本
           #if defined ( SHADER_API_GLES)||defined(SHADER_API_GLES3)
            #pragma target 3.0
            #else
            #pragma target 4.5
            #endif
            
            // -------------------------------------
            // Material Keywords
 
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDebugPragmas.hlsl"
            
            #pragma shader_feature_local _ _SCREEN_DISTORT_MODE
            #pragma shader_feature_local _ _MASKMAP_ON
            #pragma shader_feature_local _MASKMAP2_ON
            #pragma shader_feature_local _MASKMAP3_ON
            // #pragma shader_feature_local _MASKMAP
            // #pragma shader_feature_local _MASKMAP2
            //#pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISEMAP
            #pragma shader_feature_local _NOISE_MASKMAP
            #pragma shader_feature_local_fragment _CHROMATIC_ABERRATION
            //#pragma shader_feature_local _EMISSION   //流光
            #pragma shader_feature_local _EMISSION
            //#pragma shader_feature_local _ _DISSOLVE    //溶解
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _DISSOLVE_MASK
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP
            #pragma shader_feature_local_fragment _DISSOLVE_RAMP_MAP
            #pragma shader_feature_local _PROGRAM_NOISE
            #pragma shader_feature_local _PROGRAM_NOISE_SIMPLE
            #pragma shader_feature_local _PROGRAM_NOISE_VORONOI
            #pragma shader_feature_local  _COLORMAPBLEND//颜色渐变
            #pragma  shader_feature_local  _COLOR_RAMP//颜色映射
            #pragma shader_feature_local_fragment _COLOR_RAMP_MAP
            #pragma shader_feature_local _SHARED_UV//公共UV
            
            
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS //UI 2D遮罩
            // #pragma shader_feature_local _ _CH_XIANXING _CH_JINGXIANG  _CH_SELF   //线性擦除  径向擦除  mask擦除
            #pragma shader_feature_local _PARCUSTOMDATA_ON

            //用于特效层关键字
            // #pragma shader_feature_local  _UIEFFECT_ON
            #pragma shader_feature_local _CUSTOM_LOCAL_TRANSFORM
            #pragma shader_feature_local_vertex _VERTEX_OFFSET
            #pragma shader_feature_local_vertex _VERTEX_OFFSET_MASKMAP
            
            #pragma shader_feature_local_fragment _FRESNEL

            
            // #pragma multi_compile _ _UIPARTICLE_ON//用于UIParticle组件动态更改参数//暂时注释掉，觉得没什么意义
            #pragma multi_compile _ SOFT_UI_FRAME EVALUATE_SH_MIXED EVALUATE_SH_VERTEX//用于UI软蒙版
            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON     //设置alpah Add 。。组合
            #pragma shader_feature_local _ALPHATEST_ON
            //#pragma shader_feature_local _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON  //粒子颜色和材质颜色的混合运算  暂时先不要了
            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            // #pragma shader_feature_local _SOFTPARTICLES_ON
            // #pragma shader_feature_local _OCCLUDEOPACITY_ON
            // #pragma shader_feature_local _ _SATURABILITY_ON

            //UnscaleTime用于接收项目传的公开不受缩放影响的Time值
            #pragma shader_feature_local _UNSCALETIME
            //scriptableTime用于程序每帧传值
            #pragma shader_feature_local _SCRIPTABLETIME
            //#pragma shader_feature_local _DISTORTION_ON

            // #pragma shader_feature_local _DEPTH_DECAL
            // #pragma shader_feature_local _PARALLAX_MAPPING

            #pragma shader_feature_local _STENCIL_WITHOUT_PLAYER

            //LIGHTING
			// #pragma shader_feature_local _FX_LIGHT_MODE_UNLIT _FX_LIGHT_MODE_BLINN_PHONG _FX_LIGHT_MODE_HALF_LAMBERT _FX_LIGHT_MODE_PBR _FX_LIGHT_MODE_SIX_WAY 
            #pragma shader_feature_local _FX_LIGHT_MODE_UNLIT
			#pragma shader_feature_local _ _NORMALMAP
   //          #pragma shader_feature_local _ _MATCAP
   //          #pragma shader_feature_local _ _SPECULAR_COLOR
   //          #pragma shader_feature_local _ VFX_SIX_WAY_ABSORPTION
            
            // -------------------------------------
            // Unity defined keywords
            // 之后进行优化时再说。
            #pragma multi_compile_fog
            // #define FOG_EXP2 1 
            
            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit
            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The DeclareDepthTexture.hlsl file contains utilities for sampling the Camera
            // depth texture.
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            
			#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

            #include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl"
            
            
            ENDHLSL
            
        }
    }
    
    CustomEditor "NBShaderEditor.NBShaderGUI"
}
