using System.Net;
using FoodDiary.API.Mcp.Tools.Products;

namespace FoodDiary.UnitTests.Mcp;

public class PhotoDownloaderTests
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("172.32.0.1")]
    [InlineData("2606:4700:4700::1111")]
    public void Public_address_can_be_downloaded_from(string address)
    {
        PhotoDownloader.IsPublic(IPAddress.Parse(address)).Should().BeTrue();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("10.1.2.3")]
    [InlineData("100.64.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    public void Private_address_cannot_be_downloaded_from(string address)
    {
        PhotoDownloader.IsPublic(IPAddress.Parse(address)).Should().BeFalse();
    }
}
