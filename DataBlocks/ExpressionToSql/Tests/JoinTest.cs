using DataBlocks.DataAccess;
using ExpressionToSql;
using ScheMigrator.Migrations;

namespace DataBlocks.ExpressionToSql.Tests
{
    // Test classes with schema attributes
    public class Customer
    {
        [ScheData("customer_id")]
        public int Id { get; set; }
        
        [ScheData("customer_name")]
        public string Name { get; set; }
        
        [ScheData("order_id")]
        public int OrderId { get; set; }
        
        [ScheData("is_deleted")]
        public bool IsDeleted { get; set; }
    }
    
    public class Order
    {
        [ScheData("order_id")]
        public int Id { get; set; }
        
        [ScheData("order_date")]
        public DateTime OrderDate { get; set; }
        
        [ScheData("order_amount")]
        public decimal Amount { get; set; }
    }
    
    public static class JoinTest
    {
        public static string TestSelect()
        {
            // Simple select query
            var query = PSql.Select<Customer, Customer>(c => c);
                
            // Get the SQL string representation
            return query.ToString();
        }
        
        public static string TestWhere()
        {
            // Simple where query
            var query = PSql.Select<Customer, Customer>(c => c)
                .Where(c => c.IsDeleted == false);
                
            // Get the SQL string representation
            return query.ToString();
        }
        
        public static string TestJoin()
        {
            // Join query
            var query = PSql.Select<Customer, Customer>(c => c)
                .Join<Order>(DataSchema.Create<Order>("orders"), (c, o) => c.OrderId == o.Id);
                
            // Get the SQL string representation
            return query.ToString();
        }
        
        public static string TestJoinWhere()
        {
            // Join with where clause
            var query = PSql.Select<Customer, Customer>(c => c)
                .Join<Order>(DataSchema.Create<Order>("orders"), (c, o) => c.OrderId == o.Id)
                .Where(c => c.IsDeleted == false);
                
            // Get the SQL string representation
            return query.ToString();
        }
        
        public static string TestComplexQuery()
        {
            DateTime startDate = new DateTime(2023, 1, 1);
            
            // More complex query with multiple conditions
            var query = PSql.Select<Customer, Customer>(c => c)
                .Join<Order>(DataSchema.Create<Order>("orders"), (c, o) => c.OrderId == o.Id && o.OrderDate > startDate)
                .Where(c => !c.IsDeleted && c.Name == "Smith");
                
            // Get the SQL string representation
            return query.ToString();
        }
    }
} 