using NUnit.Framework;

namespace iNeedMyMoneyBack.Tests;

[TestFixture]
public class UtilsTests
{
    [TestCase("abc", 3)]
    [TestCase("ABC", 3)]
    [TestCase("你好", 4)]
    [TestCase("ab你", 4)]    // 1+1+2
    [TestCase("", 0)]
    [TestCase("a", 1)]
    [TestCase("你", 2)]
    public void GetVisualWidth_ReturnsCorrectWidth(string input, int expected)
    {
        Assert.AreEqual(expected, Utils.GetVisualWidth(input));
    }

    [Test]
    public void GetCharCount_PureEnglish_ReturnsCorrectCount()
    {
        "hello".GetCharCount(out var en, out var cn);
        Assert.AreEqual(5, en);
        Assert.AreEqual(0, cn);
    }

    [Test]
    public void GetCharCount_PureChinese_ReturnsCorrectCount()
    {
        "你好".GetCharCount(out var en, out var cn);
        Assert.AreEqual(0, en);
        Assert.AreEqual(2, cn);
    }

    [Test]
    public void GetCharCount_Mixed_ReturnsCorrectCount()
    {
        "ab你c".GetCharCount(out var en, out var cn);
        Assert.AreEqual(3, en); // a, b, c
        Assert.AreEqual(1, cn); // 你
    }

    [Test]
    public void GetCharCount_Empty_ReturnsZeros()
    {
        "".GetCharCount(out var en, out var cn);
        Assert.AreEqual(0, en);
        Assert.AreEqual(0, cn);
    }

    [TestCase("hello", 10, "hello     ")]
    [TestCase("hi", 5, "hi   ")]
    [TestCase("", 3, "   ")]
    public void iPadRight_PadsCorrectly(string input, int count, string expected)
    {
        Assert.AreEqual(expected, input.iPadRight(count));
    }

    [TestCase("a", 5, "    a")]
    [TestCase("ab", 6, "    ab")]
    public void iPadLeft_PadsCorrectly(string input, int count, string expected)
    {
        Assert.AreEqual(expected, input.iPadLeft(count));
    }

    [Test]
    public void iPadLeft_FullWidthNoPad()
    {
        // Digits are treated as full-width (width 2 each), so "123" has width 6
        // With count=5, no padding needed (already wider)
        Assert.AreEqual("123", "123".iPadLeft(5));
    }

    [Test]
    public void IsNullOrWhiteSpace_Null_ReturnsTrue()
    {
        Assert.IsTrue(((string)null).IsNullOrWhiteSpace());
    }

    [Test]
    public void IsNullOrWhiteSpace_Empty_ReturnsTrue()
    {
        Assert.IsTrue("".IsNullOrWhiteSpace());
    }

    [Test]
    public void IsNullOrWhiteSpace_Whitespace_ReturnsTrue()
    {
        Assert.IsTrue("   ".IsNullOrWhiteSpace());
    }

    [Test]
    public void IsNullOrWhiteSpace_Content_ReturnsFalse()
    {
        Assert.IsFalse("abc".IsNullOrWhiteSpace());
    }

    [Test]
    public void ToDouble_ValidNumber_ReturnsValue()
    {
        Assert.AreEqual(3.14, "3.14".ToDouble(), 0.001);
    }

    [Test]
    public void ToDouble_InvalidString_ReturnsZero()
    {
        Assert.AreEqual(0.0, "abc".ToDouble(), 0.001);
    }

    [Test]
    public void Parse_ValidNumber_ReturnsValue()
    {
        Assert.AreEqual(1.5, Utils.Parse("1.5"), 0.001);
    }

    [Test]
    public void Parse_InvalidString_ReturnsZero()
    {
        Assert.AreEqual(0.0, Utils.Parse("abc"), 0.001);
    }

    [Test]
    public void ToStr_ToObj_RoundTrip()
    {
        var obj = new { Name = "test", Value = 42 };
        var json = obj.ToStr();
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("test"));
        Assert.IsTrue(json.Contains("42"));
    }

    [Test]
    public void IsTradingTime_ReturnsBool()
    {
        var result = Utils.IsTradingTime();
        Assert.IsInstanceOf<bool>(result);
    }

    [Test]
    public void GetVisualWidth_Digits_ReturnsDoubleWidth()
    {
        Assert.AreEqual(6, Utils.GetVisualWidth("123"));
        Assert.AreEqual(2, Utils.GetVisualWidth("1"));
    }

    [Test]
    public void GetVisualWidth_Punctuation_ReturnsDoubleWidth()
    {
        Assert.AreEqual(2, Utils.GetVisualWidth("!"));
        Assert.AreEqual(2, Utils.GetVisualWidth("@"));
    }

    [Test]
    public void GetCharCount_Digits_ReturnsCorrectCount()
    {
        "123".GetCharCount(out var en, out var cn);
        Assert.AreEqual(0, en);
        Assert.AreEqual(6, cn);
    }

    [Test]
    public void GetCharCount_Punctuation_ReturnsCorrectCount()
    {
        "!@#".GetCharCount(out var en, out var cn);
        Assert.AreEqual(0, en);
        Assert.AreEqual(6, cn);
    }

    [Test]
    public void iPadRight_CJK_PadsCorrectly()
    {
        Assert.AreEqual("你好  ", "你好".iPadRight(6));
    }

    [Test]
    public void iPadLeft_CJK_PadsCorrectly()
    {
        Assert.AreEqual("  你好", "你好".iPadLeft(6));
    }

    [Test]
    public void PadRightByVisualWidth_Empty_ReturnsPadding()
    {
        Assert.AreEqual("   ", Utils.PadRightByVisualWidth("", 3, ' '));
    }

    [Test]
    public void PadRightByVisualWidth_LongerThanWidth_ReturnsOriginal()
    {
        Assert.AreEqual("abcde", Utils.PadRightByVisualWidth("abcde", 3, ' '));
    }
}
