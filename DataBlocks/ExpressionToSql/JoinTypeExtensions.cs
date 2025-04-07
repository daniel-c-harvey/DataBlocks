namespace ExpressionToSql
{
    /// <summary>
    /// Extensions for JoinType enum values
    /// </summary>
    public static class JoinTypeExtensions
    {
        /// <summary>
        /// Converts a JoinType to its SQL representation
        /// </summary>
        public static string ToSqlString(this JoinType joinType)
        {
            return joinType switch
            {
                JoinType.Inner => "INNER JOIN",
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                JoinType.Full => "FULL JOIN",
                _ => "JOIN"
            };
        }
    }
} 