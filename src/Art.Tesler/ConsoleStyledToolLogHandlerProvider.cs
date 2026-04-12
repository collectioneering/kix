using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Art.Common.Logging;
using ConFormat;

namespace Art.Tesler;

public class ConsoleStyledToolLogHandlerProvider : ToolLogHandlerProviderBase
{
    private readonly Func<bool> _errorRedirectedFunc;
    private readonly Func<int> _widthFunc;
    private readonly Func<int> _heightFunc;
    private readonly Func<int> _initialRowFunc;

    public ConsoleStyledToolLogHandlerProvider(
        TextWriter outWriter,
        TextWriter warnWriter,
        TextWriter errorWriter,
        Func<bool> errorRedirectedFunc,
        Func<int> widthFunc,
        Func<int> heightFunc,
        Func<int> initialRowFunc,
        Func<Stream> outStreamAccessFunc)
        : base(outWriter, warnWriter, errorWriter, outStreamAccessFunc)
    {
        _errorRedirectedFunc = errorRedirectedFunc;
        _widthFunc = widthFunc;
        _heightFunc = heightFunc;
        _initialRowFunc = initialRowFunc;
    }

    public static ConsoleStyledToolLogHandlerProvider FromSystemConsole()
    {
        return new ConsoleStyledToolLogHandlerProvider(
            Console.Out,
            Console.Error,
            Console.Error,
            static () => Console.IsErrorRedirected,
            static () => Console.BufferWidth,
            static () => Console.WindowHeight,
            static () => Console.CursorTop - Console.WindowTop,
            Console.OpenStandardOutput);
    }

    public override IToolLogHandler GetStreamToolLogHandler(LogPreferences logPreferences)
    {
        return new ConsoleStyledLogHandler(
            Out,
            Warn,
            Error,
            true,
            _errorRedirectedFunc,
            _widthFunc,
            _heightFunc,
            _initialRowFunc,
            logPreferences,
            true);
    }

    public override IToolLogHandler GetDefaultToolLogHandler(LogPreferences logPreferences)
    {
        return new ConsoleStyledLogHandler(
            Out,
            Warn,
            Error,
            false,
            _errorRedirectedFunc,
            _widthFunc,
            _heightFunc,
            _initialRowFunc,
            logPreferences,
            false,
            OperatingSystem.IsMacOS());
    }
}

public class ConsoleStyledLogHandler : StyledLogHandler, IOperationProgressContextOwner
{
    private readonly bool _forceFallback;
    private readonly Func<bool> _errorRedirectedFunc;
    private readonly Func<int> _widthFunc;
    private readonly Func<int> _heightFunc;
    private readonly Func<int> _initialRowFunc;
    private static readonly Guid s_downloadOperation = Guid.ParseExact("c6d42b18f0ae452385f180aa74e9ef29", "N");
    private static readonly Guid s_timedNamedProgressOperation = Guid.ParseExact("923b6d0747667facd296e1019dd7eae8", "N");
    private static readonly Guid s_operationWaitingForResult = Guid.ParseExact("4fd5c851a88c430c8f8da54dbcf70ab2", "N");
    private readonly Dictionary<object, Guid> _multiObjects = new();
    private readonly HashSet<Guid> _registeredMultiObjects = [];
    private MultiBarContext<Guid>? _multiBarContext;
    private readonly object _lock = new();

    public ConsoleStyledLogHandler(
        TextWriter outWriter,
        TextWriter warnWriter,
        TextWriter errorWriter,
        bool forceFallback,
        Func<bool> errorRedirectedFunc,
        Func<int> widthFunc,
        Func<int> heightFunc,
        Func<int> initialRowFunc,
        LogPreferences logPreferences,
        bool alwaysPrintToErrorStream,
        bool enableFancy = false)
        : base(outWriter, warnWriter, errorWriter, logPreferences, alwaysPrintToErrorStream, enableFancy)
    {
        _forceFallback = forceFallback;
        _errorRedirectedFunc = errorRedirectedFunc;
        _widthFunc = widthFunc;
        _heightFunc = heightFunc;
        _initialRowFunc = initialRowFunc;
    }

