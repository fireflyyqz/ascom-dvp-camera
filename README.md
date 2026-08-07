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
2. 将编译产出的 DLL 注册到 ASCOM（使用 ASCOM Profile Explorer 或注册工具）
3. 在天文软件（如 N.I.N.A.、SharpCap、MaxIm DL 等）中选择 `ASCOM.DVPCamera.Camera` 即可

## 协议

MIT License
