# T01/G1：共享 HLSL v1 可冻结接口提案（2026-09-27）

**主 Agent 审核决定：下述四个跨模块入口及类型作为 G1 v1 设计契约冻结；尚未写入 Package、编译或通过 Unity。**这里的 `NBFX_*V1` 全是拟新增项目函数，不是 Unity/URP/Shader Graph API。仅冻结以下四个跨模块入口及其完整输入/输出类型；其余 Feature UV、纹理采样、Dissolve、VAT、URP Lighting 等在模块内部继续设计，**不宣称已纳入 v1 契约**。依据 `NBShaderInput.hlsl:871-977,1142-1197`、`NBShaderForwardPass.hlsl:660-725,1278-1337`、`NBShaderFlags.hlsl:178-263`。

## 1. 拟冻结的全部类型与签名（没有未定义参数类型）

```hlsl
// NBShaderSharedContractV1.hlsl：由主 Agent 单独创建/持有。
// include 顺序须使 NBShaderFlags.hlsl 的 BaseUVs 和常量先可见。

struct NBFX_BaseUVInputV1 {
    float4 meshTexcoord0;          // xy 默认 UV，zw 特殊 UV
    float2 specialUVInTexcoord3;   // Flipbook 粒子特殊 UV；非该路径传 0
    float4 custom1, custom2;      // 原 Varyings 为 float4，SpecialUV 保持 float；传入 GetCustomData 时按原函数 half4 参数转换
    float3 positionOS, positionWS; // 调用点当前有效空间；vertex 为位移前，fragment 为位移后
    float2 screenUV;              // 顶点 clip 投影或片元 raster，由 T05 按原路径选择
};

struct NBFX_BaseUVParamsV1 {
    uint flags0, flags1;
    uint customDataFlag0, customDataFlag3;
    uint uvModeFlag0, uvModeFlagType0;
    float4 mainTexReverseST, uiMainTexST, baseMapST;
    half4 sharedUVST, sharedUVVec, baseMapMaskMapOffset;
    float4 twirlParameter, polarCenter;
    float4x4 cylinderUVMatrix;    // T05 以原 _CylinderMatrix0..3 原顺序构造
    float twirlStrength;
    half baseMapUVRotation, baseMapUVRotationSpeed;
    half worldSpaceUVModeSelector, objectSpaceUVModeSelector;
    float timeY;                   // 当前原链路为 _Time.y，不改时间来源
};

struct NBFX_BaseColorInputV1 {
    half4 sampledAlbedo;          // T05 原 BlendTexture/色散/UI 选择之后的样本
    half selectedAlpha;           // T05 原 GetColorChannel(...MAINTEX_ALPHA) 结果
    half4 effectiveBaseColor;     // T05 按 facing/BackColor 得出的局部值
    half timelineIntensity;       // 原 _BaseColorIntensityForTimeline
    bool applyTimelineIntensity;  // 原 !NB_DEPTH_SHADOW_PASS，编译期常量传入
};

struct NBFX_DistortionInputV1 {
    half2 signedNoise;            // 原 screenDistort_Noise.xy，不先乘 mask/alpha
    half noiseMask;               // 原 screenDistort_Noise.z
    half alphaBeforePremultiply;  // 原 alphaStrength（总 alpha 饱和后、预乘前）
    bool refineAlpha;             // flags1 SCREEN_DISTORT_ALPHA_REFINE 解码结果
    half alphaPow, alphaMultiplier, alphaAdd;
    half intensity;               // 原 _ScreenDistortIntensity
};

struct NBFX_DistortionPayloadV1 {
    half2 signedRG;
    half coverage;                // refine 后、尚未乘 intensity
    half intensity;
};

struct NBFX_VertexOffsetPreparedV1 {
    half3 normalOS;
    half3 directionOS;            // 原 map.rgb/vertexColor 经中心化、必要时 WS→OS
    half3 customDirectionOS;      // 原 _VertexOffset_CustomDir
    half sampledScalar;           // 原通道采样经 StartFromZero 中心化；mode 2 可填 1
    half maskWeight;              // 无 mask 为 1；有 mask 为 lerp(1,sample,strength)
    half intensity;               // 原 _VertexOffset_Vec.z 经 CustomData 替换后的局部值
    int directionMode;            // (int)round(_VertexOffset_NormalDir_Toggle)
};

// T02，依赖既有唯一 BaseUVs 定义；不新造等价 UV struct。
BaseUVs NBFX_BuildBaseUVsV1(NBFX_BaseUVInputV1 i, NBFX_BaseUVParamsV1 p);

// T03，只有纯数值计算；不声明 Texture/CBUFFER/Pass 输出。
half4 NBFX_ComposeBaseColorV1(NBFX_BaseColorInputV1 i);
NBFX_DistortionPayloadV1 NBFX_BuildDistortionPayloadV1(NBFX_DistortionInputV1 i);

// T04，当前只冻结位移量数学；不把 VAT、transform/UV/采样绑进跨模块签名。
half3 NBFX_ComputeVertexOffsetOSV1(NBFX_VertexOffsetPreparedV1 i);
```