    private MultiBarContext<Guid> GetMultiBarContext()
    {
        lock (_lock)
        {
            if (_multiBarContext != null)
            {
                return _multiBarContext;
            }
            return _multiBarContext = MultiBarContext<Guid>.Create(
                Error,
                _forceFallback,
                _errorRedirectedFunc,
                _widthFunc,
                _heightFunc,
                _initialRowFunc());
        }
    }

    private Guid AllocateGuid()
    {
        Guid result;
        do
        {
            result = Guid.NewGuid();
        } while (!_registeredMultiObjects.Add(result));
        return result;
    }

    public override bool TryGetConcurrentOperationProgressContext(string operationName, Guid operationGuid, [NotNullWhen(true)] out IOperationProgressContext? operationProgressContext)
    {
        lock (_lock)
        {
            if (operationGuid.Equals(s_downloadOperation) || operationGuid.Equals(s_timedNamedProgressOperation))
            {
                Guid guid = AllocateGuid();
                try
                {
                    operationProgressContext = new NamedProgressUpdateContextForMulti(
                        GetMultiBarContext(),
                        guid,
                        operationName,
                        this);
                    _multiObjects.Add(operationProgressContext, guid);
                }
                catch
                {
                    _registeredMultiObjects.Remove(guid);
                    throw;
                }
                return true;
            }
            if (operationGuid.Equals(s_operationWaitingForResult))
            {
                Guid guid = AllocateGuid();
                try
                {
                    operationProgressContext = new WaitUpdateContextForMulti(
                        GetMultiBarContext(),
                        guid,
                        operationName,
                        this);
                    _multiObjects.Add(operationProgressContext, guid);
                }
                catch
                {
                    _registeredMultiObjects.Remove(guid);
                    throw;
                }
                return true;
            }
            operationProgressContext = null;
            return false;
        }
    }

    public override bool TryGetOperationProgressContext(string operationName, Guid operationGuid, [NotNullWhen(true)] out IOperationProgressContext? operationProgressContext)
    {
        lock (_lock)
        {
            if (_multiObjects.Count > 0)
            {
                operationProgressContext = null;
                return false;
            }
            if (operationGuid.Equals(s_downloadOperation) || operationGuid.Equals(s_timedNamedProgressOperation))
            {
                operationProgressContext = new NamedProgressUpdateContext(operationName, Error, _forceFallback, _errorRedirectedFunc, _widthFunc);
                return true;
            }
            if (operationGuid.Equals(s_operationWaitingForResult))
            {
                operationProgressContext = new WaitUpdateContext(operationName, Error, _forceFallback, _errorRedirectedFunc, _widthFunc);
                return true;
            }
            operationProgressContext = null;
            return false;
        }
    }

    void IOperationsOwner.Release(object self, bool isSafeExit)
    {
        lock (_lock)
        {
            if (!isSafeExit)
            {
                // this is not a safe exit; dump all active progress contexts and dispose bar context
                // (disposal is still to be managed by callees)
                _multiObjects.Clear();
                _registeredMultiObjects.Clear();
                if (_multiBarContext != null)
                {
                    _multiBarContext.ClearAll();
                    _multiBarContext.Dispose();
                    _multiBarContext = null;
                }
                return;
            }
            if (!_multiObjects.Remove(self, out var guid))
            {
                return;
            }
            _registeredMultiObjects.Remove(guid);
            if (_multiObjects.Count == 0 && _multiBarContext != null)
            {
                _multiBarContext.ClearAll();
                _multiBarContext.Dispose();
                _multiBarContext = null;
            }
        }
    }

    void IOperationProgressContextOwner.GuardRefresh(IGuardedOperationProgressContext context)
    {
        lock (_lock)
        {
            if (_multiObjects.ContainsKey(context))
            {
                context.RefreshGuarded();
            }
        }
    }

