using System;
using EMILtools.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMILtools.Timers
{
    [Serializable]
    public class DecayTimer : Timer
    {
        [ShowInInspector] Ref<float> decayMult;

        public DecayTimer(float _initialTime, float _decayMult) : base(_initialTime)
         => decayMult = _decayMult;
        public DecayTimer(float _initialTime, Ref<float> _decayMult) : base(_initialTime)
            => decayMult = _decayMult;
        public DecayTimer(Ref<float> _initialTime, Ref<float> _decayMult) : base(_initialTime)
            => decayMult = _decayMult;
        public DecayTimer(float _initialTime, Action[] OnTimerStartCbs = null, Action[] OnTimerTickCbs = null, Action[] OnTimerStopCbs = null) : base(_initialTime, OnTimerStartCbs, OnTimerTickCbs, OnTimerStopCbs) { }

        public override void TickImplementation(float deltaTime)
        {
            //Debug.Log("Decaying: " + Time);
            if (Time > 0) Time -= (deltaTime * decayMult);
            if (Time < 0) { Time = 0; }
        }
        
        public void ResetToFull() => Time = initialTime;
        public void ResetToFullNewInitial(float newInitialTime) => Time = newInitialTime;
        public void ResetToFullyDecayed() => Time = 0;
        
        public override void Start()
        {
            ResetToFullyDecayed();
            if (!isRunning)
            {
                isRunning = true;
                OnTimerStart?.Invoke();
                this.Log("Started Decay Timer");
            }
        }
    }
}