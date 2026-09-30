# CodexUsage

[简体中文](README.md) | [English](README.en.md)

轻量 Windows Codex 额度小窗，在任务栏通知区旁查看剩余额度和重置倒计时，并提供浅色、深色和日出日落自动主题。

**[下载稳定版 v1.0.3](https://github.com/Amygdala42/CodexUsage/releases/download/v1.0.3/CodexUsage.exe)** · [稳定版发行说明](https://github.com/Amygdala42/CodexUsage/releases) · [当前开发分支](https://github.com/bigbig-mua/CodexUsage/tree/feature/light-theme)

适用于 Windows x64。

![CodexUsage 任务栏小条](assets/widget-preview.png)

## 功能

- 透明任务栏组件使用青色圆盘和百分比显示剩余额度，蓝色圆盘和小时、分钟倒计时显示同一周期的剩余时间；两行内容右对齐，并在浅色、深色主题下保持一致的高对比度配色。
- 当前开发分支中，组件仅在任务栏上显示；视频、图像或其他应用全屏覆盖所在任务栏时自动隐藏，退出全屏后恢复。
- 详情页使用半透明玻璃背景，支持浅色、深色和日出日落自动主题。
- 自动主题根据手动填写的经纬度离线计算当地日出、日落时间，无需联网或调用系统定位服务。
- 按实际返回显示额度卡片；Plus、Pro 使用相同规则，已移除 Spark 额度。
- 每 5 分钟自动刷新，也可手动刷新。
- 显示最近公共重置公告的时间和类型，点击来源查看原文；公告不代表个人账号到账。
- 中英文、主题和经纬度设置会自动保存；应用尺寸固定 100%，跟随 Windows 系统 DPI。

## 主题设置

- 选择“浅色”或“深色”可固定使用对应的玻璃主题。
- 选择“日出日落自动”后，点击“位置”填写纬度和经度；纬度以北为正，经度以东为正。
- 日出、日落时间按本机日期和时区计算。程序在接近日出或日落时自动切换主题，并在跨日、修改位置或唤醒后重新计算。

## 界面示例

以下图片是稳定版 v1.0.3 的布局示意，使用示例数据；当前开发分支已经采用玻璃主题，界面会有所不同。额度项目以账号实际返回为准。

### Plus

![Plus 界面示例](assets/plus-preview-zh.png)

### Pro

![Pro 界面示例](assets/pro-preview-zh.png)

## 下载与运行

需要 Windows x64、.NET Framework 4.8 或更新的 4.x 版本，以及已安装并登录订阅账号的 Codex。

1. 在稳定版 Releases 中下载 `CodexUsage.exe`，或下载 `CodexUsage-Windows-x64.zip` 后解压。玻璃主题与自动主题功能目前位于 `feature/light-theme` 开发分支。
2. 运行 `CodexUsage.exe`。点击额度条查看详情，鼠标移开后自动收起。

悬停额度条可查看简要说明，移开或点击即关闭；右键打开菜单。详情顶部可查看版本号，并打开 GitHub 项目主页。

升级时先从托盘退出旧版，再替换 EXE。设置保存在 `%LOCALAPPDATA%\CodexUsage`，首次运行会导入同目录旧版的设置。

验证范围见 [测试说明](docs/TESTING.md)。

## 开发

```powershell
.\scripts\build.ps1
```

如果现有程序正在运行、默认输出文件被占用，可以指定另一个输出文件名：

```powershell
.\scripts\build.ps1 -OutputName CodexUsage-update.exe
```

[构建说明](docs/BUILD.md) · [测试说明](docs/TESTING.md) · [隐私说明](docs/PRIVACY.md)

## 许可

[MIT](LICENSE) · Amygdala42
