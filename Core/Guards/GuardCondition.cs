using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMILtools.Core
{

    public readonly struct LazyGuardCondition
    {
        [ShowInInspector, ReadOnly]
        public string Name { get; }

        [ShowInInspector, ReadOnly]
        public bool Blocked => observed;

        readonly LazyFuncLite<bool> observed;

        public LazyGuardCondition(string name, PersistentAction observedOnChanged,  Func<bool> observed)
        {
            this.Name = name;
            this.observed = new LazyFuncLite<bool>(observedOnChanged, observed);
        }
        
        public static implicit operator bool(LazyGuardCondition guards) => guards.Blocked;

    }
    
    public readonly struct GuardCondition
    {
        [ShowInInspector, ReadOnly]
        public string Name { get; }

        [ShowInInspector, ReadOnly]
        public bool Blocked => observed();

        readonly Func<bool> observed;

        public GuardCondition(string name, Func<bool> observed)
        {
            this.Name = name;
            this.observed = observed;
        }
        
        public static implicit operator bool(GuardCondition guards) => guards.Blocked;

    }

}
