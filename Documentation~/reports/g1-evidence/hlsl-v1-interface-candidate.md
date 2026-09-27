> **Main-Agent review status:** Input proposal for G1. No API or module below exists yet; the first vertical slice is specified, later feature-local fields remain design obligations.

# T01 / G1：共享 HLSL v1 最小闭合接口候选（只读，2026-09-27）

**状态：待主 Agent 审查，不是已编译的 API，不授权改 Shader。**依据当前 `NBShaderInput.hlsl`、`NBShaderForwardPass.hlsl`、`NBShaderFlags.hlsl`、`NBShader.shader` 及 GF `NBGFUnlitSubTarget.cs`；未运行 Unity Editor，未修改 `Packages/NB_FX`、`Assets` 或 `ProjectSettings`。以下 `NBFX*V1` 是项目拟定义的 HLSL 类型/函数名，**不是 Unity/URP/SG 内置 API**。文件名均为拟新增的独占文件，尚不存在。当前源 SHA-256：Input `4dedef4a...7bd9ace75a9`、Forward `18fd0be3...58efb056`、Flags `ff61aa93...d5b014df3`、Shader `9ea2ad77...0a43cb00`、GF SubTarget `00402edc...3fd0a879`。

## 1. 已读出的原链路与边界

- ShaderLab 七 Pass 共用 `NBShaderForwardPass.hlsl`；Vertex 入口 `vertParticleUnlit(AttributesParticle)` 位于 `:117-345`，Fragment 入口 `fragParticleUnlit(VaryingsParticle, half facing : VFACE)` 位于 `:367-1357`。`NBShaderInput.hlsl:13-251` 有单一 `UnityPerMaterial` CBUFFER；`:1376-1476` 的 `AttributesParticle`/`VaryingsParticle` 绑定了旧 ShaderLab 顶点流/插值语义，不应作为跨 Graph 模块签名。
- 顶点次序：CustomLocal 变换 → VAT → 初始 WS/clip/normal/tangent → 根据 `isProcessUVInFrag()` 决定 UV 在 vertex 或 fragment → VertexOffset → **重算** WS/OS/clip → ShadowCaster 偏移（Forward `:128-147,203-315,318-344`）。Depth/Shadow 也走位移和 Alpha，不可只抽 Forward 彩色路径。
- 片元次序：screen/depth/view → UV → PNoise/POM/Normal → Noise/Refraction → 主色 → 光照/MatCap → Overlay1/Ramp/Dissolve/Overlay2/Mask/Fresnel → 深度淡出/软粒子 → 顶点色/附加色/Fog/颜色调整 → 总 Alpha → Pass 输出（Forward `:367-430,433-603,605-797,799-1136,1138-1279,1281-1356`）。主贴图阶段或 Fog 后两处 ColorAdjustment 分支互斥（`:719-725,1260-1268`）。
- `NBShaderFlags.hlsl:178-225,228-263,281-318` 已有 CustomData/UV/PNoise 解码，保持唯一协议实现；`NBShaderInput.hlsl:367-489` 已有 Flags、Wrap、ForceNoMip、Channel 与采样调用；不复制第二套 bit 含义。各个 packed 字在 `UnityPerMaterial` 为 `uint`（Input `:222-240`），Graph Float/half 桥接仍未证实。
- GF SubTarget `NBGFUnlitSubTarget.cs:39-89` 从 URP Unlit Forward Pass 添加两个 LightMode，再经 `PostProcessSubShader` 转 VFX；它证明 Pass 结构候选，**不**提供本共享计算 API、Graph 资源绑定或产品 Pass HLSL。T06 必须重测。

## 2. v1 最小闭合数据契约（候选）

**原则：**只放跨模块会传递的少量状态；450 个 Property 不做一份巨型 `Params` 拷贝。每个功能的只读参数切片由绑定层按原 `UnityPerMaterial` 类型装配。`NBShaderInput.hlsl` 的 CBUFFER、纹理和 sampler 声明在 ShaderLab 只保留**一份**；Graph/VFX 由 T06 host wrapper 提供，公共计算文件不得重声明同名全局资源。编译期 keyword 保持原语义，不伪装成逐粒子开关。

