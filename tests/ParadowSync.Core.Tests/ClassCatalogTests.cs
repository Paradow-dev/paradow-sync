using ParadowSync.Core.Catalog;

namespace ParadowSync.Core.Tests;

public class ClassCatalogTests
{
    private static readonly string[] ExpectedClasses =
    [
        "Feca", "Osamodas", "Enutrof", "Sram", "Xelor", "Ecaflip", "Eniripsa", "Iop",
        "Cra", "Sadida", "Sacrieur", "Pandawa", "Roublard", "Zobal", "Steamer",
        "Eliotrope", "Huppermage", "Ouginak", "Forgelance"
    ];

    [Fact]
    public void All_contains_all_19_classes()
    {
        var all = ClassCatalog.All;

        Assert.Equal(19, all.Count);

        foreach (var className in ExpectedClasses)
        {
            Assert.Contains(all, c => c.Name == className);
        }
    }

    [Theory]
    [InlineData("iop", "Iop")]
    [InlineData("IOP", "Iop")]
    [InlineData("Eniripsa", "Eniripsa")]
    [InlineData("forgelance", "Forgelance")]
    public void Get_is_case_insensitive(string query, string expectedName)
    {
        var info = ClassCatalog.Get(query);

        Assert.NotNull(info);
        Assert.Equal(expectedName, info.Name);
    }

    [Fact]
    public void Get_returns_null_for_unknown_class()
    {
        Assert.Null(ClassCatalog.Get("NotAClass"));
    }
}
