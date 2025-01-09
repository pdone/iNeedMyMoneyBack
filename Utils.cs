using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace iNeedMyMoneyBack;

public static class Utils
{
    #region 扩展方法
    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static double Parse(string input)
    {
        double.TryParse(input, out var result);
        return result;
    }
    public static T ToObj<T>(this string input)
    {
        return JsonSerializer.Deserialize<T>(input, options);
    }
    public static string ToStr(this object obj)
    {
        return JsonSerializer.Serialize(obj, options);
    }
    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
    public static double ToDouble(this string value)
    {
        double.TryParse(value, out var result);
        return result;
    }
    public static string iPadRight(this string value, int count, char paddingChar = ' ')
    {
        return PadRightByVisualWidth(value, count, paddingChar);
    }
    public static string iPadLeft(this string value, int count, char paddingChar = ' ')
    {
        return PadRightByVisualWidth(value, count, paddingChar, false);
    }
    public static string PadRightByVisualWidth(string input, int totalWidth, char paddingChar, bool isRight = true)
    {
        var currentWidth = GetVisualWidth(input);
        var paddingLength = totalWidth - currentWidth;

        if (paddingLength > 0)
        {
            if (isRight)
            {
                return input + new string(paddingChar, paddingLength);
            }
            else
            {
                return new string(paddingChar, paddingLength) + input;
            }
        }
        else
        {
            return input;
        }
    }

    public static int GetVisualWidth(string input)
    {
        var visualWidth = 0;
        foreach (var c in input)
        {
            if (char.IsHighSurrogate(c) || char.IsLowSurrogate(c))
            {
                // Surrogate pairs (used for characters outside the Basic Multilingual Plane)
                visualWidth += 2;
            }
            else if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
            {
                // Assume full-width characters (common for CJK)
                visualWidth += 2;
            }
            else
            {
                // Assume half-width characters
                visualWidth += 1;
            }
        }
        return visualWidth;
    }
    #endregion

    #region 多语言
    public const string StockIndexPrefix = "ui_index_";// 指数控件名称前缀
    public const string NewlineSuffix = "_newline";// 换行控件名称后缀

    private static readonly Dictionary<string, string[]> LanguageDatas = new()
    {
        ["menu_ver"] = ["Version", "版本"],
        ["menu_check_update"] = ["Check Update", "检查更新"],
        ["menu_debug_mode"] = ["Debug Mode", "调试模式"],
        ["menu_exit"] = ["Exit", "退出"],
        ["menu_dark"] = ["Dark Mode", "深色模式"],
        ["menu_topmost"] = ["Top Most", "置顶"],
        ["menu_conf"] = ["Open Config", "打开配置"],
        ["menu_conf_file"] = ["Open Config File...", "打开配置文件..."],
        ["menu_data_dir"] = ["Open Data Dir", "打开数据目录"],
        ["menu_show_in_taskbar"] = ["Show in Taskbar", "在任务栏显示"],
        ["menu_data_roll"] = ["Data Roll", "数据滚动显示"],
        ["menu_ui"] = ["UI Option", "界面选项"],
        ["menu_dev"] = ["Dev Option", "开发者选项"],
        ["menu_lang"] = ["UI Chinese", "英文界面"],
        ["menu_lang_en"] = ["English", "English"],
        ["menu_lang_cn"] = ["中文", "中文"],
        ["menu_opacity"] = ["Opacity {0}%", "不透明度 {0}%"],
        ["menu_opacity_igt"] = ["Ctrl+Wheel", "Ctrl+滚轮"],
        ["menu_hide_border"] = ["Hide Border", "隐藏边框"],
        ["menu_transparent"] = ["Transparent", "背景透明"],
        ["btn_close"] = ["Close", "关闭"],
        ["col_code"] = ["Code", "代码"],
        ["col_name"] = ["Name", "名称"],
        ["col_nickname"] = ["NickName", "别名"],
        ["col_buyprice"] = ["BuyPrice", "买价"],
        ["col_buycount"] = ["BuyCount", "数量"],
        ["ui_nontrading"] = ["Non-trading", "非交易时间"],
        ["ui_getdatafialed"] = ["Failed to get data", "获取数据失败"],
        ["ui_main_label_tooltip"] = ["Double-click to view details{0}", "双击查看详情{0}"],

        ["ui_title_tip"] = ["Tip", "提示"],
        ["ui_title_err"] = ["Error", "错误"],
        ["ui_title_warn"] = ["Warning", "警告"],
        ["ui_title_check_update"] = ["Check Updates", "检查更新"],
        ["ui_title_config"] = ["Config", "配置"],
        ["ui_msg_check_update"] = ["Version {0}\n\nYou're up to date.", "版本 {0}\n\n已经是最新版本。"],
        ["ui_drag_block"] = ["Drag the block to move the window", "拖动块来移动窗口"],

        ["ui_name"] = ["Name", "名称"],
        ["ui_price"] = ["Price", "价格"],
        ["ui_change"] = ["Change", "涨幅"],
        ["ui_buy_price"] = ["BuyPrice", "买价"],
        ["ui_num"] = ["Num", "数量"],
        ["ui_cost"] = ["Cost", "成本"],
        ["ui_market_value"] = ["MarketValue", "市值"],
        ["ui_day_make"] = ["DayMake", "日盈"],
        ["ui_all_make"] = ["AllMake", "总盈"],
        ["ui_yield"] = ["Yield", "收益率"],
        ["ui_yesterday"] = ["LastClose", "昨收"],
        ["ui_todayopen"] = ["TodayOpen", "今开"],
        ["ui_highest"] = ["Highest", "最高"],
        ["ui_lowest"] = ["Lowest", "最低"],
        ["ui_limitup"] = ["UpLimit", "涨停"],
        ["ui_limitdown"] = ["DownLimit", "跌停"],

        [NewlineSuffix] = ["NewLine", "换行"],
        ["ui_fieldname"] = ["FieldName", "字段名称"],

        [StockIndexPrefix + "sh000001"] = ["SZZS", "上证指数"],
        [StockIndexPrefix + "sz399001"] = ["SZCZ", "深证成指"],
        [StockIndexPrefix + "sz399006"] = ["CYBZ", "创业板指"],
        [StockIndexPrefix + "sz399300"] = ["HS300", "沪深300"],
        [StockIndexPrefix + "bj899050"] = ["BZ50", "北证50"],

        ["ui_all_stock_day_make"] = ["AllStockDayMake", "总持日盈"],
        ["ui_all_stock_all_make"] = ["AllStockAllMake", "总持总盈"],
        ["ui_all_yield_day"] = ["AllYieldDay", "日收益率"],
        ["ui_all_yield"] = ["AllYield", "总收益率"],
        ["ui_all_cost"] = ["AllCost", "总成本"],
        ["ui_all_market_value"] = ["AllMarketValue", "总市值"],

        ["ui_yesterday_todayopen"] = ["LastClose TodayOpen", "昨收今开"],
        ["ui_lowest_highest"] = ["Lowest Highest", "最低最高"],
        ["ui_limitup_limitdown"] = ["UpLimit DownLimit", "涨停跌停"],
    };

