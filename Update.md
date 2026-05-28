<div align="center">

<img alt="logo" src="assets/inmmb.png" width=128 height=128>
<h1>iNeedMyMoneyBack</h1>

[![](https://img.shields.io/github/release/pdone/iNeedMyMoneyBack?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/releases/latest)
[![](https://img.shields.io/github/downloads/pdone/iNeedMyMoneyBack/total?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/releases)
[![](https://img.shields.io/github/stars/pdone/iNeedMyMoneyBack?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/stargazers)
[![](https://img.shields.io/github/issues/pdone/iNeedMyMoneyBack?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/issues)

一个监控股票的工具

</div>

## 更新日志

### 版本 2.0

#### 界面重构
- 重构 主界面显示：采用Grid表格布局，支持列对齐和表头显示
- 重构 配置界面：从DataGrid改为卡片式股票列表，支持添加、编辑、删除操作
- 新增 配置界面Tab分组：分为"股票管理"和"显示字段"两个标签页
- 新增 指数监控按市场分组显示（A股、港股、美股）
- 新增 股票编辑弹窗：支持修改昵称、买入价、数量、提醒价格等
- 优化 深色模式：支持窗口标题栏深色模式，优化颜色配置
- 优化 界面样式：统一按钮、复选框、输入框样式，添加圆角和悬停效果
- 优化 滚动条样式：自定义滚动条样式，更符合现代UI设计

#### 功能增强
- 新增 股票代码验证：添加股票时自动验证代码有效性
- 新增 多市场支持：支持美股（us）和港股（hk）股票代码
- 新增 多个内置指数：上证50、中证500、中证1000、科创50、恒生指数、国企指数、红筹指数、道琼斯、纳斯达克100
- 新增 配置版本迁移：支持配置文件自动升级，无需手动修改
- 新增 单元测试项目：添加iNeedMyMoneyBack.Tests测试项目

#### 性能优化
- 优化 线程安全：添加共享数据锁，修复并发访问问题
- 优化 异步任务：DataUpdate方法改为async Task，避免未捕获异常
- 优化 UI更新机制：实现Grid结构缓存和增量更新，提升性能
- 优化 数据解析：StockInfo.Get()使用命名常量索引，添加容错处理

#### 移除功能
- 移除 名称对齐功能：Grid表格布局自动处理对齐，无需手动配置

### 版本 1.4

- 增加 名称对齐（勾选 `名称对齐` 时，会在所有名称后填充全角空格和半角空格，可能导致名称后的空白会显得比较多，这由于大部分字体的中英文字符宽度不是1比2，想实现完全对齐就要保证每一个名称里的中英文字符数量一致）

### 版本 1.3

- 增加 系统托盘图标（双击显示/隐藏主界面）
- 增加 高于或低于指定价格时发送系统通知

### 版本 1.2

- 增加 双击数据行查看详情（跳转到雪球网，勾选 `背景透明` 时不生效）

### 版本 1.1

- 增加 数据动态对齐
- 增加 背景透明（勾选 `背景透明` 时，主界面右上角会显示一个实心圆，用于拖动）
- 修复 点击置顶等选项后，隐藏边框对主界面不生效的问题

### 版本 1.0

- 初始版本