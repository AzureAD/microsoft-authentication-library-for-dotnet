using System.Runtime.InteropServices;

namespace KeyGuard.Attestation
{
    /// <summary>Ensures <c>AttestationClientLib.dll</c> is resolved from the exe folder.</summary>
    internal static class NativeDllResolver
    {
        private const string NativeDll = "AttestationClientLib.dll";

        static NativeDllResolver()   // runs once, automatically
        {
            string exeDir = AppContext.BaseDirectory;
            string full = Path.Combine(exeDir, NativeDll);

            NativeLibrary.SetDllImportResolver(
                typeof(NativeDllResolver).Assembly,
                (name, _, _) =>
                {
                    if (name.Equals(NativeDll, StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(full))
                    {
                        return NativeLibrary.Load(full);
                    }
                    return IntPtr.Zero;           // fall back to default probing
                });
        }

        internal static void EnsureLoaded() { /*touching the type triggers the ctor*/ }
    }
}
