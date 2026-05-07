using System;
using System.IO;
using System.Web.Script.Serialization;

namespace SyncBranchAgent.Agent
{
    public class WatermarkStore
    {
        private readonly string path;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public WatermarkStore(string path)
        {
            this.path = path;
            var directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public Watermark Read()
        {
            if (!File.Exists(path))
            {
                return new Watermark();
            }

            var json = File.ReadAllText(path);
            return String.IsNullOrWhiteSpace(json) ? new Watermark() : serializer.Deserialize<Watermark>(json);
        }

        public void Write(Watermark watermark)
        {
            watermark.UpdatedAtUtc = DateTime.UtcNow;
            File.WriteAllText(path, serializer.Serialize(watermark));
        }
    }

    public class Watermark
    {
        public long LastTransactionId { get; set; }
        public DateTime? LastScanUtc { get; set; }
        public DateTime? LastSendUtc { get; set; }
        public DateTime? LastHeartbeatUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
