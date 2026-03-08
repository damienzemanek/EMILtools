using System;
using EMILtools.Core;

public class LazyFuncFactory<TLazyFunc, T> : ILazyFuncFactory<TLazyFunc, T>
    where TLazyFunc : ILazyFunc<T>, new()
    where T : struct
{

    public LazyFuncFactory() { }
    public TLazyFunc CreateLazyFuncBool(PersistentAction onChanged, Func<bool> func) => _factory(onChanged, func);
    
    static readonly Func<PersistentAction, Func<bool>, TLazyFunc> _factory = 
        (pa, f) => (TLazyFunc)Activator.CreateInstance(typeof(TLazyFunc), pa, f);
}