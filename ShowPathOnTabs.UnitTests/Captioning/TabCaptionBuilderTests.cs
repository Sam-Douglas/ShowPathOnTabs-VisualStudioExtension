using static ShowPathOnTabs.Core.Captioning.TabCaptionBuilder;

namespace ShowPathOnTabs.UnitTests.Captioning;

public class TabCaptionBuilderTests
{
    [Fact]
    public void UniqueFilename_ReturnsUnmodified_Caption()
    {
        List<string> tabPaths = [
            @"C:\Test\Pages\Page.razor",
            @"C:\Test\Pages\DifferentPage.razor"];

        Assert.Equal("Page.razor", Build(tabPaths.First(), tabPaths));
    }

    [Fact]
    public void TwoDuplicateFilenames_ReturnsCaptionWithParentFolder()
    {
        List<string> tabPaths = [
            @"C:\Test\Pages\1\Page.razor",
            @"C:\Test\Pages\2\Page.razor"];

        Assert.Equal("1/Page.razor", Build(tabPaths.First(), tabPaths));
    }
}
