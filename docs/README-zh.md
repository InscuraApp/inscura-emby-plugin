# Inscura 的 Emby 元数据插件

语言： [English](../README.md) | **简体中文** | [日本語](README-ja.md) | [한국어](README-ko.md)

Inscura 是一个本地媒体库应用，可以整理影片信息、演员、分类、合集、封面、背景图、相册图片、预告片和媒体流等资料。Emby 负责播放和管理媒体库，但它自身并不知道 Inscura 已经整理好的这些数据。

这个插件用于把 Inscura 的本地接口服务接入 Emby。Emby 扫描或刷新电影时，插件会把标题、简介、发布日期、评分、类型、标签、合集、演员、导演、编剧、制片人、图片、媒体流信息和 YouTube 预告片写入 Emby 元数据。

插件只读取 Inscura 的媒体库数据和库内生成的图片资源，不会下载、移动、重命名、删除或修改原始媒体文件。

## 当前能力

- 按 Emby 传入的真实文件路径和文件名优先匹配，减少因 Emby 标题被手动修改导致的误匹配。
- 找不到路径匹配时，会继续尝试编号、文件名中的识别码和 Emby 标题。
- 支持写入 Emby 可接收的电影元数据字段，包括标题、原始标题、排序标题、简介、标语、发布日期、年份、评分、内容分级、制作方、类型、国家、标签、合集、演员、导演、编剧、制片人和外部 ID。
- 支持导入媒体流信息，包括视频、音频和字幕流的编码、分辨率、帧率、码率、语言、标题、声道和默认/强制字幕标记等信息。
- 支持导入 Emby 的多种电影图片类型，包括海报、背景图、缩略图、横幅图、标徽、艺术图和光盘封面。
- 支持在元数据刷新后自动补齐 Emby 缺失的图片类型。默认只补齐缺失图片，不覆盖 Emby 中已经存在的图片。
- 背景图会优先使用 Inscura 的横版封面/背景图，然后加入预览图、视频截图和相册中的默认类型图片。
- 支持导入演员、导演、编剧、制片人等人物关系，并可为演员导入头像、简介、生日、国家/地区和演员标签。
- 远程预告片当前只导入 YouTube 地址。非 YouTube 本地或在线预告片请通过 Inscura App 的 NFO 功能导出后交给 Emby 扫描。

## 开启 Inscura 本地接口服务

1. 打开 Inscura，并打开要同步到 Emby 的媒体库。
2. 进入设置中的 API 接口服务设置，开启本地接口服务。
3. 鉴权方式建议保持令牌模式，并保存设置页显示的接口令牌。
4. 在 Emby 服务器所在设备上访问健康检查地址，确认服务可达。

示例：

```bash
curl "http://[ip]:28687/api/v1/health"
```

如果 Emby 和 Inscura 不在同一台机器上，插件里的服务地址不能填写 `127.0.0.1`，要填写运行 Inscura 的那台电脑在局域网中的地址，例如：

```text
http://[ip]:28687
```

本地接口服务会跟随当前媒体库生命周期运行：媒体库打开且服务已启用时监听端口，**媒体库锁定、关闭或应用退出时停止服务。**

## 插件下载

