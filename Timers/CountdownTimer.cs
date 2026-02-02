using System;
using EMILtools.Core;
using EMILtools.Signals;

namespace EMILtools.Timers
{
    [Serializable]
    public class CountdownTimer : Timer
    { 
        public CountdownTimer(float _initialTime) : base(_initialTime) { }
        public CountdownTimer(Ref<float> _initialTime) : base(_initialTime) { }

        
        public CountdownTimer(float _initialTime, bool isRef, 
            Action[] OnTimerStartCbs = null, Action[] OnTimerTickCbs = null, Action[] OnTimerStopCbs = null)
        : base (_initialTime, OnTimerStartCbs, OnTimerTickCbs, OnTimerStopCbs) { }
        
        public override void TickImplementation(float deltaTime)
        {
            if (Time > 0) Time -= deltaTime;
            if (Time <= 0) { Time = 0; Stop(); }
        }
        
        public bool isFinished() => Time <= 0;
        public void Reset() => Time = initialTime;
        public void Reset(float newInitialTime) => Time = newInitialTime;
    }
}

