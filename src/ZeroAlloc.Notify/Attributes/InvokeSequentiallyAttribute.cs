namespace ZeroAlloc.Notify;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class InvokeSequentiallyAttribute : Attribute { }
