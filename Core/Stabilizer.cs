using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EMILtools.Core
{
    
    [AttributeUsage(AttributeTargets.Field)]
    public class StabilizeAttribute : Attribute { }
    
    public interface IStablizableUser { }
    public interface IStablizable
    {
        /// <summary>
        /// param fromStabilizer is not intended for manual Stabilization.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="fromStabilizer"></param>
        void Stabilize(IStablizableUser user, bool fromStabilizer = false);
    }
    
    public static class Stabilizer
    {
        public static HashSet<IStablizableUser> stabilizedUsers = new();

        public static void StabilizeAttributed(this IStablizableUser user)
        {
            //Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IStablizable).IsAssignableFrom(f.FieldType)
                            && CustomAttributeExtensions.GetCustomAttribute<StabilizeAttribute>((MemberInfo)f) != null)
                .ToList();
            //Debug.Log("Fields marked with [Stabilize]: " + stableFields.Count);

            user.StabilizeFields(stableFields);
        }

        public static void StabilizeAll(this IStablizableUser user)
        {
            //Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IStablizable).IsAssignableFrom(f.FieldType))
                .ToList();
            //Debug.Log("Stabilizing All fields " + stableFields.Count);


            user.StabilizeFields(stableFields);
        }

        static void StabilizeFields(this IStablizableUser user, List<FieldInfo> stableFields)
        {
            foreach (var field in stableFields)
            {
                var value = field.GetValue(user);
                ((IStablizable)value).Stabilize(user);
                field.SetValue(user, value); // re-assining back struct value
                //Debug.Log($"Initialized reference on field {field.Name}");
            }

            // Avoid re-adding
            if (!Stabilizer.stabilizedUsers.Contains(user))
                stabilizedUsers.Add(user);
        }
       
    }
}