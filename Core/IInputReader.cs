using System;
using UnityEngine;

public interface IInputReader<TInputMap>
    where TInputMap : class, IInputMap, new()
{
    
}


public interface IInputReaderSubordinate<TInputMap, TSubordinateEnum> : IInputReader<TInputMap>
    where TSubordinateEnum : Enum
    where TInputMap : class, IInputMap, new()
{
    public IInputSubordinate<TInputMap, TSubordinateEnum> subordinate { get; set; }
    
}