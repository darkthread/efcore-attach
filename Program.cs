using CRUDExample.Models;
using efcore_attach;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

Action<string> print = (s) =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(s);
    Console.ResetColor();
};

var recDate = new DateTime(2022, 1, 1);
// 產生待更新資料 / EventSummary 含隨機內容
Func<DailyRecord> GenDailyRecord = () => new DailyRecord
{
    // Id 為 Primary Key，採自動編號，不需指定
    Date = recDate,
    EventSummary = $"Update - {new Random().NextDouble():n6}",
    Remark = "",
    User = "darkthread"
};

// 先新增一筆資料
using (var dbCtx = DbCtxHelper.CreateDbContext())
{
    dbCtx.Records.Add(GenDailyRecord());
    dbCtx.SaveChanges();
}

using (var dbCtx = DbCtxHelper.CreateDbContext())
{
    // 使用 Attach 相當於 Entry(...).State = EntityState.Unchanged
    print("實驗一 Attach / 不指定 Id");
    var record = GenDailyRecord();
    var entEntry = dbCtx.Attach(record);
    DbCtxHelper.WriteRemark("未指定自動跳號 Primary Key 時，State = " + entEntry.State);
    // 但日期相同，違反 Unique Index
    dbCtx.SaveChangesWithLogging();
}

int recId;
using (var dbCtx = DbCtxHelper.CreateDbContext())
{
    print("實驗二 Attach / 查詢過同一筆無法 Attach");
    var record = GenDailyRecord();
    recId = dbCtx.Records.First(r => r.Date == recDate).Id;
    record.Id = recId;
    try
    {
        dbCtx.Attach(record);
    }
    catch (Exception ex)
    {
        DbCtxHelper.WriteError(ex);
    }
}

using (var dbCtx = DbCtxHelper.CreateDbContext())
{
    print("實驗三 Attach / 指定 Id");
    var record = GenDailyRecord();
    record.Id = recId;
    var entEntry = dbCtx.Attach(record);
    DbCtxHelper.WriteRemark("指定自動跳號 Primary Key 時，State = " + entEntry.State);
    dbCtx.SaveChangesWithLogging();
    DbCtxHelper.WriteRemark("純 Attach 未改屬性，無動作");
    print("實驗四 Attach 後修改 EventSummary");
    record.EventSummary = "Attach 後修改";
    dbCtx.SaveChangesWithLogging();
    DbCtxHelper.WriteRemark("只更新異動欄位");
}

using (var dbCtx = DbCtxHelper.CreateDbContext())
{
    print("實驗五 Entry().State = EntityState.Modified");
    var record = GenDailyRecord();
    record.Id = recId;
    dbCtx.Entry(record).State = EntityState.Modified;
    dbCtx.SaveChangesWithLogging();
    DbCtxHelper.WriteRemark("更新所有欄位，不管是否與原來相同");
}

using (var dbCtx = DbCtxHelper.CreateDbContext())
{
    print("實驗六 SetValues()");
    var record = GenDailyRecord();
    var exist = dbCtx.Records.Find(recId);
    record.Id = recId;
    dbCtx.Entry(exist).CurrentValues.SetValues(record);
    dbCtx.SaveChangesWithLogging();
    DbCtxHelper.WriteRemark("只會更新異動欄位 EventSummary");
}

using (var dbCtx = DbCtxHelper.CreateDbContext()) 
{
    print("實驗七 SetValues(任意物件)");
    var exist = dbCtx.Records.Find(recId);
    dbCtx.Entry(exist).CurrentValues.SetValues(new {
        EventSummary = "ViewModel 或匿名物件",
        Remark = "",
        NoMappingProp = 1234
    });
    dbCtx.SaveChangesWithLogging();
    DbCtxHelper.WriteRemark("只更新有對映且異動的欄位");    
}

using (var dbCtx = DbCtxHelper.CreateDbContext()) 
{
    print("實驗八 SetValues(Dictionary<string, object>)");
    var exist = dbCtx.Records.Find(recId);
    dbCtx.Entry(exist).CurrentValues.SetValues(new Dictionary<string, object>() {
        ["EventSummary"] = "來自 Dictionary",
        ["Remark"] = "",
        ["NoMappingProp"] = 1234
    });
    dbCtx.SaveChangesWithLogging();
    DbCtxHelper.WriteRemark("只更新有對映且異動的欄位");    
}