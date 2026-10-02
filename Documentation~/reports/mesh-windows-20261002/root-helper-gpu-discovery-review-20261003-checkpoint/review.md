# Helper GPU discovery repair

根因是我新增测试meta中的GUID为33位hex。Unity log:238–239明确YAML/GUID无效，并忽略对应cs Asset。XML total0、asserts0，log:558为No tests executed；虽然XML顶层标Passed，实际0项通过。未出现GPU设备失败/crash标记。原空XML保留，后3批未启动。

源442e0792与原预览相同；Cases/SetName（32–37）静态生成的28身份和原4×7计划完全相同。OneTimeSetUp（27–30）未运行；元数据修复后它的实际warm/type查找仍需要真实验证，本文不把静态审核当运行成功。

唯一修复文件是新测试的`.cs.meta`：33位无效GUID→32位有效GUID，附沿用现有MonoImporter布局。无当前Root/隔离已有同GUID；没有合法旧GUID序列化合同需要迁移。源、断言、SetName、原filter均不修改。`manifest.json`记录before/afterSHA，`original-invalid-meta.txt`保留原错误字节。

主Agent仅安装meta，然后在安全独占隔离窗口重新import/发现，沿原plan串行执行到全新证据目录。每批实际XML必须严格匹配7个身份；不能使用空XML顶层Passed或CLI0证明验证。此预览未安装、未运行Unity。
