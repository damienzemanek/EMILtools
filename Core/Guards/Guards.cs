using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace EMILtools.Core
{
    public class GuardsMutable
    {
        public IReadOnlyList<GuardCondition> Guards => guards;
        
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)]
        readonly List<GuardCondition> guards;    
        
        public GuardsMutable(params (string name, Func<bool> method)[] guards)
        {
            this.guards = new List<GuardCondition>(guards.Length);
            foreach (var g in guards)
                this.guards.Add(new GuardCondition(g.name, g.method));
        }
    
        public GuardsMutable AddGuard(GuardCondition guard)
        {
            guards.Add(guard);
            return this;
        }
    
        public void AddGuard(params GuardCondition[] guard)
            => guards.AddRange(guard);
    
        bool AnyBlocked
        {
            get
            {
                for (int i = 0; i < Guards.Count; i++)
                {
                    if (Guards[i].Blocked) return true;
                }
                return false;
            }
        }
    
        public static implicit operator bool(GuardsMutable guards) => guards.AnyBlocked;

    }

    /// <summary>
    /// Intended to be set one in initialization to easily see what bools interact with what guards
    /// </summary>
    public readonly struct GuardsImmutable
    {
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)] 
        GuardCondition[] InspectGuards => guards;
        
        readonly GuardCondition[] guards;

        public GuardsImmutable(params (string name, Func<bool> method)[] guards)
        {
            this.guards = new GuardCondition[guards.Length];
            for (int i = 0; i < guards.Length; i++)
            {
                this.guards[i] = new GuardCondition(guards[i].name, guards[i].method);
            }
        }
        
        bool AnyBlocked
        {
            get
            {
                for (int i = 0; i < guards.Length; i++)
                {
                    if (guards[i].Blocked) return true;
                }
                return false;
            }
        }
    
        public static implicit operator bool(GuardsImmutable guards) => guards.AnyBlocked;
    }
}