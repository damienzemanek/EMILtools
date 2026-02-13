using System;
using EMILtools.Extensions;
using EMILtools.Signals;
using EMILtools.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMILtools.Timers
{
    [Serializable]
    public abstract class Timer
    {
        [ShowInInspector] protected Ref<float> initialTime;
        [ShowInInspector] [ReadOnly] public float Time { get; set; }
        [field: ShowInInspector] [field: ReadOnly] public bool isRunning { get; protected set; } = false;
        public float Progress => Mathf.Clamp01(Time / initialTime);
        public float Duration => initialTime;
        
        [HideInInspector] public PersistentAction OnTimerStart = new();
        [HideInInspector] public PersistentAction OnTimerStop = new();
        [HideInInspector] public PersistentAction OnTimerTick = new();

        public Timer(float _initialTime)
        {
            Debug.Log($"Creating Timer with initial time {_initialTime}");
            initialTime = _initialTime;
        }
        
        public Timer(Ref<float> _initialTime) 
        {
            Debug.Log($"Creating Timer with initial time {_initialTime.val}");
            initialTime = _initialTime;
        }

        public Timer(float _initialTime,
                Action[] OnTimerStartCbs = null,
                Action[] OnTimerTickCbs = null,
                Action[] OnTimerStopCbs = null)
        {
            if(OnTimerStartCbs != null && OnTimerStartCbs.Length > 0) OnTimerStart.Add(OnTimerStartCbs);
            if(OnTimerTickCbs != null && OnTimerTickCbs.Length > 0) OnTimerTick.Add(OnTimerTickCbs);
            if(OnTimerStopCbs != null && OnTimerStopCbs.Length > 0) OnTimerStop.Add(OnTimerStopCbs);

            initialTime = _initialTime;
            isRunning = false;
        }
        
        public void SetInitialTime(float _initialTime) => initialTime = _initialTime;
        public void SetInitialTime(Ref<float> _initialTime) => initialTime = _initialTime;

        public virtual void InitializeTime() => Time = initialTime;

        public virtual void Start()
        {
            InitializeTime();
            if (initialTime <= 0) this.Warn("Please set an initial time for this timer");
            StartCore();
        }

        protected void StartCore()
        {          
            if (!isRunning)
            {
                isRunning = true;
                OnTimerStart?.Invoke();
                //this.Log("Started Timer");
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            OnTimerStop?.Invoke();
            //this.Log("Stopped Timer");
        }

        public void Pause() => isRunning = false;
        public void Resume() => isRunning = true;

        public abstract void TickImplementation(float deltaTime);

        public void TryTick(float deltaTime)
        {
            //Debug.Log("Trying tick");
            if (!isRunning) return;
            if(Time > initialTime) Time = initialTime; // Clamp Time to initialTime
            // this.Log($"Ticking, Prog: {Progress}");
            TickImplementation(deltaTime);
            OnTimerTick.Invoke();
            //Debug.Log("completed tick");

        }
    }
}