namespace ExpressionToSql.Composite
{

    public abstract class CompositePageBase<TSelect> : Query
        where TSelect : CompositeSelectBase
    {
        private readonly int _pageSize;
        private readonly int _pageIndex;
        private readonly int _offset;
        private readonly TSelect _select;

        /// <summary>
        /// Creates a paged query with the specified page size and page number from a select query
        /// </summary>
        internal CompositePageBase(TSelect select, int pageIndex, int pageSize)
            : base(select.Dialect)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));
            
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize));
                
            _pageSize = pageSize;
            _pageIndex = pageIndex;
            _offset = pageIndex * pageSize;
            _select = select;
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            if (_select != null)
            {
                _select.ToSql(qb);
                CopyParametersFromType(_select);
                qb.LimitOffset(_pageSize, _offset);
                return qb;
            }
            
            throw new InvalidOperationException("Query is in an invalid state");
        }
    }
    
    /// <summary>
    /// Represents a SQL query with paging (LIMIT and OFFSET) clauses
    /// </summary>
    public class CompositePage<TRoot, TResult> : CompositePageBase<CompositeSelect<TRoot, TResult>>
    {
        internal CompositePage(CompositeSelect<TRoot, TResult> select, int pageIndex, int pageSize) 
            : base(select, pageIndex, pageSize) { }
    }
    
    /// <summary>
    /// Represents a SQL query with paging (LIMIT and OFFSET) clauses
    /// </summary>
    public class CompositePage<TRoot, TJoin, TResult> : CompositePageBase<CompositeSelect<TRoot, TJoin, TResult>>
    {
        internal CompositePage(CompositeSelect<TRoot, TJoin, TResult> select, int pageIndex, int pageSize) 
            : base(select, pageIndex, pageSize) { }
    }
    
    /// <summary>
    /// Represents a SQL query with paging (LIMIT and OFFSET) clauses
    /// </summary>
    public class CompositePage<TRoot, TJoin1, TJoin2, TResult> : CompositePageBase<CompositeSelect<TRoot, TJoin1, TJoin2, TResult>>
    {
        internal CompositePage(CompositeSelect<TRoot, TJoin1, TJoin2, TResult> select, int pageIndex, int pageSize) 
            : base(select, pageIndex, pageSize) { }
    }

    public static class CompositePageExtensions
    {
        public static CompositePage<TRoot, TResult> Page<TRoot, TResult>(this CompositeSelect<TRoot, TResult> query, int pageIndex, int pageSize)
        {
            return new CompositePage<TRoot, TResult>(query, pageIndex, pageSize);
        }
        
        public static CompositePage<TRoot, TJoin, TResult> Page<TRoot, TJoin, TResult>(this CompositeSelect<TRoot, TJoin, TResult> query, int pageIndex, int pageSize)
        {
            return new CompositePage<TRoot, TJoin, TResult>(query, pageIndex, pageSize);
        }
        
        public static CompositePage<TRoot, TJoin, TJoin2, TResult> Page<TRoot, TJoin, TJoin2, TResult>(this CompositeSelect<TRoot, TJoin, TJoin2, TResult> query, int pageIndex, int pageSize)
        {
            return new CompositePage<TRoot, TJoin, TJoin2, TResult>(query, pageIndex, pageSize);
        }
    }
}