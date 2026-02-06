using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMILtools.Core
{
    [Serializable]
    public class GuardsMutable
    {
        [ShowInInspector, ReadOnly, ListDrawerSettings(Expanded = true)]
        public List<GuardCondition> Guards;
    
        public GuardsMutable(params (string name, Func<bool> method)[] guards)
        {
            this.Guards = new List<GuardCondition>(guards.Length);
            foreach (var g in guards)
                this.Guards.Add(new GuardCondition(g.name, g.method));
        }
    
        public GuardsMutable AddGuard(GuardCondition guard)
        {
            Guards.Add(guard);
            return this;
        }
    
        public void AddGuard(params GuardCondition[] guard)
            => Guards.AddRange(guard);
    
        bool AtLeastOneBlocked
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
    
        public static implicit operator bool(GuardsMutable guards) => guards.AtLeastOneBlocked;

    }

    /// <summary>
    /// Intended to be set one in initialization to easily see what bools interact with what guards
    /// </summary>
    [Serializable]
    public readonly struct GuardsImmutable
    {
        [ShowInInspector, ReadOnly, ListDrawerSettings(Expanded = true)] 
        public GuardCondition[] Guards => guards;
        
        readonly GuardCondition[] guards;

        public GuardsImmutable(params (string name, Func<bool> method)[] guards)
        {
            this.guards = new GuardCondition[guards.Length];
            for (int i = 0; i < guards.Length; i++)
            {
                this.guards[i] = new GuardCondition(guards[i].name, guards[i].method);
            }
        }
        
        bool AtLeastOneBlocked
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
    
        public static implicit operator bool(GuardsImmutable guards) => guards.AtLeastOneBlocked;

    }
    
    [Serializable]
    public readonly struct GuardCondition
    {
        [ShowInInspector, ReadOnly]
        public string Name { get; }

        [ShowInInspector, ReadOnly]
        public bool Blocked => check();

        readonly Func<bool> check;

        public GuardCondition(string name, Func<bool> check)
        {
            this.Name = name;
            this.check = check;
        }
        
        public static implicit operator bool(GuardCondition guards) => guards.Blocked;

    }

}
