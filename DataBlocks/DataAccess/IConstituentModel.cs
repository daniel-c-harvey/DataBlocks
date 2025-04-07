using System.Linq.Expressions;

namespace DataBlocks.DataAccess;

using System;
using System.Collections.Generic;

// public class JoinChain<TDomainModel, TLeftModel, TJoinModel>
// {
//     // Store the expression that represents the join
//     private readonly Expression<Func<TLeftModel, TJoinModel, bool>> _joinExpression;
//     
//     // Constructor for initial join
//     public JoinChain()
//     {
//         // Initialize with default join expression if needed
//         _joinExpression = null;
//     }
//     
//     // Private constructor used during chaining
//     private JoinChain(Expression<Func<TLeftModel, TJoinModel, bool>> joinExpression)
//     {
//         _joinExpression = joinExpression;
//     }
//     
//     // Join method that adds a new join to the chain
//     // Returns a new JoinChain with updated type parameters
//     public JoinChain<TDomainModel, TLeftModel, TJoinModel, TRightModel> Join<TRightModel>()
//     {
//         // Here you would build the actual join expression
//         // For now, we'll just create a placeholder
//         var newJoinExpression = new { 
//             PreviousJoin = _joinExpression,
//             RightModel = typeof(TRightModel)
//         };
//         
//         // Return new JoinChain with updated type parameters
//         return new JoinChain<TDomainModel, TLeftModel, TJoinModel, TRightModel>(newJoinExpression);
//     }
//     
//     // Execute method to actually perform the join operations
//     public IEnumerable<TResult> Execute<TResult>(Func<object, TResult> selector)
//     {
//         // Implementation of query execution would go here
//         // This would use the _joinExpression to build and execute the actual query
//         
//         throw new NotImplementedException("Query execution not implemented");
//     }
// }



// // Extension to continue the chain with a new join
// public class JoinChain<TQueryable, TLeftModel, TJoinModel, TRightModel> : JoinChain<TQueryable, TLeftModel, TJoinModel>
// {
//     // Store the extended join expression
//     private readonly Expression<Func<TJoinModel, TRightModel, bool>> _extendedJoinExpression;
//     
//     // Constructor used during chaining
//     internal JoinChain(Expression<Func<TJoinModel, TRightModel, bool>> joinExpression) : base()
//     {
//         _extendedJoinExpression = joinExpression;
//     }
//     
//     // Extend the chain with another join
//     public JoinChain<TQueryable, TLeftModel, TJoinModel, TRightModel> Join(Expression<Func<TJoinModel, TRightModel, bool>> predicate)
//     {
//         // Return new JoinChain with updated type parameters
//         return new JoinChain<TQueryable, TLeftModel, TJoinModel, TRightModel>(predicate);
//     }
// }

// public class JoinChain()
// {
//     private class JoinData
//     {
//         public required Type LeftModelType { get; init; }
//         public required Type RightModelType { get; init; }
//         public required Expression<Func<object, object, bool>> UntypedPredicate { private get; init; }
//         public Expression
//         
//         public static JoinData Create<TLeftModel, TRightModel>(
//             Expression<Func<TLeftModel, TRightModel, bool>> expression)
//         {
//             
//         }
//     }
//     public JoinChain Join<TNextJoin, TDataModel, TTargetDataModel>()
//         where TNextJoin : IJoinModel<TDataModel, TTargetDataModel>
//         where TDataModel : IModel
//         where TTargetDataModel : IModel
//     {
//         return new JoinChain(TNextJoin.Predicate);
//     }
// }

// Base join chain class that works with our type tracking system
public class JoinChain<TDomainModel, TLeft, TRight>
    where TDomainModel : IJoinModel<TLeft, TRight>
    where TLeft : IModel
    where TRight : IModel
{
    // List to store all join expressions in the chain
    protected readonly List<object> _joinExpressions = new List<object>();
    
    
    // Constructor for continuing the chain
    protected JoinChain(Expression<Func<TLeft, TRight, bool>> predicate)
    {
        _joinExpressions.Add(predicate);
    }
    
    // Create initial join
    public static JoinChain<TDomainModel, TLeft, TRight> CreateJoin()
    {
        return new JoinChain<TDomainModel, TLeft, TRight>(TDomainModel.Predicate);
    }
    
    // Method to extend the join chain
    public JoinChain<TNextDomainModel, TRight, TNextRight> Join<TNextDomainModel, TNextRight>()
        where TNextDomainModel : IJoinModel<TRight, TNextRight>
        where TNextRight : IModel
    {
        return  new JoinChain<TNextDomainModel, TRight, TNextRight>(TNextDomainModel.Predicate);
    }
    
    // Helper method for debugging
    public List<(Type LeftType, Type RightType, object Predicate)> GetJoinInfo()
    {
        var result = new List<(Type, Type, object)>();
        foreach (dynamic expr in _joinExpressions)
        {
            result.Add((expr.LeftType, expr.RightType, expr.Predicate));
        }
        return result;
    }
}

public interface IJoinModel<TDataModel, TNextDataModel> : IConstituentModel<TDataModel>
    // where TDomainModel : IConstituentModel<TDataModel>
    where TDataModel : IModel
    // where TNextDomainModel : IConstituentModel<TNextDataModel>
    where TNextDataModel : IModel
{
    static abstract Expression<Func<TDataModel, TNextDataModel, bool>> Predicate { get; }    
}

public interface IConstituentModel<TDataModel> : IConstituentModel<long, TDataModel>
    where TDataModel : IModel { }

public interface IConstituentModel<TKey, TDataModel>
    where TDataModel : IModel<TKey>
{
    TKey ID { get; set; }
}
