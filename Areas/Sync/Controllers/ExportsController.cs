using System.Text;
using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;
using MyERP.Areas.Sync.ViewModels;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.Export)]
    public class ExportsController : SyncControllerBase
    {
        private readonly SyncReadRepository repository = new SyncReadRepository();

        public ActionResult QueueCsv(QueueFilter filter)
        {
            var queue = repository.GetQueue(filter);
            var csv = new StringBuilder();
            csv.AppendLine("SyncKey,BranchId,EntityType,EntityKey,ProfileName,Status,PayloadHash,TryCount,CreatedAt,CompletedAt,LastError");
            foreach (var row in queue.Rows)
            {
                csv.Append(Csv(row.SyncKey)).Append(',')
                   .Append(row.BranchId).Append(',')
                   .Append(Csv(row.EntityType)).Append(',')
                   .Append(Csv(row.EntityKey)).Append(',')
                   .Append(Csv(row.ProfileName)).Append(',')
                   .Append(Csv(row.Status)).Append(',')
                   .Append(Csv(row.PayloadHash)).Append(',')
                   .Append(row.TryCount).Append(',')
                   .Append(Csv(row.CreatedAt.HasValue ? row.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")).Append(',')
                   .Append(Csv(row.CompletedAt.HasValue ? row.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")).Append(',')
                   .Append(Csv(row.LastError)).AppendLine();
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "sync-queue.csv");
        }

        private static string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
