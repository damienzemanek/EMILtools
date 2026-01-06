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
public class RefSync<T> where T : struct
{
    public T val;
    public Action OnValueChanged;
    
    public RefSync(T initialValue) => val = initialValue;
}