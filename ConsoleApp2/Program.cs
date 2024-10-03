using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Data.Entity; // Для Entity Framework
using Dapper; // Для Dapper
using System.Threading;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Configuration;
using System.Collections.Generic;
using LinqToDB.Mapping;

namespace ConsoleApp2
{
    public class Program
    {
        public static string connectionString = @"Server=RP0BLERG\SQLEXPRESS;Database=DBForTest;Trusted_Connection=True;TrustServerCertificate=True;";

        public static void Main(string[] args)
        {
            // "Разогрев" базы данных
            WarmUpDatabase();

            Thread.Sleep(1000);

            Console.WriteLine("Entity Framework 6:");
            Console.WriteLine($"Add: {EF6Benchmark.AddRecord()} ms");
            Console.WriteLine($"Update: {EF6Benchmark.UpdateRecord()} ms");
            Console.WriteLine($"Delete: {EF6Benchmark.DeleteRecord()} ms");
            Console.WriteLine($"Select All: {EF6Benchmark.SelectAll()} ms");
            Console.WriteLine($"Select One: {EF6Benchmark.SelectOne(1245)} ms");
            Console.WriteLine();

            Console.WriteLine("Dapper:");
            Console.WriteLine($"Add: {DapperBenchmark.AddRecord()} ms");
            Console.WriteLine($"Update: {DapperBenchmark.UpdateRecord()} ms");
            Console.WriteLine($"Delete: {DapperBenchmark.DeleteRecord()} ms");
            Console.WriteLine($"Select All: {DapperBenchmark.SelectAll()} ms");
            Console.WriteLine($"Select One: {DapperBenchmark.SelectOne(1245)} ms");
            Console.WriteLine();

            Console.WriteLine("Чистый SQL:");
            Console.WriteLine($"Add: {SqlBenchmark.AddRecord()} ms");
            Console.WriteLine($"Update: {SqlBenchmark.UpdateRecord()} ms");
            Console.WriteLine($"Delete: {SqlBenchmark.DeleteRecord()} ms");
            Console.WriteLine($"Select All: {SqlBenchmark.SelectAll()} ms");
            Console.WriteLine($"Select One: {SqlBenchmark.SelectOne(1245)} ms");
            Console.WriteLine();

            Linq2DbBenchmark.WarmUpDatabase();

            Console.WriteLine("LINQ to DB:");
            Console.WriteLine($"Add: {Linq2DbBenchmark.AddRecord()} ms");
            Console.WriteLine($"Update: {Linq2DbBenchmark.UpdateRecord()} ms");
            Console.WriteLine($"Delete: {Linq2DbBenchmark.DeleteRecord()} ms");
            Console.WriteLine($"Select All: {Linq2DbBenchmark.SelectAll()} ms");
            Console.WriteLine($"Select One: {Linq2DbBenchmark.SelectOne(1245)} ms");
            Console.ReadKey();
        }

        #region Метод замера времени
        public static double MeasureTime(Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }
        #endregion

        #region Метод разогрева
        public static void WarmUpDatabase()
        {
            using (var context = new AppDbContext())
            {
                // Примеры простого запроса, чтобы разогреть соединение
                context.Products.Where(w => w.Id % 2 == 0).FirstOrDefault();

                context.Products.Where(w => w.Name != "").FirstOrDefault();

                context.Products.FirstOrDefault();
            }
        }
#endregion
    }

    #region Класс продуктов
    [Table(Name = "Products")] // Явно указываем имя таблицы
    public class Product
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }

        [Column(Name = "Name"), NotNull]
        public string Name { get; set; }

        [Column(Name = "Price"), NotNull]
        public decimal Price { get; set; }
    }
    #endregion

    #region Entity Framework 6.5
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base(Program.connectionString) { }

        public DbSet<Product> Products { get; set; }
    }

    public class EF6Benchmark
    {
        public static double AddRecord()
        {
            using (var context = new AppDbContext())
            {
                var product = new Product { Name = "New Product", Price = 99.99m };
                return Program.MeasureTime(() =>
                {
                    context.Products.Add(product);
                    context.SaveChanges();
                });
            }
        }

        public static double UpdateRecord()
        {
            using (var context = new AppDbContext())
            {
                var product = context.Products.First();
                product.Price = 199.99m;

                return Program.MeasureTime(() =>
                {
                    context.SaveChanges();
                });
            }
        }

        public static double DeleteRecord()
        {
            using (var context = new AppDbContext())
            {
                var product = context.Products.First();
                return Program.MeasureTime(() =>
                {
                    context.Products.Remove(product);
                    context.SaveChanges();
                });
            }
        }

        public static double SelectAll()
        {
            using (var context = new AppDbContext())
            {
                return Program.MeasureTime(() =>
                {
                    var products = context.Products.ToList();
                });
            }
        }

        public static double SelectOne(int id)
        {
            using (var context = new AppDbContext())
            {
                return Program.MeasureTime(() =>
                {
                    var product = context.Products.Find(id);
                });
            }
        }
    }
