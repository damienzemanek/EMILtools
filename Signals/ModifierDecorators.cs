using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EMILtools.Core;
using EMILtools.Timers;
using Sirenix.OdinInspector;
using UnityEngine;
using static EMILtools.Signals.ModifierStrategies;
using static EMILtools.Signals.StatTags;
using static EMILtools.Timers.TimerUtility;

namespace EMILtools.Signals
{
    
    /// <summary>
    /// Previous Problem: Decs couldn't block the application of the mod, only react to it after the fact.
    ///     This meant that if you wanted to have a mod that was only applied under certain conditions,
    ///     you had to put those conditions in the mod itself, which could lead to messy code and made
    ///     it harder to reuse mods. 
    /// 
    /// Solution: Decs can now be blocked via the TryApplyThruDecoratorFirst method, which returns a
    ///     struct containing the output value and a bool indicating whether the mod application should
    ///     be blocked. This allows for more flexible and powerful modifiers, as they can now have
    ///     conditions that prevent them from being applied in certain situations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct DecApplyAttemptInfo<T>
        where T : struct
    {
        public T output;
        public bool blocked;
    }
    
    public interface IStatModDecorator<T, TTag>
        where T : struct
        where TTag : struct, IStatTag
    {
        public bool removable { get; set; }
        public ulong hash { get; set; } // Used to isolate the correct ModSlot
        public Type tmodType { get; } // Used to isolate the correct List<TMod> in the ModSlot, from the TMod type next to it in the tuple (tmodtype, object)
        public Stat<T, TTag> stat { get; set; }
        public DecApplyAttemptInfo<T> TryApplyThruDecoratorFirst(T input);
        public Action OnAdd { get; set; }
        public Action OnRemove{ get; set; }
    }
    
    [Serializable]
    public abstract class StatModDecorator<T, TMod, TTag> : IStatModDecorator<T, TTag>
        where T : struct
        where TMod : struct,  IStatModStrategy<T>
        where TTag : struct, IStatTag
    {
        public bool removable { get; set; }
        public ulong hash { get; set; }
        public Type tmodType => typeof(TMod);
        public Stat<T, TTag> stat { get; set; }
        public abstract DecApplyAttemptInfo<T> TryApplyThruDecoratorFirst(T input);
        public Action OnAdd { get; set; } = delegate { };
        public Action OnRemove { get; set; } = delegate { };

        public StatModDecorator() { }
        
        public StatModDecorator(ulong hash)
        {
            removable = false;
            this.hash = hash;
        }

        public void RemoveModifier()
        {
            removable = true;
            stat.RemoveModifier(hash);
        }
    }

    
    public interface IGate
    {
        bool Value { get; set; }
    }

    [Serializable]
    public class Bool : IGate
    {
        public bool enabled;
        public bool Value { get => enabled; set => enabled = value; }

        public Bool() { enabled = false; }
        public Bool(bool initial) => enabled = initial;
    }

    [Serializable]
    public class RI : IGate
    {
        public ReactiveIntercept<bool> enabled;
        public bool Value { get => enabled.Value; set => enabled.Value = value; }
        
        public RI() { enabled = new ReactiveIntercept<bool>(false); }
        public RI(bool initial) => enabled = new ReactiveIntercept<bool>(initial);
    }
    
    [Serializable]
    public class StatModDecToggleable<T, TMod, TTag, TGate> : StatModDecorator<T, TMod, TTag>
        where T: struct
        where TMod : struct, IStatModStrategy<T>
        where TTag : struct, IStatTag
        where TGate : class, IGate, new()
    {
        TGate gate = new TGate();

        [ShowInInspector, ReadOnly]
        public bool Enabled
        {
            get => gate.Value;
            set
            {
                if (gate.Value == value) return;
                gate.Value = value;
                if(stat != null) stat.dirty = true;
            }
        }

        public StatModDecToggleable() { }
        public StatModDecToggleable(ulong hash, bool startEnabled) : base(hash)
         => gate.Value = startEnabled;

        public override DecApplyAttemptInfo<T> TryApplyThruDecoratorFirst(T input)
        {
            //Debug.Log($"[StatModDecToggleable] TryApplyThruDecoratorFirst called with input: {input}, enabled: {gate.Value}");
            var DecInfo = new DecApplyAttemptInfo<T>();
            if (Enabled)
            {
                DecInfo.output = input;
                DecInfo.blocked = false;
            }
            else
            {
                DecInfo.output = input;
                DecInfo.blocked = true;
            }
            return DecInfo;
        }
        
        public static implicit operator bool(StatModDecToggleable<T, TMod, TTag, TGate> dec) => dec.gate.Value;
    }
    
    
    [Serializable]
    public class StatModDecTimed<T, TMod, TTag, TGate> : StatModDecToggleable<T, TMod, TTag, TGate>, ITimerUser
        where T : struct
        where TMod : struct, IStatModStrategy<T>
        where TTag : struct, IStatTag
        where TGate : class, IGate, new()
    {
        public CountdownTimer timer;

        public override DecApplyAttemptInfo<T> TryApplyThruDecoratorFirst(T input)
        {
            var DecInfo = base.TryApplyThruDecoratorFirst(input);
            return DecInfo;
        }
        
        public StatModDecTimed() { }
        
        public StatModDecTimed(ulong hash, CountdownTimer timer, bool startEnabled = true, 
            Action[] OnDecorAddCBs = null, Action[] OnDecorRemoveCBs = null) 
                : base(hash, startEnabled)
        {
            removable = false;
            this.timer = timer;
            this.InitTimers((timer, false));
            this.Sub(timer.OnTimerStop, RemoveModifier);
            
            OnAdd += timer.Start;
            OnRemove += this.ShutdownTimers;
            
            if(OnDecorAddCBs != null && OnDecorAddCBs.Length > 0)
                foreach(var cb in OnDecorAddCBs)
                    OnAdd += cb;
            
            if(OnDecorRemoveCBs != null && OnDecorRemoveCBs.Length > 0)
                foreach(var cb in OnDecorRemoveCBs)
                    OnRemove += cb;
        }
        

        public void ForceStop(Stat<T,TTag> stat)
        {
            removable = true;
            this.stat = stat;
            timer.OnTimerStop?.Invoke();
        }

    }
}
