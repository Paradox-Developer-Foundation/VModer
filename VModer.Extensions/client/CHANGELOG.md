## 0.8.0

### 特质预览器

- 支持显示特质 Icon
- 右键菜单新增 '在文件中打开', 可以定位特质在文件中的位置
- 加载时显示加载提示文字
- 支持手动刷新特质列表

### 其他更新

- 科技 Hover 显示科技本地化名称

### 错误修复

- 科技 Hover 排除 `show_effect_as_desc`
- 无法在 Character Hover 中正确显示特质的 `trait_xp_factor` 信息
- 无法正确格式化地形修饰符

## 0.7.1

完善遥测

## 0.7.0

### Technology

支持显示 **Technology** (`common\technologies`文件夹下) 的修饰符, 就像 **Modifier** 一样, 支持节点和单一修饰符的显示

---

### 遥测功能

此版本接入了遥测以帮助我们了解扩展的使用情况和改进用户体验。收集的数据包括：

- 功能使用情况（如特质预览器使用次数）
- 错误报告
- 性能指标

**关闭遥测方法**：在VS Code设置中将`telemetry.telemetryLevel`设置为`off`，或在用户设置中添加`"vmoder.enableTelemetry": false`

### 其他更新

- perf: 优化部分情景下内存使用
- fix: 部分情况下无法显示引用字符串
- fix: 添加缺失的 `monthly_population` Modifier 格式化符

## 0.6.0

### 全新的特质预览器

显示游戏和 MOD 中所有定义的特质, 并可以按来源(游戏, MOD), 特质类型筛选, 显示特质修饰效果,右键菜单可复制特质 ID

在右上角打开

![trait](https://www.helloimg.com/i/2025/03/12/67d18f36cdfaf.png)

### 其他更新

- feat: 本地化字符串和 interface 文件加载从单线程改为多线程
- fix: 获取本地图片缓存时 frame 参数不生效
- fix: 当图片文件重名时, 会显示错误的图片
- fix: 补充缺失的修饰符本地化

## 0.5.0

- feat: 支持显示本地化中的 Icon
- feat: 支持显示将领特质 Icon
- feat: 添加清理本地图片缓存命令
- feat: 支持显示 hidden_modifier 下的修饰符, 此前会显示多余的 `hidden_modifier:`
- fix: 部分情况下无法正确识别字符串中的Icon部分
- i18n: 一些本地化工作

## 0.4.2

- feat: 添加游戏文本语言配置项, 此配置项决定扩展从 `Hearts of Iron IV\localisation`下的指定文件夹中读取文本
- fix: 当分析未结束时触发查询时依然会结束分析状态显示
- feat: 在不打开工作区时不启动服务端以防止扩展报错
- feat: 配置项 RamQueryIntervalTime, ParseFileMaxSize 添加最小值
  - RamQueryIntervalTime 最小值: 500
  - ParseFileMaxSize 最小值: 0

## 0.4.1

- fix: 当 Mod 改写 `common\defines` 文件夹下的内容时导致的扩展崩溃

## 0.4.0

### 配置项

- feat: 添加内存查询间隔时间配置项
- feat: 添加分析黑名单配置项
- feat: 添加解析文件最大大小配置项

### 颜色选择器

- feat: 颜色选择器支持饱和度和亮度修饰符, 因此颜色选择器的颜色应该会更加准确.
- feat: 意识形态支持颜色选择器
- feat: 字体颜色添加颜色选择器

### 人物特质

- feat: 支持指针放在人物特质上时显示指向的特质效果
- feat: 支持显示特质中的 trait_xp_factor

### 其他

- feat: 支持`history\countries`文件夹下初始科技的显示
- refactor: 优化日志记录和异常处理
- fix: 补全部分缺失的修饰符本地化
- perf: 优化性能表现

## 0.3.0

- feat: 支持 Modifiers 节点显示修饰符
- feat: 支持显示 Character 中的 country_leader
- feat: 支持正确处理修饰符格式化语法中的 %% 转义写法
- feat: 底部添加状态栏显示当前 RAM 使用情况
- feat: 支持 cosmetic.txt 文件使用颜色选择器
- feat: 添加打开 Logs 文件夹的命令
- fix: 补充缺失的修饰符映射信息
- fix: 特质名称支持引用写法
- 兼容性: 禁用 CET 以支持在较低版本的 Windows 上运行此扩展

## 0.2.2

- fix: 使用错误的与服务器通讯方式导致扩展不正常工作

## 0.2.1

- fix: 使日志组件正常工作
- fix: 错误地使用 character key 而不是name来获取 character 本地化值
- fix: character 本地化键不支持引用写法

## 0.2.0

- feat: 支持颜色选择器功能
- perf: 重启full裁剪减小扩展大小
- perf: 通过减少不必要的类型解析来优化文本解析的性能

## 0.1.1

- fix: 当选择游戏根目录后依然弹出选择提示
- fix: 无法正确读取配置文件

## 0.1.0

- 发布