#endregion

    #region Dapper
    public class DapperBenchmark
    {
        public static double AddRecord()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "INSERT INTO Products (Name, Price) VALUES (@Name, @Price)";
                var parameters = new { Name = "New Product", Price = 99.99m };

                return Program.MeasureTime(() =>
                {
                    connection.Execute(sql, parameters);
                });
            }
        }

        public static double UpdateRecord()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "UPDATE Products SET Price = @Price WHERE Id = @Id";
                var parameters = new { Id = 1, Price = 199.99m };

                return Program.MeasureTime(() =>
                {
                    connection.Execute(sql, parameters);
                });
            }
        }

        public static double DeleteRecord()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "DELETE FROM Products WHERE Id = @Id";
                var parameters = new { Id = 1 };

                return Program.MeasureTime(() =>
                {
                    connection.Execute(sql, parameters);
                });
            }
        }

        public static double SelectAll()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "SELECT * FROM Products";

                return Program.MeasureTime(() =>
                {
                    var products = connection.Query<Product>(sql).ToList();
                });
            }
        }

        public static double SelectOne(int id)
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "SELECT * FROM Products WHERE Id = @Id";
                var parameters = new { Id = id };

                return Program.MeasureTime(() =>
                {
                    var product = connection.QuerySingleOrDefault<Product>(sql, parameters);
                });
            }
        }
    }
#endregion

    #region Чистый SQL
    public class SqlBenchmark
    {
        public static double AddRecord()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "INSERT INTO Products (Name, Price) VALUES ('New Product', 99.99)";

                return Program.MeasureTime(() =>
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                });
            }
        }

        public static double UpdateRecord()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "UPDATE Products SET Price = 199.99 WHERE Id = 1";

                return Program.MeasureTime(() =>
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                });
            }
        }

        public static double DeleteRecord()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "DELETE FROM Products WHERE Id = 1";

                return Program.MeasureTime(() =>
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                });
            }
        }

        public static double SelectAll()
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "SELECT * FROM Products";

                return Program.MeasureTime(() =>
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Обработка результата (можно хранить в списке, если нужно)
                                var id = reader["Id"];
                                var name = reader["Name"];
                                var price = reader["Price"];
                            }
                        }
                    }
                });
            }
        }

        public static double SelectOne(int id)
        {
            using (var connection = new SqlConnection(Program.connectionString))
            {
                var sql = "SELECT * FROM Products WHERE Id = @Id";

                return Program.MeasureTime(() =>
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Обработка результата
                                var name = reader["Name"];
                                var price = reader["Price"];
                            }
                        }
                    }
                });
            }
        }
    }
    #endregion

    #region Linq2Db
    public class MySettings : ILinqToDBSettings
    {
        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "SqlServer";
        public string DefaultDataProvider => "SqlServer";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings
        {
            get
            {
                yield return new ConnectionStringSettings
                {
                    Name = "Default",
                    ProviderName = "SqlServer",
                    ConnectionString = @"Server=RP0BLERG\SQLEXPRESS;Database=DBForTest;Trusted_Connection=True;TrustServerCertificate=True;"
                };
            }
        }
    }

    public class ConnectionStringSettings : IConnectionStringSettings
    {
        public string ConnectionString { get; set; }
        public string Name { get; set; }
        public string ProviderName { get; set; }
        public bool IsGlobal => false;
    }
    public class Linq2DbBenchmark
    {
        static Linq2DbBenchmark()
        {
            DataConnection.DefaultSettings = new MySettings();
        }

        public static double AddRecord()
        {
            using (var db = new DataConnection())
            {
                var product = new Product { Name = "New Product", Price = 99.99m };
                return Program.MeasureTime(() =>
                {
                    db.Insert(product);
                });
            }
        }

        public static void WarmUpDatabase()
        {
            using (var db = new DataConnection())
            {
                // Выполнение простого запроса, чтобы инициализировать соединение
                var count = db.GetTable<Product>().Count();
            }
        }

        public static double UpdateRecord()
        {
            using (var db = new DataConnection())
            {
                var product = db.GetTable<Product>().First();
                product.Price = 199.99m;

                return Program.MeasureTime(() =>
                {
                    db.Update(product);
                });
            }
        }

        public static double DeleteRecord()
        {
            using (var db = new DataConnection())
            {
                var product = db.GetTable<Product>().First();
                return Program.MeasureTime(() =>
                {
                    db.Delete(product);
                });
            }
        }

        public static double SelectOne(int id)
        {
            using (var db = new DataConnection())
            {
                return Program.MeasureTime(() =>
                {
                    var product = db.GetTable<Product>().Where(w => w.Id == id);
                });
            }
        }

        public static double SelectAll()
        {
            using (var db = new DataConnection())
            {
                return Program.MeasureTime(() =>
                {
                    var products = db.GetTable<Product>().ToList();
                });
            }
        }
    }
    #endregion
}
