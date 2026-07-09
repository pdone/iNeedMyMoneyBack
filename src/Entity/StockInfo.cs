using System;

namespace iNeedMyMoneyBack;

/// <summary>
/// 单只证券的行情快照数据模型。
/// 数据来源于腾讯行情接口 <c>qt.gtimg.cn</c>，解析逻辑见 <see cref="Get"/>。
/// 股票代码前缀约定见项目说明（如 sh600519、sz300750、bj899050、usAAPL、hk00700）。
/// </summary>
internal class StockInfo
{
    // ===== 腾讯 qt.gtimg.cn 行情字段索引（0 基） =====
    // 原始响应形如： v_sh600519="1~贵州茅台~600519~1480.00~...~"
    // 即「等号右侧、首尾引号之间」的内容按 '~' 分隔后得到的数组下标。
    // 修改/新增字段时，同步更新此处常量并在 Get() 中赋值即可。
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

    /// <summary>
    /// 将腾讯行情接口的原始响应文本解析为 <see cref="StockInfo"/> 实例。
    /// </summary>
    /// <param name="content">原始响应字符串，形如 <c>v_sh600519="1~贵州茅台~600519~...~"</c>。</param>
    /// <returns>
    /// 解析成功返回实例；字段数不足 <see cref="IDX_PRICE_CHANGE_PERCENT"/> 时返回 <c>null</c>；
    /// 字段数介于 <see cref="IDX_PRICE_CHANGE_PERCENT"/> 与 <see cref="MIN_FIELD_COUNT"/> 之间时，
    /// 返回「精简模式」实例（仅填充基础字段，其余保持默认值）；
    /// 解析过程中抛出的任何异常均被捕获并返回 <c>null</c>。
    /// </returns>
    public static StockInfo Get(string content)
    {
        StockInfo info = new();
        try
        {
            // 去掉开头的 " 及其之前的内容（含等号右侧前缀）
            content = content.Remove(0, content.IndexOf("\"") + 1);
            // 去掉结尾的 " 及其之后的内容，仅保留引号内的字段串
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

    /// <summary>
    /// 股票名称
    /// </summary>
    public string StockName
    {
        get; set;
    }
    /// <summary>
    /// 股票代码（请求接口时需要带前缀，此处是接口返回的，不含前缀，如 600519、300750、AAPL、00700）。
    /// </summary>
    public string StockCode
    {
        get; set;
    }
    /// <summary>
    /// 当前价格（最新成交价）。
    /// </summary>
    public double CurrentPrice
    {
        get; set;
    }
    /// <summary>
    /// 昨收价（前一交易日收盘价）。
    /// </summary>
    public double YesterdayClose
    {
        get; set;
    }
    /// <summary>
    /// 今开盘价。
    /// </summary>
    public double TodayOpen
    {
        get; set;
    }
    /// <summary>
    /// 成交量（单位：手，1 手 = 100 股）。基础字段，精简模式下也可填充。
    /// </summary>
    public long Volume
    {
        get; set;
    }
    /// <summary>
    /// 外盘：主动以卖出价成交的累计成交量（手），反映买盘力量。
    /// </summary>
    public long OuterDisk
    {
        get; set;
    }
    /// <summary>
    /// 内盘：主动以买入价成交的累计成交量（手），反映卖盘力量。
    /// </summary>
    public long InnerDisk
    {
        get; set;
    }
    /// <summary>
    /// 买一价（最高买价）。
    /// </summary>
    public double Buy1
    {
        get; set;
    }
    /// <summary>
    /// 买一量（手）。
    /// </summary>
    public long Buy1Volume
    {
        get; set;
    }
    /// <summary>
    /// 买二价。
    /// </summary>
    public double Buy2
    {
        get; set;
    }
    /// <summary>
    /// 买二量（手）。
    /// </summary>
    public long Buy2Volume
    {
        get; set;
    }
    /// <summary>
    /// 买三价。
    /// </summary>
    public double Buy3
    {
        get; set;
    }
    /// <summary>
    /// 买三量（手）。
    /// </summary>
    public long Buy3Volume
    {
        get; set;
    }
    /// <summary>
    /// 买四价。
    /// </summary>
    public double Buy4
    {
        get; set;
    }
    /// <summary>
    /// 买四量（手）。
    /// </summary>
    public long Buy4Volume
    {
        get; set;
    }
    /// <summary>
    /// 买五价。
    /// </summary>
    public double Buy5
    {
        get; set;
    }
    /// <summary>
    /// 买五量（手）。
    /// </summary>
    public long Buy5Volume
    {
        get; set;
    }
    /// <summary>
    /// 卖一价（最低卖价）。
    /// </summary>
    public double Sell1
    {
        get; set;
    }
    /// <summary>
    /// 卖一量（手）。
    /// </summary>
    public long Sell1Volume
    {
        get; set;
    }
    /// <summary>
    /// 卖二价。
    /// </summary>
    public double Sell2
    {
        get; set;
    }
    /// <summary>
    /// 卖二量（手）。
    /// </summary>
    public long Sell2Volume
    {
        get; set;
    }
    /// <summary>
    /// 卖三价。
    /// </summary>
    public double Sell3
    {
        get; set;
    }
    /// <summary>
    /// 卖三量（手）。
    /// </summary>
    public long Sell3Volume
    {
        get; set;
    }
    /// <summary>
    /// 卖四价。
    /// </summary>
    public double Sell4
    {
        get; set;
    }
    /// <summary>
    /// 卖四量（手）。
    /// </summary>
    public long Sell4Volume
    {
        get; set;
    }
    /// <summary>
    /// 卖五价。
    /// </summary>
    public double Sell5
    {
        get; set;
    }
    /// <summary>
    /// 卖五量（手）。
    /// </summary>
    public long Sell5Volume
    {
        get; set;
    }
    /// <summary>
    /// 最近一笔逐笔成交信息（原始字符串）。
    /// </summary>
    public string RecentTransaction
    {
        get; set;
    }
    /// <summary>
    /// 行情时间戳（格式 yyyyMMddHHmmss）。
    /// </summary>
    public DateTime Time
    {
        get; set;
    }
    /// <summary>
    /// 涨跌额 = 当前价 - 昨收价。
    /// </summary>
    public double PriceChange
    {
        get; set;
    }
    /// <summary>
    /// 涨跌幅（百分比，如 1.23 表示 +1.23%）。
    /// </summary>
    public double PriceChangePercent
    {
        get; set;
    }
    /// <summary>
    /// 当日最高价。基础字段，精简模式下也可填充。
    /// </summary>
    public double HighestPrice
    {
        get; set;
    }
    /// <summary>
    /// 当日最低价。基础字段，精简模式下也可填充。
    /// </summary>
    public double LowestPrice
    {
        get; set;
    }
    /// <summary>
    /// 价格/成交量（手）/成交额 组合串（原始字符串，含三段）。
    /// </summary>
    public string PriceVolumeAmount
    {
        get; set;
    }
    /// <summary>
    /// 成交量（手，扩展字段，完整模式下填充）。
    /// </summary>
    public long VolumeHand
    {
        get; set;
    }
    /// <summary>
    /// 成交额（单位：万元，扩展字段）。
    /// </summary>
    public double Turnover
    {
        get; set;
    }
    /// <summary>
    /// 换手率（百分比，扩展字段）。
    /// </summary>
    public double TurnoverRate
    {
        get; set;
    }
    /// <summary>
    /// 市盈率 TTM（扩展字段）。
    /// </summary>
    public double PE
    {
        get; set;
    }
    /// <summary>
    /// 预留字段，当前 Get 中未赋值，请勿依赖。
    /// </summary>
    public double HighestPrice2_
    {
        get; set;
    }
    /// <summary>
    /// 扩展最高价（扩展字段，完整模式下填充）。
    /// </summary>
    public double HighestPrice2
    {
        get; set;
    }
    /// <summary>
    /// 扩展最低价（扩展字段，完整模式下填充）。
    /// </summary>
    public double LowestPrice2
    {
        get; set;
    }
    /// <summary>
    /// 扩展涨跌幅（扩展字段，完整模式下填充）。
    /// </summary>
    public double PriceChange2
    {
        get; set;
    }
    /// <summary>
    /// 流通市值（单位：元，扩展字段）。
    /// </summary>
    public double CirculationMarketValue
    {
        get; set;
    }
    /// <summary>
    /// 总市值（单位：元，扩展字段）。
    /// </summary>
    public double TotalMarketValue
    {
        get; set;
    }
    /// <summary>
    /// 市净率（扩展字段）。
    /// </summary>
    public double PB
    {
        get; set;
    }
    /// <summary>
    /// 涨停价（扩展字段）。
    /// </summary>
    public double PriceLimitUp
    {
        get; set;
    }
    /// <summary>
    /// 跌停价（扩展字段）。
    /// </summary>
    public double PriceLimitDown
    {
        get; set;
    }
    /// <summary>
    /// 货币类型（如 CNY / USD / HKD，扩展字段）。
    /// </summary>
    public string Currency
    {
        get; set;
    }

    /// <summary>
    /// 是否为 ETF（场内交易型开放式指数基金等）。
    /// 依据代码前缀判定（沪市 51/56/58 开头、深市 15/16 开头），
    /// 名称以 "ETF" 结尾作为兜底，避免名称不带 ETF 后缀的货币/债券/黄金/跨境 ETF 误判。
    /// </summary>
    public bool IsETF
    {
        get
        {
            if (!string.IsNullOrEmpty(StockCode))
            {
                var num = StockCode;
                if (num.StartsWith("51") || num.StartsWith("56") || num.StartsWith("58") ||
                    num.StartsWith("15") || num.StartsWith("16"))
                {
                    Console.WriteLine(num);
                    return true;
                }
            }
            return StockName != null && StockName.Contains("ETF");
        }
    }
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
