using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public partial class CustomPerfMonitor {

    private DisposableStopwatch _last;

    /// <summary>
    /// Usage:
    ///     var randInt = new RandomNumberGenerator().Randi();
    ///     Performance.AddCustomMonitor("car/name" + randInt, new Callable(this, MethodName.LastProcessTime));
    ///
    ///     private long _lastProcessTime;
    ///     public long LastProcessTime() => _lastProcessTime;
    ///     ...
    ///
    ///     var perf = new CustomPerfMonitor();
    ///     using (var a = perf.Create()) {
    ///         the bit you want to track the time of
    ///     }
    ///     get the last result with perf.Elapsed
    /// </summary>
    /// <returns></returns>

    public DisposableStopwatch Create() {
        _last = new DisposableStopwatch();
        return _last;
    }

    public long Elapsed => _last.Elapsed.Microseconds;
}

public class DisposableStopwatch : Stopwatch, IDisposable {

    public DisposableStopwatch() {
        Start();
    }

    public void Dispose() {
        Stop();
    }
}