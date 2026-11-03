using nlwc.ems.Data.EFCore;

namespace nlwc.ems.Tests.Services;

public class ConnectionStringsTests
{
    [Theory]
    [InlineData("Server=h;SslMode=Require;User=u", "Server=h;SslMode=Required;User=u")]
    [InlineData("Server=h;User=u;ssl-mode=require", "Server=h;User=u;ssl-mode=Required")]
    [InlineData("Server=h;SslMode=Disabled", "Server=h;SslMode=None")]
    [InlineData("Server=h;SslMode=Required", "Server=h;SslMode=Required")]
    [InlineData("Server=h;SslMode=VerifyFull", "Server=h;SslMode=VerifyFull")]
    [InlineData("Server=h;Password=Require;User=u", "Server=h;Password=Require;User=u")]
    public void Spells_the_ssl_mode_the_way_MySqlConnector_expects(string input, string expected)
        => ConnectionStrings.Normalize(input).Should().Be(expected);

    [Fact]
    public void Leaves_a_missing_string_alone() => ConnectionStrings.Normalize(null).Should().BeNull();
}
