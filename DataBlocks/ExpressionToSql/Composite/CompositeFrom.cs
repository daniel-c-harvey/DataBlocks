using System.Linq.Expressions;
using DataBlocks.DataAccess;

namespace ExpressionToSql.Composite;

/// <summary>
    /// A composite Select query that works with multiple table joins
    /// </summary>
    public class CompositeFrom<TRoot> : QueryRoot<TRoot>
    {
        private readonly Table _rootTable;
        
        internal CompositeFrom(Table rootTable, ISqlDialect dialect)
            : base(dialect)
        {
            _rootTable = rootTable;
            
            // Register the root entity type with AliasRegistry
            Aliases.RegisterType(typeof(TRoot), QueryBuilder.TableAliasName);
            
            // For backward compatibility
            RegisterEntityType(QueryBuilder.TableAliasName, typeof(TRoot));
        }
        
        /// <summary>
        /// Adds a JOIN clause to the query
        /// </summary>
        public CompositeJoin<TRoot, TJoin> Join<TJoin>(
            DataSchema schema, 
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            var joinTable = new Table<TJoin> { Name = schema.CollectionName, Schema = schema.SchemaName };
            var baseJoin = new CompositeJoin<TRoot>(this);
            return baseJoin.Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a JOIN clause to the query with a custom table
        /// </summary>
        public CompositeJoin<TRoot, TJoin> Join<TJoin>(
            Table joinTable, 
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            var baseJoin = new CompositeJoin<TRoot>(this);
            return baseJoin.Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query
        /// </summary>
        public CompositeWhere<TRoot> Where(Expression<Func<TRoot, bool>> predicate)
        {
            var baseJoin = new CompositeJoin<TRoot>(this);
            return baseJoin.Where(predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Register aliases with QueryBuilder
            ApplyEntityTypesToQueryBuilder(qb);

            qb.AddTable(_rootTable, qb.GetAliasForType(typeof(TRoot))!);
            
            return qb;
        }
    }