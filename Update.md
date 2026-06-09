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

### 版本 2.3

#### 功能增强
- 新增 股票排序功能：支持按涨跌幅、买价、总成本、总市值、日盈、总盈、收益率排序
- 新增 排序方式选择：支持升序和降序排列
- 优化 更多设置布局：排序设置与API设置分行显示，界面更清晰

#### 配置项新增
- 新增 SortField：排序字段，值为 default/changePercent/buyPrice/cost/marketValue/dayMake/allMake/yield，默认 default
- 新增 SortOrder：排序方式，值为 asc/desc，默认 desc
- 配置版本升级至 v7

#### 界面优化
- 新增 更多设置中排序字段和排序方式的下拉框
- 优化 按钮区域布局：美股/港股启用控件与重置按钮分开显示

### 版本 2.2

#### 功能增强
- 新增 美股/港股启用/禁用功能：在 `更多设置` 中可控制是否显示美股和港股数据
- 新增 市场启用确认提示：启用美股/港股时弹出警告，提示数据延迟问题
- 优化 添加股票时市场检查：未启用对应市场时提示用户先启用
- 优化 指数显示过滤：根据市场启用状态自动过滤显示的指数
- 优化 个股过滤：禁用市场时自动隐藏对应市场的个股

#### 配置项新增
- 新增 EnableUS：启用美股数据（默认false）
- 新增 EnableHK：启用港股数据（默认false）
- 配置版本升级至 v6

#### 界面优化
- 新增 更多设置中启用美股/港股的CheckBox控件
- 新增 绿色✔标识显示市场启用状态

### 版本 2.1

#### 功能增强
- 新增 股票排序功能：支持上移/下移按钮调整股票显示顺序
- 新增 双击跳转配置：支持选择跳转到雪球或同花顺
- 新增 更多设置Tab页：集中管理字体大小、查询间隔、列间距、API地址等配置
- 新增 API验证机制：自定义API地址时自动验证可用性
- 新增 自定义对话框：替代原生MessageBox，完美适配深色模式

#### 界面优化
- 重构 主窗口布局：扩展字段独立显示在主表格下方
- 优化 股票卡片样式：移除边框，调整间距，更简洁美观
- 优化 输入框样式：统圆角和内边距，提升视觉体验
- 优化 深色模式适配：所有对话框和弹窗支持深色模式

#### 配置项新增
- 新增 FontSizeMain：主窗口字体大小（默认16）
- 新增 DoubleClickAction：双击跳转目标（xueqiu/tonghuashun）
- 新增 GridColumnSpacing：表格列间距（默认4）
- 优化 Api默认值：从http改为https

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