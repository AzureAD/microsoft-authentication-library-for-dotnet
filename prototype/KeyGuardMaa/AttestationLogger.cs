namespace KeyGuard.Attestation
{
    internal static class AttestationLogger
    {
        /// <summary>Default logger that pipes native messages to <c>Console.WriteLine</c>.</summary>
        internal static readonly NativeMethods.LogFunc ConsoleLogger = (_,
            tag, lvl, func, line, msg) =>
            Console.WriteLine($"[{lvl}] {tag} {func}:{line}  {msg}");
    }
}