```hlsl
// 候选伪码：字段列表是第一垂直切片的最低数据集，不是 450 项完整映射。
struct NBFXFrameV1 {
    float timeY;                 // 原 vert/frag 各自写入的 _Time.y，不擅改 Unscaled/ScriptableTime
};

struct NBFXVertexStreamsV1 {
    float4 positionInput;       // 原 input.vertex；CustomLocal 路径语义另见歧义 A1
    float3 normalInput;
    float4 tangentInput;        // .w 为 handedness 输入；原变换后还乘 odd negative scale
    float4 meshTexcoord0;       // xy 默认、zw 特殊；Flipbook 时有独立 texcoordBlend
    float4 custom1, custom2;   // 原 TEXCOORD1/2；不能与 packed selector 混同
    half4 vertexColor;
    // VAT 所需 TEXCOORD3..7、Flipbook 数据按已启用宿主另列，不默认为 0。
};

struct NBFXFragmentGeometryV1 {
    float3 positionOS, positionWS;
    half3 normalWS;
    float3 viewDirWS;
    float2 rasterScreenUV;     // 原 SV_POSITION.xy / _ScaledScreenParams.xy
    float sceneEyeDepth, thisEyeDepth;
    half facing;               // 原 VFACE；用于双面 normal/baseColor
    half4 vertexColor;
    float4 custom1, custom2;
};

struct NBFXSurfaceV1 {
    half3 rgb;
    half alpha;
    half2 cumulativeNoise;
    half screenNoiseMask;
    // 仅在对应阶段有定义；不要依赖零初始化替代 feature guard。
};

struct NBFXDistortionV1 {
    half2 signedRG;           // 原 screenDistort_Noise.xy；不预先乘 coverage
    half coverage;            // 原 screenDistortAlpha，经可选 pow/mul/add
    half intensity;           // 原 _ScreenDistortIntensity
};
```

`BaseUVs` 的九个坐标候选已在 `NBShaderFlags.hlsl:228-239`；`ParticleUVs` 的 17 个用途已在 `NBShaderInput.hlsl:835-854`。**v1 不再定义语义等价的第二套 UV struct。**若新文件必须独立包含这些类型，由主 Agent 决定一次性把现有 struct 定义迁入唯一 shared-types 文件，并在 T05 调整旧 include；T02/T03/T04 不能各自复制 typedef。旧 `AttributesParticle`/`VaryingsParticle` 仍留在 ShaderLab binding，由 T05 装配上述窄输入；Graph 端是否能供齐字段是 T06 实测项。

## 3. 模块签名和调用时机（待编译验证）

以下是**功能签名**。为使首个 BaseUV → 主贴图采样 → 扭曲 payload 切片不是空泛的 `Params` 占位，先把该切片的字段列全；其他功能参数切片仍由逐属性表冻结，不能以整个 `UnityPerMaterial` 隐式可写全局代替。HLSL `Texture2D`/Sampler 参数是否可被 SG File Custom Function 原样传入，由 T06 生成代码核对；这里不虚构 Graph API。`SamplerState` 聚合是否能跨所有目标编译器，也是 T02 首个离线编译/Unity 验证点；若失败，改成四个显式 sampler 实参，而不是回退到隐式重声明。

