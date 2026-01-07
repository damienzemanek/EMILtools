using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EMILtools.Core;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
[InlineProperty]
public class Ref<T> where T : struct
{
    public T val;
    public virtual ref T ValueRef => ref val;
    public Ref(T initialValue) => val = initialValue;
    public Ref(ref T initialValue) => val = initialValue;
    public static implicit operator T(Ref<T> r) => (r != null) ? r.val : default;
}

[Serializable]
[InlineProperty]
public class Box<T> 
    where T : struct
{
    public T unbox;
    public static implicit operator T(Box<T> rb) => rb.unbox;
    public static implicit operator Fluid<T>(Box<T> rb) =>
        new Fluid<T>() { isRef = true, sharedheap = rb, stack = rb.unbox };
}