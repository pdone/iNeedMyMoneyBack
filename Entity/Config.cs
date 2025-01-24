using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace iNeedMyMoneyBack;

public class StockConfig
{
    public StockConfig()
    {
    }
    public StockConfig(string code)
    {
        Code = code;
    }
    public StockConfig(string code, string nickName)
    {
        Code = code;
        NickName = nickName;
    }

    public StockConfig(string code, string nickName, double buyPrice)
    {
        Code = code;
        NickName = nickName;
        BuyPrice = buyPrice;
    }
    public StockConfig(string code, string nickName, double buyPrice, int buyCount)
    {
        Code = code;
        NickName = nickName;
        BuyPrice = buyPrice;
        BuyCount = buyCount;
    }
    /// <summary>
    /// 代码
    /// </summary>
    public string Code
    {
        get; set;
    }
    /// <summary>
    /// 名称
    /// </summary>
    public string Name
    {
        get; set;
    }
    /// <summary>
    /// 别名
    /// </summary>
    public string NickName
    {
        get; set;
    }
    /// <summary>
    /// 买入单价
    /// </summary>
    public double BuyPrice
    {
        get; set;
    }
    /// <summary>
    /// 买入数量
    /// </summary>
    public int BuyCount
    {
        get; set;
    }
    public string DisplayName => string.IsNullOrWhiteSpace(NickName) ? Name : NickName;
    /// <summary>
    /// 日盈
    /// </summary>
    public double DayMake
    {
        get; set;
    }
    /// <summary>
    /// 总盈
    /// </summary>
    public double AllMake
    {
        get; set;
    }
    /// <summary>
    /// 成本
    /// </summary>
    public double Cost
    {
        get; set;
    }
    /// <summary>
    /// 市值
    /// </summary>
    public double MarketValue
    {
        get; set;
    }
    /// <summary>
    /// 收益率
    /// </summary>
    public double Yield
    {
        get; set;
    }
    /// <summary>
    /// 指数专用字段
    /// </summary>
    [JsonIgnore]
    public string IndexInfo
    {
        get; set;
    }
    /// <summary>
    /// 提醒价格 大于等于时提醒
    /// </summary>
    public double ReminderPriceUp
    {
        get; set;
    }
    /// <summary>
    /// 提醒价格 小于等于时提醒
    /// </summary>
    public double ReminderPriceDown
    {
        get; set;
    }
    /// <summary>
    /// 提醒次数
    /// </summary>
    public int ReminderTimes
    {
        get; set;
    } = 1;

    public override bool Equals(object obj)
    {
        return obj is StockConfig other && other.Code == Code;
    }

    public override int GetHashCode()
    {
        return Code == null ? Guid.NewGuid().GetHashCode() : Code.GetHashCode();
    }
}

public class StockConfigArray : List<StockConfig>
{
    public StockConfig this[string code]
    {
        get
        {
            if (code.StartsWith(Utils.StockIndexPrefix))
            {
                return this.FirstOrDefault(x => x.Code == code.Remove(0, Utils.StockIndexPrefix.Length));
            }
            return this.FirstOrDefault(x => x.Code == code);
        }
    }

    /// <summary>
    /// 重要指数集合
    /// </summary>
    public static readonly StockConfigArray ImportantIndexs =
    [
        new StockConfig("sh000001"),
        new StockConfig("sz399001"),
        new StockConfig("sz399006"),
        new StockConfig("sz399300"),
        new StockConfig("bj899050"),
    ];