```hlsl
// 首切片的精确字段候选；flag 常量/解码仍调用唯一的 NBShaderFlags.hlsl。
struct NBFXUVInputV1 {
    float4 meshTexcoord0;
    float2 specialUVInTexcoord3;
    half4 custom1, custom2; // 保留原 GetCustomData(half4) 输入精度
    float3 positionOS, positionWS;
    float2 screenUV;         // 调用方标注 raster 或 clip 来源
};
struct NBFXUVParamsV1 {
    uint flags0, flags1, customDataFlag0, customDataFlag3;
    uint uvModeFlag0, uvModeFlagType0;
    float4 mainTexReverseST, uiMainTexST, baseMapST, sharedUVST;
    float4 baseMapMaskMapOffset, sharedUVVec;
    float4 twirlParameter, polarCenter;
    float4x4 cylinderMatrix;
    float twirlStrength, baseMapUVRotation, baseMapUVRotationSpeed;
    half worldSpaceUVModeSelector, objectSpaceUVModeSelector;
    // 上述字段足以覆盖当前 ProcessBaseUVs 的所有 *数值* 读取；
    // _FLIPBOOKBLENDING_ON 仍是宿主的编译期条件，绝不伪装成 runtime 字段。
};
struct NBFXSamplerBindingsV1 {
    SamplerState repeatSampler, clampSampler;
    SamplerState repeatUClampVSampler, clampURepeatVSampler;
    // point_clamp 非当前 SampleTexture2DWithWrapFlags 的分支，不放进首切片。
};
struct NBFXDistortionParamsV1 {
    bool refineAlpha;         // flags1 的 SCREEN_DISTORT_ALPHA_REFINE 位的解码结果
    half alphaPow, alphaMultiplier, alphaAdd, intensity;
};
```

这个 `NBFXUVParamsV1` **只封闭 `ProcessBaseUVs`**（Input `:871-977`），不能直接传给 `ParticleProcessUV`：后者读取 Flipbook、Mask、Emission、Dissolve、Noise 等各自 ST/rotation/offset 和更多 custom-data flag（`:980-1129`），须拆为 feature-local 参数切片。采样还须由调用方传 `packedWrapFlags`、`wrapBit`、`forceLod0`；`_CAMERA_OPAQUE_DISTORT_PASS` 对 BaseMap 强制 Clamp 的特殊分支（Input `:434-447`）必须保留为独立明确的 pass 输入或编译期分支，不能由普通 wrap flag 推导。

**纯函数实现约束：**原 `CheckLocalFlags*` 隐式读 CBUFFER（Input `:367-374`）；T02 在新函数中可对 `p.flags0/flags1` 按 `NBShaderFlags.hlsl` 的**原常量**执行位测试，或者由 T05 host 传已解码 bool，但不应在公共计算文件再次声明 `_W9ParticleShaderFlags*`。原 `GetCustomData` 与 `GetUVByUVMode` 本身接受显式 packed 值，可以直接复用（Flags `:178-263`）。`NBFX_BuildBaseUVsV1` 的字段闭合**不等于**完整 Alpha/Noise/Distortion 功能已闭合；后者须待 T03 功能切片参数和 T05 调度逐步冻结。

```hlsl
// T02：UV / 协议使用 / 采样，vertex 或 fragment 由宿主按原条件选择。
BaseUVs NBFX_BuildBaseUVsV1(
    NBFXUVInputV1 input, NBFXUVParamsV1 p, NBFXFrameV1 frame);
// 第二行是功能目标，feature-local 参数字段尚未冻结，不属于第一闭合切片。
ParticleUVs NBFX_BuildFeatureUVsV1(
    NBFXUVInputV1 input, NBFXFeatureUVParamsV1 p, NBFXFrameV1 frame, BaseUVs baseUV);
half4 NBFX_SampleWithWrapV1(
    Texture2D texture, float2 uv, uint packedWrapFlags,
    uint wrapBit, bool forceLod0, bool cameraOpaquePass,
    NBFXSamplerBindingsV1 samplers);

// T03：给已经按 T02/宿主采好的值做顺序计算，不拥有 Texture/CBUFFER 声明。
void NBFX_ApplyBaseV1(half4 sampledBase, half4 effectiveBaseColor,
    half timelineIntensity, inout NBFXSurfaceV1 s);
void NBFX_ApplyDissolveV1(NBFXDissolveInputsV1 values,
    NBFXDissolveParamsV1 p, inout NBFXSurfaceV1 s);
void NBFX_ApplyMaskV1(half maskProduct, half maskStrength,
    inout NBFXSurfaceV1 s);
NBFXDistortionV1 NBFX_FinalizeDistortionV1(
    half2 signedNoise, half noiseMask, half alphaBeforePremultiply,
    NBFXDistortionParamsV1 p);

// T04：顶点几何 + 当前 URP 光照；URP 的 InputData 只出现在 URP adapter。
void NBFX_EvaluateVertexOffsetV1(
    NBFXVertexStreamsV1 streams, NBFXVertexOffsetParamsV1 p,
    float2 offsetUV, float2 maskUV,
    out float3 offsetOS);
void NBFX_ApplyURPLightingV1(
    InputData urpInput, NBFXLightingInputsV1 values,
    NBFXLightingParamsV1 p, inout NBFXSurfaceV1 s);

// T05：Pass binding，不放进共享数学模块。
// Deferred: half4(signedRG.x, signedRG.y, 1, coverage * intensity)
// Camera:   sample _CameraOpaqueTexture at rasterScreenUV
//           + signedRG * coverage * intensity; alpha = 1
```

