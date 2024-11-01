using System.Collections.Generic;

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
    public string Code
    {
        get; set;
    }
    public string Name
    {
        get; set;
    }
    public string NickName
    {
        get; set;
    }
    public double BuyPrice
    {
        get; set;
    }
    public int BuyCount
    {
        get; set;
    }
    public string DiaplayName => string.IsNullOrWhiteSpace(NickName) ? Name : NickName;

    public double DayMake
    {
        get; set;
    }
    public double AllMake
    {
        get; set;
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
    /// 暗色模式
    /// </summary>
    public bool DarkMode { get; set; } = false;
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
    public bool DataRoll { get; set; } = true;
    /// <summary>
    /// 透明度
    /// </summary>
    public double Opacity { get; set; } = 0.8;
    public double Left { get; set; } = 200;
    public double Top { get; set; } = 200;
    public double Width { get; set; } = 180;
    public double Height { get; set; } = 26;
    public double ConfigWindowWidth { get; set; } = 200;
    public double ConfigWindowHeight { get; set; } = 165;
    public List<StockConfig> Stocks { get; set; } = [new StockConfig("sh000001", "上证指数")];

    public Dictionary<string, bool> FieldControls
    {
        get; set;
    } = new()
    {
        {"ui_price",true},
        {"ui_change",true},
        {"ui_cost",true},
        {"ui_num",true},
        {"ui_day_make",true},
        {"ui_all_make",true},
        {"ui_yesterday_todayopen",true},
        {"ui_lowest_highest",true},
        {"ui_limitup_limitdown",true},
    };
    public List<ExtendControlObj> ExtendControls
    {
        get; set;
    } =
    [
        new ExtendControlObj("ui_fieldname"),
        new ExtendControlObj("ui_all_stock_day_make"),
        new ExtendControlObj("ui_all_stock_all_make"),
    ];
}

public class ExtendControlObj(string key)
{
    public static string NewlineSuffix => "_newline";
    public string GetNewLineKey() => Key + NewlineSuffix;

    public string Key { get; set; } = key;
    public bool Visable { get; set; } = true;
    public bool NewLine { get; set; } = true;
}