从 [GitHub](https://github.com/InscuraApp/inscura-emby-plugin/archive/refs/heads/main.zip) 或 [Releases](https://github.com/InscuraApp/inscura-emby-plugin/releases) 下载最新的插件文件。

推荐优先下载 Releases 中已经构建好的插件包。插件安装文件是：

| 文件 | 用途 |
| --- | --- |
| `Emby.Plugin.Inscura.dll` | Emby 插件主体，负责搜索、匹配、写入元数据、导入图片和预告片 |

如果下载的是源码包，也可以在本地构建：

```bash
cd inscura-emby-plugin
dotnet build -c Release
```

构建产物位于：

```text
bin/Release/net8.0/Emby.Plugin.Inscura.dll
```

## 插件安装

把 `Emby.Plugin.Inscura.dll` 复制到 Emby Server 的插件目录，然后重启 Emby Server。

Emby 插件目录会受系统、套件来源、容器映射和安装方式影响。下面列出的是常见位置；如果你的设备不一致，以 Emby Server 控制台、套件页面、容器映射或系统中的实际数据目录为准。

| 环境 | 常见插件目录 |
| --- | --- |
| Windows | `%APPDATA%\Emby-Server\programdata\plugins` |
| Linux | `/var/lib/emby/plugins` |
| macOS | `~/.config/emby-server/plugins` |
| Docker | 容器内 `/config/plugins`，或宿主机映射的 `config/plugins` |
| 群晖套件 | 以实际套件数据目录为准，例如 `/vol1/@appdata/EmbyServer4-9/plugins` |

安装步骤：

1. 停止或准备重启 Emby Server。
2. 将 `Emby.Plugin.Inscura.dll` 放入 Emby 插件目录。
3. 确认 Emby 运行用户有权限读取该文件。
4. 重启 Emby Server。
5. 打开 Emby 管理后台，在插件列表或插件配置页中确认能看到 `Inscura`。

## 在 Emby 中启用插件

1. 进入 Emby 管理后台。
2. 打开插件设置页，进入 `Inscura`。
3. 填写 Inscura API 地址和 API Token。
4. 根据需要启用电影元数据刮削、图片导入、自动补齐图片类型、演员元数据和头像导入、YouTube 预告片、媒体流信息等选项。
5. 打开电影媒体库设置，在元数据下载器和图片下载器中启用 `Inscura`。
6. 建议把 `Inscura` 排在更靠前的位置，让 Emby 优先使用 Inscura 的数据。
7. 保存设置。
8. 对电影库或单个影片执行刷新元数据。首次验证时建议先刷新少量影片。

## 插件设置说明

| 设置 | 说明 |
| --- | --- |
| 界面语言 | 插件配置页显示语言，支持英文和简体中文 |
| Inscura API 地址 | Inscura 本地接口服务地址。Emby 和 Inscura 不在同一台机器时必须填写局域网地址 |
| API Token | 本地接口服务使用令牌鉴权时填写；如果 Inscura 设置为无鉴权，可以留空 |
| 搜索候选数量 | 每次匹配时从 Inscura 请求的候选数量 |
| 请求超时 | 插件访问 Inscura API 的超时时间，单位为秒 |
| 启用电影元数据刮削 | 开启后，Emby 刷新电影元数据时会从 Inscura 获取影片信息 |
| 启用图片导入 | 开启后，Emby 可以从 Inscura 获取电影图片 |
| 自动补齐 Emby 缺失的图片类型 | 开启后，电影元数据刷新完成时插件会自动保存 Emby 缺失的海报、背景图、缩略图、横幅图、标徽、艺术图和光盘封面 |
| 启用演员元数据和头像导入 | 开启后，Emby 可以从 Inscura 获取人物资料和人物图片 |
| 导入 YouTube 预告片 | 开启后，插件只导入 YouTube 格式的远程预告片 |
| 将 Inscura 预览图作为 Thumb 图片候选 | 开启后，Inscura 预览图会作为 Emby 缩略图候选 |
| 将图库图片和截图作为 Backdrop/Screenshot 候选 | 开启后，Inscura 背景图、预览图、视频截图和图库默认图片会作为背景图或截图候选 |
| 导入 Inscura 媒体流信息 | 开启后，插件会把 Inscura 记录的视频、音频和字幕流信息提供给 Emby |

## 图片类型对应关系

| Inscura 图片类型 | Emby 图片类型 |
| --- | --- |
| 海报 / poster | Primary |
| 横版封面、背景图、图库默认图 / fanart | Backdrop |
| 视频截图 / screenshot | Screenshot |
| 横版图 / landscape | Thumb |
| 预览图 / preview | Thumb，也会参与 Backdrop 候选 |
| 横幅图 / banner | Banner |
| 标徽 / clearlogo | Logo |
| 艺术图 / clearart、keyart | Art |
| 光盘封面 / discart | Disc |

## 使用建议

- 首次使用时，建议先选择少量影片刷新元数据，确认标题、演员、图片和合集符合预期后，再批量刷新资料库。
- 如果 Emby 中影片标题曾被手动改过，插件仍会优先使用真实文件路径和文件名匹配，不依赖标题作为唯一依据。
- 如果 Inscura 服务地址或令牌修改过，需要在 Emby 插件设置中同步更新，然后刷新元数据。
- 如果 Inscura 媒体库被锁定或关闭，Emby 将无法读取元数据。
- 如果升级插件后配置页仍显示旧内容，先强制刷新浏览器页面，再重新打开插件配置页。

## 排查问题

### Emby 看不到 Inscura 插件

1. 确认 `Emby.Plugin.Inscura.dll` 位于 Emby 插件目录。
2. 确认 Emby 进程有读取该文件的权限。
3. 确认当前 Emby Server 版本支持 `.NET 8` 插件。
4. 重启 Emby Server。
5. 重新打开 Emby 网页端，进入插件列表或配置页检查。
6. 如果仍看不到插件，查看 Emby Server 日志中的插件加载错误。

### Emby 能看到插件，但没有元数据

1. 在 Emby 服务器所在设备上访问 Inscura 健康检查地址。
2. 确认 Inscura 媒体库已经打开，且本地接口服务处于开启状态。
3. 确认 Emby 插件里的 API 地址不是错误的 `127.0.0.1`。
4. 如果接口使用令牌鉴权，确认 Emby 插件里填写了正确令牌。
5. 确认电影媒体库的元数据下载器中启用了 `Inscura`。
6. 对影片执行刷新元数据或重新识别。

### 图片或演员头像不显示

1. 确认 Emby 服务器能访问 Inscura API 地址。
2. 如果接口使用令牌鉴权，确认插件令牌正确。
3. 确认插件设置中已启用图片导入、演员元数据和头像导入。
4. 确认 Inscura 中对应媒体或演员确实有可用图片资源。
5. 对该影片或人物执行刷新元数据。

### 横幅图、光盘封面、艺术图或多张背景图没有导入

1. 确认插件设置中已启用图片导入。
2. 确认插件设置中已启用自动补齐 Emby 缺失的图片类型。
3. 确认 Inscura 中对应相册图片已经设置为正确类型。
4. 对影片执行刷新元数据，让插件在刷新完成后保存缺失图片。
5. 如果 Emby 中已经有同类型单图，插件默认不会覆盖已有图片。

### 预告片没有导入

1. 确认插件设置中已启用导入 YouTube 预告片。
2. 确认 Inscura 中的预告片地址是 YouTube URL。
3. 非 YouTube 本地或在线预告片不会由本插件直接导入，请通过 Inscura App 的 NFO 功能导出后交给 Emby 扫描。

## 升级插件

1. 停止或准备重启 Emby Server。
2. 用新的 `Emby.Plugin.Inscura.dll` 覆盖 Emby 插件目录中的旧文件。
3. 确认 Emby 运行用户有权限读取新文件。
4. 重启 Emby Server。
5. 重新打开 Emby 网页端，进入插件列表或插件配置页检查版本和设置。

升级后如果 Emby 网页端仍显示旧设置，先强制刷新浏览器页面，再重新打开插件配置页。