**第一垂直切片闭环：**T05 给原材质/顶点流和 `UnityPerMaterial` → T02 在原 stage 产生 Base/Feature UV 与采样 → T03 得到主色/Alpha、Noise 与两 Pass 的 `NBFXDistortionV1` → T04 先做 VertexOffset/URP lighting（按原先后）→ T05 分流 Forward、Depth/Shadow、Deferred、CameraOpaque，保持旧 Pass/Blend/clip。片元功能迁出可以按 Overlay/Ramp/Dissolve/Mask/Fresnel 分批，但每批必须通过 T05 旧 ShaderLab A/B；不能先只在 Graph 渲染颜色当作共用计算已经成立。`NBFX_FinalizeDistortionV1` 必须在全 Alpha 链和 `alphaStrength` 固定后运行（Forward `:1278-1337`）；Deferred RG/Alpha 与 Camera 采样/Alpha 是两种输出，不可混同。

## 4. 阶段、精度、坐标、参数所有权

| 项 | 必须固定的 v1 语义 / 证据 | 责任 |
|---|---|---|
| 时间 | 原 `time` 在 vertex/fragment 分别从 `_Time.y` 写入（Input `:132`，Forward `:128,383`）；`_UNSCALETIME`/`_SCRIPTABLETIME` 有 pragma 但该链未见替代读取。`Frame.timeY` 只描述**当前**行为。 | T05 host 注入；T02 消费；其他时间需求另提案。 |
| UV 阶段 | `isProcessUVInFrag()` 在 Twirl/Polar、DepthDecal、POM、ScreenDistort、CameraOpaque 时为真（Input `:1478-1488`）；否则顶点计算再插值。VertexOffset 自有顶点 UV（Forward `:275-304`）。不能所有 UV 都搬到 fragment，也不能由 Graph 预览默认 UV 推断同 stage。 | T02 函数；T05 选择/插值；T06 检查 Graph 生成 stage。 |
| screenUV | vertex 路径计算 `clip.xy/clip.w*0.5+0.5`（Forward `:222-224,284-286`）；fragment 路径用 `SV_POSITION.xy/_ScaledScreenParams.xy`（`:385`），深度和 CameraOpaque 采样依赖后者。两者不可合并成未注明来源的 `screenUV`。 | T05 binding；T02/3 用带来源的输入。 |
| 空间 | CustomLocal helper 写 local/world 两矩阵（`NBParticleLocalTransformHelper.cs:73-78`）；`vert` 把 `input.vertex.xyz` 传给 `TransformWorldToObject_NB` 后再 VAT（Forward `:135-145`），不是可凭变量名推断的普通 OS 流；负缩放手性用于 tangent/bitangent（`:158-185`）。 | T04 明确有效空间与矩阵；T05 注入；A/B 覆盖非均匀和负缩放。 |
| 精度 | 位置/UV/屏幕/深度维持 `float`/`real`，颜色/部分采样保持现有 `half`，UV 选择和 packed 槽维持 `uint`，`GetCustomData` 现有输入 `half4`、返回 `float`（Flags `:178-225`）。不因 Graph 连线方便改成 half 或把完整 uint 通过 float/interpolator。 | T01 类型清单；T02/3/4 不擅改；T06 做类型/高位 GPU 证明。 |
| 采样 | `SampleTexture2DWithWrapFlags` 有 GLES 特殊 UV 修正与 sampler 选择（Input `:432-480`）；ForceNoMip 显式 LOD0 与普通隐式导数不等价（`:415-429`）；Noise UV 动画在实际 `SampleNoise`，不是 `ParticleProcessUV`（`:703-712,1113-1123`）。 | T02 保留调用点和 sampler/LOD；T05 提供资源；T06 校验 SG 生成 HLSL。 |
| 参数所有权 | ShaderLab 属性、191 字段 `UnityPerMaterial`、同名资源仍由原 Shader/`NBShaderInput` 绑定；T02/3/4 只读有效参数，不写材质全局。Graph 产品属性独立宿主绑定，逐粒子数据由 VFX 驱动。MPB `SetInt`/`SetInteger` 现存通道风险见 T01 主报告，不能作为安全桥接假设。 | T05 ShaderLab binding；T06 Graph binding；T07/08 GUI/VFX；主 Agent 审协议。 |
| Pass | 旧 ShaderLab 保留七 Pass 原名/顺序/RenderState；GF Graph 10、VFX 8 Pass 仅是试验。两精确 LightMode 需 T06/G3 以产品 wrapper 复验。T03 只算 payload，T05 负责 `SV_Target`/`SV_Depth`、clip/Blend/Stencil/Pass 选择。 | T05（旧）；T06（Graph/VFX）。 |

