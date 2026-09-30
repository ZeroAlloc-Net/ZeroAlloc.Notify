using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Notify.Generator;

/// <summary>
/// Diagnostics of the Notify generator. Each one means that nothing is generated for the class
/// it is reported on; every other class is still generated.
/// </summary>
internal static class NotifyDiagnostics
{
    private const string Category = "ZeroAlloc.Notify";
    private const string HelpLink = "https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/diagnostics.md#";

    /// <summary>
    /// A nested class whose containing type is not <c>partial</c>. The generated file has to
    /// reopen every containing type, which only a partial type allows.
    /// </summary>
    public static readonly DiagnosticDescriptor ContainingTypeNotPartial = new(
        id: "ZAN001",
        title: "Containing type of a Notify class is not partial",
        messageFormat: "The notification members of class '{0}' are not generated because its containing type '{1}' is not partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "zan001");

    /// <summary>
    /// A file-local class, or one nested in a file-local type. A file-local type is visible only
    /// in its own file, so a generated file cannot extend it.
    /// </summary>
    public static readonly DiagnosticDescriptor FileLocalType = new(
        id: "ZAN002",
        title: "File-local Notify class is not generated",
        messageFormat: "The notification members of class '{0}' are not generated because it is file-local or nested in a file-local type, and a generated file cannot extend a file-local type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "zan002");

    /// <summary>
    /// Roslyn compares hint names ignoring case, so a class whose qualified name differs only in
    /// case from an earlier class's cannot get its own file.
    /// </summary>
    public static readonly DiagnosticDescriptor NameDiffersOnlyInCase = new(
        id: "ZAN003",
        title: "Class name differs only in case from another Notify class",
        messageFormat: "The notification members of class '{0}' are not generated because its file name '{1}' differs only in case from that of class '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "zan003");

    /// <summary>
    /// A class with a Notify attribute that is not <c>partial</c>. The generated file can only add
    /// members to a partial class.
    /// </summary>
    public static readonly DiagnosticDescriptor ClassNotPartial = new(
        id: "ZAN004",
        title: "Notify class is not partial",
        messageFormat: "The notification members of class '{0}' are not generated because it is not partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "zan004");

    /// <summary>
    /// A record with a Notify attribute. The generator only supports classes.
    /// </summary>
    public static readonly DiagnosticDescriptor RecordNotSupported = new(
        id: "ZAN005",
        title: "Notify attributes are not supported on records",
        messageFormat: "The notification members of record '{0}' are not generated because Notify attributes are not supported on records",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "zan005");

    /// <summary>
    /// An <c>[ObservableProperty]</c> field in a class without a class-level Notify attribute. The
    /// generator only reads the fields of a class that carries one.
    /// </summary>
    public static readonly DiagnosticDescriptor ObservablePropertyWithoutNotifyAttribute = new(
        id: "ZAN006",
        title: "[ObservableProperty] field in a class without a Notify attribute",
        messageFormat: "No property is generated for field '{0}' because its class '{1}' has no Notify attribute such as [NotifyPropertyChangedAsync]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink + "zan006");
}
