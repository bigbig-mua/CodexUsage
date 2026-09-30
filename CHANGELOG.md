# Changelog

## Unreleased

- 修复视频、图像等应用保留最大化标记时漏判全屏的问题；同一窗口切换全屏时立即检查，并按组件所在显示器判断遮挡。
  Detect fullscreen media windows that retain the maximized flag, react to fullscreen size changes, and check coverage on the widget's monitor.
- 启动、托盘显示和详情操作统一遵守任务栏与全屏可见性，任务栏不可用时隐藏组件。
  Apply taskbar and fullscreen visibility rules at startup and during tray and detail actions; hide the widget when the taskbar is unavailable.
- 额度条窗口显式禁止激活，避免系统启动参数导致它抢走全屏应用的焦点。
  Mark the widget as non-activating so startup window flags cannot steal focus from fullscreen applications.

## 1.0.3 — 2026-09-15

- 移除全部 Spark 额度，旧选择自动回到可用额度。
  Remove Spark quotas and fall back to an available quota window.
- 增加公共重置公告的时间、类型和独立来源链接，支持中英文及本地缓存。
  Add public reset announcements with time, type, a separate source link, bilingual text and caching.
- 移除详情页白色悬浮提示，放大公告文字。
  Remove detail tooltips and enlarge the announcement text.

## 1.0.2 — 2026-09-09

- 详情标题右侧新增版本号和 GitHub 主页链接。
  Add the version and a GitHub project link beside the detail title.
- 调整标题行对齐与链接宽度，完整显示“GITHUB主页”。
  Align the header text and give the project link enough room.

## 1.0.1 — 2026-09-09

- 额度条说明支持未激活窗口悬停显示；移开或按下鼠标立即关闭，点击后需要移出再悬停才重新显示。
  Show the widget tooltip on hover without activation; dismiss it on pointer leave or mouse press, and suppress it until the pointer leaves after a click.

- 前台窗口切换、右键菜单关闭和详情失去焦点后立即检查额度条层级，减少点击任务栏后被短暂遮挡的情况。
  Restore widget ordering after foreground changes, menu dismissal and detail deactivation instead of waiting for the next timer tick.

- 刷新状态使用固定页脚，详情始终贴齐额度条，避免刷新后留下空隙。
  Keep refresh status in a fixed footer and anchor details to the widget.
- 鼠标离开详情和额度条后约半秒自动收起，操作下拉菜单时保持显示。
  Dismiss details after the pointer leaves for half a second, while allowing dropdown interaction.
- 数据改存到用户应用数据目录，首次运行导入旧版设置。
  Store runtime data under Local AppData and import existing settings on first run.
- 刷新时保留已有倒计时，过滤任务栏短暂隐藏信号并减少背景擦除。
  Preserve the countdown during refresh, filter transient taskbar visibility changes, and avoid background erasure.

## 1.0.0 — 2026-09-09

- 显示 Codex 账号套餐及实际返回的额度窗口。
  Display the Codex account plan and available quota windows.
- 任务栏双圆盘展示同一周期的剩余额度和重置倒计时，点击查看详情。
  Show remaining quota and reset time for the same window in a two-disk taskbar widget, with click-to-open details.
- 支持中文与 English 切换、每 5 分钟自动刷新和手动刷新。
  Support Chinese and English, automatic refresh every five minutes, and manual refresh.
- 提供托盘显示与隐藏，设置保存在 EXE 相邻的 `env/`。
  Show or hide the widget from the tray and save settings in `env/` beside the EXE.
