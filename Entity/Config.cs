using System.Collections.Generic;

namespace iNeedMyMoneyBack
{
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
    }

    public class Config
    {
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
        /// 语言 当前仅支持：zh_CN 和 en
        /// </summary>
        public string Lang { get; set; } = "zh_CN";
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

        public List<StockConfig> Stocks { get; set; } = new List<StockConfig>();
    }
}
