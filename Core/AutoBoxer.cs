using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EMILtools.Core
{
    
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoBoxAttribute : Attribute { }
    
    public interface IBoxUser { }
    public interface IBoxable
    {
        void Box();
    }
    
    public static class AutoBoxer
    {
        public static void BoxAutos(this IBoxUser user)
        {
            //Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IBoxable).IsAssignableFrom(f.FieldType)
                            && CustomAttributeExtensions.GetCustomAttribute<AutoBoxAttribute>((MemberInfo)f) != null)
                .ToList();
            //Debug.Log("Fields marked with [AutoBox]: " + stableFields.Count);
            user.BoxFields(stableFields);
        }

        public static void BoxAll(this IBoxUser user)
        {
            //Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IBoxable).IsAssignableFrom(f.FieldType))
                .ToList();
            //Debug.Log("Stabilizing All fields " + stableFields.Count);
            
            user.BoxFields(stableFields);
        }

        static void BoxFields(this IBoxUser user, List<FieldInfo> stableFields)
        {
            foreach (var field in stableFields)
            {
                var value = field.GetValue(user);
                ((IBoxable)value).Box();
                field.SetValue(user, value); // re-assining back struct value
            }
        }
       
    }
}