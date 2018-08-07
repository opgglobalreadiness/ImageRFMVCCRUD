using ImageRFMVCCRUD.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Data.SqlClient;
using System.Data.Entity.Core.EntityClient;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ImageRFMVCCRUD.Controllers
{
    public class ImageRequestController : Controller
    {
        //HelperMethods helper = new HelperMethods();

        // GET: ImageRequest
        public ActionResult Index()
        {
            //GenerateConnectionString("ImageRFDatabase", "opgglobal.database.windows.net,1433");
            return View();
        }
        public ActionResult GetData()
        {
            try
            {
                using (DBModels2 db = new DBModels2())
                {
                    List<ImageRFTable> imageRequestList = db.ImageRFTables.ToList<ImageRFTable>();
                    return Json(new { data = imageRequestList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine(string.Format(" GetData(): Error Displaying {0}", exp.Message));
                throw;
            }
        }

        public void PopulateTargetMarketsCheckBox()
        {
            List<TargetMarketCheckbox> targetMarketList = new List<TargetMarketCheckbox>();
            targetMarketList.Add(new TargetMarketCheckbox { Id = 1, Name = "Global release", IsChecked = false });
            targetMarketList.Add(new TargetMarketCheckbox { Id = 2, Name = "US-only release", IsChecked = false });
            targetMarketList.Add(new TargetMarketCheckbox { Id = 3, Name = "Western markets", IsChecked = false });
            targetMarketList.Add(new TargetMarketCheckbox { Id = 4, Name = "Latin-American markets", IsChecked = false });
            targetMarketList.Add(new TargetMarketCheckbox { Id = 5, Name = "African/Middle Eastern markets", IsChecked = false });
            targetMarketList.Add(new TargetMarketCheckbox { Id = 6, Name = "Asian markets", IsChecked = false });

            ViewBag.targetMarketList = targetMarketList;
        }
        [HttpGet]
        public ActionResult AddOrEdit(int id = 0)
        {
            //Populate Target Markets/Countries
            PopulateTargetMarketsCheckBox();

            if (id == 0)
            {
                return View(new ImageRFTable());
            }
            else
            {
                using (DBModels2 db = new DBModels2())
                {
                    return View(db.ImageRFTables.Where(x => x.ImageRFId == id).FirstOrDefault<ImageRFTable>());
                }
            }
        }

        [HttpPost]//Add, Edit or Update
        public ActionResult AddOrEdit(ImageRFTable img)
        {
            using (DBModels2 db = new DBModels2())
            {
                if (img.ImageRFId == 0)
                {
                    try
                    {
                        db.ImageRFTables.Add(img);
                        db.SaveChanges();
                        //SendTheEmailAsync(img);
                        return Json(new { success = true, message = "Saved Successfully! Email sent..." }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception exp)
                    {
                        Debug.WriteLine(string.Format(" HTTPPOST (ImageRFID=0): Error Adding {0}", exp.Message));
                        throw;
                    }
                }
                else
                {
                    db.Entry(img).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Updated Successfully! Email sent..." }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public async System.Threading.Tasks.Task SendTheEmailAsync(ImageRFTable img)
        {
            var apiKey = System.Environment.GetEnvironmentVariable("GRSendGridAPIKey");
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("jchoe@microsoft.com", "Image Request Form"),
                Subject = "subject" + img.ImageRFId,
                PlainTextContent = "Hello email",
                HtmlContent = "Hello email"
            };
            msg.AddTo(new EmailAddress("jchoe@microsoft.com", "Jon Choe"));

            var response = await client.SendEmailAsync(msg);
        }

        [HttpPost]//Delete
        public ActionResult Delete(int id)
        {
            using (DBModels2 db = new DBModels2())
            {
                ImageRFTable img = db.ImageRFTables.Where(x => x.ImageRFId == id).FirstOrDefault<ImageRFTable>();
                db.ImageRFTables.Remove(img);
                db.SaveChanges();
                return Json(new { success = true, message = "Deleted Successfully!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public void SendMail(string _title,
                             string _requestId,
                             string _requestor,
                             string _emailaddress,
                             string _requestDate,
                             string _inquiryCategory,
                             string _teamname,
                             string _emailCCaddress,
                             string _reviewRequestTitle,
                             string _requestDetails,
                             string _productReleaseDate,
                             string _targetMarkets,
                             string _numberOfImages,
                             string _listofFilesUpload,
                             string _uploadFolderPath)
        {
            //Replace targetMarkets (1, 2, 3, etc...) with the actual value
            string[] markets = _targetMarkets.Split(',');
            string listOfMarkets = String.Empty;
            foreach (var market in markets)
            {
                switch (market)
                {
                    case "1":
                        listOfMarkets += "Global release, ";
                        break;
                    case "2":
                        listOfMarkets += "US-only release, ";
                        break;
                    case "3":
                        listOfMarkets += "Western markets, ";
                        break;
                    case "4":
                        listOfMarkets += "Latin-American markets, ";
                        break;
                    case "5":
                        listOfMarkets += "African/Middle Eastern markets, ";
                        break;
                    case "6":
                        listOfMarkets += "Asian markets, ";
                        break;
                    default:
                        break;
                }
            }
            //Remove unwanted comma at the end
            listOfMarkets = listOfMarkets.Remove(listOfMarkets.LastIndexOf(", "));

            //Email CC address
            string ccEmailAddress = _emailaddress + ";" + _emailCCaddress;

            //Send email
            SmtpMail sm = new SmtpMail();
            String subject = string.Format(_title + " by " + _requestor + " on {0}", DateTime.Now);
            string emailBody = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">" +
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">" +
                "<head>" +
                "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />" +
                "<style>" +
                ".imageRF {table-layout: fixed;width: 100%;white-space: nowrap;padding-right: 15px}" +
                ".imageRF td {valign: top;white-space: nowrap;overflow: hidden;text-overflow ellipsis;}" +
                ".imageRF th {padding: 3px;text-align: left;background-color: #4CAF50;color: white;border: 1px solid #ddd;}" +
                ".row-field {width: 30%;}" +
                ".row-response {width: 70%;}" +
                "</style>" +
                "</head>" +
                "<body>" +
                "<h2>Image Request Form Details</h2>" +
                "<p>" + _title + "</p>" +
                "<table class=\"imageRF\">" +
                "<tr>" +
                "<th><div class=\"row-field\">Input Fields</div></th>" +
                "<th><div class=\"row-response\">User Response</div></th>" +
                "</tr>" +
                "<tr><td>Request ID</td><td>" + _requestId + "</td></tr>" +
                "<tr><td>Requestor</td><td>" + _requestor + "</td></tr>" +
                "<tr><td>Email Address</td><td>" + _emailaddress + "</td></tr>" +
                "<tr><td>Request Date</td><td>" + _requestDate + "</td></tr>" +
                "<tr><td>Inquiry Category</td><td>" + _inquiryCategory + "</td></tr>" +
                "<tr><td>Team name</td><td>" + _teamname + "</td></tr>" +
                "<tr><td>Email CC</td><td>" + _emailCCaddress + "</td></tr>" +
                "<tr><td>Review Request Title</td><td>" + _reviewRequestTitle + "</td></tr>" +
                "<tr><td>Request Details</td><td>" + _requestDetails + "</td></tr>" +
                "<tr><td>Product Release Date</td><td>" + _productReleaseDate + "</td></tr>" +
                "<tr><td>Target Market(s)</td><td>" + listOfMarkets + "</td></tr>" +
                "<tr><td>Number of Images</td><td>" + _numberOfImages + "</td></tr>" +
                "<tr><td>List of Images</td><td>" + _listofFilesUpload + "</td></tr>" +
                "<tr><td>Uploaded Folder Path</td><td>" + _uploadFolderPath + "</td></tr>" +
                "</table>" +
                "</body>" +
                "</html>";
            sm.SendMail("jchoe@microsoft.com", "jchoe@microsoft.com", ccEmailAddress, subject, emailBody, false, "");
        }
        public async System.Threading.Tasks.Task SendEMailReviewFormFromAzureAsync(string _requestID,
                                                                                   string _modifiedBy,
                                                                                   string _modifiedDate,
                                                                                   string _imageSrc)
        {
            string subject = string.Format("Image Review Submission: ID: " + _requestID);

            var emailBody = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">" +
                            "<html xmlns=\"http://www.w3.org/1999/xhtml\">" +
                            "<head>" +
                            "<title>Image Reviewer Tool Email Summary" +
                            "</title>" +
                            "<link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css\">" +
                            "</head>" +
                            "<body>" +
                            "<div class=\"container-fluid\">" +
                                "<div class=\"row\">" +
                                    "<div class=\"col-md-12\">" +
                                        "<span><hr></span>" +
                                        "<div class=\"row\">" +
                                            "<div class=\"col-md-1\">" +
                                                "<img alt=\"Image Preview\" src=\"" + _imageSrc + "\" width=\"150 height=\"150\" style=\"padding-right: 15px;padding-bottom:5px\"/>" +
                                            "</div>" +
                                            "<div class=\"col-md-1\">" +
                                                "<dl>" +
                                                    "<dt>" +
                                                        "Image ID" +
                                                    "</dt>" +
                                                    "<dd>" +
                                                        _imageSrc +
                                                    "</dd>" +
                                                    "<dt>" +
                                                        "Image Name" +
                                                    "</dt>" +
                                                    "<dd>" +
                                                        "ABC.png" +
                                                    "</dd>" +
                                                "</dl>" +
                                            "</div>" +
                                            "<div class=\"col-md-1\">" +
                                                "<dl>" +
                                                    "<dt>" +
                                                        "Modified By" +
                                                    "</dt>" +
                                                    "<dd>" +
                                                        _modifiedBy + 
                                                    "</dd>" +
                                                    "<dt>" +
                                                        "Modified Date" +
                                                    "</dt>" +
                                                    "<dd>" +
                                                        _modifiedDate + 
                                                    "</dd>" +
                                                "</dl>" +
                                            "</div>" +
                                            "<div class=\"col-md-9\">" +
                                                "<dl>" +
                                                    "<dt>" +
                                                        "Image Comments" +
                                                    "</dt>" +
                                                    "<dd>" +
                                                        "This image contains a person riding a bicycle." +
                                                    "</dd>" +
                                                    "<dt>" +
                                                        "Theme or Subcategories" +
                                                    "</dt>" +
                                                    "<dd>" +
                                                        "Culture, Text, Religion" +
                                                    "</dd>" +
                                                    "<dt>" +
                                                        "Target Markets" +
                                                    "</dt>" +
                                                    "<dd>" +
                                                        "Global-Release, Asian-Release" +
                                                    "</dd>" +
                                                "</dl>" +
                                            "</div>" +
                                        "</div>" +
                                        "<table class=\"table\" style=\"font-size: small\">" +
                                            "<thead>" +
                                                "<tr>" +
                                                    "<th class=\"col-md-2\">" +
                                                        "Comments" +
                                                    "</th>" +
                                                    "<th class=\"col-md-8\">" +
                                                        "Description" +
                                                    "</th>" +
                                                    "<th class=\"col-md-2\">" +
                                                        "Status" +
                                                    "</th>" +
                                                "</tr>" +
                                            "</thead>" +
                                            "<tbody>" +
                                                "<tr>" +
                                                    "<td>" +
                                                        "Designer Comments" +
                                                    "</td>" +
                                                    "<td>" +
                                                        "This is a designer comments." +
                                                    "</td>" +
                                                    "<td>" +
                                                        "Accepted" +
                                                    "</td>" +
                                                "</tr>" +
                                                "<tr class=\"table-active\">" +
                                                    "<td>" +
                                                        "Cela Comments" +
                                                    "</td>" +
                                                    "<td>" +
                                                        "This is a CELA comments." +
                                                    "</td>" +
                                                    "<td>" +
                                                        "Pending" +
                                                    "</td>" +
                                                "</tr>" +
                                                "<tr class=\"table-success\">" +
                                                    "<td>" +
                                                        "GeoPol Comments" +
                                                    "</td>" +
                                                    "<td>" +
                                                        "This is a GeoPol comments." +
                                                    "</td>" +
                                                    "<td>" +
                                                        "Rejected" +
                                                    "</td>" +
                                                "</tr>" +
                                                "<tr class=\"table-warning\">" +
                                                    "<td>" +
                                                        "LSP Comments" +
                                                    "</td>" +
                                                    "<td>" +
                                                        "This is a LSP comments." +
                                                    "</td>" +
                                                    "<td>" +
                                                        "Pending" +
                                                    "</td>" +
                                                "</tr>" +
                                            "</tbody>" +
                                        "</table>" +
                                    "</div>" +
                                "</div>" +
                            "</div>" +
                            "</body>" +
                            "</html>";

            var apiKey = System.Environment.GetEnvironmentVariable("GRSendGridAPIKey");
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("jchoe@microsoft.com", "Image Reviewer Tool"),
                Subject = subject,
                PlainTextContent = emailBody,
                HtmlContent = emailBody
            };

            msg.AddTo(new EmailAddress("jchoe@microsoft.com", "Jon C. Choe"));

            var response = await client.SendEmailAsync(msg);

        }


        public async System.Threading.Tasks.Task SendMailFromAzureAsync(string _requestId,
                                                                         string _requestor,
                                                                         string _emailaddress,
                                                                         string _requestDate,
                                                                         string _inquiryCategory,
                                                                         string _teamname,
                                                                         string _emailCCaddress,
                                                                         string _reviewRequestTitle,
                                                                         string _requestDetails,
                                                                         string _productReleaseDate,
                                                                         string _targetMarkets,
                                                                         string _numberOfImages,
                                                                         string _listofFilesUpload,
                                                                         string _uploadFolderPath)
        {
            int imageRFID = 0;
            //Get the latest Image Request ID
            using (DBModels2 db = new DBModels2())
            {
                //New request
                if (_requestId == "0")
                {
                    imageRFID = db.ImageRFTables.Max(x => x.ImageRFId) + 1;
                }
                else
                {
                    //Existing request
                    imageRFID = Convert.ToInt32(_requestId);
                }
            }

            //Replace targetMarkets (1, 2, 3, etc...) with the actual value
            string[] markets = _targetMarkets.Split(',');
            string listOfMarkets = String.Empty;
            foreach (var market in markets)
            {
                switch (market)
                {
                    case "1":
                        listOfMarkets += "Global release, ";
                        break;
                    case "2":
                        listOfMarkets += "US-only release, ";
                        break;
                    case "3":
                        listOfMarkets += "Western markets, ";
                        break;
                    case "4":
                        listOfMarkets += "Latin-American markets, ";
                        break;
                    case "5":
                        listOfMarkets += "African/Middle Eastern markets, ";
                        break;
                    case "6":
                        listOfMarkets += "Asian markets, ";
                        break;
                    default:
                        break;
                }
            }
            //Remove unwanted comma at the end
            if (listOfMarkets != "")
            {
                listOfMarkets = listOfMarkets.Remove(listOfMarkets.LastIndexOf(", "));
            }

            //Email CC address
            string ccEmailAddress = _emailaddress + ";" + _emailCCaddress;

            //Datetime
            //DateTime dtnow = new DateTime();

            //Send email
            string subject = string.Format("Image Request Submission: ID:" + imageRFID + ", Title:'" + _reviewRequestTitle + "' by " + _requestor);

            var emailBody = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">" +
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">" +
                "<head>" +
                "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />" +
                "<style>" +
                ".imageRF {table-layout: fixed;width: 100%;white-space: nowrap;padding-right: 15px}" +
                ".imageRF td {valign: top;white-space: nowrap;overflow: hidden;text-overflow ellipsis;}" +
                ".imageRF th {padding: 3px;text-align: left;background-color: #4CAF50;color: white;border: 1px solid #ddd;}" +
                ".row-field {width: 20%;}" +
                ".row-response {width: 80%;}" +
                "</style>" +
                "</head>" +
                "<body>" +
                "<h2>Image Request Form Details</h2>" +
                //"<p>New Image Request Form has been submitted on " + dtnow.AddHours(-7).ToString() + "</p>" +
                "<table class=\"imageRF\">" +
                "<tr>" +
                "<th><div class=\"row-field\">Input Fields</div></th>" +
                "<th><div class=\"row-response\">User Response</div></th>" +
                "</tr>" +
                "<tr><td>Request ID</td><td>" + imageRFID + "</td></tr>" +
                "<tr><td>Requestor</td><td>" + _requestor + "</td></tr>" +
                "<tr><td>Email Address</td><td>" + _emailaddress + "</td></tr>" +
                "<tr><td>Request Date</td><td>" + _requestDate + "</td></tr>" +
                "<tr><td>Inquiry Category</td><td>" + _inquiryCategory + "</td></tr>" +
                "<tr><td>Team name</td><td>" + _teamname + "</td></tr>" +
                "<tr><td>Email CC</td><td>" + _emailCCaddress + "</td></tr>" +
                "<tr><td>Review Request Title</td><td>" + _reviewRequestTitle + "</td></tr>" +
                "<tr><td>Request Details</td><td>" + _requestDetails + "</td></tr>" +
                "<tr><td>Product Release Date</td><td>" + _productReleaseDate + "</td></tr>" +
                "<tr><td>Target Market(s)</td><td>" + listOfMarkets + "</td></tr>" +
                "<tr><td>Number of Images</td><td>" + _numberOfImages + "</td></tr>" +
                "<tr><td>List of Images</td><td>" + _listofFilesUpload + "</td></tr>" +
                "<tr><td>Uploaded Folder Name</td><td>" + _uploadFolderPath + "</td></tr>" +
                "<tr><td>Image Request Form URL</td><td><a href=\"http://imagerfdatabase.azurewebsites.net/\">Click here</a></td></tr>" +
                "</table>" +
                "</body>" +
                "</html>";

            var apiKey = System.Environment.GetEnvironmentVariable("GRSendGridAPIKey");
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("cdowling@microsoft.com", "Image Request Form"),
                Subject = subject,
                PlainTextContent = emailBody,
                HtmlContent = emailBody
            };
            msg.AddTo(new EmailAddress("jchoe@microsoft.com", "Jon Choe"));
            msg.AddTo(new EmailAddress("cdowling@microsoft.com", "Clair Noone"));
            msg.AddTo(new EmailAddress("jenyuan@microsoft.com", "Jennifer Yuan"));
            msg.AddTo(new EmailAddress("manuelco@microsoft.com", "Manuel Coltorti"));
            msg.AddTo(new EmailAddress("fionaog@microsoft.com", "Fiona O'Grady"));
            msg.AddTo(new EmailAddress("Alfred.Hellstern@microsoft.com", "Alfred Hellstern"));
            msg.AddTo(new EmailAddress("burnsm@microsoft.com", "Burns Montgomery"));
            msg.AddTo(new EmailAddress("charhem@microsoft.com", "Charles Hemstreet"));
            msg.AddTo(new EmailAddress("davidsa@microsoft.com", "David Salaguinto"));
            msg.AddTo(new EmailAddress("elleno@microsoft.com", "Ellen O'Neill (IRELAND)"));
            msg.AddTo(new EmailAddress("florja@exchange.microsoft.com", "Florence Jarzynski"));
            msg.AddTo(new EmailAddress("Gerard.Veloo@microsoft.com", "Gerard Veloo"));
            msg.AddTo(new EmailAddress("Hideatsu.Hosokai@microsoft.com", "Hideatsu Hosokai"));
            msg.AddTo(new EmailAddress("hongda@microsoft.com", "HongLing Da"));
            msg.AddTo(new EmailAddress("v-ivluka@microsoft.com", "Ivan Lukavsky (Moravia IT s.r.o.)"));
            msg.AddTo(new EmailAddress("Marie.Meade@microsoft.com", "Marie Meade"));
            msg.AddTo(new EmailAddress("v-micarn@microsoft.com", "Michal Carnicky (Moravia IT)"));
            msg.AddTo(new EmailAddress("ppower@microsoft.com", "Patricia Power"));
            msg.AddTo(new EmailAddress("simonda@microsoft.com", "Si Daniels"));
            msg.AddTo(new EmailAddress("v-tobosa@microsoft.com", "Tomas Bosak (Moravia IT)"));
            msg.AddTo(new EmailAddress("vsievers@exchange.microsoft.com", "Veronica Sievers"));
            msg.AddTo(new EmailAddress("v-veruz@microsoft.com", "Veronika Ruzickova (Moravia IT)"));
            msg.AddTo(new EmailAddress("v-zoszan@microsoft.com", "Zoltan Szanto (Moravia IT)"));
            msg.AddTo(new EmailAddress("GR_MTT@microsoft.com", "Global Readiness Mail to Task"));
            msg.AddTo(new EmailAddress("opgglobal@microsoft.com", "OPG Global Readiness"));
            msg.AddTo(new EmailAddress("GSXSME-ImgRvw@microsoft.com", "Image & Video Review Team"));
            msg.AddTo(new EmailAddress(ccEmailAddress, ""));

            var response = await client.SendEmailAsync(msg);
        }
        [HttpPost]//Image Upload
        public ActionResult UploadFilesToFileStorage()
        {
            //Azure File Storage Account Name
            const string StorageAccountName = "globalreadinessfileshare";

            //Azure File Storage Account Key
            const string StorageAccountKey = "6jMlUXXIoM723dj1E7GbgM6InQBS29WXrTUC00nwMB4yWvqkjwaii/ao4SO3zq8IaT7aUvL/vVBhH8F80a6CIQ==";

            //Retrieve storage account info by leveraging the Storage Account and Key Cendentials
            var storageAccount = new CloudStorageAccount(new StorageCredentials(StorageAccountName, StorageAccountKey), false);

            //Get a reference to the file share called "imagereview" we created
            var share = storageAccount.CreateCloudFileClient().GetShareReference("imagereview");

            try
            {
                //Target filename
                string filename = String.Empty;

                //Target filename without extension
                string filenameWithoutExtension = String.Empty;

                //List string to store the contents of Review.xml information
                List<string> reviewXML = new List<string>();

                //Files selected from the Browse button
                HttpFileCollectionBase selectedFiles = Request.Files;

                //Determine the location of last slash "/"
                int forwardSlashLoc = Request.FilePath.LastIndexOf("/");

                //Extract the target folder name from the files selected
                string targetFolderName = Request.FilePath.Substring(forwardSlashLoc + 1, Request.FilePath.Length - forwardSlashLoc - 1).Replace(" ", String.Empty);

                //Temporarily store the image files into the "Upload" target folder. From the Upload folder, we can upload images into the Azure File Storage
                string localPathToUploadTargetFolderDir = Server.MapPath("~/Upload/" + targetFolderName);

                //Ensure that the share exists.
                if (share.Exists())
                {
                    // Get a reference to the root directory for the share in Azure.
                    CloudFileDirectory rootPathToAzureDir = share.GetRootDirectoryReference();

                    // Get a reference to the new target directory that we're creating in Azure.
                    CloudFileDirectory newTargetFolderInAzureDir = rootPathToAzureDir.GetDirectoryReference(targetFolderName);
                    newTargetFolderInAzureDir.CreateIfNotExists();

                    // Ensure that the directory exists.
                    if (newTargetFolderInAzureDir.Exists())
                    {
                        //Iterate files selected
                        for (int i = 0; i < selectedFiles.Count; i++)
                        {
                            //Provides access to individual files that have been uploaded by a client
                            HttpPostedFileBase imgFile = selectedFiles[i];
                            try
                            {
                                //Get target filename
                                filename = Path.GetFileName(imgFile.FileName);
                                filenameWithoutExtension = Path.GetFileNameWithoutExtension(imgFile.FileName);

                                //Append an entry to the Review.xml file
                                reviewXML.Add("<ReviewEntry><Id>" + filenameWithoutExtension + "</Id><Word>" + targetFolderName + "</Word><PartOfSpeech></PartOfSpeech><Images><string>" + filename + "</string></Images><BackupImages /><DesignerSignoff></DesignerSignoff><DesignerComments></DesignerComments><CelaSignoff></CelaSignoff><CelaComments></CelaComments><GeopolSignoff></GeopolSignoff><GeopolComments></GeopolComments><GeopolFullMarketSignoff></GeopolFullMarketSignoff><GeopolFullMarketComments></GeopolFullMarketComments></ReviewEntry>");

                                // Determine whether the local directory exists.
                                if (!Directory.Exists(localPathToUploadTargetFolderDir))
                                {
                                    // Try to create the directory.
                                    DirectoryInfo di = Directory.CreateDirectory(localPathToUploadTargetFolderDir);
                                }

                                //Physical path to the image file
                                string rootPathFilename = localPathToUploadTargetFolderDir + "/" + filename;

                                //Save the image file to the Upload/TargetFolder location (temporarily)
                                imgFile.SaveAs(rootPathFilename);

                                // Get a reference to the file we created previously.
                                CloudFile cloudfile = newTargetFolderInAzureDir.GetFileReference(filename);

                                //Upload the file to Azure.
                                cloudfile.UploadFromFile(rootPathFilename, FileMode.OpenOrCreate);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                        //Create and Upload Review.xml file to Azure File Storage
                        CreateAndUploadReviewXMLDocument(localPathToUploadTargetFolderDir, reviewXML, newTargetFolderInAzureDir);

                        //Clean up : delete temporary directory
                        Directory.Delete(localPathToUploadTargetFolderDir, true);
                    }
                }
                return Json(selectedFiles.Count + " Files Uploaded!");
            }
            catch (Exception)
            {
                throw;
            }
        }
        [HttpPost]//Image Upload
        public ActionResult UploadFilesToBlobStorage(string folderDir)
        {
            var modifiedBy = "";
            var modifiedDate = "";

            //Azure File Storage Account Name
            const string StorageAccountName = "globalreadinessfileshare";

            //Azure File Storage Account Key
            const string StorageAccountKey = "6jMlUXXIoM723dj1E7GbgM6InQBS29WXrTUC00nwMB4yWvqkjwaii/ao4SO3zq8IaT7aUvL/vVBhH8F80a6CIQ==";

            //Blob Container name
            const string BlobContainerName = "imagereview";

            //File Counter
            int fileCounter = 0;

            //Retrieve storage account info by leveraging the Storage Account and Key Cendentials
            var storageAccount = new CloudStorageAccount(new StorageCredentials(StorageAccountName, StorageAccountKey), false);

            //Get a reference to the file share called "imagereview" we created
            var share = storageAccount.CreateCloudBlobClient().GetContainerReference(BlobContainerName);

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(BlobContainerName);

            try
            {
                //Target filename
                string filename = String.Empty;

                //Target filename without extension
                string filenameWithoutExtension = String.Empty;

                //List string to store the contents of Review.xml information
                List<string> reviewEntryJsonFile = new List<string>();

                //Files selected from the Browse button
                HttpFileCollectionBase selectedFiles = Request.Files;

                //Determine the location of last slash "/"
                int forwardSlashLoc = Request.FilePath.LastIndexOf("/");

                //Extract the target folder name from the files selected
                string targetFolderDir = Request.FilePath.Substring(forwardSlashLoc + 1, Request.FilePath.Length - forwardSlashLoc - 1).Replace(" ", String.Empty);

                //Temporarily store the image files into the "Upload" target folder. From the Upload folder, we can upload images into the Azure File Storage
                string localFolderDir = Server.MapPath("~/Upload/" + targetFolderDir);

                //Ensure that the share exists.
                if (share.Exists())
                {
                    // Get a reference to the new target directory that we're creating in Azure.
                    blobContainer.CreateIfNotExists();

                    // Ensure that the directory exists.
                    if (blobContainer.Exists())
                    {
                        reviewEntryJsonFile.Add("{");
                        reviewEntryJsonFile.Add("\"ModifiedBy\": \"" + modifiedBy + "\",");
                        reviewEntryJsonFile.Add("\"ModifiedDate\": \"" + modifiedDate + "\",");
                        reviewEntryJsonFile.Add("\"Metadata\": [");

                        //Iterate files selected
                        for (int i = 0; i < selectedFiles.Count; i++)
                        {
                            //Provides access to individual files that have been uploaded by a client
                            HttpPostedFileBase imgFile = selectedFiles[i];
                            try
                            {
                                fileCounter = i + 1;
                                //Get target filename
                                filename = Path.GetFileName(imgFile.FileName);
                                filenameWithoutExtension = Path.GetFileNameWithoutExtension(imgFile.FileName);

                                // Determine whether the local directory exists.
                                if (!Directory.Exists(localFolderDir))
                                {
                                    // Try to create the directory.
                                    DirectoryInfo di = Directory.CreateDirectory(localFolderDir);
                                }

                                //Physical path to the image file
                                string rootPathFilename = localFolderDir + "/" + filename;

                                //Save the image file to the Upload/TargetFolder location (temporarily)
                                imgFile.SaveAs(rootPathFilename);

                                //Append an entry to the ReviewEntry.json file
                                reviewEntryJsonFile.Add("{");
                                reviewEntryJsonFile.Add("\"Id\": " + fileCounter + ",");
                                reviewEntryJsonFile.Add("\"Filename\": \"" + selectedFiles[i].FileName + "\",");
                                reviewEntryJsonFile.Add("\"Comments\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"DesignerSignoff\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"DesignerComments\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"CelaSignoff\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"CelaComments\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"GeopolFullMarketSignoff\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"GeopolFullMarketComments\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"LSPSignoff\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"LSPComments\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"ThemeCategories\": " + "\"\",");
                                reviewEntryJsonFile.Add("\"TargetMarkets\": " + "\"\",");

                                // Get a reference to the file we created previously.
                                CloudBlockBlob blob = blobContainer.GetBlockBlobReference(targetFolderDir + "/" + selectedFiles[i].FileName);

                                //Upload the file to Azure.
                                blob.UploadFromFile(rootPathFilename, FileMode.OpenOrCreate);

                                //Generate a shared access signature URI for a blob
                                //Set the expiry time and permissions for the blob.
                                //In this case, the start time is specified as a few minutes in the past, to mitigate clock skew.
                                //The shared access signature will be valid immediately.
                                SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
                                sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
                                sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddYears(5);
                                sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

                                //Generate the shared access signature on the blob, setting the constraints directly on the signature.
                                string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

                                //Return the URI string for the container, including the SAS token.
                                reviewEntryJsonFile.Add("\"Images\": \"" + blob.Uri + sasBlobToken + "\"");

                                //Comma not needed for the last item
                                if (i == selectedFiles.Count - 1)
                                {
                                    reviewEntryJsonFile.Add("}");
                                }
                                else
                                {
                                    reviewEntryJsonFile.Add("},");
                                }

                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                        reviewEntryJsonFile.Add("]");
                        reviewEntryJsonFile.Add("}");

                        //Create and Upload ReviewEntry.json file to Azure File Storage
                        CreateAndUploadReviewJSONDocument(localFolderDir, reviewEntryJsonFile, targetFolderDir, blobContainer);

                        //Clean up : delete temporary directory
                        Directory.Delete(localFolderDir, true);
                    }
                }
                return Json(selectedFiles.Count + " Files Uploaded!");
            }
            catch (Exception)
            {
                throw;
            }
        }
        [HttpPost]//TargetMarkets Checkbox (not being used)
        public JsonResult SaveTargetMarketList(string targetMarketList)
        {
            string[] arr = targetMarketList.Split(',');
            foreach (var id in arr)
            {
                var currentID = id;
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }
        [HttpPost]//Review Sub Categories or Themes Checkbox (not being used)
        public JsonResult ReviewSubCategoriesOrThemesList(string subCategoryOrThemesList)
        {
            string[] arr = subCategoryOrThemesList.Split(',');
            foreach (var id in arr)
            {
                var currentID = id;
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }
        //Create and upload Revie.xml document
        public void CreateAndUploadReviewXMLDocument(string rootPathDir, List<string> reviewXML, CloudFileDirectory newDir)
        {
            //Create Review.xml file
            string reviewXMLPath = rootPathDir + "/Review.xml";
            if (!System.IO.File.Exists(reviewXMLPath))
            {
                // Create a file to write to.
                using (StreamWriter sw = System.IO.File.CreateText(reviewXMLPath))
                {
                    sw.WriteLine(" <?xml version=\"1.0\"?>");
                    sw.WriteLine("<ArrayOfReviewEntry xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");

                    foreach (var item in reviewXML)
                    {
                        sw.WriteLine(item);
                    }
                    sw.WriteLine(" </ArrayOfReviewEntry>");
                }
            }
            // Get a reference to the XML file
            CloudFile cloudfileXML = newDir.GetFileReference("Review.xml");

            //Upload the Review.xml file to Azure.
            cloudfileXML.UploadFromFile(reviewXMLPath, FileMode.OpenOrCreate);
        }
        //Create and upload Revie.xml document
        public void CreateAndUploadReviewJSONDocument(string rootPathDir, List<string> reviewEntryJson, string newDir, CloudBlobContainer blobContainer)
        {
            //Json file name
            string JsonFileName = newDir + ".json";

            //Create json file
            string reviewEntryJsonPath = rootPathDir + "/" + JsonFileName;

            //Archive existing json file
            string archiveReviewEntryJsonPath = rootPathDir + "/" + newDir + "_old.json";

            if (!System.IO.File.Exists(reviewEntryJsonPath))
            {
                // Create a file to write to.
                using (StreamWriter sw = System.IO.File.CreateText(reviewEntryJsonPath))
                {
                    foreach (var item in reviewEntryJson)
                    {
                        sw.WriteLine(item);
                    }
                }
            }
            // Get a reference to the json file
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(newDir + "/" + newDir + ".json");

            if (blob.Exists())
            {
                //rename existing json file
                CloudBlockBlob newBlob = null;
                if (blob is CloudBlockBlob)
                {
                    newBlob = blobContainer.GetBlockBlobReference(newDir + "/" + newDir + "_old.json");
                }
                //Initiate blob copy
                newBlob.StartCopyFromBlob(blob.Uri);

                //Now wait in the loop for the copy operation to finish
                while (true)
                {
                    newBlob.FetchAttributes();
                    if (newBlob.CopyState.Status != CopyStatus.Pending)
                    {
                        break;
                    }
                    //Sleep for a second may be
                    System.Threading.Thread.Sleep(1000);
                }
                //Upload the new json file to Azure.
                blob.UploadFromFile(reviewEntryJsonPath, FileMode.OpenOrCreate);
            }
            else
            {
                //Upload the json file to Azure.
                blob.UploadFromFile(reviewEntryJsonPath, FileMode.OpenOrCreate);
            }

        }
        [HttpGet]
        public ActionResult Review(int id)
        {
            //Azure File Storage Account Name
            const string StorageAccountName = "globalreadinessfileshare";

            //Azure File Storage Account Key
            const string StorageAccountKey = "6jMlUXXIoM723dj1E7GbgM6InQBS29WXrTUC00nwMB4yWvqkjwaii/ao4SO3zq8IaT7aUvL/vVBhH8F80a6CIQ==";

            //Blob Container name
            const string BlobContainerName = "imagereview";

            //Retrieve storage account info by leveraging the Storage Account and Key Cendentials
            var storageAccount = new CloudStorageAccount(new StorageCredentials(StorageAccountName, StorageAccountKey), false);

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(BlobContainerName);

            //Generate a shared access signature URI for a blob
            //Set the expiry time and permissions for the blob.
            //In this case, the start time is specified as a few minutes in the past, to mitigate clock skew.
            //The shared access signature will be valid immediately.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddYears(5);
            //sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;


            //return View();
            using (DBModels2 db = new DBModels2())
            {
                ImageRFTable img = new ImageRFTable();
                img = db.ImageRFTables.Where(x => x.ImageRFId == id).FirstOrDefault<ImageRFTable>();
                var targetFolderDir = img.UploadedFolderPath;
                ViewBag.targetFolder = targetFolderDir;
                TempData["targetFolder"] = targetFolderDir;

                // Get a reference to the XML file
                CloudBlockBlob blob = blobContainer.GetBlockBlobReference(targetFolderDir + "/" + targetFolderDir + ".json");

                //Generate the shared access signature on the blob, setting the constraints directly on the signature.
                string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

                //Return the URI string for the container, including the SAS token.
                ViewBag.jsonFile = blob.Uri + sasBlobToken;

                return View(db.ImageRFTables.Where(x => x.ImageRFId == id).FirstOrDefault<ImageRFTable>());
            }
        }

        [HttpPost]//Save Reviewed form
        public JsonResult Review(Dictionary<string, string> formdata)
        {
            //Calculate number of images to be processes
            List<string> imageCounterListString = new List<string>();

            List<string> totalNumberOfImages = new List<string>();

            //List string to store the contents into the json file
            List<string> reviewEntryJsonFile = new List<string>();

            reviewEntryJsonFile.Add("{");

            //Parse the form data
            foreach (KeyValuePair<string, string> item in formdata)
            {
                if (item.Key == "modifiedBy")
                {
                    reviewEntryJsonFile.Add("\"ModifiedBy\": \"" + item.Value + "\",");
                }
                else if (item.Key == "modifiedDate")
                {
                    reviewEntryJsonFile.Add("\"ModifiedDate\": \"" + item.Value + "\",");
                    reviewEntryJsonFile.Add("\"Metadata\": [");
                }
                //Count the total number of images
                if (item.Key.Contains("_") == true)
                {
                    //for ex: DesignerSignoff_1, CelaSignoff_1, etc...
                    string[] itemKey = item.Key.Split('_');
                    int counter = 0;
                    foreach (string id in itemKey)
                    {
                        //Only grab the number
                        if (counter == 1)
                        {
                            bool alreadyExist = totalNumberOfImages.Contains(id);
                            if (!alreadyExist)
                            {
                                totalNumberOfImages.Add(id);
                            }
                        }
                        counter++;
                    }
                }
            }

            //image index id
            int imageID = 0;

            //Parse the form data
            foreach (KeyValuePair<string, string> item in formdata)
            {
                if (item.Key.Contains("_") == true)
                {
                    //for ex: DesignerSignoff_1, CelaSignoff_1, etc...
                    string[] itemKey = item.Key.Split('_');
                    int counter = 0;

                    foreach (string id in itemKey)
                    {
                        //Only grab the number
                        if (counter == 1)
                        {
                            bool alreadyExist = imageCounterListString.Contains(id);
                            if (!alreadyExist)
                            {
                                //image index id
                                imageID = Int32.Parse(id);

                                //Determine the form element
                                string caseSwitch = item.Key.Substring(0, item.Key.IndexOf("_"));

                                switch (caseSwitch)
                                {
                                    case "imageID":
                                        reviewEntryJsonFile.Add("{");
                                        reviewEntryJsonFile.Add("\"Id\": \"" + item.Value + "\",");
                                        break;
                                    case "imageFilename":
                                        reviewEntryJsonFile.Add("\"Filename\": \"" + item.Value + "\",");
                                        break;
                                    case "imageComments":
                                        reviewEntryJsonFile.Add("\"Comments\": \"" + item.Value + "\",");
                                        break;
                                    case "imageSrc":
                                        reviewEntryJsonFile.Add("\"Images\": \"" + item.Value + "\",");
                                        break;
                                    case "DesignerSignoff":
                                        reviewEntryJsonFile.Add("\"DesignerSignoff\": \"" + item.Value + "\",");
                                        break;
                                    case "DesignerComments":
                                        reviewEntryJsonFile.Add("\"DesignerComments\": \"" + item.Value + "\",");
                                        break;
                                    case "CelaSignoff":
                                        reviewEntryJsonFile.Add("\"CelaSignoff\": \"" + item.Value + "\",");
                                        break;
                                    case "CelaComments":
                                        reviewEntryJsonFile.Add("\"CelaComments\": \"" + item.Value + "\",");
                                        break;
                                    case "GeopolFullMarketSignoff":
                                        reviewEntryJsonFile.Add("\"GeopolFullMarketSignoff\": \"" + item.Value + "\",");
                                        break;
                                    case "GeopolFullMarketComments":
                                        reviewEntryJsonFile.Add("\"GeopolFullMarketComments\": \"" + item.Value + "\",");
                                        break;
                                    case "LSPSignoff":
                                        reviewEntryJsonFile.Add("\"LSPSignoff\": \"" + item.Value + "\",");
                                        break;
                                    case "LSPComments":
                                        reviewEntryJsonFile.Add("\"LSPComments\": \"" + item.Value + "\",");
                                        break;
                                    case "themeCategoriesID":
                                        reviewEntryJsonFile.Add("\"ThemeCategories\": \"" + item.Value + "\",");
                                        break;
                                    case "targetMarketsID":
                                        reviewEntryJsonFile.Add("\"TargetMarkets\": \"" + item.Value + "\"");
                                        //Comma not needed for the last item
                                        if (imageID == totalNumberOfImages.Count)
                                        {
                                            reviewEntryJsonFile.Add("}");
                                        }
                                        else
                                        {
                                            reviewEntryJsonFile.Add("},");
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        counter++;
                    }

                }

            }

            reviewEntryJsonFile.Add("]");
            reviewEntryJsonFile.Add("}");

            //Azure File Storage Account Name
            const string StorageAccountName = "globalreadinessfileshare";

            //Azure File Storage Account Key
            const string StorageAccountKey = "6jMlUXXIoM723dj1E7GbgM6InQBS29WXrTUC00nwMB4yWvqkjwaii/ao4SO3zq8IaT7aUvL/vVBhH8F80a6CIQ==";

            //Blob Container name
            const string BlobContainerName = "imagereview";

            //Retrieve storage account info by leveraging the Storage Account and Key Cendentials
            var storageAccount = new CloudStorageAccount(new StorageCredentials(StorageAccountName, StorageAccountKey), false);

            //Get a reference to the file share called "imagereview" we created
            var share = storageAccount.CreateCloudBlobClient().GetContainerReference(BlobContainerName);

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(BlobContainerName);

            string targetFolder = TempData["targetFolder"].ToString();

            //Create and Upload ReviewEntry.json file to Azure File Storage
            SaveReviewForm(reviewEntryJsonFile, targetFolder, blobContainer);


            return Json(new { success = true, message = "Saved Successfully!" }, JsonRequestBehavior.AllowGet);
        }

        public void SaveReviewForm(List<string> reviewEntryJson, string newDir, CloudBlobContainer blobContainer)
        {
            //Json file name
            string JsonFileName = newDir + ".json";

            string rootPathDir = Server.MapPath("~/Upload/" + newDir);

            //Create json file
            string reviewEntryJsonPath = rootPathDir + "/" + JsonFileName;

            //Archive existing json file
            string archiveReviewEntryJsonPath = rootPathDir + "/" + newDir + "_ori.json";

            //Delete directory
            DeleteDirectory(rootPathDir); 

            if (!System.IO.File.Exists(reviewEntryJsonPath))
            {
                try
                {
                    //Create folder
                    System.IO.Directory.CreateDirectory(rootPathDir);

                    // Create a file to write to.
                    using (StreamWriter sw = System.IO.File.CreateText(reviewEntryJsonPath))
                    {
                        foreach (var item in reviewEntryJson)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            // Get a reference to the json file
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(newDir + "/" + newDir + ".json");

            if (blob.Exists())
            {
                //rename existing json file
                CloudBlockBlob newBlob = null;
                if (blob is CloudBlockBlob)
                {
                    newBlob = blobContainer.GetBlockBlobReference(newDir + "/" + newDir + "_ori.json");
                }
                //Initiate blob copy
                newBlob.StartCopyFromBlob(blob.Uri);

                //Now wait in the loop for the copy operation to finish
                while (true)
                {
                    newBlob.FetchAttributes();
                    if (newBlob.CopyState.Status != CopyStatus.Pending)
                    {
                        break;
                    }
                    //Sleep for a second may be
                    System.Threading.Thread.Sleep(1000);
                }
                //Upload the new json file to Azure.
                blob.UploadFromFile(reviewEntryJsonPath, FileMode.OpenOrCreate);
            }
            else
            {
                //Upload the json file to Azure.
                blob.UploadFromFile(reviewEntryJsonPath, FileMode.OpenOrCreate);
            }

        }

        private void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                //Delete all files from the Directory
                foreach (string file in Directory.GetFiles(path))
                {
                    System.IO.File.Delete(file);
                }
                //Delete all child Directories
                foreach (string directory in Directory.GetDirectories(path))
                {
                    DeleteDirectory(directory);
                }
                //Delete a Directory
                Directory.Delete(path);
            }
        }
    }
}
