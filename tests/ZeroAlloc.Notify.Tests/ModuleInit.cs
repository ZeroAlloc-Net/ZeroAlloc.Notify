using System.Runtime.CompilerServices;

namespace ZeroAlloc.Notify.Tests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
