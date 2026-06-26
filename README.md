# 通用生图

> 本插件功能由 **初心、爱奈丽** 实现，本人仅略加优化。

通用生图插件，支持最多 4 组 API 配置，兼容 OpenAI / Agnes / Wan 三种格式，支持文生图和图生图。

AI 可直接传入 QQ 图片链接作为参考图，插件自动下载缓存后进行图生图。

## 功能

| 功能 | 说明 |
|------|------|
| **文生图** | 根据描述生成图片 |
| **图生图** | 传入参考图（本地路径或QQ链接）进行编辑/改图 |
| **QQ链接图生图** | AI 在 QQ 聊天中收到图片后，自动下载缓存并作为参考图传入 |
| **3种API格式** | OpenAI 兼容 / Agnes / 通义万相 |
| **4组API配置** | 1组主 API + 3组备用，通过函数序号切换 |
| **QQ发送** | 生图完成后自动用 `<qimage>` 标签发送到 QQ 聊天 |
| **智能重试** | 服务端错误自动重试（3次），配置错误不重试 |

## 快速上手

1. 在插件 UI 中填写 API 配置（类型、Endpoint、Key、模型）
2. 也可用预设按钮一键填充常见服务商
3. 对 AI 说「生成一张xxx的图片」

### 图生图

AI 在 QQ 中收到图片后，可以直接传入链接进行改图：
> **「把这张图里的裙子改成蓝色」**

AI 会调用 `GenerateImage` 并传入图片链接，插件自动下载缓存后传给 API。

## 配置项

| 配置 | 默认 | 说明 |
|------|------|------|
| API 组 1~4 | — | 最多 4 组 API，每组含名称/类型/Endpoint/Key/模型/尺寸 |
| 接口类型 | agnes | openai（通用格式）/ agnes / wan（通义万相） |
| 默认尺寸 | 1024x1024 | 格式：宽x高 |
| 图片保存目录 | `Storage/Images/Generated` | 留空使用默认，可自定义 |

### 预设服务商

UI 提供一键预设按钮：

| 预设 | 类型 | Endpoint |
|------|------|----------|
| SiliconFlow | openai | `api.siliconflow.cn/v1/images/generations` |
| OpenAI | openai | `api.openai.com/v1/images/generations` |
| 通义万相 | wan | `dashscope.aliyuncs.com/services/aigc/multimodal-generation/generation` |
| Agnes | agnes | `apihub.agnes-ai.com/v1/images/generations` |

## AI 可用函数

```
<GenerateImage_1 prompt="描述" img="参考图路径/链接" width="宽" height="高" />   使用第1组API
<GenerateImage_2 ... />                                                         使用第2组API
<GenerateImage_3 ... />                                                         使用第3组API
<GenerateImage_4 ... />                                                         使用第4组API
```

### 发图格式（QQ）

生图完成后自动发送：
```
<qimage image="保存路径/文件名.后缀" />
```

## 日志

控制台输出格式：
```
[通用生图] 下载参考图: https://...
[通用生图] 参考图已缓存 (xxxKB) -> ref_xxx.png
[通用生图] 生图请求 [1] agnes agnes-image-2.1-flash
[通用生图] 保存完成 (xxxKB) -> universal_xxx.png
[通用生图] 连接失败: xxx
[通用生图] 重试 (2/3)...
```

## 安装

将 `Alife.Plugin.UniversalImageGen` 文件夹放入 Alife 的 `Storage/Plugins` 目录，在客户端重载模块即可。

## 致谢

本插件功能实现由 **初心、爱奈丽** 完成，本人仅做集成优化和插件化封装。
