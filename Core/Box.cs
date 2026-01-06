namespace EMILtools.Core
{
    public static class Box
    {
        public static void FastHeap<T, T2>(this OptionalRef<T> or, T2 operationZone)
            where T : struct
            => or.PullHeap();
    }
}