## 5. 建议的精确文件所有权（**仅候选，未分配**）

所有路径相对 `/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX/`。给每个 SubAgent **独占文件**；跨文件接口改变必须先给主 Agent proposal。不要让两个任务同时编辑 `NBShaderInput.hlsl` 或 `NBShaderForwardPass.hlsl`。

| 所有者 | 独占写入候选 | 只读依赖 / 限制 |
|---|---|---|
| 主 Agent / T01 接口管理员 | `NBShaders2/Shader/HLSL/NBShaderSharedContractV1.hlsl`（拟新增：上述公共结构/签名声明）；`Documentation~/contracts.html`、G1 报告 | `NBShaders2/Shader/HLSL/NBShaderFlags.hlsl` 协议锁定；是否迁移 `BaseUVs`/`ParticleUVs` 定义由主 Agent 单独切片处理。 |
| T02 | `NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl`、`NBShaders2/Shader/HLSL/NBShaderSamplingV1.hlsl`（拟新增） | 读 Input `:367-1129`、Flags、`XuanXuan_Utility.hlsl`；不写 Input、Flags、Shader；资源只由 host 绑定。 |
| T03 | `NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl`、`NBShaders2/Shader/HLSL/NBShaderDistortionV1.hlsl`（拟新增） | 读 Forward fragment 和 T02 API；不写原 Forward、Input、GF HLSL、NBPostprocess。 |
| T04 | `NBShaders2/Shader/HLSL/NBShaderGeometryV1.hlsl`、`NBShaders2/Shader/HLSL/NBShaderURPLightingV1.hlsl`（拟新增） | 读 Forward vertex、Input transform、`XuanXuanRenderUtility/Shader/HLSL/{VAT,HoudiniVAT,TyflowVAT,SixWaySmokeLit}.hlsl`；这些原有 utility 文件暂锁，若必须改须主 Agent 逐文件指定所有权。 |
| T05 / 主 Agent 集成 lane | `NBShaders2/Shader/NBShader.shader`、`NBShaders2/Shader/HLSL/NBShaderInput.hlsl`、`NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl`（现存中央入口，唯一写者） | 必须保留名称/GUID/Properties/CBUFFER/Pass/Keywords/RenderState/旧材质兼容；按批准模块逐批接入并串行 Unity A/B。 |

