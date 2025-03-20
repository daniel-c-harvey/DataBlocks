using System.Reflection;
using MongoDB.Bson.Serialization;

namespace DataBlocks.DataAccess.Mongo
{
    internal static class MongoModelMapper
    {
        [Flags]
        private enum SearchDirection
        {
            Stop = 0x00,
            Derived = 0x01,
            Based = 0x02,
            Both = Derived | Based,
        }

        private static HashSet<Type> _registry = new();

        internal static void RegisterModel<TModel>()
        {
            Type type = typeof(TModel);
            RegisterModel(type, SearchDirection.Both);
        }

        private static void RegisterModel(Type type, SearchDirection direction)
        {
            if (type == null || _registry.Contains(type)) return;
            _registry.Add(type);

            // Traverse the inheritance heirarchy to register all class members in the inheritance/composition graph
            if (type.IsClass)
            {
                if (direction.HasFlag(SearchDirection.Derived)) RegisterDerivedTypes(type);
                if (direction.HasFlag(SearchDirection.Based)) RegisterBaseTypes(type);
            }

            if (type.IsGenericType)
            {
                foreach (Type genericType in type.GetGenericArguments())
                {
                    RegisterModel(genericType, SearchDirection.Both);
                }
            }

            foreach (Type? memberType in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(m => m.PropertyType))
            {
                if (memberType is null || memberType.IsPrimitive) continue;
                
                RegisterModel(memberType, SearchDirection.Both);
            }
        }

        private static void RegisterBaseTypes(Type type)
        {
           Type? baseType = type.BaseType;

            if (baseType != null && !BsonClassMap.IsClassMapRegistered(baseType))
            {
                BsonClassMap cm = new(baseType);

                MapModel(cm);
                cm.AddKnownType(type);
                
                BsonClassMap.RegisterClassMap(cm);

                RegisterModel(baseType, baseType.IsAbstract ? SearchDirection.Stop : SearchDirection.Based);
            }
        }

        private static void RegisterDerivedTypes(Type baseType)
        {
            if (BsonClassMap.IsClassMapRegistered(baseType)) return;
            
            var derivedTypes = Assembly.GetAssembly(baseType)?
                                       .GetTypes()
                                       .Where(t => !t.IsAbstract && t.IsSubclassOf(baseType))
                                       .ToList() ?? [];

            BsonClassMap cm = new(baseType);

            MapModel(cm);

            foreach (var derivedType in derivedTypes)
            {
                cm.AddKnownType(derivedType);
                RegisterModel(derivedType, SearchDirection.Derived);
            }

            BsonClassMap.RegisterClassMap(cm);
        }

        private static void MapModel(BsonClassMap cm)
        {
            // Map everything, then remove non-storage properties
            cm.AutoMap();
            foreach (var prop in cm.ClassType.GetProperties())
            {
                if (prop.GetCustomAttribute<MediaStorageAttribute>() is {ShouldStore: false})
                {
                    cm.UnmapProperty(prop.Name);
                }
            }
        }
    }
}
