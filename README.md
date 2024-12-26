<div align="center">

<img alt="logo" src="assets/inmmb.png" width=128 height=128>
<h1>iNeedMyMoneyBack</h1>

[![](https://img.shields.io/github/release/pdone/iNeedMyMoneyBack?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/releases/latest)
[![](https://img.shields.io/github/downloads/pdone/iNeedMyMoneyBack/total?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/releases)
[![](https://img.shields.io/github/stars/pdone/iNeedMyMoneyBack?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/stargazers)
[![](https://img.shields.io/github/issues/pdone/iNeedMyMoneyBack?style=for-the-badge)](https://github.com/pdone/iNeedMyMoneyBack/issues)

</div>

## 简介

一个监控股票的工具。

## 使用说明

### 主界面

![](assets/inmmb_0.gif)

![](assets/inmmb_1.gif)

- 勾选 `数据滚动显示` 时，始终只显示一条数据，按配置列表滚动显示。
- 未勾选 `数据滚动显示` 时，将显示所有配置中的数据。
- 界面右下角处，可以拖动来改变大小。
- **建议安装 fonts 文件夹中的两个字体，以达到最佳显示效果。**

### 右键菜单

![](assets/inmmb_menu1.png)
![](assets/inmmb_menu2.png)

- 菜单项中显示的快捷键都是程序快捷键，仅焦点在本程序界面时有效。
- **程序内置了一个全局快捷键 `Ctrl + ~` ，用于显示或隐藏主界面。**

### 添加和删除

![](assets/inmmb_add.gif)

- 打开配置，在 `代码` 列双击空白单元格，输入股票代码后按回车键保存修改，即可添加要监控的股票。
- 代码前需要 `sh`、`sz`、`bj` 等前缀，分别代表上海、深圳、北京。
- 将 `代码` 删除，按回车键，可以删除监控股票。
- 设置 `别名` 后，主界面就不再显示 `名称`，而是显示 `别名`。
- 买价为单股价格，数量为股数（不是手数）。

### 显示字段控制

![](assets/inmmb_more1.png)
![](assets/inmmb_more2.png)

- 单击后按钮背景变为浅色，即为启用。
- 程序内置了一些指数，这些指数不允许添加监控。

## 下载

### GitHub Release

https://github.com/pdone/iNeedMyMoneyBack/releases/latest/download/iNeedMyMoneyBack.exe

## 常见问题

### 数据来源

https://qt.gtimg.cn/

### 数据对齐显示

安装 [fonts](/fonts/) 文件夹中的等宽字体，并且设置别名，确保别名长度一致，可以让数据对齐显示。
> 也可以到字体官方网站下载 [阿里巴巴普惠体](https://www.alibabafonts.com/#/font)、[Cascadia Code](https://github.com/microsoft/cascadia-code)

### 程序日志和配置文件路径

`%AppData%/iNeedMyMoneyBack`

### 问题反馈

提交 Issue 即可。

### 投资建议

根据程序名称不难发现，作者无法提供任何有用的投资建议。

如果你有专业的金融知识，但缺少编程知识，可以考虑联系合作。

### 开放源代码

程序比较简单，没什么值得分享的技术，代码无混淆，需要看代码的朋友使用 dnSpy 反编译即可。

## 赞助

如果您觉得这个项目对您有帮助，欢迎请作者喝杯咖啡。☕

<details>
<summary>展开</summary>

![](https://raw.githubusercontent.com/pdone/static/master/img/donate/zfb_wx.jpg)

</details>

## 更新日志

[![](https://img.shields.io/badge/updete-record-fedcba?style=for-the-badge)](/Update.md)
