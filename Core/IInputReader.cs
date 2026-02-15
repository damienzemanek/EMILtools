using UnityEngine;

public interface IInputReader<TInputMap>
    where TInputMap : class, IInputMap
{
    public TInputMap InputMap { get; set; }
}
