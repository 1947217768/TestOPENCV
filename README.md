# C# OpenCV 焊点完整性检测 DEMO

这是一个 **WinForms + OpenCvSharp** 的桌面 DEMO，用于对焊点图像做简单完整性校验。

## 功能

- 加载焊点图片（JPG/PNG/BMP/TIFF）。
- 基于阈值分割 + 轮廓分析检测焊点区域。
- 通过 `面积` 和 `圆度` 对每个焊点做“合格/疑似不完整”判定。
- 在结果图上框选并标注每个焊点统计信息。

> 说明：该 DEMO 侧重流程演示，真实产线请结合工艺样本与标定数据优化阈值、ROI、光照补偿及误检策略。

## 项目结构

- `SolderInspectionDemo.sln`
- `src/SolderInspectionDemo/SolderInspectionDemo.csproj`
- `src/SolderInspectionDemo/MainForm.cs`：界面与交互
- `src/SolderInspectionDemo/SolderJointInspector.cs`：检测逻辑

## 运行方式

1. 安装 .NET 8 SDK（Windows）。
2. 在项目根目录执行：

```bash
dotnet restore
dotnet run --project src/SolderInspectionDemo/SolderInspectionDemo.csproj
```

3. 运行后点击“加载待检图片”，然后“执行检测”。

## 检测算法（Demo版）

1. BGR 转灰度
2. 高斯滤波去噪
3. 二值化（支持反色阈值）
4. 提取外轮廓
5. 计算每个轮廓：
   - 面积：`Cv2.ContourArea`
   - 圆度：`4πA / P²`
6. 规则判定：
   - `面积 >= 最小面积`
   - `圆度 >= 最小圆度`

## 参数建议

- **最小面积**：过滤小噪点，先从 200~500 尝试。
- **最小圆度**：焊点通常较接近圆形，建议先从 0.45~0.70 尝试。
- **阈值**：受光照影响较大，可从 90~160 调整。
- **反色阈值**：如果焊点在图像里比背景更暗，勾选它。

## 后续可扩展

- 增加 ROI 限定与模板定位
- 引入自适应阈值/形态学开闭运算
- 增加统计报表导出（CSV/Excel）
- 结合 ML 模型实现缺陷分类（虚焊、连焊、偏移等）
