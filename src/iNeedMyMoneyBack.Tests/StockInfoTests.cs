using NUnit.Framework;
using System;

namespace iNeedMyMoneyBack.Tests;

[TestFixture]
public class StockInfoTests
{
    private static string BuildFields(string prefix, string code, string name,
        string currentPrice, string yesterdayClose, string todayOpen,
        string volume, string priceChange, string priceChangePercent,
        double? buy1 = null, double? sell1 = null, string currency = "CNY")
    {
        var fields = new string[83];
        fields[0] = "";
        fields[1] = name;
        fields[2] = code;
        fields[3] = currentPrice;
        fields[4] = yesterdayClose;
        fields[5] = todayOpen;
        fields[6] = volume;
        fields[7] = "0";
        fields[8] = "0";
        fields[9] = (buy1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[10] = "100";
        fields[11] = (buy1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[12] = "200";
        fields[13] = (buy1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[14] = "300";
        fields[15] = (buy1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[16] = "400";
        fields[17] = (buy1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[18] = "500";
        fields[19] = (sell1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[20] = "100";
        fields[21] = (sell1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[22] = "200";
        fields[23] = (sell1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[24] = "300";
        fields[25] = (sell1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[26] = "400";
        fields[27] = (sell1 ?? Utils.Parse(currentPrice)).ToString("F2");
        fields[28] = "500";
        fields[29] = "";
        fields[30] = "20250526111025";
        fields[31] = priceChange;
        fields[32] = priceChangePercent;
        var highest = (Utils.Parse(currentPrice) + 10).ToString("F2");
        var lowest = (Utils.Parse(currentPrice) - 15).ToString("F2");
        fields[33] = highest;
        fields[34] = lowest;
        fields[35] = $"{currentPrice}/{volume}/0";
        fields[36] = volume;
        fields[37] = "0";
        fields[38] = "0";
        fields[39] = "0";
        fields[40] = "";
        fields[41] = highest;
        fields[42] = lowest;
        fields[43] = priceChange;
        fields[44] = "2250000.00";
        fields[45] = "3500000.00";
        fields[46] = "0";
        var limitUp = (Utils.Parse(yesterdayClose) * 1.1).ToString("F2");
        var limitDown = (Utils.Parse(yesterdayClose) * 0.9).ToString("F2");
        fields[47] = limitUp;
        fields[48] = limitDown;
        for (var i = 49; i < 82; i++)
        {
            fields[i] = "";
        }

        fields[82] = currency;

        var inner = string.Join("~", fields);
        return $"v_{prefix}{code}=\"{inner}\"";
    }

    private static string BuildASampleShanghai()
    {
        return BuildFields("sh", "600519", "贵州茅台",
            "1800.00", "1790.00", "1795.00", "12345",
            "10.00", "0.56", buy1: 1799.50, sell1: 1800.50);
    }

    private static string BuildASampleShenzhen()
    {
        return BuildFields("sz", "300750", "宁德时代",
            "250.50", "245.00", "248.00", "56789",
            "5.50", "2.24", buy1: 250.40, sell1: 250.60);
    }

    private static string BuildHSampleIndex()
    {
        return BuildFields("hk", "HSI", "恒生指数",
            "22500.00", "22350.00", "22400.00", "98765",
            "150.00", "0.67", buy1: 22499.00, sell1: 22501.00, currency: "HKD");
    }

    private static string BuildHSampleStock()
    {
        return BuildFields("hk", "00700", "腾讯控股",
            "425.00", "420.00", "422.00", "34567",
            "5.00", "1.19", buy1: 424.80, sell1: 425.20, currency: "HKD");
    }

    [Test]
    public void Get_ValidAStock_ReturnsStockInfo()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("贵州茅台", info.StockName);
        Assert.AreEqual("600519", info.StockCode);
        Assert.AreEqual(1800.00, info.CurrentPrice, 0.001);
        Assert.AreEqual(1790.00, info.YesterdayClose, 0.001);
        Assert.AreEqual(1795.00, info.TodayOpen, 0.001);
        Assert.AreEqual(12345, info.Volume);
        Assert.AreEqual(10.00, info.PriceChange, 0.001);
        Assert.AreEqual(0.56, info.PriceChangePercent, 0.001);
        Assert.AreEqual(1810.00, info.HighestPrice, 0.001);
        Assert.AreEqual(1785.00, info.LowestPrice, 0.001);
    }

    [Test]
    public void Get_TooFewFields_ReturnsNull()
    {
        var content = "v_sh000001=\"1~上证指数~000001~3000.00\"";
        var info = StockInfo.Get(content);
        Assert.IsNull(info);
    }

    [Test]
    public void Get_EmptyQuotes_ReturnsNull()
    {
        var info = StockInfo.Get("v_sh000001=\"\"");
        Assert.IsNull(info);
    }

    [Test]
    public void Get_TimeField_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(new DateTime(2025, 5, 26, 11, 10, 25), info.Time);
    }

    [Test]
    public void Get_BuySellFields_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(1799.50, info.Buy1, 0.001);
        Assert.AreEqual(100, info.Buy1Volume);
        Assert.AreEqual(1799.50, info.Buy5, 0.001);
        Assert.AreEqual(500, info.Buy5Volume);
        Assert.AreEqual(1800.50, info.Sell1, 0.001);
        Assert.AreEqual(100, info.Sell1Volume);
        Assert.AreEqual(1800.50, info.Sell5, 0.001);
        Assert.AreEqual(500, info.Sell5Volume);
    }

    [Test]
    public void Get_MarketValueFields_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(2250000.00, info.CirculationMarketValue, 0.001);
        Assert.AreEqual(3500000.00, info.TotalMarketValue, 0.001);
    }

    [Test]
    public void Get_LimitUpDown_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(1969.00, info.PriceLimitUp, 0.001);
        Assert.AreEqual(1611.00, info.PriceLimitDown, 0.001);
    }

    [Test]
    public void Get_AStockShenzhen_ParsedCorrectly()
    {
        var content = BuildASampleShenzhen();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("宁德时代", info.StockName);
        Assert.AreEqual("300750", info.StockCode);
        Assert.AreEqual(250.50, info.CurrentPrice, 0.001);
        Assert.AreEqual(245.00, info.YesterdayClose, 0.001);
        Assert.AreEqual(248.00, info.TodayOpen, 0.001);
        Assert.AreEqual(56789, info.Volume);
        Assert.AreEqual(5.50, info.PriceChange, 0.001);
        Assert.AreEqual(2.24, info.PriceChangePercent, 0.001);
    }

    [Test]
    public void Get_HStockIndex_ParsedCorrectly()
    {
        var content = BuildHSampleIndex();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("恒生指数", info.StockName);
        Assert.AreEqual("HSI", info.StockCode);
        Assert.AreEqual(22500.00, info.CurrentPrice, 0.001);
        Assert.AreEqual(22350.00, info.YesterdayClose, 0.001);
        Assert.AreEqual(22400.00, info.TodayOpen, 0.001);
        Assert.AreEqual(98765, info.Volume);
        Assert.AreEqual(150.00, info.PriceChange, 0.001);
        Assert.AreEqual(0.67, info.PriceChangePercent, 0.001);
    }

    [Test]
    public void Get_HStockIndividual_ParsedCorrectly()
    {
        var content = BuildHSampleStock();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("腾讯控股", info.StockName);
        Assert.AreEqual("00700", info.StockCode);
        Assert.AreEqual(425.00, info.CurrentPrice, 0.001);
        Assert.AreEqual(420.00, info.YesterdayClose, 0.001);
        Assert.AreEqual(422.00, info.TodayOpen, 0.001);
        Assert.AreEqual(34567, info.Volume);
        Assert.AreEqual(5.00, info.PriceChange, 0.001);
        Assert.AreEqual(1.19, info.PriceChangePercent, 0.001);
    }

    [Test]
    public void Get_HStockCurrency_HKD()
    {
        var content = BuildHSampleStock();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.IsTrue(info.Currency.Contains("HKD"));
    }

    [Test]
    public void Get_AStockCurrency_CNY()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.IsTrue(info.Currency.Contains("CNY"));
    }

    [Test]
    public void Get_HStock_HasBuySellData()
    {
        var content = BuildHSampleStock();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(424.80, info.Buy1, 0.001);
        Assert.AreEqual(425.20, info.Sell1, 0.001);
        Assert.AreEqual(100, info.Buy1Volume);
        Assert.AreEqual(100, info.Sell1Volume);
    }

    [Test]
    public void Get_AllBuySellFields_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        // Buy1-5
        Assert.AreEqual(1799.50, info.Buy1, 0.001);
        Assert.AreEqual(100, info.Buy1Volume);
        Assert.AreEqual(1799.50, info.Buy2, 0.001);
        Assert.AreEqual(200, info.Buy2Volume);
        Assert.AreEqual(1799.50, info.Buy3, 0.001);
        Assert.AreEqual(300, info.Buy3Volume);
        Assert.AreEqual(1799.50, info.Buy4, 0.001);
        Assert.AreEqual(400, info.Buy4Volume);
        Assert.AreEqual(1799.50, info.Buy5, 0.001);
        Assert.AreEqual(500, info.Buy5Volume);
        // Sell1-5
        Assert.AreEqual(1800.50, info.Sell1, 0.001);
        Assert.AreEqual(100, info.Sell1Volume);
        Assert.AreEqual(1800.50, info.Sell2, 0.001);
        Assert.AreEqual(200, info.Sell2Volume);
        Assert.AreEqual(1800.50, info.Sell3, 0.001);
        Assert.AreEqual(300, info.Sell3Volume);
        Assert.AreEqual(1800.50, info.Sell4, 0.001);
        Assert.AreEqual(400, info.Sell4Volume);
        Assert.AreEqual(1800.50, info.Sell5, 0.001);
        Assert.AreEqual(500, info.Sell5Volume);
    }

    [Test]
    public void Get_VolumeFields_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(12345, info.Volume);
        Assert.AreEqual(0, info.OuterDisk);
        Assert.AreEqual(0, info.InnerDisk);
        Assert.AreEqual(12345, info.VolumeHand);
    }

    [Test]
    public void Get_MarketDataFields_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("", info.RecentTransaction);
        Assert.AreEqual("1800.00/12345/0", info.PriceVolumeAmount);
        Assert.AreEqual(0, info.Turnover, 0.001);
        Assert.AreEqual(0, info.TurnoverRate, 0.001);
        Assert.AreEqual(0, info.PE, 0.001);
        Assert.AreEqual(0, info.PB, 0.001);
    }