    void IOperationProgressContextOwner.GuardCall(IGuardedOperationProgressContext context, float value)
    {
        lock (_lock)
        {
            if (_multiObjects.ContainsKey(context))
            {
                context.ReportGuarded(value);
            }
        }
    }

    void IOperationProgressContextOwner.GuardCallNamed(IGuardedOperationProgressContext context, float value, string name)
    {
        lock (_lock)
        {
            if (_multiObjects.ContainsKey(context))
            {
                context.ReportNamedGuarded(value, name);
            }
        }
    }
}

internal interface IOperationsOwner
{
    void Release(object self, bool isSafeExit);
}

internal interface IOperationProgressContextOwner : IOperationsOwner
{
    void GuardRefresh(IGuardedOperationProgressContext context);

    void GuardCall(IGuardedOperationProgressContext context, float value);

    void GuardCallNamed(IGuardedOperationProgressContext context, float value, string name);
}

internal interface IGuardedOperationProgressContext : IOperationProgressContext
{
    void RefreshGuarded();

    void ReportGuarded(float value);

    void ReportNamedGuarded(float value, string name);
}

internal class WaitUpdateContext : IOperationProgressContext
{
    private readonly BarContext _context;
    private EllipsisSuffixContentFiller _filler;

    public WaitUpdateContext(string name, TextWriter output, bool forceFallback, Func<bool> errorRedirectedFunc, Func<int> widthFunc)
    {
        _context = BarContext.Create(output, forceFallback, errorRedirectedFunc, widthFunc);
        _filler = new EllipsisSuffixContentFiller(name, 0);
        _context.Write(ref _filler);
    }

    public void Report(float value)
    {
        _context.Update(ref _filler);
    }

    public void ReportNamed(float value, string name)
    {
        _filler.SetMessage(name);
        _context.Update(ref _filler);
    }

    public void Refresh()
    {
        _context.Update(ref _filler);
    }

    public void Dispose()
    {
        _context.Clear();
        _context.Dispose();
    }

    public void MarkSafe()
    {
    }
}

internal class WaitUpdateContextForMulti : IGuardedOperationProgressContext
{
    private readonly object _lock = new();
    private readonly MultiBarContext<Guid> _context;
    private readonly Guid _key;
    private EllipsisSuffixContentFiller _filler;
    private readonly IOperationProgressContextOwner _operationsOwner;
    private bool _disposed;
    private bool _safeExit;

    public WaitUpdateContextForMulti(MultiBarContext<Guid> context, Guid key, string name, IOperationProgressContextOwner owner)
    {
        _context = context;
        _key = key;
        _filler = new EllipsisSuffixContentFiller(name, 0);
        _context.Allocate(_key);
        _context.Write(_key, ref _filler);
        _operationsOwner = owner;
    }

    public void Refresh()
    {
        EnsureNotDisposed();
        _operationsOwner.GuardRefresh(this);
    }

    public void Report(float value)
    {
        EnsureNotDisposed();
        _operationsOwner.GuardCall(this, value);
    }

    public void ReportNamed(float value, string name)
    {
        EnsureNotDisposed();
        _operationsOwner.GuardCallNamed(this, value, name);
    }

    void IGuardedOperationProgressContext.RefreshGuarded()
    {
        EnsureNotDisposed();
        _context.Update(_key, ref _filler);
    }

    void IGuardedOperationProgressContext.ReportGuarded(float value)
    {
        EnsureNotDisposed();
        _context.Update(_key, ref _filler);
    }

    void IGuardedOperationProgressContext.ReportNamedGuarded(float value, string name)
    {
        EnsureNotDisposed();
        _filler.SetMessage(name);
        _context.Update(_key, ref _filler);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _context.Clear(_key);
        _context.Remove(_key);
        _context.Dispose();
        lock (_lock)
        {
            _operationsOwner.Release(this, _safeExit);
        }
    }

