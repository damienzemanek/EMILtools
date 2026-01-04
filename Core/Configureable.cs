using System;
using System.Linq;
using System.Reflection;
using UnityEngine;


public interface IBinaryChoice { }
public interface No


public interface IConfigUser { }

public interface IConfigureable
{
    void Initialize<T>(T val, bool isRef = false)
        where T : struct;
}

public static class ConfigInitializer
{
    public static void InitializeConfigurables(this IConfigUser user)
    {
        var configureableFields = user.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => typeof(IConfigureable).IsAssignableFrom(f.FieldType))
            .ToList();

        foreach (var field in configureableFields)
        {
            var value = field.GetValue(user);
            ((IConfigureable)value).EnsureRefInitialized
        }
    }
}


/// <summary>
/// Value-types that can be optionally set to be reference types for stable configuration
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
public struct Configureable<T> : IConfigureable
    where T : struct
{
    [SerializeField] T val;
    [NonSerialized] Ref<T> reference;
    [SerializeField] bool canConfigure;
    public T Value
    {
        get => (canConfigure && EnsureRefInitialized) ? reference.val : val;
        set
        {
            if (canConfigure && EnsureRefInitialized) reference.val = value;
            else val = value;
        }
    }
    public void Initialize(T val, bool isRef = false)
    {
        this.val = val;
        this.canConfigure = isRef;
        if(isRef) LazyInitializeReference();
    }
    
    public bool EnsureRefInitialized
    {
        get 
        {
            if (canConfigure && reference == null)
                reference = new Ref<T>(val);
            return true;
        }
    }
    
    void LazyInitializeReference() => reference = new Ref<T>(val);
}