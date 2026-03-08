public interface ILazyFuncFactory<TLazyFunc, T>
    where TLazyFunc : ILazyFunc<T>
    where T : struct
{
}