using System;

namespace iNeedMyMoneyBack;

internal class StockInfo
{
    public static StockInfo Get(string content)
    {
        StockInfo info = new();
        try
        {
            content = content.Remove(0, content.IndexOf("\"") + 1);
            content = content.Remove(content.IndexOf("\""), content.Length - 1 - content.IndexOf("\""));
            var args = content.Split('~');
            info.StockName = args[1];
            info.StockCode = args[2];
            info.CurrentPrice = Utils.Parse(args[3]);
            info.YesterdayClose = Utils.Parse(args[4]);
            info.TodayOpen = Utils.Parse(args[5]);
            info.Volume = long.Parse(args[6]);
            info.OuterDisk = long.Parse(args[7]);
            info.InnerDisk = long.Parse(args[8]);
            info.Buy1 = Utils.Parse(args[9]);
            info.Buy1Volume = long.Parse(args[10]);
            info.Buy2 = Utils.Parse(args[11]);
            info.Buy2Volume = long.Parse(args[12]);
            info.Buy3 = Utils.Parse(args[13]);
            info.Buy3Volume = long.Parse(args[14]);
            info.Buy4 = Utils.Parse(args[15]);
            info.Buy4Volume = long.Parse(args[16]);
            info.Buy5 = Utils.Parse(args[17]);
            info.Buy5Volume = long.Parse(args[18]);
            info.Sell1 = Utils.Parse(args[19]);
            info.Sell1Volume = long.Parse(args[20]);
            info.Sell2 = Utils.Parse(args[21]);
            info.Sell2Volume = long.Parse(args[22]);
            info.Sell3 = Utils.Parse(args[23]);
            info.Sell3Volume = long.Parse(args[24]);
            info.Sell4 = Utils.Parse(args[25]);
            info.Sell4Volume = long.Parse(args[26]);
            info.Sell5 = Utils.Parse(args[27]);
            info.Sell5Volume = long.Parse(args[28]);
            info.RecentTransaction = args[29];
            info.Time = DateTime.ParseExact(args[30], "yyyyMMddHHmmss", null);
            info.PriceChange = Utils.Parse(args[31]);
            info.PriceChangePercent = Utils.Parse(args[32]);
            info.HighestPrice = Utils.Parse(args[33]);
            info.LowestPrice = Utils.Parse(args[34]);
            info.PriceVolumeAmount = args[35];
            info.VolumeHand = long.Parse(args[36]);
            info.Turnover = Utils.Parse(args[37]);
            info.TurnoverRate = Utils.Parse(args[38]);
            info.PE = Utils.Parse(args[39]);
            //info.HighestPrice2_ = Utils.Parse(args[40]);
            info.HighestPrice2 = Utils.Parse(args[41]);
            info.LowestPrice2 = Utils.Parse(args[42]);
            info.PriceChange2 = Utils.Parse(args[43]);
            info.CirculationMarketValue = Utils.Parse(args[44]);
            info.TotalMarketValue = Utils.Parse(args[45]);
            info.PB = Utils.Parse(args[46]);
            info.PriceLimitUp = Utils.Parse(args[47]);
            info.PriceLimitDown = Utils.Parse(args[48]);
            info.Currency = args[82];
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