    /// <summary>
    /// 加载股票数据
    /// </summary>
    /// <returns></returns>
    public static StockConfigArray Load()
    {
        var conf = new StockConfigArray();
        try
        {
            var fullPath = Path.Combine(Utils.UserDataPath, "stocks.json");
            Directory.CreateDirectory(Utils.UserDataPath);
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, conf.ToStr());
            }
            var reader = File.OpenText(fullPath);
            conf = reader.ReadToEnd().ToObj<StockConfigArray>();
            reader.Close();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        return conf;
    }
    /// <summary>
    /// 保存股票数据
    /// </summary>
    /// <param name="conf"></param>
    public void Save()
    {
        try
        {
            var fullPath = Path.Combine(Utils.UserDataPath, "stocks.json");
            Directory.CreateDirectory(Utils.UserDataPath);
            RemoveAll(x => x.Code.IsNullOrWhiteSpace());
            File.WriteAllText(fullPath, this.ToStr());
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
}

public class Config
{
    /// <summary>
    /// 调试模式
    /// </summary>
    public bool Debug { get; set; } = false;
    /// <summary>
    /// 数据接口
    /// </summary>
    public string Api { get; set; } = "http://qt.gtimg.cn";
    /// <summary>
    /// UA
    /// </summary>
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0";
    /// <summary>
    /// 隐藏边框
    /// </summary>
    public bool HideBorder { get; set; } = false;
    /// <summary>
    /// 暗色模式
    /// </summary>
    public bool DarkMode { get; set; } = true;
    /// <summary>
    /// 置顶
    /// </summary>
    public bool Topmost { get; set; } = false;
    /// <summary>
    /// 请求间隔
    /// </summary>
    public int Interval { get; set; } = 5;
    /// <summary>
    /// 语言 当前仅支持：cn 和 en
    /// </summary>
    public string Lang { get; set; } = "cn";
    /// <summary>
    /// 在任务栏显示
    /// </summary>
    public bool ShowInTaskbar { get; set; } = true;
    /// <summary>
    /// 滚动展示数据
    /// </summary>
    public bool DataRoll { get; set; } = false;
    /// <summary>
    /// 透明背景
    /// </summary>
    public bool Transparent { get; set; } = false;
    /// <summary>
    /// 不透明度
    /// </summary>
    public double Opacity { get; set; } = 0.8;
    /// <summary>
    /// 主窗口左上角X坐标
    /// </summary>
    public double Left { get; set; } = 200;
    /// <summary>
    /// 主窗口左上角Y坐标
    /// </summary>
    public double Top { get; set; } = 200;
    /// <summary>
    /// 主窗口宽度
    /// </summary>
    public double Width { get; set; } = 180;
    /// <summary>
    /// 主窗口高度
    /// </summary>
    public double Height { get; set; } = 26;
    /// <summary>
    /// 配置窗口宽度
    /// </summary>
    public double ConfigWindowWidth { get; set; } = 200;
    /// <summary>
    /// 配置窗口高度
    /// </summary>
    public double ConfigWindowHeight { get; set; } = 165;
    /// <summary>
    /// 是否每日重置提醒次数
    /// </summary>
    public bool DailyResetReminder
    {
        get; set;
    } = true;
    /// <summary>
    /// 上次提醒时间 格式 yyyy-MM-ddTHH:mm:ss
    /// </summary>
    public DateTime LastReminderTime
    {
        get; set;
    } = DateTime.MaxValue;
    /// <summary>
    /// 字段显示控制
    /// </summary>
    public Dictionary<string, bool> FieldControls
    {
        get; set;
    } = new()
    {
        {"ui_price",true},
        {"ui_change",true},
        {"ui_buy_price",false},
        {"ui_num",false},
        {"ui_cost",false},
        {"ui_market_value",false},
        {"ui_day_make",false},
        {"ui_all_make",false},
        {"ui_yield",false},
        {"ui_yesterday_todayopen",false},
        {"ui_lowest_highest",false},
        {"ui_limitup_limitdown",false},
    };

    /// <summary>
    /// 扩展字段显示控制
    /// </summary>
    public List<ExtendControlObj> ExtendControls
    {
        get; set;
    } =
    [
        // 字段说明
        new ExtendControlObj("ui_fieldname"),
        // 各大指数
        new ExtendControlObj("ui_index_sh000001"),
        new ExtendControlObj("ui_index_sz399001"),
        new ExtendControlObj("ui_index_sz399006"),
        new ExtendControlObj("ui_index_sz399300"),
        new ExtendControlObj("ui_index_bj899050"),
        // 个人数据
        new ExtendControlObj("ui_all_stock_day_make"),
        new ExtendControlObj("ui_all_stock_all_make"),
        new ExtendControlObj("ui_all_yield_day"),
        new ExtendControlObj("ui_all_yield"),
        new ExtendControlObj("ui_all_cost"),
        new ExtendControlObj("ui_all_market_value"),
    ];

    /// <summary>
    /// 获取用户配置
    /// </summary>
    /// <returns></returns>
    public static Config Load()
    {
        var conf = new Config();
        try
        {
            var fullPath = Path.Combine(Utils.UserDataPath, "config.json");
            Directory.CreateDirectory(Utils.UserDataPath);
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, conf.ToStr());
            }
            var reader = File.OpenText(fullPath);
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
    public void Save()
    {
        try
        {
            var fullPath = Path.Combine(Utils.UserDataPath, "config.json");
            Directory.CreateDirectory(Utils.UserDataPath);
            File.WriteAllText(fullPath, this.ToStr());
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
}

public class ExtendControlObj(string key)
{
    public string GetNewLineKey() => Key + Utils.NewlineSuffix;

    public string Key { get; set; } = key;
    public bool Visable { get; set; } = false;
    public bool NewLine { get; set; } = false;
}