    private static Dictionary<string, Dictionary<string, string>> _i18n = null;
    public static Dictionary<string, Dictionary<string, string>> i18n
    {
        get
        {
            if (_i18n == null)
            {
                var en = LanguageDatas.Select(x => new KeyValuePair<string, string>(x.Key, x.Value[0]))
                    .ToDictionary(x => x.Key, x => x.Value);
                var zh_CN = LanguageDatas.Select(x => new KeyValuePair<string, string>(x.Key, x.Value[1]))
                    .ToDictionary(x => x.Key, x => x.Value); ;
                _i18n = new Dictionary<string, Dictionary<string, string>>
                {
                    { "en", en },
                    { "cn", zh_CN }
                };
                return _i18n;
            }
            return _i18n;
        }
    }
    #endregion

    #region 本地数据
    /// <summary>
    /// 用户数据目录
    /// </summary>
    public static string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), App.ProductName);
    #endregion

    #region 拖动窗口
    [DllImport("user32.dll")]// 拖动无窗体的控件
    public static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
    public const int WM_SYSCOMMAND = 0x0112;
    public const int SC_MOVE = 0xF010;
    public const int HTCAPTION = 0x0002;

    /// <summary>
    /// 拖动窗体
    /// </summary>
    public static void DragWindow(Window window)
    {
        ReleaseCapture();
        SendMessage(new WindowInteropHelper(window).Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
    }
    #endregion

    #region 交易时间判断
    /// <summary>
    /// 判断当前时间是否在A股交易时间内（不包括节假日检查）
    /// </summary>
    /// <returns>如果在交易时间内返回true，否则返回false</returns>
    public static bool IsTradingTime()
    {
        var now = DateTime.Now;

        // 检查是否是工作日（这里简单认为是周一到周五，实际可能需要考虑节假日）
        var dayOfWeek = now.DayOfWeek;
        if (dayOfWeek < DayOfWeek.Monday || dayOfWeek > DayOfWeek.Friday)
        {
            return false; // 不是工作日，不交易
        }

        // 定义交易时间
        var startTime = new DateTime(now.Year, now.Month, now.Day, 9, 30, 0);
        var endTimeMorning = new DateTime(now.Year, now.Month, now.Day, 11, 30, 0);
        var startTimeAfternoon = new DateTime(now.Year, now.Month, now.Day, 13, 0, 0);
        var endTime = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0);

        // 判断是否在上午交易时间段内
        if (now >= startTime && now <= endTimeMorning)
        {
            return true;
        }

        // 判断是否在下午交易时间段内
        if (now >= startTimeAfternoon && now <= endTime)
        {
            return true;
        }

        // 如果都不满足，则不在交易时间内
        return false;
    }
    #endregion

    #region 全局快捷键
    public const int MOD_CTRL = 0x0002; // Ctrl 键

    [DllImport("user32.dll")]
    public static extern int RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    public static extern int UnregisterHotKey(IntPtr hWnd, int id);
    #endregion
}

