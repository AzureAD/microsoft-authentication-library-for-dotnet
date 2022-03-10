using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace msalruntime
{
    /*
     * Bug 1: Invalid compilation of msalruntime.dll module - api-ms-win-core-libraryloader-l1-2-0.dll marked a delay load, that causes recursion and stackoverflow.
     * Bug 2: logging callback cannot be shared between multiple commponents 
     * Bug 3: to prevent recursion MSALRUNTIME_ReleaseError must return void or simple type: bool, int
     * Bug 4: the same completion callback for MSALRUNTIME_ReadAccountByIdAsync and MSALRUNTIME_SignInAsync it is not clear what should I handle when I read account, which fields are important.
     * Bug 5: we need AcquireToken API that produces both UI and silent call in one shot.
     * Bug 6: Logging callback over complicated and not consitent, we produce object, then with one method read multiple properties from it. I think this prototype will be simplier:
     *          typedef void(MSALRUNTIME_API* MSALRUNTIME_LOG_CALLBACK_ROUTINE)(MSALRUNTIME_LOG_LEVEL logLevel, const char* logEntry, void* callbackData);
     * Bug 7: SignIn must have account hint, like signInInteractively
     * Bug 8: Scope must be space separated list, not a JSON list.
     * Bug 9: SignInInteractively must support null account hint.
    */

    public class Core : CriticalFinalizerObject, IDisposable
    {
        private bool _alive = false;
        public Core()
        {
            Module.AddRef();
            _alive = true;
        }

        ~Core()
        {
            Dispose(false);
        }

        public Task<AuthResult> SignInAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.SignInAsync(parentHwnd, authParameters, correlationId, out asyncObj);
            DisposeAsync(asyncObj);
            return result;
        }

        public Task<AuthResult> SignInAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, CancellationToken cancellationToken)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.SignInAsync(parentHwnd, authParameters, correlationId, out asyncObj);
            LinkAsync(asyncObj, cancellationToken);
            return result;
        }

        public Task<AuthResult> SignInSilentlyAsync(AuthParameters authParameters, string correlationId)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.SignInSilentlyAsync(authParameters, correlationId, out asyncObj);
            DisposeAsync(asyncObj);
            return result;
        }

        public Task<AuthResult> SignInSilentlyAsync(AuthParameters authParameters, string correlationId, CancellationToken cancellationToken)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.SignInSilentlyAsync(authParameters, correlationId, out asyncObj);
            LinkAsync(asyncObj, cancellationToken);
            return result;
        }

        public Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, string accountHint = "")
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.SignInInteractivelyAsync(parentHwnd, authParameters, correlationId, accountHint, out asyncObj);
            DisposeAsync(asyncObj);
            return result;
        }

        public Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, CancellationToken cancellationToken)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.SignInInteractivelyAsync(parentHwnd, authParameters, correlationId, "", out asyncObj);
            LinkAsync(asyncObj, cancellationToken);
            return result;
        }

        public Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, string accountHint, CancellationToken cancellationToken)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.SignInInteractivelyAsync(parentHwnd, authParameters, correlationId, accountHint, out asyncObj);
            LinkAsync(asyncObj, cancellationToken);
            return result;
        }

        public Task<Account> ReadAccountByIdAsync(string accountId, string correlationId)
        {
            Async asyncObj;
            Task<Account> result = API.CPU.ReadAccountByIdAsync(accountId, correlationId, out asyncObj);
            DisposeAsync(asyncObj);
            return result;
        }

        public Task<Account> ReadAccountByIdAsync(string accountId, string correlationId, CancellationToken cancellationToken)
        {
            Async asyncObj;
            Task<Account> result = API.CPU.ReadAccountByIdAsync(accountId, correlationId, out asyncObj);
            LinkAsync(asyncObj, cancellationToken);
            return result;
        }

        public Task<AuthResult> AcquireTokenSilentlyAsync(AuthParameters authParameters, string correlationId, Account account)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.AcquireTokenSilentlyAsync(authParameters, correlationId, account, out asyncObj);
            DisposeAsync(asyncObj);
            return result;
        }

        public Task<AuthResult> AcquireTokenSilentlyAsync(AuthParameters authParameters, string correlationId, Account account, CancellationToken cancellationToken)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.AcquireTokenSilentlyAsync(authParameters, correlationId, account, out asyncObj);
            LinkAsync(asyncObj, cancellationToken);
            return result;
        }

        public Task<AuthResult> AcquireTokenInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, Account account)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.AcquireTokenInteractivelyAsync(parentHwnd, authParameters, correlationId, account, out asyncObj);
            DisposeAsync(asyncObj);
            return result;
        }

        public Task<AuthResult> AcquireTokenInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, Account account, CancellationToken cancellationToken)
        {
            Async asyncObj;
            Task<AuthResult> result = API.CPU.AcquireTokenInteractivelyAsync(parentHwnd, authParameters, correlationId, account, out asyncObj);
            LinkAsync(asyncObj, cancellationToken);
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_alive)
            {
                Module.RemoveRef();
                _alive = false;
            }

            if (disposing)
            {
                //GC.SuppressFinalize(this);
            }
        }

        private static void LinkAsync(Async asyncObj, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                asyncObj.Cancel();
                if (asyncObj != null)
                {
                    asyncObj.Dispose();
                }
            });
        }

        private static void DisposeAsync(Async asyncObj)
        {
            if (asyncObj != null)
            {
                asyncObj.Dispose();
            }
        }

    }

    public class AuthParameters : IDisposable
    {
        private MSALRUNTIME_AUTH_PARAMETERS_HANDLE _params;
        private string _requestedScopes;
        private string _redirectUri;
        private string _decodedClaims;
        private string _accessTokenToRenew;

        private PropertyCollection _properties;

        public AuthParameters(string clientId, string authority)
        {
            _params = API.CPU.CreateAuthParameters(clientId, authority);
            _properties = new PropertyCollection((key, value) => API.CPU.SetAdditionalParameter(_params, key, value));
        }

        public string RequestedScopes
        {
            get
            {
                return _requestedScopes;
            }

            set
            {
                API.CPU.SetRequestedScopes(_params, value);
                _requestedScopes = value;
            }
        }

        public string RedirectUri
        {
            get
            {
                return _redirectUri;
            }

            set
            {
                API.CPU.SetRedirectUri(_params, value);
                _redirectUri = value;
            }
        }

        public string DecodedClaims
        {
            get
            {
                return _decodedClaims;
            }

            set
            {
                API.CPU.SetDecodedClaims(_params, value);
                _decodedClaims = value;
            }
        }

        public string AccessTokenToRenew
        {
            get
            {
                return _accessTokenToRenew;
            }
            set
            {
                API.CPU.SetAccessTokenToRenew(_params, value);
                _accessTokenToRenew = value;
            }
        }

        public PropertyCollection Properties => _properties;

        internal MSALRUNTIME_AUTH_PARAMETERS_HANDLE Handle => _params;
        public void Dispose()
        {
            if (_params != null)
            {
                _params.Dispose();
                _params = null;
            }
        }
    }

    public class AuthResult : IDisposable
    {
        private MSALRUNTIME_AUTH_RESULT_HANDLE _hAuthResult;
        private Lazy<Account> _account;
        private Lazy<string> _accessToken;
        private Lazy<string> _idToken;
        private Lazy<string> _grantedScopes;
        private Lazy<DateTime> _expiresOn;
        private Lazy<Error> _error;
        private Lazy<string> _telemetryData;

        internal AuthResult(MSALRUNTIME_AUTH_RESULT_HANDLE hAuthResult)
        {
            _hAuthResult = hAuthResult;
            _account = new Lazy<Account>(() =>
            {

                MSALRUNTIME_ACCOUNT_HANDLE hAccount = API.CPU.GetAccount(_hAuthResult);

                if (hAccount.IsInvalid)
                {
                    hAccount.Dispose();
                    return null;
                }

                return new Account(hAccount);
            });

            _accessToken = new Lazy<string>(() => API.CPU.GetAccessToken(_hAuthResult));
            _idToken = new Lazy<string>(() => API.CPU.GetIdToken(_hAuthResult));
            _grantedScopes = new Lazy<string>(() => API.CPU.GetGrantedScopes(_hAuthResult));
            _expiresOn = new Lazy<DateTime>(() =>
            {
                Int64 expires = API.CPU.GetExpiresOn(_hAuthResult);
                return (new DateTime(1970, 1, 1)).AddSeconds(expires);
            });
            _error = new Lazy<Error>(() => API.CPU.GetError(_hAuthResult));
            _telemetryData = new Lazy<string>(() => API.CPU.GetTelemetryData(_hAuthResult));
        }

        public bool IsSuccess => Error == null;
        public Account Account => _account.Value;
        public string AccessToken => _accessToken.Value;
        public string IdToken => _idToken.Value;
        public string GrantedScopes => _grantedScopes.Value;
        public DateTime ExpiresOn => _expiresOn.Value;
        public Error Error => _error.Value;
        public string TelemetryData => _telemetryData.Value;

        public void Dispose()
        {
            if (_hAuthResult != null)
            {
                _hAuthResult.Dispose();
                _hAuthResult = null;
            }
        }
    }

    public class Account : IDisposable
    {
        private MSALRUNTIME_ACCOUNT_HANDLE _hAccount;
        private Lazy<string> _id;
        private Lazy<string> _client_info;
        private PropertyROCollection _properties;

        internal Account(MSALRUNTIME_ACCOUNT_HANDLE hAccount)
        {
            _hAccount = hAccount;
            _id = new Lazy<string>(() => API.CPU.GetAccountId(_hAccount));
            _client_info = new Lazy<string>(() => API.CPU.GetClientInfo(_hAccount));
            _properties = new PropertyROCollection((string key) => API.CPU.GetAccountProperty(_hAccount, key));
        }

        public string Id => _id.Value;
        public string ClientInfo => _client_info.Value;
        public PropertyROCollection Properties => _properties;
        internal MSALRUNTIME_ACCOUNT_HANDLE Handle => _hAccount;

        public void Dispose()
        {
            if (_hAccount != null)
            {
                _hAccount.Dispose();
                _hAccount = null;
            }
        }
    }

    public class Async : IDisposable
    {
        MSALRUNTIME_ASYNC_HANDLE _hAsync;
        internal Async(MSALRUNTIME_ASYNC_HANDLE hAsync)
        {
            _hAsync = hAsync;
        }

        public void Cancel()
        {
            if (_hAsync != null)
            {
                API.CPU.CancelAsyncOperation(_hAsync);
            }
        }

        public void Dispose()
        {
            if (_hAsync != null)
            {
                _hAsync.Dispose();
                _hAsync = null;
            }
        }
    }

    public enum ResponseStatus
    {
        Unexpected = 0,
        Reserved = 1,
        InteractionRequired = 2,
        NoNetwork = 3,
        NetworkTemporarilyUnavailable = 4,
        ServerTemporarilyUnavailable = 5,
        ApiContractViolation = 6,
        UserCanceled = 7,
        ApplicationCanceled = 8,
        IncorrectConfiguration = 9,
        InsufficientBuffer = 10,
        AuthorityUntrusted = 11,
        UserSwitch = 12,
        AccountUnusable = 13
    };

    public class Error
    {
        private ResponseStatus _status;
        private Int32 _errorCode;
        private Int32 _tag;
        private string _context;

        private Error(MSALRUNTIME_ERROR_HANDLE hError)
        {
            _status = API.CPU.GetStatus(hError);
            _errorCode = API.CPU.GetErrorCode(hError);
            _tag = API.CPU.GetTag(hError);
            _context = API.CPU.GetContext(hError);
            hError.Dispose();
        }

        internal static Error TryCreate(MSALRUNTIME_ERROR_HANDLE hError)
        {
            if (hError.IsSuccess)
            {
                // release fast, to free module.
                hError.Dispose();
                return null;
            }

            return new Error(hError);
        }


        public ResponseStatus Status => _status;
        public int ErrorCode => _errorCode;
        public int Tag => _tag;
        public string Context => _context;

        public override string ToString()
        {
            return $"Status: {Status}\r\n" +
                   (ErrorCode == 0 ? "" : $"Error: 0x{ErrorCode:x08}\r\n") +
                   $"Context: {Context}\r\n" +
                   $"Tag: 0x{Tag:x}";

        }

    }

    public class Exception : System.Exception
    {
        Error _result;
        public Exception(Error result)
            : base(result.ToString())
        {
            _result = result;
        }

        public ResponseStatus Status => _result.Status;
        public int ErrorCode => _result.ErrorCode;
        public int Tag => _result.Tag;
        public string Context => _result.Context;

    }

    public class PropertyROCollection
    {
        private Dictionary<string, string> _properties = new Dictionary<string, string>();

        Func<string, string> _reader = null;

        internal PropertyROCollection(Func<string, string> reader)
        {
            _reader = reader;
        }

        public string this[string key]
        {
            get
            {
                string value;
                if (_properties.TryGetValue(key, out value))
                {
                    return value;
                }
                else
                {
                    value = _reader(key);
                    _properties[key] = value;
                    return value;
                }
            }
        }
    }

    public class PropertyCollection
    {
        private Dictionary<string, string> _properties = new Dictionary<string, string>();

        Action<string, string> _setter = null;

        internal PropertyCollection(Action<string, string> setter)
        {
            _setter = setter;
        }

        public string this[string key]
        {
            set
            {
                _setter(key, value);
                _properties[key] = value;
            }

            get
            {
                string value;
                if (_properties.TryGetValue(key, out value))
                {
                    return value;
                }

                return value;
            }
        }
    }

    internal class Module
    {
        private static int handleCount = 0;
        private static object lockRuntime = new object();

        public static void AddRef()
        {
            int count = Interlocked.Increment(ref handleCount);
            if (count == 1)
            {
                lock (lockRuntime)
                {
                    if (handleCount == 1)
                    {
                        API.CPU.Startup();
                    }
                }
            }
        }

        public static void RemoveRef()
        {
            int count = Interlocked.Decrement(ref handleCount);
            if (count == 0)
            {
                lock (lockRuntime)
                {
                    if (handleCount <= 0)
                    {
                        API.CPU.Shutdown();
                        handleCount = 0;
                    }
                }
            }
        }

        public static int RefCount()
        {
            return handleCount;
        }
    }

    internal abstract class Handle : SafeHandle
    {
        bool _releaseModule = true;
        public Handle(bool releaseModule = true) : base(IntPtr.Zero, true)
        {
            _releaseModule = releaseModule;

            if (_releaseModule)
            {
                Module.AddRef();
            }
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        protected override sealed bool ReleaseHandle()
        {
            try
            {
                Release();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        protected abstract void Release();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_releaseModule)
            {
                Module.RemoveRef();
                _releaseModule = false;
            }
        }
    }

    internal class MSALRUNTIME_AUTH_PARAMETERS_HANDLE : Handle
    {
        public MSALRUNTIME_AUTH_PARAMETERS_HANDLE()
        {
        }

        protected override void Release()
        {
            API.CPU.ReleaseAuthParameters(this.handle);
        }
    }

    internal class MSALRUNTIME_AUTH_RESULT_HANDLE : Handle
    {
        public MSALRUNTIME_AUTH_RESULT_HANDLE()
        {
        }

        public MSALRUNTIME_AUTH_RESULT_HANDLE(IntPtr hndl)
        {
            this.SetHandle(hndl);
        }

        protected override void Release()
        {
            API.CPU.ReleaseAuthResult(this.handle);
        }
    }

    internal class MSALRUNTIME_ACCOUNT_HANDLE : Handle
    {
        public MSALRUNTIME_ACCOUNT_HANDLE()
        {
        }

        protected override void Release()
        {
            API.CPU.ReleaseAccount(this.handle);
        }
    }

    internal class MSALRUNTIME_ASYNC_HANDLE : Handle
    {
        public MSALRUNTIME_ASYNC_HANDLE()
        {
        }

        protected override void Release()
        {
            API.CPU.ReleaseAsyncHandle(this.handle);
        }
    }

    internal class MSALRUNTIME_ERROR_HANDLE : Handle
    {
        MSALRUNTIME_ERROR_HANDLE() : base(releaseModule: true)
        {
        }

        public MSALRUNTIME_ERROR_HANDLE(bool releaseModule) : base(releaseModule)
        {
        }

        protected override void Release()
        {
            API.CPU.ReleaseError(this.handle);
        }

        public bool IsSuccess => IsInvalid;

    }

    internal class MSALRUNTIME_ERROR_HANDLE_MODULE : MSALRUNTIME_ERROR_HANDLE
    {
        public MSALRUNTIME_ERROR_HANDLE_MODULE() : base(releaseModule: false)
        {
        }
    }

    internal abstract class API
    {
        public static readonly API CPU = CreateAPI();

        partial class x86 : API
        {
            const string Name = "msalruntime_x86.dll";
        }

        partial class x64 : API
        {
            const string Name = "msalruntime.dll";
        }

        partial class arm : API
        {
            const string Name = "msalruntime_arm.dll";
        }

        partial class arm64 : API
        {
            const string Name = "msalruntime_arm64.dll";
        }

        private static API CreateAPI()
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    return new x86();
                case Architecture.X64:
                    return new x64();
                case Architecture.Arm:
                    return new arm();
                case Architecture.Arm64:
                    return new arm64();
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        public abstract Task<AuthResult> SignInAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, out Async asyncObj);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInAsync(parentHwnd, authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInAsync(parentHwnd, authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInAsync(parentHwnd, authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInAsync(parentHwnd, authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        public abstract Task<AuthResult> SignInSilentlyAsync(AuthParameters authParameters, string correlationId, out Async asyncObj);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInSilentlyAsync(AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInSilentlyAsync(authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInSilentlyAsync(AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInSilentlyAsync(authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInSilentlyAsync(AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInSilentlyAsync(authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInSilentlyAsync(AuthParameters authParameters, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInSilentlyAsync(authParameters.Handle, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        public abstract Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, string accountHint, out Async asyncObj);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, string accountHint, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, string accountHint, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, accountHint, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, string accountHint, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, string accountHint, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, accountHint, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, string accountHint, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, string accountHint, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, accountHint, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SignInInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, string accountHint, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> SignInInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, string accountHint, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_SignInInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, accountHint, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        public Task<Account> ReadAccountByIdAsync(string accountId, string correlationId, out Async asyncObj)
        {
            return ReadAccountByIdAsyncInternal(accountId, correlationId, out asyncObj)
                .ContinueWith((Task<AuthResult> authResultTask) =>
                {
                    AuthResult authResult = authResultTask.Result;

                    if (authResult.Error != null)
                    {
                        Error error = authResult.Error;

                        // to release resource faster, it is better to dispose it eariler.
                        authResult.Dispose();

                        throw new Exception(error);
                    }

                    Account result = authResult.Account;

                    // to release resource faster, it is better to dispose it eariler.
                    authResult.Dispose();

                    return result;
                });
        }

        protected abstract Task<AuthResult> ReadAccountByIdAsyncInternal(string accountId, string correlationId, out Async asyncObj);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReadAccountByIdAsync(string accountId, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            protected override Task<AuthResult> ReadAccountByIdAsyncInternal(string accountId, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_ReadAccountByIdAsync(accountId, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReadAccountByIdAsync(string accountId, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            protected override Task<AuthResult> ReadAccountByIdAsyncInternal(string accountId, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_ReadAccountByIdAsync(accountId, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReadAccountByIdAsync(string accountId, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            protected override Task<AuthResult> ReadAccountByIdAsyncInternal(string accountId, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_ReadAccountByIdAsync(accountId, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReadAccountByIdAsync(string accountId, string correlationId, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            protected override Task<AuthResult> ReadAccountByIdAsyncInternal(string accountId, string correlationId, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_ReadAccountByIdAsync(accountId, correlationId, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        public abstract Task<AuthResult> AcquireTokenSilentlyAsync(AuthParameters authParameters, string correlationId, Account account, out Async asyncObj);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenSilentlyAsync(AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenSilentlyAsync(authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenSilentlyAsync(AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenSilentlyAsync(authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenSilentlyAsync(AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenSilentlyAsync(authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenSilentlyAsync(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenSilentlyAsync(AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenSilentlyAsync(authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        public abstract Task<AuthResult> AcquireTokenInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, Account account, out Async asyncObj);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_AcquireTokenInteractivelyAsync(IntPtr parentHwnd, MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string correlationId, MSALRUNTIME_ACCOUNT_HANDLE account, MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override Task<AuthResult> AcquireTokenInteractivelyAsync(IntPtr parentHwnd, AuthParameters authParameters, string correlationId, Account account, out Async asyncObj)
            {
                return CreateAsync(
                    (MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle) =>
                        MSALRUNTIME_AcquireTokenInteractivelyAsync(parentHwnd, authParameters.Handle, correlationId, account.Handle, callback, callbackData, out asyncHandle)
                    , out asyncObj);
            }
        }

        #region Helpers

        private delegate void MSALRUNTIME_COMPLETION_ROUTINE(IntPtr hResponse, IntPtr callbackData);

        private static void CallbackCompletion(IntPtr hResponse, IntPtr callbackData)
        {
            GCHandle gchCallback = GCHandle.FromIntPtr(callbackData);
            Action<MSALRUNTIME_AUTH_RESULT_HANDLE> callback = gchCallback.Target as Action<MSALRUNTIME_AUTH_RESULT_HANDLE>;
            gchCallback.Free();

            if (callback != null)
            {
                callback(new MSALRUNTIME_AUTH_RESULT_HANDLE(hResponse));
            }
        }

        private delegate MSALRUNTIME_ERROR_HANDLE MSALAsync(MSALRUNTIME_COMPLETION_ROUTINE callback, IntPtr callbackData, out MSALRUNTIME_ASYNC_HANDLE asyncHandle);

        private static Task<AuthResult> CreateAsync(MSALAsync asyncFunc, out Async asyncObj)
        {
            asyncObj = null;
            TaskCompletionSource<AuthResult> tcs = new TaskCompletionSource<AuthResult>();
            try
            {

                Action<MSALRUNTIME_AUTH_RESULT_HANDLE> callback = (MSALRUNTIME_AUTH_RESULT_HANDLE hResponse) =>
                {
                    tcs.SetResult(new AuthResult(hResponse));
                };

                GCHandle gchCallback = GCHandle.Alloc(callback);

                MSALRUNTIME_ASYNC_HANDLE asyncHandle;
                MSALRUNTIME_ERROR_HANDLE hError = asyncFunc(CallbackCompletion, GCHandle.ToIntPtr(gchCallback), out asyncHandle);

                if (hError != null)
                {
                    Error error = Error.TryCreate(hError);
                    if (error != null)
                    {
                        gchCallback.Free();
                        tcs.SetException(new Exception(error));
                    }
                }
                else
                {
                    gchCallback.Free();
                    tcs.SetException(new System.Exception("Unexpected behaviour from mid-tier."));
                }

                if (asyncHandle != null)
                {
                    asyncObj = new Async(asyncHandle);
                }

            }
            catch (System.Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        private void ThrowIfFailed(MSALRUNTIME_ERROR_HANDLE hError)
        {
            Error error = Error.TryCreate(hError);

            if (error != null)
            {
                throw new Exception(error);
            }
        }

        protected delegate MSALRUNTIME_ERROR_HANDLE APIFunc(char[] result, ref Int32 bufferSize);

        protected string GetString(APIFunc func)
        {
            char[] result = null;
            int size = 0;
            MSALRUNTIME_ERROR_HANDLE error = func(null, ref size);

            if (error.IsSuccess)
            {
                error.Dispose();
                return string.Empty;
            }

            if (GetStatus(error) == ResponseStatus.InsufficientBuffer && size > 0)
            {
                error.Dispose();
                result = new char[size];

                error = func(result, ref size);
            }
            ThrowIfFailed(error);

            if (result != null && size > 0)
                return new string(result, 0, size - 1);

            return string.Empty;
        }

        #endregion

        #region Msal startup/shutdown

        public abstract void Startup();

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE_MODULE MSALRUNTIME_Startup();

            public override void Startup()
            {
                ThrowIfFailed(MSALRUNTIME_Startup());
            }

        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE_MODULE MSALRUNTIME_Startup();

            public override void Startup()
            {
                ThrowIfFailed(MSALRUNTIME_Startup());
            }

        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE_MODULE MSALRUNTIME_Startup();

            public override void Startup()
            {
                ThrowIfFailed(MSALRUNTIME_Startup());
            }

        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE_MODULE MSALRUNTIME_Startup();

            public override void Startup()
            {
                ThrowIfFailed(MSALRUNTIME_Startup());
            }
        }


        public abstract void Shutdown();

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern void MSALRUNTIME_Shutdown();

            public override void Shutdown()
            {
                MSALRUNTIME_Startup();
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern void MSALRUNTIME_Shutdown();

            public override void Shutdown()
            {
                MSALRUNTIME_Shutdown();
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern void MSALRUNTIME_Shutdown();

            public override void Shutdown()
            {
                MSALRUNTIME_Shutdown();
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern void MSALRUNTIME_Shutdown();

            public override void Shutdown()
            {
                MSALRUNTIME_Shutdown();
            }
        }

        #endregion

        #region MSALRUNTIME_AUTH_PARAMETERS_HANDLE

        public abstract void ReleaseAuthParameters(IntPtr asyncHandle);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthParameters(IntPtr asyncHandle);

            public override void ReleaseAuthParameters(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthParameters(asyncHandle));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthParameters(IntPtr asyncHandle);

            public override void ReleaseAuthParameters(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthParameters(asyncHandle));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthParameters(IntPtr asyncHandle);

            public override void ReleaseAuthParameters(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthParameters(asyncHandle));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthParameters(IntPtr asyncHandle);

            public override void ReleaseAuthParameters(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthParameters(asyncHandle));
            }
        }

        public abstract MSALRUNTIME_AUTH_PARAMETERS_HANDLE CreateAuthParameters(string clientId, string authority);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CreateAuthParameters(string clientId, string authority, out MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters);
            public override MSALRUNTIME_AUTH_PARAMETERS_HANDLE CreateAuthParameters(string clientId, string authority)
            {
                MSALRUNTIME_AUTH_PARAMETERS_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_CreateAuthParameters(clientId, authority, out result));
                return result;
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CreateAuthParameters(string clientId, string authority, out MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters);
            public override MSALRUNTIME_AUTH_PARAMETERS_HANDLE CreateAuthParameters(string clientId, string authority)
            {
                MSALRUNTIME_AUTH_PARAMETERS_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_CreateAuthParameters(clientId, authority, out result));
                return result;
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CreateAuthParameters(string clientId, string authority, out MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters);
            public override MSALRUNTIME_AUTH_PARAMETERS_HANDLE CreateAuthParameters(string clientId, string authority)
            {
                MSALRUNTIME_AUTH_PARAMETERS_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_CreateAuthParameters(clientId, authority, out result));
                return result;
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CreateAuthParameters(string clientId, string authority, out MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters);
            public override MSALRUNTIME_AUTH_PARAMETERS_HANDLE CreateAuthParameters(string clientId, string authority)
            {
                MSALRUNTIME_AUTH_PARAMETERS_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_CreateAuthParameters(clientId, authority, out result));
                return result;
            }
        }

        public abstract void SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes);

            public override void SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes)
            {
                ThrowIfFailed(MSALRUNTIME_SetRequestedScopes(authParameters, scopes));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes);

            public override void SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes)
            {
                ThrowIfFailed(MSALRUNTIME_SetRequestedScopes(authParameters, scopes));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes);

            public override void SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes)
            {
                ThrowIfFailed(MSALRUNTIME_SetRequestedScopes(authParameters, scopes));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes);

            public override void SetRequestedScopes(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string scopes)
            {
                ThrowIfFailed(MSALRUNTIME_SetRequestedScopes(authParameters, scopes));
            }
        }

        public abstract void SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetDecodedClaims(handle, value));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetDecodedClaims(handle, value));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetDecodedClaims(handle, value));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetDecodedClaims(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetDecodedClaims(handle, value));
            }
        }

        public abstract void SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetRedirectUri(handle, value));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetRedirectUri(handle, value));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetRedirectUri(handle, value));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetRedirectUri(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetRedirectUri(handle, value));
            }
        }

        public abstract void SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAccessTokenToRenew(handle, value));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAccessTokenToRenew(handle, value));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAccessTokenToRenew(handle, value));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value);

            public override void SetAccessTokenToRenew(MSALRUNTIME_AUTH_PARAMETERS_HANDLE handle, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAccessTokenToRenew(handle, value));
            }
        }

        public abstract void SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value);

            public override void SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAdditionalParameter(authParameters, key, value));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value);

            public override void SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAdditionalParameter(authParameters, key, value));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value);

            public override void SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAdditionalParameter(authParameters, key, value));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value);

            public override void SetAdditionalParameter(MSALRUNTIME_AUTH_PARAMETERS_HANDLE authParameters, string key, string value)
            {
                ThrowIfFailed(MSALRUNTIME_SetAdditionalParameter(authParameters, key, value));
            }
        }

        #endregion

        #region MSALRUNTIME_AUTH_RESULT_HANDLE

        public abstract void ReleaseAuthResult(IntPtr authResult);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthResult(IntPtr authResult);

            public override void ReleaseAuthResult(IntPtr authResult)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthResult(authResult));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthResult(IntPtr authResult);

            public override void ReleaseAuthResult(IntPtr authResult)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthResult(authResult));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthResult(IntPtr authResult);

            public override void ReleaseAuthResult(IntPtr authResult)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthResult(authResult));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAuthResult(IntPtr authResult);

            public override void ReleaseAuthResult(IntPtr authResult)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAuthResult(authResult));
            }
        }

        public abstract MSALRUNTIME_ACCOUNT_HANDLE GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, out MSALRUNTIME_ACCOUNT_HANDLE account);

            public override MSALRUNTIME_ACCOUNT_HANDLE GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                MSALRUNTIME_ACCOUNT_HANDLE account = null;
                ThrowIfFailed(MSALRUNTIME_GetAccount(authResult, out account));
                return account;
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, out MSALRUNTIME_ACCOUNT_HANDLE account);

            public override MSALRUNTIME_ACCOUNT_HANDLE GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                MSALRUNTIME_ACCOUNT_HANDLE account = null;
                ThrowIfFailed(MSALRUNTIME_GetAccount(authResult, out account));
                return account;
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, out MSALRUNTIME_ACCOUNT_HANDLE account);

            public override MSALRUNTIME_ACCOUNT_HANDLE GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                MSALRUNTIME_ACCOUNT_HANDLE account = null;
                ThrowIfFailed(MSALRUNTIME_GetAccount(authResult, out account));
                return account;
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, out MSALRUNTIME_ACCOUNT_HANDLE account);

            public override MSALRUNTIME_ACCOUNT_HANDLE GetAccount(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                MSALRUNTIME_ACCOUNT_HANDLE account = null;
                ThrowIfFailed(MSALRUNTIME_GetAccount(authResult, out account));
                return account;
            }
        }

        public abstract string GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetIdToken(authResult, value, ref bufferSize));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetIdToken(authResult, value, ref bufferSize));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetIdToken(authResult, value, ref bufferSize));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetIdToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetIdToken(authResult, value, ref bufferSize));
            }
        }

        public abstract string GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccessToken(authResult, value, ref bufferSize));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccessToken(authResult, value, ref bufferSize));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccessToken(authResult, value, ref bufferSize));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetAccessToken(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccessToken(authResult, value, ref bufferSize));
            }
        }

        public abstract string GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetGrantedScopes(authResult, value, ref bufferSize));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetGrantedScopes(authResult, value, ref bufferSize));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetGrantedScopes(authResult, value, ref bufferSize));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult, char[] value, ref Int32 bufferSize);
            public override string GetGrantedScopes(MSALRUNTIME_AUTH_RESULT_HANDLE authResult)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetGrantedScopes(authResult, value, ref bufferSize));
            }
        }

        public abstract Int64 GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle);
        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out Int64 result);

            public override Int64 GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                Int64 result;
                ThrowIfFailed(MSALRUNTIME_GetExpiresOn(handle, out result));
                return result;
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out Int64 result);

            public override Int64 GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                Int64 result;
                ThrowIfFailed(MSALRUNTIME_GetExpiresOn(handle, out result));
                return result;
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out Int64 result);

            public override Int64 GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                Int64 result;
                ThrowIfFailed(MSALRUNTIME_GetExpiresOn(handle, out result));
                return result;
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out Int64 result);

            public override Int64 GetExpiresOn(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                Int64 result;
                ThrowIfFailed(MSALRUNTIME_GetExpiresOn(handle, out result));
                return result;
            }
        }

        public Error GetError(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
        {
            return Error.TryCreate(API.CPU.GetErrorInternal(handle));
        }

        protected abstract MSALRUNTIME_ERROR_HANDLE GetErrorInternal(MSALRUNTIME_AUTH_RESULT_HANDLE handle);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetError(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out MSALRUNTIME_ERROR_HANDLE result);

            protected override MSALRUNTIME_ERROR_HANDLE GetErrorInternal(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                MSALRUNTIME_ERROR_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_GetError(handle, out result));
                return result;
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetError(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out MSALRUNTIME_ERROR_HANDLE result);

            protected override MSALRUNTIME_ERROR_HANDLE GetErrorInternal(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                MSALRUNTIME_ERROR_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_GetError(handle, out result));
                return result;
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetError(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out MSALRUNTIME_ERROR_HANDLE result);

            protected override MSALRUNTIME_ERROR_HANDLE GetErrorInternal(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                MSALRUNTIME_ERROR_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_GetError(handle, out result));
                return result;
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetError(MSALRUNTIME_AUTH_RESULT_HANDLE handle, out MSALRUNTIME_ERROR_HANDLE result);

            protected override MSALRUNTIME_ERROR_HANDLE GetErrorInternal(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                MSALRUNTIME_ERROR_HANDLE result;
                ThrowIfFailed(MSALRUNTIME_GetError(handle, out result));
                return result;
            }
        }

        public abstract string GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle, char[] value, ref Int32 bufferSize);
            public override string GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetTelemetryData(handle, value, ref bufferSize));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle, char[] value, ref Int32 bufferSize);
            public override string GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetTelemetryData(handle, value, ref bufferSize));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle, char[] value, ref Int32 bufferSize);
            public override string GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetTelemetryData(handle, value, ref bufferSize));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle, char[] value, ref Int32 bufferSize);
            public override string GetTelemetryData(MSALRUNTIME_AUTH_RESULT_HANDLE handle)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetTelemetryData(handle, value, ref bufferSize));
            }
        }

        #endregion

        #region MSALRUNTIME_ACCOUNT_HANDLE

        public abstract void ReleaseAccount(IntPtr account);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAccount(IntPtr account);

            public override void ReleaseAccount(IntPtr account)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAccount(account));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAccount(IntPtr account);

            public override void ReleaseAccount(IntPtr account)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAccount(account));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAccount(IntPtr account);

            public override void ReleaseAccount(IntPtr account)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAccount(account));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAccount(IntPtr account);

            public override void ReleaseAccount(IntPtr account)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAccount(account));
            }
        }

        public abstract string GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account, char[] accountId, ref Int32 bufferSize);
            public override string GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] accountId, ref Int32 bufferSize) => MSALRUNTIME_GetAccountId(account, accountId, ref bufferSize));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account, char[] accountId, ref Int32 bufferSize);
            public override string GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] accountId, ref Int32 bufferSize) => MSALRUNTIME_GetAccountId(account, accountId, ref bufferSize));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account, char[] accountId, ref Int32 bufferSize);
            public override string GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] accountId, ref Int32 bufferSize) => MSALRUNTIME_GetAccountId(account, accountId, ref bufferSize));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account, char[] accountId, ref Int32 bufferSize);
            public override string GetAccountId(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] accountId, ref Int32 bufferSize) => MSALRUNTIME_GetAccountId(account, accountId, ref bufferSize));
            }
        }

        public abstract string GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account, char[] value, ref Int32 bufferSize);
            public override string GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetClientInfo(account, value, ref bufferSize));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account, char[] value, ref Int32 bufferSize);
            public override string GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetClientInfo(account, value, ref bufferSize));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account, char[] value, ref Int32 bufferSize);
            public override string GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetClientInfo(account, value, ref bufferSize));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account, char[] value, ref Int32 bufferSize);
            public override string GetClientInfo(MSALRUNTIME_ACCOUNT_HANDLE account)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetClientInfo(account, value, ref bufferSize));
            }
        }

        public abstract string GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key, char[] value, ref Int32 bufferSize);
            public override string GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccountProperty(account, key, value, ref bufferSize));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key, char[] value, ref Int32 bufferSize);
            public override string GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccountProperty(account, key, value, ref bufferSize));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key, char[] value, ref Int32 bufferSize);
            public override string GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccountProperty(account, key, value, ref bufferSize));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key, char[] value, ref Int32 bufferSize);
            public override string GetAccountProperty(MSALRUNTIME_ACCOUNT_HANDLE account, string key)
            {
                return GetString((char[] value, ref Int32 bufferSize) => MSALRUNTIME_GetAccountProperty(account, key, value, ref bufferSize));
            }
        }

        #endregion

        #region MSALRUNTIME_ASYNC_HANDLE

        public abstract void ReleaseAsyncHandle(IntPtr asyncHandle);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAsyncHandle(IntPtr asyncHandle);

            public override void ReleaseAsyncHandle(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAsyncHandle(asyncHandle));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAsyncHandle(IntPtr asyncHandle);

            public override void ReleaseAsyncHandle(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAsyncHandle(asyncHandle));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAsyncHandle(IntPtr asyncHandle);

            public override void ReleaseAsyncHandle(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAsyncHandle(asyncHandle));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseAsyncHandle(IntPtr asyncHandle);

            public override void ReleaseAsyncHandle(IntPtr asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseAsyncHandle(asyncHandle));
            }
        }

        public abstract void CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override void CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_CancelAsyncOperation(asyncHandle));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override void CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_CancelAsyncOperation(asyncHandle));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override void CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_CancelAsyncOperation(asyncHandle));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle);

            public override void CancelAsyncOperation(MSALRUNTIME_ASYNC_HANDLE asyncHandle)
            {
                ThrowIfFailed(MSALRUNTIME_CancelAsyncOperation(asyncHandle));
            }
        }

        #endregion

        #region MSALRUNTIME_ERROR_HANDLE

        public abstract void ReleaseError(IntPtr error);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseError(IntPtr error);

            public override void ReleaseError(IntPtr error)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseError(error));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseError(IntPtr error);

            public override void ReleaseError(IntPtr error)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseError(error));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseError(IntPtr error);

            public override void ReleaseError(IntPtr error)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseError(error));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_ReleaseError(IntPtr error);

            public override void ReleaseError(IntPtr error)
            {
                ThrowIfFailed(MSALRUNTIME_ReleaseError(error));
            }
        }

        public abstract ResponseStatus GetStatus(MSALRUNTIME_ERROR_HANDLE error);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetStatus(MSALRUNTIME_ERROR_HANDLE error, out ResponseStatus responseStatus);

            public override ResponseStatus GetStatus(MSALRUNTIME_ERROR_HANDLE error)
            {
                ResponseStatus result;
                ThrowIfFailed(MSALRUNTIME_GetStatus(error, out result));
                return result;
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetStatus(MSALRUNTIME_ERROR_HANDLE error, out ResponseStatus responseStatus);

            public override ResponseStatus GetStatus(MSALRUNTIME_ERROR_HANDLE error)
            {
                ResponseStatus result;
                ThrowIfFailed(MSALRUNTIME_GetStatus(error, out result));
                return result;
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetStatus(MSALRUNTIME_ERROR_HANDLE error, out ResponseStatus responseStatus);

            public override ResponseStatus GetStatus(MSALRUNTIME_ERROR_HANDLE error)
            {
                ResponseStatus result;
                ThrowIfFailed(MSALRUNTIME_GetStatus(error, out result));
                return result;
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetStatus(MSALRUNTIME_ERROR_HANDLE error, out ResponseStatus responseStatus);

            public override ResponseStatus GetStatus(MSALRUNTIME_ERROR_HANDLE error)
            {
                ResponseStatus result;
                ThrowIfFailed(MSALRUNTIME_GetStatus(error, out result));
                return result;
            }
        }

        public abstract Int32 GetErrorCode(MSALRUNTIME_ERROR_HANDLE error);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetErrorCode(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetErrorCode(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetErrorCode(error, out result));
                return result;
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetErrorCode(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetErrorCode(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetErrorCode(error, out result));
                return result;
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetErrorCode(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetErrorCode(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetErrorCode(error, out result));
                return result;
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetErrorCode(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetErrorCode(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetErrorCode(error, out result));
                return result;
            }
        }

        public abstract Int32 GetTag(MSALRUNTIME_ERROR_HANDLE error);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTag(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetTag(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetTag(error, out result));
                return result;
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTag(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetTag(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetTag(error, out result));
                return result;
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTag(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetTag(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetTag(error, out result));
                return result;
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetTag(MSALRUNTIME_ERROR_HANDLE error, out Int32 responseErrorCode);

            public override Int32 GetTag(MSALRUNTIME_ERROR_HANDLE error)
            {
                Int32 result;
                ThrowIfFailed(MSALRUNTIME_GetTag(error, out result));
                return result;
            }
        }

        public abstract string GetContext(MSALRUNTIME_ERROR_HANDLE error);

        partial class x86
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetContext(MSALRUNTIME_ERROR_HANDLE error, char[] context, ref Int32 bufferSize);

            public override string GetContext(MSALRUNTIME_ERROR_HANDLE error)
            {
                return GetString((char[] result, ref Int32 size) => MSALRUNTIME_GetContext(error, result, ref size));
            }
        }

        partial class x64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetContext(MSALRUNTIME_ERROR_HANDLE error, char[] context, ref Int32 bufferSize);

            public override string GetContext(MSALRUNTIME_ERROR_HANDLE error)
            {
                return GetString((char[] result, ref Int32 size) => MSALRUNTIME_GetContext(error, result, ref size));
            }
        }

        partial class arm
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetContext(MSALRUNTIME_ERROR_HANDLE error, char[] context, ref Int32 bufferSize);

            public override string GetContext(MSALRUNTIME_ERROR_HANDLE error)
            {
                return GetString((char[] result, ref Int32 size) => MSALRUNTIME_GetContext(error, result, ref size));
            }
        }

        partial class arm64
        {
            [DllImport(Name, CharSet = CharSet.Unicode)]
            private static extern MSALRUNTIME_ERROR_HANDLE MSALRUNTIME_GetContext(MSALRUNTIME_ERROR_HANDLE error, char[] context, ref Int32 bufferSize);

            public override string GetContext(MSALRUNTIME_ERROR_HANDLE error)
            {
                return GetString((char[] result, ref Int32 size) => MSALRUNTIME_GetContext(error, result, ref size));
            }
        }

        #endregion

    }

}
