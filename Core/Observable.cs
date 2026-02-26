using System;
using EMILtools.Core;
using Sirenix.OdinInspector;
using UnityEngine;


public interface ILazyFunc<T>
{
    T InvokeLazy();
}

public interface ILazyFuncFactory<TLazyFunc, T>
    where TLazyFunc : ILazyFunc<T>
    where T : struct
{
}


public class LazyFuncFactory<TLazyFunc, T> : ILazyFuncFactory<TLazyFunc, T>
    where TLazyFunc : ILazyFunc<T>, new()
    where T : struct
{

    public LazyFuncFactory() { }
    public TLazyFunc CreateLazyFuncBool(PersistentAction onChanged, Func<bool> func) => _factory(onChanged, func);
    
    static readonly Func<PersistentAction, Func<bool>, TLazyFunc> _factory = 
        (pa, f) => (TLazyFunc)Activator.CreateInstance(typeof(TLazyFunc), pa, f);
}

// Func observing some variable 
// Func needs to know when variable changes
// Func then knows the Invoke next time its get
public class LazyFuncLite<T> : ILazyFunc<T>
    where T : struct
{
    T storedFuncEvaluation;
    readonly Func<T> func;
    T ILazyFunc<T>.InvokeLazy() => storedFuncEvaluation;

    public LazyFuncLite() { }
    
    public LazyFuncLite(PersistentAction onChangedReEvaluate, Func<T> func)
    {
        this.func = func;  
        storedFuncEvaluation = func != null ? func.Invoke() : default(T);
        onChangedReEvaluate?.Add(Evaluate);
    }
    
    public void Dispose(PersistentAction observedOnChanged) => observedOnChanged.Remove(Evaluate);
    
    public static implicit operator T(LazyFuncLite<T> lazyFuncLite) => lazyFuncLite.storedFuncEvaluation;
    

    
    void Evaluate() => storedFuncEvaluation = func.Invoke();
    
}


// Func observing some variable 
// Func needs to know when variable changes
// Func then knows the Invoke next time its get
public class LazyFunc<T> : ILazyFunc<T>
    where T : struct
{
    T storedFuncEvaluation;
    readonly Func<T> func;
    T ILazyFunc<T>.InvokeLazy() => storedFuncEvaluation;
    
    [NonSerialized] PersistentAction _onChangedReEvaluate;

    public LazyFunc() { }
    
    public LazyFunc(PersistentAction onChangedReEvaluate, Func<T> func)
    {
        this.func = func;
        if(func == null) storedFuncEvaluation = default;
        else storedFuncEvaluation = func.Invoke();
        if (onChangedReEvaluate == null) _onChangedReEvaluate = null;
        else
        {
            _onChangedReEvaluate = onChangedReEvaluate;
            _onChangedReEvaluate.Add(Evaluate);
        }
    }
    
    public void Dispose() => _onChangedReEvaluate.Remove(Evaluate);
    
    public static implicit operator T(LazyFunc<T> lazyFuncLite) => lazyFuncLite.storedFuncEvaluation;
    
    
    void Evaluate() => storedFuncEvaluation = func.Invoke();
}