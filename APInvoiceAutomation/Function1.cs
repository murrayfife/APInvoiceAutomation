using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;

namespace APInvoiceAutomation
{
    public static class ProcessPackage
    {
        [FunctionName("ProcessPackage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string invoiceNo = req.Query["invoice"];

            string source = @"D:\home\site\template\template.zip";
            string target = $@"D:\home\site\template\{Guid.NewGuid().ToString()}.zip";

            File.Copy(source, target);
            using (FileStream zipToOpen = new FileStream(target, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    //write attachment to the package
                    ZipArchiveEntry invoiceEntry = archive.CreateEntry(@"Resources\Vendor invoice document attachment\invoice.pdf");
                    req.Body.CopyTo(invoiceEntry.Open());

                    //update the CSV content
                    ZipArchiveEntry csvFile = archive.GetEntry(@"Vendor invoice document attachment.csv");
                    StringBuilder csvContent;
                    using (StreamReader csvStream = new StreamReader(csvFile.Open()))
                    {
                        csvContent = new StringBuilder(csvStream.ReadToEnd());
                        csvContent.Replace("docid", Guid.NewGuid().ToString());
                        csvContent.Replace("{invoiceno}", invoiceNo);
                    }
                    csvFile.Delete();

                    ZipArchiveEntry newCsvFile = archive.CreateEntry(@"Vendor invoice document attachment.csv");
                    MemoryStream msCsv = new MemoryStream(Encoding.ASCII.GetBytes(csvContent.ToString()));
                    msCsv.CopyTo(newCsvFile.Open());
                }
            }

            var output = File.ReadAllBytes(target);
            File.Delete(target);
            FileContentResult result = new FileContentResult(output, @"application/zip")
            {
                FileDownloadName = "package.zip"
            };
            return (ActionResult)result;
        }
    }
}
