using System;

namespace iNeedMyMoneyBack;

internal class StockInfo
{
    // 腾讯 qt.gtimg.cn 响应字段索引常量
    private const int IDX_NAME = 1;
    private const int IDX_CODE = 2;
    private const int IDX_CURRENT_PRICE = 3;
    private const int IDX_YESTERDAY_CLOSE = 4;
    private const int IDX_TODAY_OPEN = 5;
    private const int IDX_VOLUME = 6;
    private const int IDX_OUTER_DISK = 7;
    private const int IDX_INNER_DISK = 8;
    private const int IDX_BUY1 = 9;
    private const int IDX_BUY1_VOL = 10;
    private const int IDX_BUY2 = 11;
    private const int IDX_BUY2_VOL = 12;
    private const int IDX_BUY3 = 13;
    private const int IDX_BUY3_VOL = 14;
    private const int IDX_BUY4 = 15;
    private const int IDX_BUY4_VOL = 16;
    private const int IDX_BUY5 = 17;
    private const int IDX_BUY5_VOL = 18;
    private const int IDX_SELL1 = 19;
    private const int IDX_SELL1_VOL = 20;
    private const int IDX_SELL2 = 21;
    private const int IDX_SELL2_VOL = 22;
    private const int IDX_SELL3 = 23;
    private const int IDX_SELL3_VOL = 24;
    private const int IDX_SELL4 = 25;
    private const int IDX_SELL4_VOL = 26;
    private const int IDX_SELL5 = 27;
    private const int IDX_SELL5_VOL = 28;
    private const int IDX_RECENT_TRANSACTION = 29;
    private const int IDX_TIME = 30;
    private const int IDX_PRICE_CHANGE = 31;
    private const int IDX_PRICE_CHANGE_PERCENT = 32;
    private const int IDX_HIGHEST_PRICE = 33;
    private const int IDX_LOWEST_PRICE = 34;
    private const int IDX_PRICE_VOLUME_AMOUNT = 35;
    private const int IDX_VOLUME_HAND = 36;
    private const int IDX_TURNOVER = 37;
    private const int IDX_TURNOVER_RATE = 38;
    private const int IDX_PE = 39;
    private const int IDX_HIGHEST_PRICE2 = 41;
    private const int IDX_LOWEST_PRICE2 = 42;
    private const int IDX_PRICE_CHANGE2 = 43;
    private const int IDX_CIRCULATION_MARKET_VALUE = 44;
    private const int IDX_TOTAL_MARKET_VALUE = 45;
    private const int IDX_PB = 46;
    private const int IDX_PRICE_LIMIT_UP = 47;
    private const int IDX_PRICE_LIMIT_DOWN = 48;
    private const int IDX_CURRENCY = 82;
    private const int MIN_FIELD_COUNT = IDX_CURRENCY + 1;

