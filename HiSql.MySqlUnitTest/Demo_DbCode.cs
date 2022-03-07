﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiSql.MySqlUnitTest
{
    class Demo_DbCode
    {
        public static void Init(HiSqlClient sqlClient)
        {
            //Demo_AddColumn(sqlClient);

            //Demo_ModiColumn(sqlClient);
            //Demo_DelColumn(sqlClient);//
            //Demo_Tables(sqlClient);
            //Demo_View(sqlClient);
            //Demo_AllTables(sqlClient);
            //Demo_GlobalTables(sqlClient);
            Demo_DropView(sqlClient);
            Demo_CreateView(sqlClient);
            Demo_ModiView(sqlClient);

            //Demo_IndexList(sqlClient);
            //Demo_Index_Create(sqlClient);
            //Demo_ReTable(sqlClient);


        }
        static void Demo_ReTable(HiSqlClient sqlClient)
        {
            //OpLevel.Execute  表示执行并返回生成的SQL
            //OpLevel.Check 表示仅做检测失败时返回消息且检测成功时返因生成的SQL
            var rtn = sqlClient.DbFirst.ReTable("htest03", "htest03_1", OpLevel.Execute);
            if (rtn.Item1)
            {
                Console.WriteLine(rtn.Item2);//输出成功消息
                Console.WriteLine(rtn.Item3);//输出重命名表 生成的SQL
            }
            else
                Console.WriteLine(rtn.Item2);//输出重命名失败原因

        }

        static void Demo_Index_Create(HiSqlClient sqlClient)
        {


            TabInfo tabInfo = sqlClient.Context.DMInitalize.GetTabStruct("H04_OrderInfo");
            List<HiColumn> hiColumns = tabInfo.Columns.Where(c => c.FieldName == "POSOrderID").ToList();
            var rtn = sqlClient.DbFirst.CreateIndex("H04_OrderInfo", "H04_OrderInfo_POSOrderID", hiColumns, OpLevel.Execute);
            if (rtn.Item1)
                Console.WriteLine(rtn.Item3);
            else
                Console.WriteLine(rtn.Item2);


            rtn = sqlClient.DbFirst.DelIndex("H04_OrderInfo", "H04_OrderInfo_POSOrderID", OpLevel.Execute);

            if (rtn.Item1)
                Console.WriteLine(rtn.Item3);
            else
                Console.WriteLine(rtn.Item2);
        }

        static void Demo_IndexList(HiSqlClient sqlClient)
        {
            List<TabIndex> lstindex = sqlClient.DbFirst.GetTabIndexs("Hi_FieldModel");

            foreach (TabIndex tabIndex in lstindex)
            {
                Console.WriteLine($"TabName:{tabIndex.TabName} IndexName:{tabIndex.IndexName} IndexType:{tabIndex.IndexType}");
            }

            List<TabIndexDetail> lstindexdetails = sqlClient.DbFirst.GetTabIndexDetail("Hi_FieldModel", "PK_Hi_FieldModel_ed721f6b-296a-447e-ac67-7d02fd8e338c");
            foreach (TabIndexDetail tabIndexDetail in lstindexdetails)
            {
                Console.WriteLine($"TabName:{tabIndexDetail.TabName} IndexName:{tabIndexDetail.IndexName} IndexType:{tabIndexDetail.IndexType} ColumnName:{tabIndexDetail.ColumnName}");

            }
        }

        static void Demo_Tables(HiSqlClient sqlClient)
        {
            List<TableInfo> lsttales = sqlClient.DbFirst.GetTables();
            foreach (TableInfo tableInfo in lsttales)
            {
                Console.WriteLine($"{tableInfo.TabName}  {tableInfo.TabReName}  {tableInfo.TabDescript}  {tableInfo.TableType} 表结构:{tableInfo.HasTabStruct}");
            }
        }

        static void Demo_View(HiSqlClient sqlClient)
        {
            List<TableInfo> lsttales = sqlClient.DbFirst.GetViews();
            foreach (TableInfo tableInfo in lsttales)
            {
                Console.WriteLine($"{tableInfo.TabName}  {tableInfo.TabReName}  {tableInfo.TabDescript}  {tableInfo.TableType} 表结构:{tableInfo.HasTabStruct}");
            }
        }
        static void Demo_GlobalTables(HiSqlClient sqlClient)
        {
            List<TableInfo> lsttales = sqlClient.DbFirst.GetGlobalTempTables();
            foreach (TableInfo tableInfo in lsttales)
            {
                Console.WriteLine($"{tableInfo.TabName}  {tableInfo.TabReName}  {tableInfo.TabDescript}  {tableInfo.TableType} 表结构:{tableInfo.HasTabStruct}");
            }
        }

        static void Demo_AllTables(HiSqlClient sqlClient)
        {
            List<TableInfo> lsttales = sqlClient.DbFirst.GetAllTables();
            foreach (TableInfo tableInfo in lsttales)
            {
                Console.WriteLine($"{tableInfo.TabName}  {tableInfo.TabReName}  {tableInfo.TabDescript}  {tableInfo.TableType} 表结构:{tableInfo.HasTabStruct}");
            }
        }

        static void Demo_CreateView(HiSqlClient sqlClient)
        {
            var rtn = sqlClient.DbFirst.CreateView("vw_FModel",
                sqlClient.HiSql("select a.TabName,b.TabReName,b.TabDescript,a.FieldName,a.SortNum,a.FieldType from Hi_FieldModel as a inner join Hi_TabModel as b on a.TabName=b.TabName").ToSql(),

                OpLevel.Execute);

            Console.WriteLine(rtn.Item2);
            Console.WriteLine(rtn.Item3);
        }

        static void Demo_ModiView(HiSqlClient sqlClient)
        {
            var rtn = sqlClient.DbFirst.ModiView("vw_FModel",
                sqlClient.HiSql("select a.TabName,b.TabReName,b.TabDescript,a.FieldName,a.SortNum,a.FieldType from Hi_FieldModel as a inner join Hi_TabModel as b on a.TabName=b.TabName where b.TabType in (0,1)").ToSql(),

                OpLevel.Execute);

            Console.WriteLine(rtn.Item2);
            Console.WriteLine(rtn.Item3);
        }

        static void Demo_DropView(HiSqlClient sqlClient)
        {
            var rtn = sqlClient.DbFirst.DropView("vw_FModel",

                OpLevel.Execute);

            Console.WriteLine(rtn.Item2);
            Console.WriteLine(rtn.Item3);
        }

        static void Demo_AddColumn(HiSqlClient sqlClient)
        {
            HiColumn column = new HiColumn()
            {
                TabName = "htest01",
                FieldName = "TestAdd",
                FieldType = HiType.VARCHAR,
                FieldLen = 50,
                DBDefault = HiTypeDBDefault.EMPTY,
                DefaultValue = "",
                FieldDesc = "测试字段添加"

            };

            var rtn = sqlClient.DbFirst.AddColumn("htest01", column, OpLevel.Execute);

            Console.WriteLine(rtn.Item2);
        }

        static void Demo_DelColumn(HiSqlClient sqlClient)
        {
            HiColumn column = new HiColumn()
            {
                TabName = "htest01",
                FieldName = "TestAdd",
                FieldType = HiType.VARCHAR,
                FieldLen = 50,
                DBDefault = HiTypeDBDefault.EMPTY,
                DefaultValue = "",
                FieldDesc = "测试字段添加"

            };

            var rtn = sqlClient.DbFirst.DelColumn("htest01", column, OpLevel.Execute);

            Console.WriteLine(rtn.Item2);
        }

        static void Demo_ModiColumn(HiSqlClient sqlClient)
        {
            HiColumn column = new HiColumn()
            {
                TabName = "H_Test5",
                FieldName = "TestAdd",
                FieldType = HiType.VARCHAR,
                FieldLen = 50,
                DBDefault = HiTypeDBDefault.VALUE,
                DefaultValue = "TGM",
                FieldDesc = "测试字段添加"

            };

            var rtn = sqlClient.DbFirst.ModiColumn("H_Test5", column, OpLevel.Execute);

            Console.WriteLine(rtn.Item2);
            Console.WriteLine(rtn.Item3);



            Console.WriteLine(rtn.Item2);
        }
    }
}