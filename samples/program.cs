using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using BareORM.Abstractions;
using BareORM.Advanced.Graph;
using BareORM.Advanced.MultiResult;
using BareORM.Advanced.Split;
using BareORM.Core;
using BareORM.Mapping;
using BareORM.samples.Models;
using BareORM.samples;
using BareORM.Serialization;
using BareORM.SqlServer;




Schema.SampleOnMemory();

MigrationExample.Run();


//// -----------------------------
//// 0) Config
//// -----------------------------
//var connectionString =
//  "Data Source=(localdb)\\ProjectModels;Initial Catalog=BenchDb;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;Command Timeout=30";
//var connFactory = new SqlServerConnectionFactory(connectionString);

//var executor = new SqlServerExecutor(connFactory);
//var cmdFactory = new SqlServerCommandFactory();
//var tx = new SqlServerTransactionManager(connFactory);
//var bulk = new SqlServerBulkProvider(connFactory);

//// Observer opcional: log simple a consola
//ICommandObserver observer = new ConsoleObserver();

//var db = new DbContextLite(executor, cmdFactory, tx, bulk, observer);





//var jsonOptions = new JsonSerializerOptions
//{
//    WriteIndented = true, // pretty print
//    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
//    ReferenceHandler = ReferenceHandler.IgnoreCycles, // por si hay ciclos (User->Orders->User)
//};



//var options = new MappingOptions
//{
//    IgnoreCase = true,
//    StrictColumnMatch = false,
//    // si usas ByOrdinal:
//    // Mode = MappingMode.ByOrdinal,
//    // StrictOrdinalMatch = true,

//    // JSON:
//    Serializer = new SystemTextJsonSerializer()
//};



//var meta = db.ExecuteReaderWithMeta("dbo.spDemo_UserOrders_Join", parameters: new { UserId = 3 });
//using IDataReader reader = meta.Data;

//var rows = SplitOnMapper.ReadSplit<User, Order, (User u, Order o)>(
//    reader,
//    (u, o) => (u, o),
//    mappingOptions: options,
//    splitOptions: new SplitOptions { SplitOn = "OrderId" }
//);






//foreach (var row in rows)
//{
//    var u = row.u;
//    var o = row.o;
//    Console.WriteLine($"User: {u.UserId} - {u.DisplayName} ({u.Email})");
//    if (o != null)
//    {
//        Console.WriteLine($"   Order: {o.OrderId} - {o.OrderNumber} - Total: {o.Total} - CreatedAt: {o.CreatedAt}");
//    }
//    else
//    {
//        Console.WriteLine("   No orders");
//    }
//}



//Console.WriteLine("\n-------------------------------------------------------------------------------\n");
//Console.WriteLine("spDemo_User_OrderSnapshot");


//var meta2 = db.ExecuteReaderWithMeta("dbo.spDemo_User_OrderSnapshot", parameters: new { UserId = 3 });

//using var rs = ((IDataReader)meta2.Data).AsResultSetReader(options);

//var user = rs.ReadSingle<User>();
//rs.NextResult();

//var orders = rs.Read<Order>();
//rs.NextResult();

//var items = rs.Read<OrderItem>();

//if (user is not null)
//{
//    var users = new List<User> { user };

//    GraphStitch.OneToMany(users, orders, u => u.UserId, o => o.UserId, (u, list) => u.Orders = list);
//    GraphStitch.OneToMany(orders, items, o => o.OrderId, i => i.OrderId, (o, list) => o.Items = list);


//    ISerializer serializer = new SystemTextJsonSerializer();

//var json = serializer.Serialize(users[0], jsonOptions);
//    Console.WriteLine(json);
//}




//Console.WriteLine("\n-------------------------------------------------------------------------------\n");
////using var r = db.ExecuteReaderWithMeta("dbo.sp_ListarUsuarios", parameters: new { Active = true }).Data;


////var mapper = new DefaultEntityMapper<User>();

////var users = ((IDataReader)r).ReadAll(mapper);

////Console.WriteLine($"Total active users: {users.Count}");

////foreach(var u in users)
////{
////    Console.WriteLine(  
////        @$"- UserId:    {u.UserId}
////        Email:          {u.Email}
////        DisplayName:    {u.DisplayName}
////        IsActive:       {u.IsActive} 
////        CreatedAt:      {u.CreatedAt}
////        Settings:       
////            Theme:          {u.Settings.Theme}
////            Notifications:  {u.Settings.Notifications}
////    ");
////}


//Console.WriteLine("=== BareORM Demo (SqlServer) ===");

////// -----------------------------
////// 1) Upsert user: OUTPUT + RETURN VALUE
////// -----------------------------
////Console.WriteLine("\n--- 1) spDemo_User_Upsert (OUTPUT + RETURN) ---");

////var upsertParams = new DbParam[]
////{
////    new("Email", "admin@bareorm.dev"),
////    new("DisplayName", "Demo User"),
////    new("UserId", null, DbType.Int32, ParameterDirection.Output),
////    new("ReturnCode", null, DbType.Int32, ParameterDirection.ReturnValue),
////};

////var upsertMeta = db.ExecuteWithMeta("dbo.spDemo_User_Upsert", CommandType.StoredProcedure, upsertParams);

////Helpers.DumpOutputs(upsertMeta.OutputValues);

