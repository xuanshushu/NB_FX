# CA 普通 Mesh 当前功能证据

原58输入配方已各有Current/Graph实际捕获；116健康身份为114P/2原强响应F，1196raw/PNG。严格BC按配方计：48完全一致、10差异待人工。Frozen ABC、首帧、最终Player/性能仍未闭合。

| 功能组 | 原输入配方 | 双宿主健康结论 | 核心消费者 |
|---|---:|---|---|
| Forward11类×两投影 | 22 | 44P | 径向/纹理Noise、PNoise、Refraction、POM、Custom1X/2W、Wrap/LOD、背面/负Scale |
| 两NB Pass×五类×两投影 | 20 | 38P/2F | 编码payload/真实SceneColor采样、POM惰性、无NoiseOpaque独立active输入控制 |
| Opaque四Wrap×两LOD×两投影 | 16 | 32P | 四真实packed模式/LOD位、静态Clamp0、UV+1强响应、精确恢复 |

强响应失败仅CameraOpaque/refraction/ortho：两宿主WithNoise=.002197265625，原>.003失败；可见/有限/重复/恢复/cleanup有效。它与该配方的微差是独立账。

| 严格BC差异配方（每配方仅一行） | 最大差 | 单帧最多不同分量 |
|---|---:|---:|
| Forward-pnoise-ortho | 0.00048828125 | 3 |
| Forward-pnoise-perspective | 0.00048828125 | 5 |
| Forward-pom-perspective | 0.00341796875 | 10 |
| Forward-refraction-ortho | 0.000244140625 | 5 |
| Forward-refraction-perspective | 0.00048828125 | 7 |
| Forward-with-noise-ortho | 0.00048828125 | 7 |
| Forward-with-noise-perspective | 6.103515625e-05 | 1 |
| NBCameraOpaqueDistortPass-refraction-ortho | 0.000244140625 | 1 |
| NBDeferredDistortPass-refraction-ortho | 3.051757812e-05 | 4 |
| NBDeferredDistortPass-refraction-perspective | 3.051757812e-05 | 12 |

原始完成回执、raw配对与各recipe源入口见同目录JSON；本表不重算或改写旧XML/summary，不将旧58无XML或Frozen事故改成通过。

## Frozen 最少3输入增量

A1 Forward/with-noise/ortho健康P且A/B九帧严格0。A2 Forward/POM/perspective健康P，A/B最多.000244140625/6分量；A/C最大.00341796875/7分量，B/C同最大/10分量：A更接近B，仍不能称A/B严格一致，保Native/Frozen微差及Graph较大POM差异。A2 Opaque/refraction/ortho原强阈值F，三宿主WithNoise均.002197265625，确认共同强响应基线边界；颜色A/B最多.000244140625/1分量但A/C九帧全0，颜色与强度两账独立。只3/58 Frozen输入受控捕获，剩55及旧事故/首帧不追认。
