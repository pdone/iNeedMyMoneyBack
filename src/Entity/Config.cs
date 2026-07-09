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
        new StockConfig("sh000016"),
        new StockConfig("sh000688"),
        new StockConfig("sh000905"),
        new StockConfig("sh000852"),
        new StockConfig("sz399001"),
        new StockConfig("sz399006"),
        new StockConfig("sz399300"),
        new StockConfig("bj899050"),
        new StockConfig("hkHSI"),
        new StockConfig("hkHSCEI"),
        new StockConfig("hkHSCCI"),
        new StockConfig("usDJI"),
        new StockConfig("usNDX"),
    ];

    /// <summary>
    /// 根据配置获取过滤后的重要指数集合
    /// </summary>
    public static StockConfigArray GetFilteredImportantIndexs(bool enableUS, bool enableHK)
    {
        var result = new StockConfigArray();
        foreach (var index in ImportantIndexs)
        {
            if (index.Code.StartsWith("us") && !enableUS) continue;
            if (index.Code.StartsWith("hk") && !enableHK) continue;
            result.Add(index);
        }
        return result;
    }

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
            conf = File.ReadAllText(fullPath).ToObj<StockConfigArray>();
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
    private const int CurrentConfigVersion = 8;

    /// <summary>
    /// 配置文件版本号，用于迁移
    /// </summary>
    public int ConfigVersion { get; set; } = 0;

    /// <summary>
    /// 启用美股数据
    /// </summary>
    public bool EnableUS { get; set; } = false;

    /// <summary>
    /// 启用港股数据
    /// </summary>
    public bool EnableHK { get; set; } = false;

    /// <summary>
    /// 调试模式
    /// </summary>
    public bool Debug { get; set; } = false;
    /// <summary>
    /// 数据接口
    /// </summary>
    public string Api { get; set; } = "https://qt.gtimg.cn";
    /// <summary>
    /// UA
    /// </summary>
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0";
    public string FontFamilyMain { get; set; } = "Cascadia Mono,阿里巴巴普惠体 3.0,Courier New,Consolas,Microsoft Yahei UI,Arial";
    public string FontFamilyConfig { get; set; } = "Microsoft YaHei UI,Arial";
    public string FontFamilyMenu { get; set; } = "Microsoft YaHei UI,Arial";
    /// <summary>
    /// 主窗口字体大小
    /// </summary>
    public double FontSizeMain { get; set; } = 16;
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
    public double ConfigWindowWidth { get; set; } = 420;
    /// <summary>
    /// 配置窗口高度
    /// </summary>
    public double ConfigWindowHeight { get; set; } = 450;
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
    /// 双击行为 xueqiu:雪球 tonghuashun:同花顺
    /// </summary>
    public string DoubleClickAction { get; set; } = "xueqiu";
    /// <summary>
    /// MainGrid列间距
    /// </summary>
    public double GridColumnSpacing { get; set; } = 4;
    /// <summary>
    /// 排序字段：default/changePercent/buyPrice/cost/marketValue/dayMake/allMake/yield
    /// </summary>
    public string SortField { get; set; } = "default";
    /// <summary>
    /// 排序方式：asc/desc
    /// </summary>
    public string SortOrder { get; set; } = "desc";
    /// <summary>
    /// 老板键：Ctrl+Oemtilde/Ctrl+D1/Ctrl+D2/Alt+Oemtilde/Alt+D1/Alt+D2
    /// </summary>
    public string BossKey { get; set; } = "Ctrl+Oemtilde";
    /// <summary>
    /// 字段显示控制
    /// </summary>
    public Dictionary<string, bool> FieldControls
    {
        get; set;
    } = new()
    {
        {"ui_fieldname",false},
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
    /// 字段换行控制
    /// </summary>
    public Dictionary<string, bool> FieldNewLines
    {
        get; set;
    } = new()
    {
    };

    /// <summary>
    /// 扩展字段显示控制
    /// </summary>
    public List<ExtendControlObj> ExtendControls
    {
        get; set;
    } =
    [
        // A股指数
        new ExtendControlObj("ui_index_sh000001"),
        new ExtendControlObj("ui_index_sh000016"),
        new ExtendControlObj("ui_index_sh000688"),
        new ExtendControlObj("ui_index_sh000905"),
        new ExtendControlObj("ui_index_sh000852"),
        new ExtendControlObj("ui_index_sz399001"),
        new ExtendControlObj("ui_index_sz399006"),
        new ExtendControlObj("ui_index_sz399300"),
        new ExtendControlObj("ui_index_bj899050"),
        // H股指数
        new ExtendControlObj("ui_index_hkHSI"),
        new ExtendControlObj("ui_index_hkHSCEI"),
        new ExtendControlObj("ui_index_hkHSCCI"),
        // 美股指数
        new ExtendControlObj("ui_index_usDJI"),
        new ExtendControlObj("ui_index_usNDX"),
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
            conf = File.ReadAllText(fullPath).ToObj<Config>();

            if (conf.ConfigVersion < CurrentConfigVersion)
            {
                Migrate(conf);
                conf.ConfigVersion = CurrentConfigVersion;
                conf.Save();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        return conf;
    }

    private static void Migrate(Config conf)
    {
        if (conf.ConfigVersion < 2)
        {
            var existingKeys = new HashSet<string>(conf.ExtendControls.Select(x => x.Key));
            var insertIndex = conf.ExtendControls.FindIndex(x => x.Key == "ui_all_stock_day_make");
            if (insertIndex < 0) insertIndex = conf.ExtendControls.Count;

            foreach (var index in StockConfigArray.ImportantIndexs)
            {
                var key = Utils.StockIndexPrefix + index.Code;
                if (!existingKeys.Contains(key))
                {
                    conf.ExtendControls.Insert(insertIndex, new ExtendControlObj(key));
                    insertIndex++;
                }
            }
            Logger.Info($"Config v2: added missing index entries to ExtendControls");
        }
        if (conf.ConfigVersion < 3)
        {
            // 移动 ui_fieldname 从 ExtendControls 到 FieldControls
            conf.ExtendControls.RemoveAll(x => x.Key == "ui_fieldname");
            if (!conf.FieldControls.ContainsKey("ui_fieldname"))
            {
                var newDict = new Dictionary<string, bool> { { "ui_fieldname", false } };
                foreach (var kvp in conf.FieldControls) newDict[kvp.Key] = kvp.Value;
                conf.FieldControls = newDict;
            }
            conf.FieldNewLines ??= new Dictionary<string, bool>();
            if (!conf.FieldNewLines.ContainsKey("ui_fieldname"))
            {
                conf.FieldNewLines["ui_fieldname"] = false;
            }

            // 按市场排序指数：A股 → H股 → 美股
            var indexKeys = new HashSet<string>(
                StockConfigArray.ImportantIndexs.Select(x => Utils.StockIndexPrefix + x.Code));
            var indexItems = conf.ExtendControls
                .Where(x => indexKeys.Contains(x.Key))
                .ToList();
            var otherItems = conf.ExtendControls
                .Where(x => !indexKeys.Contains(x.Key))
                .ToList();
            var orderedIndexItems = StockConfigArray.ImportantIndexs
                .Select(x => indexItems.FirstOrDefault(i => i.Key == Utils.StockIndexPrefix + x.Code)
                         ?? new ExtendControlObj(Utils.StockIndexPrefix + x.Code))
                .ToList();
            var personalIdx = otherItems.FindIndex(x => x.Key.StartsWith("ui_all_"));
            if (personalIdx < 0) personalIdx = otherItems.Count;
            otherItems.InsertRange(personalIdx, orderedIndexItems);
            conf.ExtendControls = otherItems;

            Logger.Info($"Config v3: moved ui_fieldname, reordered indexes");
        }
        if (conf.ConfigVersion < 4)
        {
            conf.FieldNewLines?.Remove("ui_fieldname");
            Logger.Info($"Config v4: removed ui_fieldname from FieldNewLines");
        }
        if (conf.ConfigVersion < 5)
        {
            conf.FieldNewLines = null;
            Logger.Info($"Config v5: removed FieldNewLines (Grid layout no longer needs it)");
        }
        if (conf.ConfigVersion < 6)
        {
            // v6: EnableUS/EnableHK added, default false (no data migration needed)
            Logger.Info($"Config v6: added EnableUS/EnableHK settings");
        }
        if (conf.ConfigVersion < 7)
        {
            // v7: SortField/SortOrder added, default "default"/"desc" (no data migration needed)
            Logger.Info($"Config v7: added SortField/SortOrder settings");
        }
        if (conf.ConfigVersion < 8)
        {
            // v8: BossKey added, default "Ctrl+Oemtilde" (no data migration needed)
            Logger.Info($"Config v8: added BossKey setting");
        }
        Logger.Info($"Config migrated to version {CurrentConfigVersion}");
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