    public static StockInfo Get(string content)
    {
        StockInfo info = new();
        try
        {
            content = content.Remove(0, content.IndexOf("\"") + 1);
            content = content.Remove(content.IndexOf("\""), content.Length - 1 - content.IndexOf("\""));
            var args = content.Split('~');
            if (args.Length < IDX_PRICE_CHANGE_PERCENT + 1)
            {
                Logger.Debug($"Response field count {args.Length} < {IDX_PRICE_CHANGE_PERCENT + 1}: {content}");
                return null;
            }

            info.StockName = args[IDX_NAME];
            info.StockCode = args[IDX_CODE];
            info.CurrentPrice = Utils.Parse(args[IDX_CURRENT_PRICE]);
            info.YesterdayClose = Utils.Parse(args[IDX_YESTERDAY_CLOSE]);
            info.TodayOpen = Utils.Parse(args[IDX_TODAY_OPEN]);
            info.PriceChange = Utils.Parse(args[IDX_PRICE_CHANGE]);
            info.PriceChangePercent = Utils.Parse(args[IDX_PRICE_CHANGE_PERCENT]);
            info.HighestPrice = args.Length > IDX_HIGHEST_PRICE ? Utils.Parse(args[IDX_HIGHEST_PRICE]) : 0;
            info.LowestPrice = args.Length > IDX_LOWEST_PRICE ? Utils.Parse(args[IDX_LOWEST_PRICE]) : 0;

            if (args.Length < MIN_FIELD_COUNT)
            {
                Logger.Debug($"Response field count {args.Length} < {MIN_FIELD_COUNT} (minimal mode): {content}");
                return info;
            }

            info.Volume = long.Parse(args[IDX_VOLUME]);
            info.OuterDisk = long.Parse(args[IDX_OUTER_DISK]);
            info.InnerDisk = long.Parse(args[IDX_INNER_DISK]);
            info.Buy1 = Utils.Parse(args[IDX_BUY1]);
            info.Buy1Volume = long.Parse(args[IDX_BUY1_VOL]);
            info.Buy2 = Utils.Parse(args[IDX_BUY2]);
            info.Buy2Volume = long.Parse(args[IDX_BUY2_VOL]);
            info.Buy3 = Utils.Parse(args[IDX_BUY3]);
            info.Buy3Volume = long.Parse(args[IDX_BUY3_VOL]);
            info.Buy4 = Utils.Parse(args[IDX_BUY4]);
            info.Buy4Volume = long.Parse(args[IDX_BUY4_VOL]);
            info.Buy5 = Utils.Parse(args[IDX_BUY5]);
            info.Buy5Volume = long.Parse(args[IDX_BUY5_VOL]);
            info.Sell1 = Utils.Parse(args[IDX_SELL1]);
            info.Sell1Volume = long.Parse(args[IDX_SELL1_VOL]);
            info.Sell2 = Utils.Parse(args[IDX_SELL2]);
            info.Sell2Volume = long.Parse(args[IDX_SELL2_VOL]);
            info.Sell3 = Utils.Parse(args[IDX_SELL3]);
            info.Sell3Volume = long.Parse(args[IDX_SELL3_VOL]);
            info.Sell4 = Utils.Parse(args[IDX_SELL4]);
            info.Sell4Volume = long.Parse(args[IDX_SELL4_VOL]);
            info.Sell5 = Utils.Parse(args[IDX_SELL5]);
            info.Sell5Volume = long.Parse(args[IDX_SELL5_VOL]);
            info.RecentTransaction = args[IDX_RECENT_TRANSACTION];
            info.Time = DateTime.ParseExact(args[IDX_TIME], "yyyyMMddHHmmss", null);
            info.PriceVolumeAmount = args[IDX_PRICE_VOLUME_AMOUNT];
            info.VolumeHand = long.Parse(args[IDX_VOLUME_HAND]);
            info.Turnover = Utils.Parse(args[IDX_TURNOVER]);
            info.TurnoverRate = Utils.Parse(args[IDX_TURNOVER_RATE]);
            info.PE = Utils.Parse(args[IDX_PE]);
            info.HighestPrice2 = Utils.Parse(args[IDX_HIGHEST_PRICE2]);
            info.LowestPrice2 = Utils.Parse(args[IDX_LOWEST_PRICE2]);
            info.PriceChange2 = Utils.Parse(args[IDX_PRICE_CHANGE2]);
            info.CirculationMarketValue = Utils.Parse(args[IDX_CIRCULATION_MARKET_VALUE]);
            info.TotalMarketValue = Utils.Parse(args[IDX_TOTAL_MARKET_VALUE]);
            info.PB = Utils.Parse(args[IDX_PB]);
            info.PriceLimitUp = Utils.Parse(args[IDX_PRICE_LIMIT_UP]);
            info.PriceLimitDown = Utils.Parse(args[IDX_PRICE_LIMIT_DOWN]);
            info.Currency = args[IDX_CURRENCY];
        }
        catch (Exception e)
        {
            Logger.Debug(content);
            Logger.Error(e);
            info = null;
        }
        return info;
    }

