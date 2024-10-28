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
            RegisterDerivedTypes<TModel>(typeof(TModel));
        }

        private static void RegisterDerivedTypes<TModel>(Type baseType)
        {;
            var derivedTypes = Assembly.GetAssembly(baseType)?
                                       .GetTypes()
                                       .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t))
                                       .ToList() ?? [];

            BsonClassMap.RegisterClassMap<TModel>(cm =>
            {
                cm.AutoMap();

                // Register each derived type as a known type
                foreach (var derivedType in derivedTypes)
                {
                    cm.AddKnownType(derivedType);
                }
            });
        }

        private static bool IsKnown(Type source, Type target)
        {
            return target.IsSubclassOf(source) || source.GetFields().Aggregate(true, (sofar, next) => sofar || IsKnown(next.FieldType, target));
        }
    }
}
