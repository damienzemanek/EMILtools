using System;
using UnityEngine;

namespace EMILtools.Extensions
{
    public static class NumEX
    {
        public const float ZeroF = 0f;
        public static readonly Index LastIndex = ^1;
        private const float NinetyF = 90f;

        /// <summary>
        /// Ensure Tolerance is between 0 and 1
        /// </summary>
        /// <param name="set"></param>
        /// <param name="compare"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static float ToleranceSet(float value, float target, float tolerance)
        {
            tolerance = Mathf.Clamp01(tolerance);
            float range = Mathf.Abs(target) * tolerance;
            return Mathf.Abs(value - target) <= range ? target : value;
        }

        /// <summary>
        /// Flips 0 to 1 and 1 to 0
        /// or 0.25 to 0.75
        /// or 0.5 to 0.5
        /// or 0.75 to 0.25
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static float Flip01(float val)
        => Mathf.Abs(val - 1f);
    }
}