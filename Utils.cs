using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

#pragma warning disable IDE0007 // 使用隐式类型
#pragma warning disable IDE0090 // 使用 "new(...)"
#pragma warning disable IDE1006 // 命名样式

namespace iNeedMyMoneyBack;

public static class Utils
{
    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true
    };
    #region 扩展方法
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
        int visualWidth = 0;
        foreach (char c in input)
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
    public static Dictionary<string, Dictionary<string, string>> LoadLangData()
    {
        var langs = new Dictionary<string, string[]>
        {
            ["menu_ver"] = ["Version", "版本"],
            ["menu_exit"] = ["Exit(_X)", "退出(_X)"],
            ["menu_dark"] = ["Dark mode", "深色模式"],
            ["menu_topmost"] = ["Topmost", "置顶"],
            ["menu_conf"] = ["Open config(_C).", "打开配置(_C)"],
            ["menu_conf_file"] = ["Open config file...(_F)", "打开配置文件...(_F)"],
            ["menu_show_in_taskbar"] = ["Show in taskbar", "在任务栏显示"],
            ["menu_data_roll"] = ["Data roll", "数据滚动显示"],
            ["menu_ui"] = ["UI option", "界面选项"],
            ["menu_dev"] = ["Dev option", "开发者选项"],
            ["menu_lang"] = ["中文界面(_E)", "English UI(_E)"],
            ["btn_close"] = ["Close", "关闭"],
            ["col_code"] = ["Code", "代码"],
            ["col_name"] = ["Name", "名称"],
            ["col_nickname"] = ["NickName", "别名"],
            ["col_buyprice"] = ["BuyPrice", "买价"],
            ["col_buycount"] = ["BuyCount", "数量"],
            ["ui_nontrading"] = ["Non-trading", "非交易时间"],
            ["ui_getdatafialed"] = ["Failed to get data", "获取数据失败"],

            ["ui_name"] = ["Name", "名称"],
            ["ui_price"] = ["Price", "价格"],
            ["ui_change"] = ["Change", "涨幅"],
            ["ui_buy_price"] = ["BuyPrice", "买价"],
            ["ui_num"] = ["Num", "数量"],
            ["ui_cost"] = ["Cost", "成本"],
            ["ui_market_value"] = ["MarketValue", "市值"],
            ["ui_day_make"] = ["DayMake", "日盈"],
            ["ui_all_make"] = ["AllMake", "总盈"],
            ["ui_yesterday"] = ["LastClose", "昨收"],
            ["ui_todayopen"] = ["TodayOpen", "今开"],
            ["ui_highest"] = ["Highest", "最高"],
            ["ui_lowest"] = ["Lowest", "最低"],
            ["ui_limitup"] = ["UpLimit", "涨停"],
            ["ui_limitdown"] = ["DownLimit", "跌停"],

            [ExtendControlObj.NewlineSuffix] = ["NewLine", "换行"],
            ["ui_fieldname"] = ["FieldName", "字段名称"],
            ["ui_all_stock_day_make"] = ["AllStockDayMake", "总持日盈"],
            ["ui_all_stock_all_make"] = ["AllStockAllMake", "总持总盈"],
            ["ui_all_cost"] = ["AllCost", "总成本"],
            ["ui_all_market_value"] = ["AllMarketValue", "总市值"],

            ["ui_yesterday_todayopen"] = ["LastClose TodayOpen", "昨收今开"],
            ["ui_lowest_highest"] = ["Lowest Highest", "最低最高"],
            ["ui_limitup_limitdown"] = ["UpLimit DownLimit", "涨停跌停"],
        };

        var en = langs.Select(x => new KeyValuePair<string, string>(x.Key, x.Value[0]))
            .ToDictionary(x => x.Key, x => x.Value);
        var zh_CN = langs.Select(x => new KeyValuePair<string, string>(x.Key, x.Value[1]))
            .ToDictionary(x => x.Key, x => x.Value); ;

        var dict = new Dictionary<string, Dictionary<string, string>>
        {
            { "en", en },
            { "cn", zh_CN }
        };
        return dict;
    }
    #endregion

    #region 本地数据
    public const string ProductName = "iNeedMyMoneyBack";
    /// <summary>
    /// 用户数据目录
    /// </summary>
    public static string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductName);
    /// <summary>
    /// 获取用户配置
    /// </summary>
    /// <returns></returns>
    public static Config LoadConfig()
    {
        var conf = new Config();
        try
        {
            var fullPath = Path.Combine(UserDataPath, "config.json");
            Directory.CreateDirectory(UserDataPath);
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, conf.ToStr());
            }
            StreamReader reader = File.OpenText(fullPath);
            conf = reader.ReadToEnd().ToObj<Config>();
            reader.Close();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        return conf;
    }
    /// <summary>
    /// 写入用户配置
    /// </summary>
    public static void SaveConfig(Config conf)
    {
        try
        {
            var fullPath = Path.Combine(UserDataPath, "config.json");
            Directory.CreateDirectory(UserDataPath);
            var invalids = new List<StockConfig>();
            conf.Stocks.RemoveAll(x =>
            {
                return x.Code.IsNullOrWhiteSpace();
            });
            File.WriteAllText(fullPath, conf.ToStr());
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
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

    /// <summary>
    /// 判断当前时间是否在A股交易时间内（不包括节假日检查）
    /// </summary>
    /// <returns>如果在交易时间内返回true，否则返回false</returns>
    public static bool IsTradingTime()
    {
        DateTime now = DateTime.Now;

        // 检查是否是工作日（这里简单认为是周一到周五，实际可能需要考虑节假日）
        DayOfWeek dayOfWeek = now.DayOfWeek;
        if (dayOfWeek < DayOfWeek.Monday || dayOfWeek > DayOfWeek.Friday)
        {
            return false; // 不是工作日，不交易
        }

        // 定义交易时间
        DateTime startTime = new DateTime(now.Year, now.Month, now.Day, 9, 30, 0);
        DateTime endTimeMorning = new DateTime(now.Year, now.Month, now.Day, 11, 30, 0);
        DateTime startTimeAfternoon = new DateTime(now.Year, now.Month, now.Day, 13, 0, 0);
        DateTime endTime = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0);

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
}

#region 日志
public class Logger
{
    public static string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");

    /// <summary>
    /// 启用日志
    /// </summary>
    public static bool enable = true;

    //死锁
    public static object loglock = new object();

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

            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");//获取当前系统时间
            string filename = path + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";//用日期对日志文件命名

            //创建或打开日志文件，向日志文件末尾追加记录
            StreamWriter mySw = File.AppendText(filename);

            //向日志文件写入内容
            string write_content = time + " [" + type + "] " + content;
            mySw.WriteLine(write_content);

            //关闭日志文件
            mySw.Close();
        }
    }
}
#endregion
