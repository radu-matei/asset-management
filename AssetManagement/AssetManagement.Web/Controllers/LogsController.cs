using AssetManagement.Common;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Web.Mvc;
using System;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Data.Entity;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using System.IO;

namespace AssetManagement.Web.Controllers
{
    public class LogsController : Controller
    {
        private AssetManagementContext _assetManagementContext { get; set; }
        private CloudQueue _requestQueue { get; set; }
        private CloudBlobContainer _imagesBlobContainer { get; set; }

        public LogsController()
        {
            _assetManagementContext = new AssetManagementContext();
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            var blobClient = storageAccount.CreateCloudBlobClient();
            _imagesBlobContainer = blobClient.GetContainerReference("images");

            var queueClient = storageAccount.CreateCloudQueueClient();
            _requestQueue = queueClient.GetQueueReference("thumbnailrequest");
        }

        public async Task<ActionResult> Index(int? assetId)
        {
            var logs = _assetManagementContext.Logs.AsQueryable();

            if (assetId != null)
                logs = logs.Where(log => log.AssetId == assetId);

            return View(await logs.ToListAsync());
        }

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var log = await _assetManagementContext.Logs.FindAsync(id);

            if (log == null)
                return HttpNotFound();

            ViewBag.AssetName = log.Asset.Name;
            return View(log);
        }

        public  ActionResult Create()
        {
            ViewBag.AssetId = new SelectList(_assetManagementContext.Assets, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            [Bind(Include = "Id,AssetId,Temperature,Pressure,Status")] Log log,
            HttpPostedFileBase imageFile)
        {
            CloudBlockBlob imageBlob = null;

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.ContentLength != 0)
                {
                    imageBlob = await UploadBlobAsync(imageFile);
                    log.ImageUrl = imageBlob.Uri.ToString();
                }
                _assetManagementContext.Logs.Add(log);
                await _assetManagementContext.SaveChangesAsync();

                if(imageBlob != null)
                {
                    var blobInfo = new BlobInformation()
                    {
                        LogId = log.Id,
                        BlobUri = new Uri(log.ImageUrl)
                    };

                    var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(blobInfo));
                    await _requestQueue.AddMessageAsync(queueMessage);
                }
                return RedirectToAction("Index");
            }
            ViewBag.AssetId = new SelectList(_assetManagementContext.Assets, "Id", "Name", log.AssetId);
            return View(log);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var log = await _assetManagementContext.Logs.FindAsync(id);

            if (log == null)
            {
                return HttpNotFound();
            }

            ViewBag.AssetId = new SelectList(_assetManagementContext.Assets, "Id", "Name", log.AssetId);
            return View(log);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            [Bind(Include = "Id,AssetId,Temperature,Pressure,ImageUrl,ThumbnailUrl,Status")] Log log,
            HttpPostedFileBase imageFile)
        {
            CloudBlockBlob imageBlob = null;
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.ContentLength != 0)
                {
                    await DeleteAdBlobsAsync(log);

                    imageBlob = await UploadBlobAsync(imageFile);
                    log.ImageUrl = imageBlob.Uri.ToString();
                }
                _assetManagementContext.Entry(log).State = EntityState.Modified;
                await _assetManagementContext.SaveChangesAsync();

                if (imageBlob != null)
                {
                    var blobInfo = new BlobInformation()
                    {
                        LogId = log.Id,
                        BlobUri = new Uri(log.ImageUrl)
                    };

                    var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(blobInfo));
                    await _requestQueue.AddMessageAsync(queueMessage);

                }
                return RedirectToAction("Index");
            }
            ViewBag.AssetId = new SelectList(_assetManagementContext.Assets, "Id", "Name", log.AssetId);
            return View(log);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var log = await _assetManagementContext.Logs.FindAsync(id);
            if (log == null)
                return HttpNotFound();

            return View(log);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var log = await _assetManagementContext.Logs.FindAsync(id);

            await DeleteAdBlobsAsync(log);

            _assetManagementContext.Logs.Remove(log);
            await _assetManagementContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        private async Task<CloudBlockBlob> UploadBlobAsync(HttpPostedFileBase imageFile)
        {
            var blobName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var imageBlob = _imagesBlobContainer.GetBlockBlobReference(blobName);

            using (var fileStream = imageFile.InputStream)
            {
                await imageBlob.UploadFromStreamAsync(fileStream);
            }

            return imageBlob;
        }

        private async Task DeleteAdBlobsAsync(Log log)
        {
            if (!string.IsNullOrWhiteSpace(log.ImageUrl))
            {
                var blobUri = new Uri(log.ImageUrl);
                await DeleteBlobAsync(blobUri);
            }

            if (!string.IsNullOrWhiteSpace(log.ThumbnailUrl))
            {
                var blobUri = new Uri(log.ThumbnailUrl);
                await DeleteBlobAsync(blobUri);
            }
        }

        private async Task DeleteBlobAsync(Uri blobUri)
        {
            var blobName = blobUri.Segments[blobUri.Segments.Length - 1];

            var blobToDelete = _imagesBlobContainer.GetBlockBlobReference(blobName);
            await blobToDelete.DeleteAsync();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _assetManagementContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}