`NBShaders2/Tests/PassFeasibility/SubTargetProbe/Editor/NBGFUnlitSubTarget.cs`、GF HLSL/Graph/VFX 属 T00A 证据，不派发给 T02–T05；T06 产品 wrapper 应有自己的独占文件，先读 GF、不直接把 GF 名称/固定噪声改成产品。`NBPostProcessing/**`、官方 URP/SG/VFX、`Assets/**`、`ProjectSettings/**` 均非本建议授权修改范围。

## 6. 主 Agent 必须先裁定的歧义 / 前置试验

1. **Struct 迁移与 include 顺序**：`BaseUVs` 在 Flags、`ParticleUVs` 在 Input；若新 module 要独立编译，需一个单写者把定义迁到共同 header，并验证原 URP Core/XuanXuan/Flags include 顺序、七 Pass 和 SRP Batcher。不能让 T02 复制结构产生两份 ABI。
2. **CBUFFER 写入等价**：已发现 16 个字段的直接写入/26 个写点（T01 `uniform-writes.json`）；局部有效值必须证明后续同次消费者拿到相同值。特别 UV 时间/CustomData、VertexOffset、Noise、背面颜色、ColorAdjustment 与 Fresnel；不能机械替换为 `const`。先取一个 UV+BaseColor+Distortion 垂直切片 A/B。
3. **T04 VAT 的可移植性**：现有 `ApplyVAT(AttributesParticle, inout float4 positionOS, inout float3 normalOS)`（`XuanXuanRenderUtility/Shader/HLSL/VAT.hlsl:7-21`）和 Houdini/Tyflow 子函数直接依赖旧结构；本轮若要抽成共享 Geometry，需先列明所有 TEXCOORD3–7 / Flipbook 互斥输入、CustomData 帧、Buffer/纹理与 Bounds。主 Agent 应决定是 T04 独占改旧 utility 文件，还是先用旧 ShaderLab adapter 保留 VAT，不能假装上述 v1 最小字段已覆盖 VAT。
4. **URP Lighting / Graph stage**：现有 `InitializeInputData`（Input `:1495-1563`）和 Forward `UniversalFragment*`、Six-Way (`:735-790`) 都依赖 URP `InputData`/`BSDFData`。T04 的 `NBShaderURPLightingV1.hlsl` 可在**当前 URP**复用，但能否被 Graph File Custom Function 接收生成的法线/阴影/SH 仍需 T06 查实际代码；不在 G1 假定标准 Lit Target 可替换。
5. **Distortion Alpha 与 RenderState**：原 `alphaStrength` 在 premultiply 前固定，Deferred 输出 RG+Alpha、CameraOpaque 采样屏幕色后 Alpha=1（Forward `:1306-1337`）。T03 负责数学，T05/T06 Pass 负责写 RT/Blend/Queue；三者接口需用 Mesh、VFX GF 与产品用例确认。
6. **数值协议 / Graph**：`_W9ParticleShaderFlags1` bit31 等值当前 Material/MPB 通道不等价；Graph 的完整 `uint` 输送、16-bit 分片、VFX 逐粒子频率未实测。主 Agent 不应在接口 v1 里把 packed 字定义成 `float`；先冻结 `uint` 内部语义及高位测试需求，T06 wrapper 另行验证传输。

**建议审核口径：**此案可让 T02/T03/T04 在独占文件里先写纯/窄函数，T05 作为唯一绑定与调度入口；但正式 G1 应由主 Agent把每个 `NBFX*ParamsV1` 具体字段、读写方向、Default/Keyword/Pass/case 填满并审签。未达到时，本文件只是可开发的提案，不是“共享 HLSL v1 已编译通过”。