////var userId = Helpers.GetInt(upsertMeta.OutputValues, "@UserId");
////var rc = Helpers.GetInt(upsertMeta.OutputValues, "@ReturnCode");

////Console.WriteLine($"ReturnCode = {rc}, UserId = {userId}");

////// -----------------------------
////// 2) GetById: DataTable
////// -----------------------------
////Console.WriteLine("\n--- 2) spDemo_User_GetById (DataTable) ---");

////var dtUser = db.ExecuteDataTable("dbo.spDemo_User_GetById", parameters: new { UserId = userId });
////Helpers.PrintDataTable(dtUser);

////var dtAllUser = db.ExecuteDataTable("select * from Users", CommandType.Text);
////Helpers.PrintDataTable(dtAllUser);

////// -----------------------------
////// 3) Count: Scalar
////// -----------------------------
////Console.WriteLine("\n--- 3) spDemo_User_Count (Scalar) ---");

////var totalUsers = db.ExecuteScalar<int>("dbo.spDemo_User_Count", parameters: new { OnlyActive = true });
////Console.WriteLine($"TotalUsers = {totalUsers}");

////// -----------------------------
////// 4) Multi result set: DataSet (User + Orders + Items)
////// -----------------------------
////Console.WriteLine("\n--- 4) spDemo_User_OrderSnapshot (DataSet - Multi result) ---");

////var dsSnap = db.ExecuteDataSet("dbo.spDemo_User_OrderSnapshot", parameters: new { UserId = userId });

////Console.WriteLine($"Tables returned: {dsSnap.Tables.Count}");
////for (int i = 0; i < dsSnap.Tables.Count; i++)
////{
////    Console.WriteLine($"-- Table[{i}] name='{dsSnap.Tables[i].TableName}', rows={dsSnap.Tables[i].Rows.Count}");
////    Helpers.PrintDataTable(dsSnap.Tables[i], maxRows: 5);
////}

////// -----------------------------
////// 5) TVP: Create order with items (OUTPUT OrderId + Total)
////// -----------------------------
////Console.WriteLine("\n--- 5) spDemo_Order_CreateWithItems (TVP + OUTPUTs) ---");

////var itemsTvp = Helpers.BuildItemsTvp(new[]
////{
////    ("SKU-1", 2, 10.50m),
////    ("SKU-2", 1, 99.99m),
////});

////var orderParams = new DbParam[]
////{
////    new("UserId", userId, DbType.Int32),
////    new("OrderNumber", "ORD-" + DateTime.UtcNow.Ticks),
////    new("Items", itemsTvp, DbType: null, Direction: ParameterDirection.Input, TypeName: "dbo.TvpOrderItem"),
////    new("OrderId", null, DbType.Int32, ParameterDirection.Output),
////    new("Total", null, DbType.Decimal, ParameterDirection.Output, Precision: 18, Scale: 2),
////};

////var orderMeta = db.ExecuteWithMeta("dbo.spDemo_Order_CreateWithItems", CommandType.StoredProcedure, orderParams);

////Helpers.DumpOutputs(orderMeta.OutputValues);

////var orderId = Helpers.GetInt(orderMeta.OutputValues, "@OrderId");
////var total = Helpers.GetDecimal(orderMeta.OutputValues, "@Total");

////Console.WriteLine($"OrderId={orderId}, Total={total}");

////// -----------------------------
////// 6) Timeout test (forzar error por timeout)
////// -----------------------------
////Console.WriteLine("\n--- 6) spDemo_Timeout (Timeout test) ---");

////try
////{
////    // SP espera 3s, pero timeout 1s
////    var dtTimeout = db.ExecuteDataTable("dbo.spDemo_Timeout", parameters: new { Seconds = 3 }, timeoutSeconds: 1);
////    Helpers.PrintDataTable(dtTimeout);
////}
////catch (Exception ex)
////{
////    Console.WriteLine("Expected timeout exception:");
////    Console.WriteLine(ex.GetType().Name + " - " + ex.Message);
////}

////// -----------------------------
////// 7) Transaction test (Begin/Commit/Rollback)
////// -----------------------------
////Console.WriteLine("\n--- 7) Transaction test (Begin + Rollback) ---");

////db.BeginTransaction();

////try
////{
////    var p = new DbParam[]
////    {
////        new("Email", "tx_" + Guid.NewGuid().ToString("N") + "@bareorm.dev"),
////        new("DisplayName", "TX User"),
////        new("UserId", null, DbType.Int32, ParameterDirection.Output),
////        new("ReturnCode", null, DbType.Int32, ParameterDirection.ReturnValue),
////        };

////    var txMeta = await db.ExecuteWithMetaAsync("dbo.spDemo_User_Upsert", parameters: p);
////    var newUserId = Helpers.GetInt(txMeta.OutputValues, "@UserId");

////    Console.WriteLine($"Inserted user inside TX: UserId={newUserId}");
////    Console.WriteLine("Rolling back...");

////    db.Rollback();

////    // Validar que NO existe después del rollback
////    var dtCheck = db.ExecuteDataTable("dbo.spDemo_User_GetById", parameters: new { UserId = newUserId });
////    Console.WriteLine($"Rows after rollback: {dtCheck.Rows.Count} (expected 0)");
////}
////catch
////{
////    db.Rollback();
////    throw;
////}

////Console.WriteLine("\n=== DONE ===");


