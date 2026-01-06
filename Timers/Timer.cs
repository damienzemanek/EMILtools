using System;
using EMILtools.Extensions;
using EMILtools.Signals;
using EMILtools.Core;
using static EMILtools.Core.Stabilizer;

namespace EMILtools.Timers
{
    [Serializable]
    public abstract class Timer : IStablizableUser
    {
        protected Stablizable<float> initialTime;
        protected float Time { get; set; }
        public bool isRunning { get; protected set; } = false;
        public float Progress => Time / initialTime.Get;
        public float Duration => initialTime.Get;
        
        public PersistentAction OnTimerStart = new();
        public PersistentAction OnTimerStop = new();
        public PersistentAction OnTimerTick = new();

        public Timer(float _initialTime)
            => initialTime.stack = _initialTime;
        
        public Timer(float _initialTime,
                Action[] OnTimerStartCbs = null,
                Action[] OnTimerTickCbs = null,
                Action[] OnTimerStopCbs = null)
        {
            if(OnTimerStartCbs != null && OnTimerStartCbs.Length > 0) OnTimerStart.Add(OnTimerStartCbs);
            if(OnTimerTickCbs != null && OnTimerTickCbs.Length > 0) OnTimerTick.Add(OnTimerTickCbs);
            if(OnTimerStopCbs != null && OnTimerStopCbs.Length > 0) OnTimerStop.Add(OnTimerStopCbs);

            initialTime.stack = _initialTime;
            isRunning = false;
        }

        public void Start()
        {
            if (initialTime.Get <= 0)
            {
                this.Warn("Please set an initial time for this timer");
                return;
            }

            Time = initialTime.Get;
            if (!isRunning)
            {
                isRunning = true;
                OnTimerStart?.Invoke();
                this.Log("Started Timer");
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            OnTimerStop?.Invoke();
            this.Log("Stopped Timer");
        }

        public void Pause() => isRunning = false;
        public void Resume() => isRunning = true;

        public abstract void TickImplementation(float deltaTime);

        public void TryTick(float deltaTime)
        {
            if (!isRunning) return;
            // this.Log($"Ticking, Prog: {Progress}");
            TickImplementation(deltaTime);
            OnTimerTick.Invoke();

        }
    }
}