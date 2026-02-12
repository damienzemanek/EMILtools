using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMILtools.Core
{
    public interface IGuardCondition
    {
        string If { get; }
        bool Blocked { get; }
    }
    

    public interface IGuardReaction : IGuardCondition
    {
        public Action branchingAction { get; }
    }

    
    public readonly struct LazyGuard : IGuardCondition
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
    
    public readonly struct Guard : IGuardCondition
    {
        [ShowInInspector, ReadOnly]
        public string If { get; }

        [ShowInInspector, ReadOnly]
        public bool Blocked => observed();

        readonly Func<bool> observed;

        public Guard(string name, Func<bool> observed)
        {
            If = name;
            this.observed = observed;
        }
        
        public static implicit operator bool(Guard guards) => guards.Blocked;

    }
    
    [Serializable]
    public readonly struct LazyActionGuard : IGuardReaction
    {
        [HorizontalGroup("Top", 250)] [ShowInInspector, ReadOnly]     public string If { get; }
        [HorizontalGroup("Bottom", 250)] [ShowInInspector, ReadOnly]  public readonly string Then;

        
        public Action branchingAction => then;
        [NonSerialized] public readonly Action then;
        
        
        [HorizontalGroup("Top")] [ShowInInspector, ReadOnly, HideLabel]
        public bool Blocked => observed == false;
        [NonSerialized] public readonly LazyFuncLite<bool> observed;
        


        public LazyActionGuard( PersistentAction onChanged, Func<bool> @if, Action then,  
                                                                   string ifName, string thenName)
        {
            observed = new LazyFuncLite<bool>(onChanged, @if);
            this.then = then;

            If = string.IsNullOrWhiteSpace(ifName) ? @if?.Method.Name : ifName;
            Then = string.IsNullOrWhiteSpace(thenName)
                ? (then != null ? then.Method.Name : "return")
                : thenName;

            If ??= "null-check";
        }

        public LazyActionGuard(PersistentAction onChanged, Func<bool> @if, Action then)
        {
            observed = new LazyFuncLite<bool>(onChanged, @if);
            this.then = then;

            If = @if?.Method.Name ?? "null-check";
            Then = then != null ? then.Method.Name : "return";
        }
        
        public LazyActionGuard(string CANACCESS = "CAN ACCESS")
        {
            If = CANACCESS;
            Then = CANACCESS;
            observed = new LazyFuncLite<bool>(null, null);
            then = null;
        }

    }
    
    [Serializable]
    public readonly struct ActionGuard : IGuardReaction
    {

        [HorizontalGroup("Top", 250)] [ShowInInspector, ReadOnly] public string If { get; }
        [HorizontalGroup("Bottom", 250)] [ShowInInspector, ReadOnly] public readonly string Then;

        public Action branchingAction => then;
        [HideInInspector] public readonly Action then;
        
        
        [HideInInspector] public readonly Func<bool> observed;
        [HorizontalGroup("Top")] [ShowInInspector, ReadOnly, HideLabel]
        public bool Blocked => observed?.Invoke() ?? false;


        public ActionGuard(Func<bool> @if, Action then, string ifName, string thenName)
        {
            observed = @if;
            this.then = then;

            If = string.IsNullOrWhiteSpace(ifName) ? @if?.Method.Name : ifName;
            Then = string.IsNullOrWhiteSpace(thenName)
                ? (then != null ? then.Method.Name : "return")
                : thenName;

            If ??= "null-check";
            Then ??= "null-action";
        }
        
        public ActionGuard(Func<bool> @if, string ifName)
        {
            observed = @if;
            this.then = null;

            If = string.IsNullOrWhiteSpace(ifName) ? @if?.Method.Name : ifName;
            Then = "return";
            If ??= "null-check";
        }

        public ActionGuard(Func<bool> @if, Action then)
        {
            observed = @if;
            this.then = then;

            If = @if?.Method.Name ?? "null-check";
            Then = then != null ? then.Method.Name : "return";
        }
        
        
    }


}
