using nlwc.ems.Services.Options;

namespace nlwc.ems.Tests.Services;

public class SecurityOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]   // right length, not hex
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]   // 66 characters
    public void A_bad_master_key_is_rejected(string key) => SecurityOptions.IsValidMasterKey(key).Should().BeFalse();

    [Fact]
    public void Sixty_four_hex_characters_are_accepted()
        => SecurityOptions.IsValidMasterKey(new string('a', 32) + new string('F', 32)).Should().BeTrue();

    [Fact]
    public void A_missing_key_is_rejected() => SecurityOptions.IsValidMasterKey(null).Should().BeFalse();
}