这份字段集合对**所列四个入口**是封闭的，不表示对整个 Shader 450 项 Property、所有纹理、所有 Pass 封闭。`BaseUVs` 是现有 `NBShaderFlags.hlsl:228-239` 的定义，不属于未定义占位。四个函数的输入结构都是普通 HLSL 数值，不依赖 Graph 节点类型；不过 Graph 包装、include 顺序及编译仍未验证。

## 2. 各入口的精确责任和原行为映射

| 入口 / 阶段 | v1 必须实现；原证据 | 明确不归它做 |
|---|---|---|
| T02 `NBFX_BuildBaseUVsV1`，原 stage 由 T05 调度 | 等价 `ProcessBaseUVs`：Mesh/Flipbook special UV、UI reverse、cylinder、Twirl/Polar、世界/物体投影、共享 UV、BaseMap UV 的旋转/ST/CustomData/时间动画。原 Input `:871-977`。`flags0/1` 用原 Flags 常量做位测；`GetCustomData` 和 `GetUVByUVMode` 继续复用唯一实现（Flags `:178-263`）。`UVOffsetAnimaiton` 使用现有三参 `(uv,speed,time)` 重载（`XuanXuan_Utility.hlsl:350-354`），不能读取原可写 `time` 全局。 | `ParticleProcessUV` 的其余 17 路 feature UV、POM、Noise 采样、Texture/ST 的 Graph 属性声明均未冻结。`_FLIPBOOKBLENDING_ON` 继续是宿主编译期条件，不增 runtime bool。 |
| T03 `NBFX_ComposeBaseColorV1`，fragment | `sampledAlbedo.a = selectedAlpha; sampledAlbedo *= effectiveBaseColor;` 非 Depth/Shadow 时 `rgb *= timelineIntensity`，返回调整后的 `half4`。对应 Forward `:663-718` 的**后半段**；T05 从返回值取 `result.rgb` 与 `alpha`。`effectiveBaseColor` 是每次调用局部值，绝不写原 `_BaseColor` uniform。 | 不选择 `_BaseMap`/`_MainTex`、不采样/Blend/色散、不做主贴图专用 ColorAdjustment（Forward `:719-725`）、不做 URP 光照和后续 Alpha。 |
| T03 `NBFX_BuildDistortionPayloadV1`，fragment 完整 Alpha 之后 | `coverage = alphaBeforePremultiply * noiseMask; if(refineAlpha){coverage=pow(coverage,alphaPow);coverage*=alphaMultiplier;coverage+=alphaAdd;}`，保留 `signedNoise` 和 `intensity`。对应 Forward `:1306-1324`。**不擅自 saturate** refine 结果。 | 不写 `SV_Target`、不采 `_CameraOpaqueTexture`、不选择 Deferred/Camera Pass、不做 RT 合成。T05 输出 Deferred `(signedRG.x,signedRG.y,1,coverage*intensity)`；Camera 用片元 raster screenUV `+signedRG*coverage*intensity` 采样并使 alpha=1（`:1325-1337`）。 |
| T04 `NBFX_ComputeVertexOffsetOSV1`，vertex | switch：mode 1=`normalOS*intensity*sampledScalar*maskWeight`；mode 2/3=`directionOS*intensity*maskWeight`（**不 normalize**）；mode 0/其他=`customDirectionOS*intensity*sampledScalar*maskWeight`。对应 Input `:1176-1195`。返回 offset，T05 执行原 `positionOS+offset` 与 WS/clip 重算。 | 纹理与 UV 采样、map/mask 通道选择、StartFromZero 居中、WS→OS 非归一化变换、VAT、Shadow bias 不在此数值函数中，仍由 T05 的现存绑定/调度准备（Input `:1142-1175`，Forward `:275-325`）。 |

