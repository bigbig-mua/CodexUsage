# 测试 / Testing

## 开发分支：任务栏与全屏 / Development: taskbar and fullscreen

2026-09-30：修复版通过 C# 5、x64、警告视为错误的构建；Domain 34 项、Bridge 8 项、Taskbar 12 项检查通过，0 失败。Taskbar 包括全屏保留最大化标记、普通最大化、自动隐藏任务栏的标题栏与边缘间隙、不同显示器及负坐标、跨屏、退出全屏和隐藏窗口。4 项原生窗口检查在虚拟桌面区域之外创建不抢焦点的测试窗口，验证真实 Win32 客户区与最大化标记。

On 2026-09-30, the fix built for x64 with C# 5 and warnings treated as errors. Domain passed 34 checks, Bridge 8, and Taskbar 12, with no failures. Taskbar covers retained maximized flags, ordinary maximization, auto-hide title bars and edge gaps, other monitors, negative coordinates, spanning windows, fullscreen exit, and hidden windows. Four native checks use windows outside the virtual desktop without activation to verify actual Win32 client rectangles and maximized flags.

本地修复版还通过 TaskbarLive 的 5 项运行检查：启动显示、带最大化标记的全屏隐藏、退出全屏恢复、无最大化标记的全屏隐藏及关闭全屏窗口后恢复。测试使用完全透明、不响应鼠标且不抢焦点的原生窗口。通过 Windows 桌面启动修复版时，前台窗口句柄在启动前后保持一致；核验工具退出后程序仍在运行。

The local build also passed five TaskbarLive checks: visible startup, hiding for fullscreen with a maximized flag, restoration after fullscreen exit, hiding for fullscreen without that flag, and restoration after the fullscreen window closes. The native test window is fully transparent, passes mouse input through, and never activates. Launching the final build through the Windows desktop launcher preserved the foreground window handle, and the widget remained running after the verification tool exited.

尚未用用户实际的视频、图像应用独立验证全屏切换和窗口事件时序；几何与原生窗口检查不等于这些应用的端到端验收。

Fullscreen transitions and event timing in the user's actual video and image applications have not been independently verified. Geometry and native-window checks do not constitute end-to-end testing of those applications.

## 1.0.3

新增检查覆盖 Spark 过滤、公共重置记录与预告区分、未知类型、安全来源链接、中英文缓存文案、损坏缓存及取消。用户已认可本地公告布局；GUI、真实Plus、其他DPI和新增HTTP异常分支尚未全面独立验证。

New checks cover Spark filtering, executed versus scheduled reset records, unknown types, safe source links, bilingual cache labels, corrupt caches and cancellation. The user accepted the local announcement layout. GUI, live Plus accounts, other DPI settings and new HTTP failure paths have not been comprehensively verified independently.

测试使用合成数据和模拟子进程，无需登录 Codex。

Tests use synthetic data and a fake server; no Codex sign-in is required.

可选 TaskbarLive 需要已经运行且当前可见的组件；它不包含在 `All` 中。

Optional TaskbarLive checks require a running, currently visible widget and are excluded from `All`.

## 命令 / Commands

从仓库根目录运行 / Run from the repository root:

```powershell
# 默认 Domain / Default: Domain
./scripts/test.ps1

./scripts/test.ps1 -Suite Domain
./scripts/test.ps1 -Suite Bridge
./scripts/test.ps1 -Suite Taskbar
./scripts/test.ps1 -Suite All

# 可选：核验正在运行的组件 / Optional: verify the running widget
./scripts/test.ps1 -Suite TaskbarLive

# 只编译 / Compile only
./scripts/test.ps1 -Suite All -BuildOnly
```

`All` 包含 Domain、Bridge 和 Taskbar。`-BuildOnly` 编译所选套件，不执行测试。

`All` runs Domain, Bridge, and Taskbar. `-BuildOnly` compiles the selected suites without running them.

## 覆盖范围 / Coverage

