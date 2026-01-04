using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
[InlineProperty]
public class Ref<T> where T : struct
{
    [HorizontalGroup("Split", Width = 0.3f)]
    [SerializeField, HideLabel] [VerticalGroup("Split/Right")] protected T baseValue;
            
    [ShowInInspector]
    public virtual T Value { get => baseValue; set => baseValue = value;}
            
    public virtual ref T ValueRef => ref baseValue;
            
    public Ref(T initialValue) => baseValue = initialValue;
    public Ref(ref T initialValue) => baseValue = initialValue;
}

