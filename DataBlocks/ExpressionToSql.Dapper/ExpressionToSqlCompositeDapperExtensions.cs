using System.Data;
using ExpressionToSql.Composite;
using global::Dapper;

namespace ExpressionToSql.Dapper;

public static class ExpressionToSqlCompositeDapperExtensions
{
    // public static Task<IEnumerable<R>> QueryAsync<T, R>(this IDbConnection cnn, CompositeSelect<T, R> sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    // {
    //     return QueryAsync<R>(cnn, sql, param, transaction, commandTimeout, commandType);
    // }
    //     
    // public static Task<IEnumerable<R>> QueryAsync<T, T2, R>(this IDbConnection cnn, CompositeSelect<T, T2, R> sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    // {
    //     return QueryAsync<R>(cnn, sql, param, transaction, commandTimeout, commandType);
    // }
        
    public static async Task<IEnumerable<TComposite>> QueryAsync<TFirst, TSecond, TThird, TComposite, TTarget, TSelector>(this IDbConnection cnn, CompositeSelect<TFirst, TSecond, TThird, TSelector> sql, Func<TComposite, TTarget, TComposite> map, string splitOn, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        return await QueryAsync(cnn, sql, map, param, transaction, commandTimeout, commandType, splitOn);
    }
    
    public static async Task<IEnumerable<TComposite>> QueryAsync<TFirst, TSecond, TThird, TComposite, TTarget, TResult>(this IDbConnection cnn, CompositePage<TFirst, TSecond, TThird, TResult> sql, Func<TComposite, TTarget, TComposite> map, string splitOn, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        return await QueryAsync(cnn, sql, map, param, transaction, commandTimeout, commandType, splitOn);
    }
    
    private static async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(IDbConnection cnn, Query sql, Func<TFirst, TSecond, TReturn> map, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType, string splitOn)
    {
        var query = sql.ToString();
        var parameters = param ?? sql.Parameters;
        return await cnn.QueryAsync<TFirst, TSecond, TReturn>(sql: query, map: map, param: parameters, transaction: transaction, commandTimeout: commandTimeout, commandType: commandType, splitOn: splitOn);
    }
}