    public string StockName
    {
        get; set;
    }       // 股票名称
    public string StockCode
    {
        get; set;
    }       // 股票代码
    public double CurrentPrice
    {
        get; set;
    }    // 当前价格
    public double YesterdayClose
    {
        get; set;
    }  // 昨收
    public double TodayOpen
    {
        get; set;
    }       // 今开
    public long Volume
    {
        get; set;
    }            // 成交量（手）
    public long OuterDisk
    {
        get; set;
    }         // 外盘
    public long InnerDisk
    {
        get; set;
    }         // 内盘
    public double Buy1
    {
        get; set;
    }            // 买一
    public long Buy1Volume
    {
        get; set;
    }        // 买一量（手）
    public double Buy2
    {
        get; set;
    }            // 买二
    public long Buy2Volume
    {
        get; set;
    }        // 买二量（手）
    public double Buy3
    {
        get; set;
    }            // 买三
    public long Buy3Volume
    {
        get; set;
    }        // 买三量（手）
    public double Buy4
    {
        get; set;
    }            // 买四
    public long Buy4Volume
    {
        get; set;
    }        // 买四量（手）
    public double Buy5
    {
        get; set;
    }            // 买五
    public long Buy5Volume
    {
        get; set;
    }        // 买五量（手）
    public double Sell1
    {
        get; set;
    }           // 卖一
    public long Sell1Volume
    {
        get; set;
    }       // 卖一量（手）
    public double Sell2
    {
        get; set;
    }           // 卖二
    public long Sell2Volume
    {
        get; set;
    }       // 卖二量（手）
    public double Sell3
    {
        get; set;
    }           // 卖三
    public long Sell3Volume
    {
        get; set;
    }       // 卖三量（手）
    public double Sell4
    {
        get; set;
    }           // 卖四
    public long Sell4Volume
    {
        get; set;
    }       // 卖四量（手）
    public double Sell5
    {
        get; set;
    }           // 卖五
    public long Sell5Volume
    {
        get; set;
    }       // 卖五量（手）
    public string RecentTransaction
    {
        get; set;
    } // 最近逐笔成交
    public DateTime Time
    {
        get; set;
    }          // 时间
    public double PriceChange
    {
        get; set;
    }     // 涨跌
    public double PriceChangePercent
    {
        get; set;
    }  // 涨跌%
    public double HighestPrice
    {
        get; set;
    }    // 最高
    public double LowestPrice
    {
        get; set;
    }     // 最低
    public string PriceVolumeAmount
    {
        get; set;
    }   // 价格/成交量（手）/成交额
    public long VolumeHand
    {
        get; set;
    }        // 成交量（手）
    public double Turnover
    {
        get; set;
    }        // 成交额（万）
    public double TurnoverRate
    {
        get; set;
    }    // 换手率
    public double PE
    {
        get; set;
    }              // 市盈率
    public double HighestPrice2_
    {
        get; set;
    }    // 最高
    public double HighestPrice2
    {
        get; set;
    }    // 最高
    public double LowestPrice2
    {
        get; set;
    }     // 最低
    public double PriceChange2
    {
        get; set;
    }     // 涨幅
    public double CirculationMarketValue
    {
        get; set;
    } // 流通市值
    public double TotalMarketValue
    {
        get; set;
    }     // 总市值
    public double PB
    {
        get; set;
    }              // 市净率
    public double PriceLimitUp
    {
        get; set;
    }    // 涨停价
    public double PriceLimitDown
    {
        get; set;
    }  // 跌停价
    public string Currency
    {
        get; set;
    }        // 货币类型
}

/// <summary>
/// 字段对齐方式
/// </summary>
internal enum FieldAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// 字段值（用于 Grid 显示）
/// </summary>
internal class FieldValue
{
    public string Key { get; set; }
    public string Value { get; set; }
    public FieldAlignment Alignment { get; set; } = FieldAlignment.Right;

    public FieldValue(string key, string value, FieldAlignment alignment = FieldAlignment.Right)
    {
        Key = key;
        Value = value;
        Alignment = alignment;
    }
}
