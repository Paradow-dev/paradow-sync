namespace ParadowSync.Core.Catalog;

public static class ClassCatalog
{
    private static readonly Dictionary<string, ClassInfo> Classes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Feca"] = new("Feca", "#1565C0", "icons/feca.png"),
        ["Osamodas"] = new("Osamodas", "#6A1B9A", "icons/osamodas.png"),
        ["Enutrof"] = new("Enutrof", "#F9A825", "icons/enutrof.png"),
        ["Sram"] = new("Sram", "#4A148C", "icons/sram.png"),
        ["Xelor"] = new("Xelor", "#00838F", "icons/xelor.png"),
        ["Ecaflip"] = new("Ecaflip", "#AD1457", "icons/ecaflip.png"),
        ["Eniripsa"] = new("Eniripsa", "#2E7D32", "icons/eniripsa.png"),
        ["Iop"] = new("Iop", "#C62828", "icons/iop.png"),
        ["Cra"] = new("Cra", "#33691E", "icons/cra.png"),
        ["Sadida"] = new("Sadida", "#558B2F", "icons/sadida.png"),
        ["Sacrieur"] = new("Sacrieur", "#B71C1C", "icons/sacrieur.png"),
        ["Pandawa"] = new("Pandawa", "#0277BD", "icons/pandawa.png"),
        ["Roublard"] = new("Roublard", "#4E342E", "icons/roublard.png"),
        ["Zobal"] = new("Zobal", "#880E4F", "icons/zobal.png"),
        ["Steamer"] = new("Steamer", "#455A64", "icons/steamer.png"),
        ["Eliotrope"] = new("Eliotrope", "#283593", "icons/eliotrope.png"),
        ["Huppermage"] = new("Huppermage", "#E65100", "icons/huppermage.png"),
        ["Ouginak"] = new("Ouginak", "#BF360C", "icons/ouginak.png"),
        ["Forgelance"] = new("Forgelance", "#37474F", "icons/forgelance.png"),
    };

    public static ClassInfo? Get(string className) =>
        Classes.TryGetValue(className, out var info) ? info : null;

    public static IReadOnlyList<ClassInfo> All => Classes.Values.ToList();
}

public sealed record ClassInfo(string Name, string ColorHex, string IconPath);
