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
