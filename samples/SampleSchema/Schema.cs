using System;
using System.Collections.Generic;
using System.Text;
using BareORM.samples.Models;
using BareORM.Schema.Builders;

namespace BareORM.samples
{
    public static class Schema
    {
        public static void SampleOnMemory()
        {
            var builder = new SchemaModelBuilder(new SchemaModelBuilderOptions
            {
                DefaultSchema = "dbo",
                RequireTableAttribute = false
            });

            var model = builder.Build(typeof(User), typeof(Order), typeof(OrderItem));

            Console.WriteLine(model);

            foreach (var t in model.AllTables())
            {
                Console.WriteLine($"{t.Schema}.{t.Name} cols={t.Columns.Count} pk={(t.PrimaryKey?.Name ?? "none")}");
                foreach (var c in t.Columns)
                {
                    Console.WriteLine($"   - {c.Name} ({c.Type.GetType().Name}) nullable={c.IsNullable} incrementalKey={c.IsIncrementalKey}");
                }
            }

            Console.WriteLine("Done.");
        }
    }
}