    [Test]
    public void Get_SecondHighestLowestPrice_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(1810.00, info.HighestPrice2, 0.001);
        Assert.AreEqual(1785.00, info.LowestPrice2, 0.001);
        Assert.AreEqual(10.00, info.PriceChange2, 0.001);
    }

    [Test]
    public void Get_ExactMinFieldCount_ReturnsFullInfo()
    {
        var content = BuildFields("sh", "600000", "测试股票",
            "10.00", "9.50", "9.80", "1000",
            "0.50", "5.26", buy1: 9.99, sell1: 10.01);
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("测试股票", info.StockName);
        Assert.AreEqual(10.00, info.CurrentPrice, 0.001);
        Assert.AreEqual(1000, info.Volume);
        Assert.AreEqual(9.99, info.Buy1, 0.001);
        Assert.AreEqual(10.01, info.Sell1, 0.001);
    }

    [Test]
    public void Get_PriceVolumeAmount_ParsedCorrectly()
    {
        var content = BuildASampleShanghai();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("1800.00/12345/0", info.PriceVolumeAmount);
    }

    [Test]
    public void Get_HStock_MarketValues()
    {
        var content = BuildHSampleStock();
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(2250000.00, info.CirculationMarketValue, 0.001);
        Assert.AreEqual(3500000.00, info.TotalMarketValue, 0.001);
    }

    [Test]
    public void Get_InvalidContent_ReturnsNull()
    {
        var info = StockInfo.Get("invalid content");
        Assert.IsNull(info);
    }

    [Test]
    public void Get_NullContent_ReturnsNull()
    {
        var info = StockInfo.Get("");
        Assert.IsNull(info);
    }

    [Test]
    public void Get_MinimalFields_ReturnsBasicInfo()
    {
        var fields = new string[35];
        fields[0] = "";
        fields[1] = "测试股票";
        fields[2] = "600000";
        fields[3] = "10.00";
        fields[4] = "9.50";
        fields[5] = "9.80";
        fields[6] = "1000";
        fields[7] = "0";
        fields[8] = "0";
        for (var i = 9; i < 30; i++)
        {
            fields[i] = "";
        }

        fields[30] = "20250526120000";
        fields[31] = "0.50";
        fields[32] = "5.26";
        fields[33] = "10.50";
        fields[34] = "9.50";

        var inner = string.Join("~", fields);
        var content = $"v_sh600000=\"{inner}\"";
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("测试股票", info.StockName);
        Assert.AreEqual("600000", info.StockCode);
        Assert.AreEqual(10.00, info.CurrentPrice, 0.001);
        Assert.AreEqual(9.50, info.YesterdayClose, 0.001);
        Assert.AreEqual(0.50, info.PriceChange, 0.001);
        Assert.AreEqual(5.26, info.PriceChangePercent, 0.001);
    }

    [Test]
    public void Get_PriceChangePercent_Negative()
    {
        var content = BuildFields("sh", "601318", "中国平安",
            "45.00", "47.00", "46.50", "88888",
            "-2.00", "-4.26");
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual("中国平安", info.StockName);
        Assert.AreEqual(45.00, info.CurrentPrice, 0.001);
        Assert.AreEqual(-2.00, info.PriceChange, 0.001);
        Assert.AreEqual(-4.26, info.PriceChangePercent, 0.001);
    }

    [Test]
    public void Get_USIndex_ReturnsStockInfo()
    {
        // 模拟美股指数响应（字段数量71，时间格式不同）
        var content = "v_usDJI=\"200~道琼斯~.DJI~50644.28~50461.68~50487.16~503582323~0~0~50553.53~0~0~0~0~0~0~0~0~0~50806.14~0~0~0~0~0~0~0~0~0~~2026-05-27 16:43:39~182.60~0.36~50830.41~50487.16~USD~503582323~25511941570709~~~~~~0.68~~~Dow Jones~~50830.41~41828.35~0~~~~5.37~2.59~ZS~~~1.78~3.06~3.56~~~0.98~~~50660.92~~~\"";
        var info = StockInfo.Get(content);

        Assert.IsNotNull(info);
        Assert.AreEqual(".DJI", info.StockCode);
        Assert.AreEqual(50644.28, info.CurrentPrice, 0.001);
        Assert.AreEqual(50461.68, info.YesterdayClose, 0.001);
        Assert.AreEqual(182.60, info.PriceChange, 0.001);
        Assert.AreEqual(0.36, info.PriceChangePercent, 0.001);
    }
}