    public void MarkSafe()
    {
        lock (_lock)
        {
            _safeExit = true;
        }
    }
}

internal class NamedProgressUpdateContext : IOperationProgressContext
{
    private readonly BarContext _context;
    private readonly Stopwatch _stopwatch;
    private TimedNamedProgressPrefabContentFiller _filler;

    public NamedProgressUpdateContext(string name, TextWriter output, bool forceFallback, Func<bool> errorRedirectedFunc, Func<int> widthFunc)
    {
        _context = BarContext.Create(output, forceFallback, errorRedirectedFunc, widthFunc);
        _filler = TimedNamedProgressPrefabContentFiller.Create(name);
        _context.Write(ref _filler);
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void Refresh()
    {
        _filler.SetDuration(_stopwatch.Elapsed);
        _context.Update(ref _filler);
    }

    public void Report(float value)
    {
        _filler.SetDuration(_stopwatch.Elapsed);
        _filler.SetProgress(value);
        _context.Update(ref _filler);
    }

    public void ReportNamed(float value, string name)
    {
        _filler.SetName(name);
        _filler.SetProgress(value);
        _context.Update(ref _filler, acceptAction: UpdateTimer);
    }

    private void UpdateTimer(ref TimedNamedProgressPrefabContentFiller filler)
    {
        filler.SetDuration(_stopwatch.Elapsed);
    }

    public void Dispose()
    {
        _context.Clear();
        _context.Dispose();
    }

    public void MarkSafe()
    {
    }
}

internal class NamedProgressUpdateContextForMulti : IGuardedOperationProgressContext
{
    private readonly object _lock = new();
    private readonly MultiBarContext<Guid> _context;
    private readonly Guid _key;
    private readonly Stopwatch _stopwatch;
    private TimedNamedProgressPrefabContentFiller _filler;
    private readonly IOperationProgressContextOwner _operationsOwner;
    private bool _disposed;
    private bool _safeExit;

    public NamedProgressUpdateContextForMulti(MultiBarContext<Guid> context, Guid key, string name, IOperationProgressContextOwner owner)
    {
        _context = context;
        _key = key;
        _context = context;
        _filler = TimedNamedProgressPrefabContentFiller.Create(name);
        _context.Allocate(_key);
        _context.Write(_key, ref _filler);
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        _operationsOwner = owner;
    }

    public void Refresh()
    {
        EnsureNotDisposed();
        _operationsOwner.GuardRefresh(this);
    }

    public void Report(float value)
    {
        EnsureNotDisposed();
        _operationsOwner.GuardCall(this, value);
    }

    public void ReportNamed(float value, string name)
    {
        EnsureNotDisposed();
        _operationsOwner.GuardCallNamed(this, value, name);
    }

    public void RefreshGuarded()
    {
        EnsureNotDisposed();
        _filler.SetDuration(_stopwatch.Elapsed);
        _context.Update(_key, ref _filler);
    }

    public void ReportGuarded(float value)
    {
        EnsureNotDisposed();
        _filler.SetDuration(_stopwatch.Elapsed);
        _filler.SetProgress(value);
        _context.Update(_key, ref _filler);
    }

    public void ReportNamedGuarded(float value, string name)
    {
        EnsureNotDisposed();
        _filler.SetName(name);
        _filler.SetProgress(value);
        _context.Update(_key, ref _filler, acceptAction: UpdateTimer);
    }

    private void UpdateTimer(ref TimedNamedProgressPrefabContentFiller filler)
    {
        filler.SetDuration(_stopwatch.Elapsed);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _context.Clear(_key);
        _context.Remove(_key);
        _context.Dispose();
        lock (_lock)
        {
            _operationsOwner.Release(this, _safeExit);
        }
    }

    public void MarkSafe()
    {
        lock (_lock)
        {
            _safeExit = true;
        }
    }
}
