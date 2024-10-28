using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    internal static class MongoModelMapper
    {
        internal static void RegisterModel<TModel>()
        {
            RegisterDerivedTypes(typeof(TModel));

            foreach(Type type in typeof(TModel).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(m => m.MemberType == (MemberTypes.Property | MemberTypes.Field)).Select(m => m.GetType()))
            {
                RegisterDerivedTypes(type);

                if (type.IsGenericType)
                {
                    foreach(Type genericType in type.GetGenericArguments())
                    {
                        RegisterDerivedTypes(genericType);
                    }
                }
            }
        }

        private static void RegisterDerivedTypes(Type baseType)
        {
            var derivedTypes = Assembly.GetAssembly(baseType)?
                                       .GetTypes()
                                       .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t))
                                       .ToList() ?? [];
            
            BsonClassMap cm = new(baseType);

            cm.AutoMap();

            // Register each derived type as a known type
            foreach (var derivedType in derivedTypes)
            {
                cm.AddKnownType(derivedType);
            }

            BsonClassMap.RegisterClassMap(cm);
        }

        //private static bool IsKnown(Type source, Type target)
        //{
        //    return target.IsSubclassOf(source) || source.GetFields().Aggregate(true, (sofar, next) => sofar || IsKnown(next.FieldType, target));
        //}
    }
}
