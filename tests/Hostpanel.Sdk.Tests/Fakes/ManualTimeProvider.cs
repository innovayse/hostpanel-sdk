namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when a test calls <see cref="Advance"/>, so a deadline is tested
/// without waiting. A timer fires once, on the <see cref="Advance"/> that takes the clock to its due time or past it.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    /// <summary>Guards the clock and the timers.</summary>
    private readonly object _gate = new();

    /// <summary>Holds the timers that have not fired or been disposed.</summary>
    private readonly List<ManualTimer> _timers = [];

    /// <summary>The time the provider believes it is.</summary>
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Gets how many timers are waiting to fire.</summary>
    public int PendingTimers
    {
        get
        {
            lock (_gate)
            {
                return _timers.Count;
            }
        }
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    /// <summary>Moves the clock forward and fires every timer that has come due.</summary>
    /// <param name="by">How far to move the clock.</param>
    public void Advance(TimeSpan by)
    {
        List<ManualTimer> due;
        lock (_gate)
        {
            _now += by;
            due = _timers.Where(timer => timer.DueAt <= _now).ToList();
            foreach (var timer in due)
            {
                _timers.Remove(timer);
            }
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    /// <summary>Schedules a timer for a moment, or takes it off the schedule when the moment is infinite.</summary>
    /// <param name="timer">The timer.</param>
    /// <param name="dueTime">How long from now it fires.</param>
    private void Schedule(ManualTimer timer, TimeSpan dueTime)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                timer.DueAt = DateTimeOffset.MaxValue;
                return;
            }

            timer.DueAt = _now + dueTime;
            _timers.Add(timer);
        }
    }

    /// <summary>Takes a timer off the schedule.</summary>
    /// <param name="timer">The timer.</param>
    private void Unschedule(ManualTimer timer)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
        }
    }

    /// <summary>A one shot timer of <see cref="ManualTimeProvider"/>.</summary>
    private sealed class ManualTimer : ITimer
    {
        /// <summary>The provider that owns the clock.</summary>
        private readonly ManualTimeProvider _owner;

        /// <summary>The method to call when the timer fires.</summary>
        private readonly TimerCallback _callback;

        /// <summary>The state passed to the callback.</summary>
        private readonly object? _state;

        /// <summary>Initializes a new instance of the <see cref="ManualTimer"/> class.</summary>
        /// <param name="owner">The provider that owns the clock.</param>
        /// <param name="callback">The method to call when the timer fires.</param>
        /// <param name="state">The state passed to the callback.</param>
        public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
        {
            _owner = owner;
            _callback = callback;
            _state = state;
        }

        /// <summary>Gets or sets the moment the timer fires.</summary>
        public DateTimeOffset DueAt { get; set; } = DateTimeOffset.MaxValue;

        /// <inheritdoc />
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _owner.Schedule(this, dueTime);
            return true;
        }

        /// <summary>Calls the callback.</summary>
        public void Fire()
        {
            _callback(_state);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _owner.Unschedule(this);
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
