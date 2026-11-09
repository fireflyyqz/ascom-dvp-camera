# ASCOM DVP Camera Driver

ASCOM 相机驱动，适配 DVP USB3 工业相机（12MP），支持天文拍摄软件通过 ASCOM 协议控制相机。

## 硬件参数

- 分辨率：4088 × 3072
- 像元尺寸：3.1 μm
- 传感器类型：GRBG
- 接口：USB3

## 依赖

- .NET Framework 4.7.2
- [ASCOM Platform 6](https://ascom-standards.org/)
- `DLL/DVPCameraCS64.dll`（DVP 相机原生 SDK）

## 构建

用 Visual Studio 打开 `ASCOM Device Driver CS.sln`，编译即可。

输出路径：`bin/Debug/` 或 `bin/Release/`

## 安装使用

1. 安装 ASCOM Platform
2. 在天文软件（如 N.I.N.A.、SharpCap、MaxIm DL 等）中选择 `ASCOM.DVPCamera.Camera` 即可

## 更新日志

### 2026-11-09

* 更新了新的ASCOM驱动程序，引入了exe服务程序用以修复在PHD2中的若干bug，如内存溢出、轮询异常、多线程导致无法找到相机等问题
* 由于引入了新的exe服务程序，总共需要两个项目单独构建，一个用于生成exe另一个用于生成ASCOM驱动，本开源代码将不再更新，取而代之的是当前项目中的AscomDriverExeVersion.zip压缩包，相关介绍说明请移步[EXE版本](READEME_ExeVersion.md)

### 2026-09-01

- 重构异步曝光逻辑，修复 PHD2 对相机轮询时的异常行为
- 优化变量生命周期与状态复用，缓解 x86 进程的内存溢出问题
