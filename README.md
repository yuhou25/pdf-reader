# PDF Reader

一个 28MB 的轻量 PDF 阅读器，基于 PDFium 引擎，.NET Framework 4.8。

## 功能

- 打开 PDF（文件对话框 / 命令行参数 / 拖拽 exe）
- 左栏目录大纲（可显示/隐藏，点击跳转）
- 页码输入跳转
- 鼠标滚轮连续滚动
- 左键拖拽平移
- 缩放（工具栏 +/- / Ctrl+滚轮）
- 打印（系统打印对话框）
- 自定义渲染控件，只画可见页，大文件不卡

## 快捷键

| 按键 | 功能 |
|------|------|
| PageDown / ↓ | 下一页 |
| PageUp / ↑ | 上一页 |
| Home | 首页 |
| End | 末页 |
| Ctrl + +/- | 放大/缩小 |
| Ctrl + 滚轮 | 放大/缩小 |

## 编译

需要 Visual Studio Build Tools 2022 + NuGet。

```cmd
nuget restore -PackagesDirectory packages
msbuild /p:Configuration=Release
```

输出 `bin\Release\PdfReader.exe`，同目录需要：
- `PdfiumViewer.dll`
- `x64\pdfium.dll`
- `x86\pdfium.dll`

## 技术栈

- C# / .NET Framework 4.8 (WinForms)
- [PdfiumViewer](https://github.com/pvginkel/PdfiumViewer) — PDFium .NET 封装
- PDFium — Google Chrome 的 PDF 渲染引擎
- 自定义 `PdfScrollPanel` 继承 Panel，OnPaint 按需渲染可见页

## License

MIT
