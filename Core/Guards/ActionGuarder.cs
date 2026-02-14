using System;
using System.Collections.Generic;
using EMILtools.Core;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;


namespace EMILtools.Core
{
    [Serializable]
    public class ActionGuard : IGuardAction
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
        
        public ActionGuard(Func<bool> @if)
        {
            observed = @if;
            this.then = null;

            If = @if?.Method.Name;
            Then = "return";
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
    public class LazyActionGuard<TLazyFunc> : IGuardAction
        where TLazyFunc : class, ILazyFunc<bool>, new()
    {
        [HorizontalGroup("Top", 250), PropertyOrder(-1)] [ShowInInspector, ReadOnly]     public string If { get; }
        [HorizontalGroup("Bottom", 250)] [ShowInInspector, ReadOnly]  public readonly string Then;

        
        public Action branchingAction => then;
        [NonSerialized] public readonly Action then;
        
        
        [HorizontalGroup("Top"), PropertyOrder(-1)] [ShowInInspector, ReadOnly, HideLabel]
        public bool Blocked => observed.InvokeLazy();
        [NonSerialized] public readonly TLazyFunc observed;



        public LazyActionGuard( PersistentAction onChanged, Func<bool> @if, Action then,  
            string ifName, string thenName)
        {
            observed = new LazyFuncFactory<TLazyFunc, bool>().CreateLazyFuncBool(onChanged, @if);
            this.then = then;

            If = string.IsNullOrWhiteSpace(ifName) ? @if?.Method.Name : ifName;
            Then = string.IsNullOrWhiteSpace(thenName)
                ? (then != null ? then.Method.Name : "return")
                : thenName;

            If ??= "null-check";
        }

        public LazyActionGuard(PersistentAction onChanged, Func<bool> @if, Action then)
        {
            observed = new LazyFuncFactory<TLazyFunc, bool>().CreateLazyFuncBool(onChanged, @if);
            this.then = then;

            If = @if?.Method.Name ?? "null-check";
            Then = then != null ? then.Method.Name : "return";
        }
        
        public LazyActionGuard(PersistentAction onChanged, Func<bool> @if)
        {
            observed = new LazyFuncFactory<TLazyFunc, bool>().CreateLazyFuncBool(onChanged, @if);
            this.then = null;

            If = @if?.Method.Name ?? "null-check";
            Then = "return";
        }
        
        public LazyActionGuard(PersistentAction onChanged, Func<bool> @if, string ifName)
        {
            observed = new LazyFuncFactory<TLazyFunc, bool>().CreateLazyFuncBool(onChanged, @if);
            this.then = null;

            If = ifName;
            Then = "return";
        }
        
        public LazyActionGuard(string CANACCESS = "CAN ACCESS")
        {
            If = CANACCESS;
            Then = CANACCESS;
            observed = new LazyFuncFactory<TLazyFunc, bool>().CreateLazyFuncBool(null, null);
            then = null;
        }

    }


    public abstract class ActionGuarder : IActionGuarder
    {
        public static readonly string NONE = "CAN ACCESS";
        public static readonly LazyActionGuard<LazyFunc<bool>> NoneResponsiveGuardConditionNonLazy = new(NONE);

        public abstract IGuardAction CurrentBlocker { get; }
        public abstract bool TryEarlyExit();

        public bool TryEarlyExit(IEnumerable<IGuardAction> guards)
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
        [ShowInInspector, PropertyOrder(-1)] public override IGuardAction CurrentBlocker
        {
            get
            {
                for(int i = 0; i < guards.Length; i++)
                    if (guards[i].Blocked) return guards[i];
                return NoneResponsiveGuardConditionNonLazy;
            }
        }
            
        [ShowInInspector] public readonly IGuardAction[] guards;

        public ActionGuarderImmutable(params IGuardAction[] guards) => this.guards = guards;

        public override bool TryEarlyExit() => base.TryEarlyExit(guards);

        public static implicit operator bool(ActionGuarderImmutable chain) => chain.TryEarlyExit();
    }

    public class ActionGuarderMutable : ActionGuarder, IActionGuarder
    {
        [ShowInInspector, PropertyOrder(-1)] public override IGuardAction CurrentBlocker
        {
            get
            {
                for(int i = 0; i < guards.Count; i++)
                    if (guards[i].Blocked) return guards[i];
                return NoneResponsiveGuardConditionNonLazy;
            }
        }
            
        [ShowInInspector] public List<IGuardAction> guards;

        public ActionGuarderMutable()
        {
            guards = new List<IGuardAction>();
        }
        
        
        public ActionGuarderMutable(params IGuardAction[] links) => this.guards = new List<IGuardAction>(links);
            
        public ActionGuarderMutable Add(IGuardAction guard)
        {
            guards.Add(guard);
            return this;
        }

        public override bool TryEarlyExit() => base.TryEarlyExit(guards);
        public static implicit operator bool(ActionGuarderMutable chain) => chain.TryEarlyExit();

    }
}