#region 日志
public class Logger
{
    public static string path = Path.Combine(Utils.UserDataPath, "logs");

    /// <summary>
    /// 启用日志
    /// </summary>
    public static bool enable = true;

    //死锁
    public static object loglock = new();

    public static void Debug(string content)
    {
        WriteLog("DEBUG", content);
    }

    public static void Info(string content)
    {
        WriteLog("INFO", content);
    }

    public static void Info(string content, string type)
    {
        WriteLog(type, content);
    }

    public static void Error(string content)
    {
        WriteLog("ERROR", content);
    }

    public static void Error(Exception ex)
    {
        WriteLog("ERROR", ex.ToString());
    }

    protected static void WriteLog(string type, string content)
    {
        if (!enable)
        {
            return;
        }
        lock (loglock)
        {
            if (!Directory.Exists(path))//如果日志目录不存在就创建
            {
                Directory.CreateDirectory(path);
            }

            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");//获取当前系统时间
            var filename = path + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";//用日期对日志文件命名

            //创建或打开日志文件，向日志文件末尾追加记录
            var mySw = File.AppendText(filename);

            //向日志文件写入内容
            var write_content = time + " [" + type + "] " + content;
            mySw.WriteLine(write_content);

            //关闭日志文件
            mySw.Close();
        }
    }
}
#endregion

public static class ContextMenuHelper
{
    /// <summary>
    /// 在 ContextMenu 中查找指定名称的 MenuItem。
    /// </summary>
    /// <param name="contextMenu">要搜索的 ContextMenu。</param>
    /// <param name="name">MenuItem 的名称。</param>
    /// <returns>找到的 MenuItem；如果没有找到，则返回 null。</returns>
    public static MenuItem FindMenuItem(this ContextMenu contextMenu, string name)
    {
        if (contextMenu == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var item in contextMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                if (IsMenuItemWithName(menuItem, name))
                {
                    return menuItem;
                }

                // 递归查找子菜单中的 MenuItem
                if (FindMenuItemInSubmenu(menuItem, name) is MenuItem subItem)
                {
                    return subItem;
                }
            }
        }

        return null;
    }

    private static bool IsMenuItemWithName(MenuItem menuItem, string name)
    {
        // 检查 x:Name 和 Name 属性
        return (string.Equals((string)menuItem.GetValue(FrameworkElement.NameProperty), name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(menuItem.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static MenuItem FindMenuItemInSubmenu(MenuItem parentMenuItem, string name)
    {
        if (parentMenuItem is not null && parentMenuItem.HasItems)
        {
            foreach (var subItem in parentMenuItem.Items)
            {
                if (subItem is MenuItem subMenuItem)
                {
                    if (IsMenuItemWithName(subMenuItem, name))
                    {
                        return subMenuItem;
                    }

                    // 继续递归查找子菜单
                    if (FindMenuItemInSubmenu(subMenuItem, name) is MenuItem foundMenuItem)
                    {
                        return foundMenuItem;
                    }
                }
            }
        }
        return null;
    }

    public static MenuItem FindMenuItemByTag(this ContextMenu contextMenu, string tag)
    {
        if (contextMenu == null || string.IsNullOrEmpty(tag))
        {
            return null;
        }

        foreach (var item in contextMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                if (IsMenuItemWithTag(menuItem, tag))
                {
                    return menuItem;
                }

                // 递归查找子菜单中的 MenuItem
                if (FindMenuItemInSubmenuByTag(menuItem, tag) is MenuItem subItem)
                {
                    return subItem;
                }
            }
        }

        return null;
    }

    private static bool IsMenuItemWithTag(MenuItem menuItem, string tag)
    {
        // 检查 x:Name 和 Name 属性
        return string.Equals(menuItem.Tag as string, tag, StringComparison.OrdinalIgnoreCase);
    }

    private static MenuItem FindMenuItemInSubmenuByTag(MenuItem parentMenuItem, string tag)
    {
        if (parentMenuItem is not null && parentMenuItem.HasItems)
        {
            foreach (var subItem in parentMenuItem.Items)
            {
                if (subItem is MenuItem subMenuItem)
                {
                    if (IsMenuItemWithTag(subMenuItem, tag))
                    {
                        return subMenuItem;
                    }

                    // 继续递归查找子菜单
                    if (FindMenuItemInSubmenu(subMenuItem, tag) is MenuItem foundMenuItem)
                    {
                        return foundMenuItem;
                    }
                }
            }
        }
        return null;
    }
}