| 套件 / Suite | 覆盖 / Coverage |
| --- | --- |
| Domain | 额度与套餐解析、百分比与重置计算、异常值、安全错误、设置保存与恢复、固定尺寸、语言及运行数据路径 |
| Bridge | 模拟服务握手、通知与响应、安全错误、超时、取消、异常退出及子进程清理 |
| Taskbar | 全屏与普通最大化区分、任务栏所在显示器、自动隐藏边缘、退出全屏及离屏原生窗口检查 |
| TaskbarLive | 可选：正在运行的组件在透明原生全屏窗口出现、缩小和关闭时隐藏与恢复 |

Domain covers parsing, calculations, settings, and runtime data paths. Bridge launches a fake server to exercise protocol and process handling. Taskbar checks fullscreen geometry and offscreen native windows without activating them.

## 结果 / Results

1.0.2：标题栏链接、版本文本及对齐修订已编译通过；用户授权发布。未独立执行浏览器点击或新版 GUI 测试，下面的历史结果不作为本次完整 GUI 验证。

1.0.2 compiles with the header link, version text and alignment changes, and the user approved release. Browser-launch and GUI interactions were not independently tested; earlier results below are not full GUI verification of this update.

2026-09-09：1.0.1 编译通过，用户确认新版提示框交互正常并同意发布。这是用户辅助实测，不是自动化 GUI 验证；不能据此认定所有屏幕、套餐或任务栏场景均已覆盖。

On 2026-09-09, version 1.0.1 compiled successfully and the user confirmed the updated tooltip interaction before approving release. This is user-assisted testing, not automated GUI verification, and does not cover every display, plan or taskbar scenario.

开发阶段的 Domain 32 项与 Bridge 8 项检查通过，0 失败，覆盖设置迁移、数据目录、鼠标移出状态与通信处理。后续原生窗口事件和提示框修改仅做编译检查，没有将旧测试结果当作新 GUI 验证。

Development checks passed: 32 Domain and 8 Bridge checks, with no failures. They cover settings migration, data paths, pointer-leave state and protocol handling. Later native-window and tooltip changes were compiled; those earlier results are not presented as GUI verification.

以下为 1.0.0 的历史实测 / Earlier user testing on 1.0.0:
用户实测补充：当前发布的 1.0.0.0 已在一台 Windows 电脑上启动，运行文件 SHA256 与发行附件一致。截图确认 Pro 额度读取、每周额度与 Codex 显示一致、中英文详情布局以及通知区左侧的额度条位置。截图仅用于本地验收，未上传账号信息。

User testing confirmed that the published 1.0.0.0 executable starts on one Windows machine; its SHA256 matches the release asset. Screenshots confirm Pro usage retrieval, the weekly value matching Codex, Chinese and English detail layouts, and widget placement beside the notification area. Account screenshots were not uploaded.

用户按手动刷新及等待自动刷新的步骤提供了连续截图，成功更新时间的变化符合手动刷新及五分钟定时刷新；界面持续显示正常。本项为用户辅助实测，不是自动化点击验证。

Sequential screenshots supplied during the manual/automatic refresh check show successful update timestamps consistent with a manual refresh and the five-minute schedule, with the UI still displaying normally. This is user-assisted testing, not automated interaction testing.

用户另确认：通过托盘 Exit 退出后额度条和托盘图标消失，重新打开同一 EXE 后仍为英文，且能够重新读取套餐和额度。本项为用户操作反馈，不代表对所有退出后的子进程做过独立检查。

The user also confirmed that Exit removes the widget and tray icon, and that reopening the same EXE preserves English and retrieves the plan and usage again. This is user-reported interaction testing, not an independent inspection of every child process after exit.

其他 DPI/屏幕环境及真实 Plus 账号仍待验证。EXE 仍未签名；历史构建和部分 UI 检查曾被 Windows 应用控制阻止，本次成功启动不代表这些检查已恢复或所有电脑均能运行。

Other DPI/display configurations and a real Plus account remain unverified. The EXE is still unsigned. Earlier builds and some UI checks were blocked by Windows application control; this successful launch does not validate those checks or guarantee compatibility on every machine.
