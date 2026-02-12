using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace EMILtools.Core
{
    public interface IGuarder { }
    
    public interface IActionGuarder : IGuarder
    {
        IGuardReaction CurrentBlocker { get; }
        bool TryEarlyExit();
    }
    
    public class GuarderMutable : IGuarder
    {
        public IReadOnlyList<Guard> Guards => guards;
        
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)]
        readonly List<Guard> guards;    
        
        public GuarderMutable(params (string name, Func<bool> method)[] guards)
        {
            this.guards = new List<Guard>(guards.Length);
            foreach (var g in guards)
                this.guards.Add(new Guard(g.name, g.method));
        }
    
        public GuarderMutable AddGuard(Guard guard)
        {
            guards.Add(guard);
            return this;
        }
    
        public void AddGuard(params Guard[] guard)
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
    
        public static implicit operator bool(GuarderMutable guarder) => guarder.AnyBlocked;

    }

    /// <summary>
    /// Intended to be set one in initialization to easily see what bools interact with what guards
    /// </summary>
    public readonly struct GuarderImmutable : IGuarder
    {
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)] 
        Guard[] InspectGuards => guards;
        
        readonly Guard[] guards;

        public GuarderImmutable(params (string name, Func<bool> method)[] guards)
        {
            this.guards = new Guard[guards.Length];
            for (int i = 0; i < guards.Length; i++)
            {
                this.guards[i] = new Guard(guards[i].name, guards[i].method);
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
    
        public static implicit operator bool(GuarderImmutable guarder) => guarder.AnyBlocked;
    }
}