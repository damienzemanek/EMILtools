using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMILtools.Core
{

    
    public readonly struct LazyGuard 
    {
        [ShowInInspector, ReadOnly]
        public string If { get; }

        [ShowInInspector, ReadOnly]
        public bool Blocked => observed;

        readonly LazyFuncLite<bool> observed;
        
        public LazyGuard(string name, PersistentAction observedOnChanged,  Func<bool> observed)
        {
            If = name;
            this.observed = new LazyFuncLite<bool>(observedOnChanged, observed);
        }
        
        public static implicit operator bool(LazyGuard guards) => guards.Blocked;

    }
    
    
    


}
