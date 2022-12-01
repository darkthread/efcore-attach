using CRUDExample.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace efcore_attach
{
    public static class DbCtxHelper
    {
        // 使用記憶體中的 SQLite 資料庫，來去不留痕跡
        // https://blog.darkthread.net/blog/ef-core-test-with-in-memory-db/
        const string cnStr = "Data Source=InMemoryDbName;Mode=Memory;Cache=Shared";
        static SqliteConnection _persistConn;
        static DbCtxHelper() {
            _persistConn = new SqliteConnection(cnStr);
            _persistConn.Open();
            new JournalDbContext(new DbContextOptionsBuilder<JournalDbContext>()
                .UseSqlite(_persistConn)
                .Options)
                .Database.EnsureCreated();
        }
        static bool enableLog = false;
        public static JournalDbContext CreateDbContext()
        {
            var dbOpt = new DbContextOptionsBuilder<JournalDbContext>()
                .UseSqlite(cnStr)
                // 設定可動態開關的 Log 輸出，並限定觀察 SQL 語法
                .LogTo(s =>
                {
                    if (enableLog && s.Contains("Microsoft.EntityFrameworkCore.Database.Command"))
                         Console.WriteLine(s);
                }, Microsoft.Extensions.Logging.LogLevel.Information)
                // 連同寫入資料庫的參數一起顯示，正式環境需留意個資或敏感資料寫入Log
                .EnableSensitiveDataLogging()
                .Options;
            return new JournalDbContext(dbOpt);
        }
        public static void WriteLog(string msg, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
        public static void WriteRemark(string msg)
            => WriteLog(msg, ConsoleColor.Yellow);
        public static void WriteError(string msg) 
            => WriteLog(msg, ConsoleColor.Red);
        public static void WriteError(Exception ex)
            => WriteError(ex.InnerException?.Message ?? ex.Message);
        // 啟用 Log 輸出並執行 SaveChanges()
        public static void SaveChangesWithLogging(this JournalDbContext dbCtx) 
        {
            try
            {
                enableLog = true;
                dbCtx.SaveChanges();
                enableLog = false;
            }
            catch (Exception ex)
            {
                WriteError(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}