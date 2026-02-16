using System;
using UnityEngine;

public interface IInputReader<TInputMap, TSubordinateEnum>
    where TSubordinateEnum : Enum
    where TInputMap : class, IInputMap, new()
{
    public IInputSubordinate<TInputMap, TSubordinateEnum> subordinate { get; set; }
    
}
