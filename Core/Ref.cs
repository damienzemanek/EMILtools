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

public interface IRefBox { }

[Serializable]
[InlineProperty]
public class RefBox<T> : IRefBox
    where T : struct
{
    public T unbox;
    public RefBox(T initialValue) => unbox = initialValue;

    public static implicit operator T(RefBox<T> rb) => rb.unbox;
}