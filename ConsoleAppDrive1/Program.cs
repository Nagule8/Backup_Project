using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppDrive1
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "BackupApi";
        static string folderPath = @"C:\Users\NagulPranaw\Downloads\Text_files_upload";

        static async Task Main(string[] args)
        {
            UserCredential credential;

            credential = GetCredentials();

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            //call ListFiles
            //string pageToken = null;

            /*do
            {
                ListFiles(service,ref pageToken);

            } while (pageToken!=null);*/


            //Itreates all the text files in the Specified folder
            foreach (string file in Directory.EnumerateFiles(folderPath, "*.txt"))
            {
                //calls the CheckForDuplicate method to check whether the file is exist
                //and returns the existing file_Id
                string val =await CheckForDuplicate(file, service);
                
                //Console.WriteLine(val);
                
                //If no File_id is returned, then it uploads the file
                if ("abcd"==val.ToString())
                {
                    _ = UploadBasicAsync(file, service);
                }
                else
                {
                    //Calls the CheckModify to check whether the file is modified or not.
                    //returns bool.
                    if (CheckModify(file, val, service)){
                        _ = UpdateBasicAsync(file, service, val);
                        Console.WriteLine("File Updated Hurray!!!!!");
                    }
                    else
                    {
                        Console.WriteLine("-------------No need to update-------------");
                    }
                    
                }
                

            }


            Console.WriteLine("Done");
            Console.Read();
        }

        //List files in google grive
        private static void ListFiles(DriveService service, ref string pageToken)
        {
            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id,name)";
            listRequest.PageToken = pageToken;
            listRequest.Q = "mimeType='text/plain'";

            // List files.
            var request = listRequest.Execute();


            if (request.Files != null && request.Files.Count > 0)
            {


                foreach (var file in request.Files)
                {
                    Console.WriteLine("{0} {1}", file.Name,file.Id);
                }

                pageToken = request.NextPageToken;

                if (request.NextPageToken != null)
                {
                    Console.WriteLine("Press any key to conti...");
                    Console.ReadLine();



                }


            }
            else
            {
                Console.WriteLine("No files found.");

            }


        }

        //returns the Credential.json file for authentication
        private static UserCredential GetCredentials()
        {
            UserCredential credential;

            using (var stream = new FileStream(@"C:\Nagul\OneDrive - CES Limited\Documents\Training_Projects_Files\Google_Api_credential\Credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            return credential;
        }

        //uploads a file in the specified parent folder
        private static async Task UploadBasicAsync(string fi, DriveService service)
        {
            //Specificing metadata for uploading a file
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(fi),
                MimeType = "text/plain",
                Parents = new List<string>() { "1c2BqHHysb3C2jsF-FGm9h48GvTX7IiZo" },
                ModifiedTime=DateTime.Now
                
            };

            string uploadedFileId;

            // Create a new file on Google Drive
            using (var fs = new FileStream(fi, FileMode.Open, FileAccess.Read))
            {
                // Create a new file, with metadata and stream.
                var request = service.Files.Create(fileMetadata, fs, "text/plain");
                request.Fields = "*";
                var results = await request.UploadAsync(CancellationToken.None);

                if (results.Status == UploadStatus.Failed)
                {
                    Console.WriteLine($"Error uploading file: {results.Exception.Message}");
                }

                // the file id of the new file we created
                uploadedFileId = request.ResponseBody?.Id;
                Console.WriteLine($"-------------------------------\n {uploadedFileId} \n------------------------- ");
            }
        }

        //check whether a file exist or not
        private static async Task<string> CheckForDuplicate(string Cfile, DriveService Cservice)
        {
            var request = Cservice.Files.List();
            request.PageSize = 1000;
            //specifying folder path
            request.Q = "parents in '1c2BqHHysb3C2jsF-FGm9h48GvTX7IiZo'";
            request.Fields = "nextPageToken, files(id,name,modifiedTime)";

            var results = await request.ExecuteAsync();
            int flag = 0;
            string f = "abcd";

            //Iterate the files in specified folder
            foreach (var driveFile in results.Files)
            {
                if (Path.GetFileName(Cfile) == driveFile.Name) {
                    Console.WriteLine(driveFile.Name);
                    flag = 1;
                    f = driveFile.Id;
                    break;
                }
            }

            if (flag==1)
            {
                Console.WriteLine("File Exist!!!!!!!");
                return (f);
            }
            return f;
        }

        //Updating a file
        private static async Task UpdateBasicAsync(string Ufile, DriveService service, string id)
        {

            var file = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(Ufile)
            };
            using (var fs = new FileStream(Ufile, FileMode.Open, FileAccess.Read))
            {
                //Updates the Drive file with local file
                var Updaterequest = service.Files.Update(file, id, fs, "text/plain");

                var results = await Updaterequest.UploadAsync(CancellationToken.None);

                if (results.Status == UploadStatus.Failed)
                {
                    Console.WriteLine($"Error updating file: {results.Exception.Message}");
                }
                string updatedFile = Updaterequest.ResponseBody?.Id;

                Console.WriteLine($"-------------------------------\n {updatedFile} \n FILE UPDATED! \n------------------------- ");
            }
        }        

        //Checks File modification
        private static bool CheckModify(string fileName,string id,DriveService service)
        {
            //Info about local file
            FileInfo fileInfo = new FileInfo(fileName);
            
            //Console.WriteLine(fileInfo.LastWriteTime);

            //Retrieving list of files in  specific folder
            var request = service.Files.List();
            request.PageSize = 1000;
            request.Q = "parents in '1c2BqHHysb3C2jsF-FGm9h48GvTX7IiZo'";
            request.Fields = "nextPageToken, files(id,name,modifiedTime)";

            var result = request.Execute();

            //Finding out the required file to modify the contents
            foreach (var driveFile in result.Files)
            {
                if (driveFile.Id == id && (driveFile.ModifiedTime < fileInfo.LastWriteTime) )
                {
                    Console.WriteLine("Need To update!!!");
                    return true;
                }

            }
            Console.WriteLine("No need to update");

            return false;
        }

    }
}