**T04 数据准备不能颠倒次序：**原 mode 2 从 vertexColor 取方向，其余 mode 先取 map.rgb；若未 StartFromZero 则都执行 `*2-1`；只有 mode 2/3 在 `_VertexOffset_DirectionSpace > 0.5` 时经 `TransformWorldToObjectDir_NB(...,false)`（Input `:1146-1187`）；CustomLocal 的矩阵分支在 `:301-309`。这由 T05 旧 ShaderLab wrapper 保持，未来 Graph host 另证。`maskWeight` 在未启用 mask keyword 时填 1。

## 3. T05 的最小接入顺序和文件所有权

1. **主 Agent**唯一写拟新建 `NBShaders2/Shader/HLSL/NBShaderSharedContractV1.hlsl`，集中上述结构；保证先有 `BaseUVs`，避免 T02/T03/T04 私自复制。`NBShaderFlags.hlsl` 的协议/结构暂锁；若需搬迁定义，单独审查切片。
2. **T02**只写拟新建 `NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl`。`NBShaderSamplingV1.hlsl` 可做其模块内部后续工作，**不属于本次冻结的跨模块 API**。T02 不写 Input、Forward、Shader、Flags。
3. **T03**只写拟新建 `NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl` 和 `NBShaderDistortionV1.hlsl`。两个签名的字段足以离线写纯数学及对照样例；不写 NBPostProcessing/GF/主 Shader。
4. **T04**只写拟新建 `NBShaders2/Shader/HLSL/NBShaderGeometryV1.hlsl`。URP Lighting/VAT 这轮仍研究，不把 `InputData` 或旧 `AttributesParticle` 冻进共享 v1；不写 `XuanXuanRenderUtility` 下的现有 VAT 文件。
5. **T05 / 主 Agent 集成 lane**唯一写现有 `NBShaders2/Shader/NBShader.shader`、`NBShaders2/Shader/HLSL/NBShaderInput.hlsl`、`NBShaderForwardPass.hlsl`。先让旧 `ProcessBaseUVs` 成为装配 `i,p` 后转调 T02 的 wrapper；其两类调用点保持 `isProcessUVInFrag()` 的 vertex/fragment 选择（Input `:1478-1488`、Forward `:203-286,466-...`）。然后在 Forward `:706-718` 和 `:1315-1337` 接 T03；最后在旧 `VetexOffset` 的原纹理/通道/坐标准备之后接 T04（Input `:1142-1197`）。每一项独立 A/B、逐 Pass/关键字验证，原七 Pass/RenderState/CBUFFER/GUID 不动。

旧 CBUFFER 的 16 个字段/26 个写点另见 T01 `uniform-writes.json`；本接口只排除了所列入口的隐式写，**没有**宣称已排除其余链路。Graph host 需单独按相同输入/输出包装；GF SubTarget 只证明候选 Pass/LightMode，不证明这些 HLSL 签名可直接作为 SG File Custom Function。

## 4. 冻结与非冻结边界 / 放行条件

- **本次冻结：**四个函数名、返回类型、上述六个新结构的完整字段/类型、`BaseUVs` 既有 ABI、数值计算顺序、绑定/Pass 的所有权。若主 Agent不同意名称，可在任何实现前一次性更名，之后按版本控制更改。
- **未冻结：**T02 Feature UV/采样器传输；T03 其余表面链；T04 VAT/URP Lighting；T06 SG/VFX wrapper、Graph 属性序列化、高位 `uint` 输送；HDRP/低版本。不得用未冻结部分来声称“完整 NBShader 已迁移”。
- **待验证：**新 include 的编译顺序与 `half`/`uint`/`bool` 可用性、ShaderLab 七 Pass/所有关键字编译、SRP Batcher、Mesh/VFX 渲染 A/B；Graph 内部的矩阵/CustomData/Pass/性能无法从此只读审计推出。Unity Editor 验证由主 Agent 串行执行。
- **Gate 判词：**G1 仅表示跨模块 v1 **接口设计已审签**，不表示接口已编译或 Graph 可用；T02–T05 实作及验证进入后续 Gate。
