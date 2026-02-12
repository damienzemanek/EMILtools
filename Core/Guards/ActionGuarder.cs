using System;
using System.Collections.Generic;
using EMILtools.Core;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;


namespace EMILtools.Core
{
    [Serializable]
    public readonly struct ActionGuard
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
    
    [Serializable]
    public readonly struct LazyActionGuard<TLazyFunc>
        where TLazyFunc : class, ILazyFunc<bool>, new()
    {
        [HorizontalGroup("Top", 250)] [ShowInInspector, ReadOnly]     public string If { get; }
        [HorizontalGroup("Bottom", 250)] [ShowInInspector, ReadOnly]  public readonly string Then;

        
        public Action branchingAction => then;
        [NonSerialized] public readonly Action then;
        
        
        [HorizontalGroup("Top")] [ShowInInspector, ReadOnly, HideLabel]
        public bool Blocked => observed.InvokeLazy() == false;
        [NonSerialized] public readonly TLazyFunc observed;
        
// Need a facotry


        public LazyActionGuard( PersistentAction onChanged, Func<bool> @if, Action then,  
            string ifName, string thenName)
        {
            observed = new TLazyFunc(onChanged, @if);
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


    public abstract class ActionGuarder : IActionGuarder
    {
        public static readonly string NONE = "CAN ACCESS";
        public static readonly IGuardReaction NoneResponsiveGuardCondition = new LazyActionGuard(NONE);
        public abstract IGuardReaction CurrentBlocker { get; }
        public abstract bool TryEarlyExit();

        public bool TryEarlyExit(IEnumerable<IGuardReaction> guards)
        {
            foreach (var guard in guards)
            {
                if (guard.Blocked)
                {
                    guard.branchingAction?.Invoke();
                    return true;
                }
            }
            return false;
        }
    }



    public class ActionGuarderImmutable : ActionGuarder, IActionGuarder
    {
        [ShowInInspector, PropertyOrder(-1)] public override IGuardReaction CurrentBlocker
        {
            get
            {
                for(int i = 0; i < guards.Length; i++)
                    if (guards[i].Blocked) return guards[i];
                return NoneResponsiveGuardCondition;
            }
        }
            
        [ShowInInspector] public readonly IGuardReaction[] guards;

        public ActionGuarderImmutable(params IGuardReaction[] guards) => this.guards = guards;

        public override bool TryEarlyExit() => base.TryEarlyExit(guards);

        public static implicit operator bool(ActionGuarderImmutable chain) => chain.TryEarlyExit();
    }

    public class ActionGuarderMutable : ActionGuarder, IActionGuarder
    {
        [ShowInInspector, PropertyOrder(-1)] public override IGuardReaction CurrentBlocker
        {
            get
            {
                for(int i = 0; i < guards.Count; i++)
                    if (guards[i].Blocked) return guards[i];
                return NoneResponsiveGuardCondition;
            }
        }
            
        [ShowInInspector] public List<IGuardReaction> guards;

        public ActionGuarderMutable()
        {
            guards = new List<IGuardReaction>();
        }
        
        
        public ActionGuarderMutable(params IGuardReaction[] links) => this.guards = new List<IGuardReaction>(links);
            
        public ActionGuarderMutable Add(IGuardReaction guard)
        {
            guards.Add(guard);
            return this;
        }

        public override bool TryEarlyExit() => base.TryEarlyExit(guards);
        public static implicit operator bool(ActionGuarderMutable chain) => chain.TryEarlyExit();

    }
}
