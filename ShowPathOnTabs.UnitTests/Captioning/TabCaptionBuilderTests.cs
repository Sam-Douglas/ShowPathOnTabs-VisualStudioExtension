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

        Assert.Equal(@"1\Page.razor", Build(tabPaths.First(), tabPaths));
    }

    [Fact]
    public void ThreeDuplicateFilenames_ReturnsCaptionsWithUniqueNames()
    {
        List<string> tabPaths = [
            @"C:\Test\Pages\1\Page.razor",
            @"C:\Test\Pages\2\Page.razor",
            @"C:\Test\Pages\3\Page.razor"];

        var resultCaptions = tabPaths.Select(path => Build(path, tabPaths)).ToList();

        Assert.Equal(resultCaptions.Count, resultCaptions.Distinct().Count());
    }

    [Fact]
    public void DuplicateFilenames_WithMatchingParentFolderNames_ReturnsCorrectCaption()
    {
        List<string> tabPaths = [
            @"C:\Test\Pages\1\Content\Page.razor",
            @"C:\Test\Pages\2\Content\Page.razor"];

        Assert.Equal(@"1\Content\Page.razor", Build(tabPaths.First(), tabPaths));
    }
}
