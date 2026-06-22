using ParadowSync.Core.Catalog;

namespace ParadowSync.Core.Tests;

public class IconLoaderTests
{
    [Fact]
    public void Loads_all_19_class_icons()
    {
        var loader = new IconLoader();

        Assert.Equal(19, loader.LoadedIconCount);
    }

    [Fact]
    public void GetIconBytes_returns_bytes_for_known_class()
    {
        var loader = new IconLoader();

        var bytes = loader.GetIconBytes("Iop");

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
    }

    [Fact]
    public void GetIconBytes_is_case_insensitive()
    {
        var loader = new IconLoader();

        var upper = loader.GetIconBytes("IOP");
        var lower = loader.GetIconBytes("iop");

        Assert.NotNull(upper);
        Assert.NotNull(lower);
        Assert.Equal(upper, lower);
    }

    [Fact]
    public void GetIconBytes_returns_null_for_unknown_class()
    {
        var loader = new IconLoader();

        Assert.Null(loader.GetIconBytes("NotAClass"));
    }
}
