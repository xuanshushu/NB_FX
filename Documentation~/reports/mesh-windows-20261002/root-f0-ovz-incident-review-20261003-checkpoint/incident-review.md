# RootF0 OVZ batch3 incident review

只读检查指定 `unity.log`、CLI invocation/stdout、captures 目录和当前安装源码。未运行 Unity、GPU、诊断脚本或 dump 分析，未改产品/测试/断言。

- Unity 6000.3.25f1，正确隔离工程，D3D11/Intel UHD770，driver32.0.101.7040（log:35、107、112–116）。编译成功（277–281）；不能把许可警告当 GPU 根因。
- 首个可观察 GPU 故障是 log:591 的 `887a0005`，2×2 RGBAHalf 白纹理创建；托管来源 `G4GraphOverrideDepthTests.cs:50`，调用者 KeywordAuthorityABC。此前 OneTimeSetUp 已调用真实 Graph warm（log:513–517；warm fixture:50、97 的同步 import/4次 Camera.Render）。缺少 warm 前后 GPU 健康遥测，不能证明故障最早由白纹理或 OVZ shader 触发。
- 唯一 captures 子目录 `keyword-off-toggle-on-d3-ortho` 对应首个进入的身份；目录空，0 raw、0 metrics，XML 不存在。没有任何 OVZ 身份可确认通过。
- 后续 SRV80070057、RT/buffer887a0005、resource assertion 是失效设备后的连续错误。最终 native crash 栈（2213–2228）为 D3DKMTOpenResource → DynamicConstantBuffer Update → SRPBatcher → Submit；managed栈为 Camera.Render/KeywordAuthorityABC（2009–2021）。这定位最终崩溃路径，不能直接证明批处理合同或某个 shader 是设备移除的根因。
- CLI Editor exit为0xC0000005；原24 incomplete/invalid，后续source/native2未运行；GUI110原 XML 保留。MCP断连在crash handler之后（2322–2324）。崩溃与微差分别记录，原54/UVP376/downstream通过不能替代本24。

`incident-audit.json` 锁定 log SHA、10项已安装源码 SHA和上述行号。`ovz24-health-guarded-plan.json` 保留原24身份、原严格断言，拆6×4；另有6份单批 plan，先跑 keyword-off/toggle-on 的4身份。仅准备未执行。

现有 runner 的 `stopOnReadbackHealthFailure` 只检查 health JSON，而 OVZ输出裸 rgba32f/metrics，单设该flag不会形成健康守卫。需主Agent按单批plan串行，在下一批前检查 log/crash/XML、每身份9张262144字节float32帧的finite/CDCD、原零误差/repeat/visible/response。任GPU/infra/缺数据异常停止后批并保留证据；off keyword的零响应是原合同，不判强响应缺失。原事故不与重新运行的24相加。下次入口先完成主Agent的crash诊断与健康重启；本文未提取dump的线程或设备移除原因。
