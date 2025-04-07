// using System.Linq.Expressions;
//
// namespace DataBlocks.DataAccess.Examples;
//
// // Example of a one-to-many relationship
// public class Customer : ICompositeModel<Customer, Order>, IConstituentModel
// {
//     public long ID { get; set; }
//     public string Name { get; set; }
//
//     // Define the relationship between Customer and Order
//     public static IRelation Relation => 
//         Relation<Customer, Order>.Create((customer, order) => customer.ID == order.CustomerID);
// }
//
// public class Order : IConstituentModel
// {
//     public long ID { get; set; }
//     public long CustomerID { get; set; }
//     public string OrderNumber { get; set; }
//     public decimal Amount { get; set; }
// }
//
// // Example of a relationship with a linking table
// public class Root : ICompositeModel<Root, Link, Target>, IConstituentModel
// {
//     public long ID { get; set; }
//     public string Name { get; set; }
//
//     // Define the relationship between Root and Link
//     public static IRelation SelfToLinkRelation => 
//         Relation<Root, Link>.Create((root, link) => root.ID == link.LeftID);
//
//     // Define the relationship between Link and Target
//     public static IRelation LinkToTargetRelation => 
//         Relation<Link, Target>.Create((link, target) => link.RightID == target.ID);
// }
//
// public class Link : ILinkageModel
// {
//     public long ID { get; set; }
//     public long LeftID { get; set; }   // Foreign key to Root
//     public long RightID { get; set; }  // Foreign key to Target
// }
//
// public class Target : IConstituentModel
// {
//     public long ID { get; set; }
//     public string Description { get; set; }
// }
//
// // Example of a three-level relationship chain
// public class Department : ICompositeModel<Department, Employee, Project, Assignment>, IConstituentModel
// {
//     public long ID { get; set; }
//     public string Name { get; set; }
//
//     // Define the relationship between Department and Employee
//     public static IRelation Relation1 => 
//         Relation<Department, Employee>.Create((dept, emp) => dept.ID == emp.DepartmentID);
//
//     // Define the relationship between Employee and Project through Assignment
//     public static IRelation Relation2 => 
//         Relation<Employee, Assignment>.Create((emp, assignment) => emp.ID == assignment.EmployeeID);
//         
//     // Additional relation that could be used
//     public static IRelation Relation3 => 
//         Relation<Assignment, Project>.Create((assignment, project) => assignment.ProjectID == project.ID);
// }
//
// public class Employee : IConstituentModel
// {
//     public long ID { get; set; }
//     public long DepartmentID { get; set; }
//     public string Name { get; set; }
// }
//
// public class Project : IConstituentModel
// {
//     public long ID { get; set; }
//     public string Name { get; set; }
// }
//
// public class Assignment : IConstituentModel
// {
//     public long ID { get; set; }
//     public long EmployeeID { get; set; }
//     public long ProjectID { get; set; }
//     public string Role { get; set; }
// } 