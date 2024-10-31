
using AzureFileServer.Azure;
using AzureFileServer.Utils;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace AzureFileServer.FileServer;


// This is the core logic of the web server and hosts all of the HTTP
// handlers used by the web server regarding File Server functionality.
public class FileServerHandlers
{
    private readonly IConfiguration _configuration;
    private readonly Logger _logger;
    private readonly CosmosDbWrapper _cosmosDbWrapper;

    public FileServerHandlers(IConfiguration configuration)
    {
        _configuration = configuration;
        if (null == _configuration)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        string serviceName = configuration["Logging:ServiceName"];
        _logger = new Logger(serviceName);

        _cosmosDbWrapper = new CosmosDbWrapper(configuration);
    }

    private static string GetParameterFromList(string parameterName, HttpRequest request, MethodLogger log)
    {
        // Obtain the parameter from the caller
        if (request.Query.TryGetValue(parameterName, out StringValues items))
        {
            if (items.Count > 1)
            {
                throw new UserErrorException($"Multiple {parameterName} found");
            }

            log.SetAttribute($"request.{parameterName}", items[0]);
        }
        else
        {
            throw new UserErrorException($"No {parameterName} found");
        }

        return items[0];
    }

    // Health Checks (aka ping) methods are handy to have on your service
    // They allow you to report that your are alive and return any other
    // information that is useful. These are often used by load balancers
    // to decide whether to send you traffic. For example, if you need a long
    // time to initialize, you can report that you are not ready yet.
    public async Task HealthCheckDelegate(HttpContext context)
    {
        // "using" is a C# system to ensure that the object is disposed of properly
        // when the block is exited. In this case, it will call the Dispose method
        using(var log = _logger.StartMethod(nameof(HealthCheckDelegate), context))
        {
            try
            {
                // Generally, a 200 OK is returned if the service is alive
                // and that is all that the load balancer needs, but a
                // text message can be useful for humans.
                // However, in some cases, the LB will be able to process more
                // health information to know how to react to your service, so
                // don't be surprised if you see code with more involved health 
                // checks.
                await context.Response.WriteAsync("Alive");
            }
            catch(Exception e)
            {
                // While you can just throw the exception back to the web server,
                // it is not recommended. It is better to catch the exception and
                // log it, then return a 500 Internal Server Error to the caller yourself.
                log.HandleException(e);
            }
        }
    }

    public async Task HomeDelegate(HttpContext context)
    {
        using(var log = _logger.StartMethod(nameof(UploadFileDelegate), context))
        {
            try
            {
                string user_email = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
                if (string.IsNullOrEmpty(user_email))
                {
                    user_email = "test@gmail.com";
                }

                // get user name from email
                string user = user_email.Split('@')[0];

                // post to https://userDatabase.com/AddIfNotExists?userid={user}&email={user_email}
                // to add user to database if not already there

                // create request to add user to database
                // var request = new HttpRequestMessage(HttpMethod.Post, $"https://userDatabase.com/AddIfNotExists?userid={user}&email={user_email}");

                // // send request to add user to database
                // var client = new HttpClient();
                // var response = await client.SendAsync(request);


                // create web page with user name at top, a logout button, and a list of files
                StringBuilder html = new StringBuilder();
                html.Append("<html>");
                html.Append("<head>");
                // css
                html.Append("<style>");
                html.Append("body { font-family: Arial, sans-serif; }");
                html.Append("ul { list-style-type: none; padding: 0; }");   

                // css for list items
                html.Append("li { padding: 10px; border: 1px solid #ccc; margin: 5px; }");

                // css for links
                html.Append("a { text-decoration: none; color: blue; margin-left: 10px; }");

                // css for links on hover
                html.Append("a:hover { text-decoration: underline; }");

                // css for nav bar
                html.Append(".navbar { background-color: #333; overflow: hidden; }");

                // css for nav bar links
                html.Append(".navbar a { float: left; display: block; color: #f2f2f2; text-align: center; padding: 14px 16px; text-decoration: none; }");

                // css for nav bar links on hover
                html.Append(".navbar a:hover { background-color: #ddd; color: black; }");

                
                // css for upload form overlay
                html.Append(".overlay { display: none; position: fixed; width: 100%; height: 100%; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.5); z-index: 2; cursor: pointer; }");

                // css for upload form overlay content
                html.Append(".overlay-content { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); -ms-transform: translate(-50%, -50%); width: 80%; height: 80%; padding: 20px; background-color: white; z-index: 3; }");


                // css for upload button
                html.Append("button { background-color: #4CAF50; color: white; padding: 14px 20px; margin: 8px 0; border: none; border-radius: 4px; cursor: pointer; }");

                // css for close button, X should be
                html.Append(".close { color: #000; float: right; font-size: 28px; font-weight: bold; }");

                // css for close button on hover should get slightly larger and change color
                html.Append(".close:hover, .close:focus { color: #3b3b3b; font-size: 30px; text-decoration: none; cursor: pointer; }");

                // css for choose file input field, button should be large and centered
                html.Append(".choosefile { background-color: #4CAF50; color: white; padding: 14px 20px; margin: 8px 0; border: none; border-radius: 4px; cursor: pointer; }");

                // css for choose file hover
                html.Append(".choosefile:hover { background-color: #45a049; }");

                // css to make input type file invisible
                html.Append("input[type=file] { display: none; }");

                // css for input type file label, should be centered
                html.Append("label { display: block; text-align: center; }");

                // css for input type file label on hover should change color
                html.Append("label:hover { color: #3b3b3b; }");

                // css for centering file name
                html.Append("p { text-align: center; }");

                // css for upload button, should be centered and blue
                html.Append(".upload { background-color: #24a0ed; color: white; padding: 14px 20px; margin: 8px 0; border: none; border-radius: 4px; cursor: pointer; }");

                // css for centering upload button
                html.Append(".upload { display: block; margin-left: auto; margin-right: auto; }");

                // css for upload button on hover should change color
                html.Append(".upload:hover { background-color: #1d8dbf; }");


        







                html.Append("</style>");
                html.Append("</head>");

                html.Append("<body>");

                // add nav bar
                html.Append("<div class=\"navbar\">");
                html.Append("<a href=\"/\">Home</a>");
                html.Append("<a href=\"/healthcheck\">Health Check</a>");


                // add change accounts link to far right
                html.Append("<a style=\"float: right;\" href=\"/.auth/login/google?prompt=login&post_login_redirect_uri=/\">Change Accounts</a>");
                // add user name to nav bar on the right 
                html.Append($"<a style=\"float: right;\" href=\"/\">Welcome {user}</a>");
                html.Append("</div>");
                
                // add user name to web page
                html.Append("<h1>File Server</h1>");
                // add separator
                html.Append("<hr>");

                // add files header centered and bold
                html.Append("<h2 style=\"text-align: center; font-weight: bold;\">Your Files</h2>");

                // add Upload button which will bring bring up a overlay with a form to upload a file
                html.Append("<button onclick=\"document.getElementById('uploadForm').style.display='block'\">Upload</button>");

                // add upload form to web page set userid to user
                // should submit files like curl does with -F
                // the curl command would be: curl -X POST -F file=@{YourFile} https://filesystemapp.wonderfulsky-750ba161.westus2.azurecontainerapps.io/uploadfile

                // style should be similar to google drive upload button
                // large button with a cloud icon and text "Upload" below it
                // form should take up about 50% of the screen and be centered
                html.Append("<div id=\"uploadForm\" style=\"display: none; position: fixed; z-index: 1; padding-top: 100px; left: 0; top: 0; width: 100%; height: 100%; overflow: auto; background-color: rgb(0,0,0); background-color: rgba(0,0,0,0.4);\">");
                html.Append("<div style=\"background-color: #fefefe; margin: 5% auto; padding: 20px; border: 1px solid #888; width: 50%;\">");
                // close button, should be an X in the top right corner on hover should turn a different color
                html.Append("<span class=\"close\" onclick=\"document.getElementById('uploadForm').style.display='none'\">&times;</span>");
                
                // header for upload form, should be centered and bold
                html.Append("<h2 style=\"text-align: center; font-weight: bold;\">Upload File</h2>");
                // divider
                html.Append("<hr>");

                // div for form to prevent button from overlapping hr
                html.Append("<div style=\"padding: 10px;\">");

                html.Append("<form action=\"/uploadfile\" method=\"post\" enctype=\"multipart/form-data\">");

                // button to choose file and upload
                html.Append("<label for=\"file_upload\" class=\"choosefile\">Choose File</label>");
                // input type file
                html.Append("<input type=\"file\" id=\"file_upload\" name=\"file\" style=\"display: none;\" onchange=\"document.getElementById('file_name').innerText = this.files[0].name\" />");
                // display file name
                html.Append("<p id=\"file_name\"></p>");


        
                // submit button
                html.Append("<button type=\"submit\" class=\"upload\">Upload</button>");
                
    

                


                html.Append("</form>");
                html.Append("</div>");
                html.Append("</div>");
                html.Append("</div>");





                // get metadata from CosmosDB
                FileMetadata m = new FileMetadata();
                m.userid = user;

                // Implement the list files delegate to return a list of files
                // that are associated with the userId provided in the HTTP request.

                // get metadata from CosmosDB
                string queryString = $"SELECT * FROM c WHERE c.userid = '{m.userid}'";  
                IEnumerable<FileMetadata> metadata = await _cosmosDbWrapper.GetItemsAsync<FileMetadata>(queryString);

                // add list of files to web page
                html.Append("<ul>");
                foreach (FileMetadata file in metadata)
                {
                    html.Append($"<li>{file.filename} <a href=\"/download?filename={file.filename}\">Download</a> <a href=\"/delete?filename={file.filename}\">Delete</a></li>");
                }

                if (!metadata.Any())
                {
                    html.Append("<li>No files found</li>");
                }

                html.Append("</ul>");

                // add upload form to web page set userid to user
                // should submit files like curl does with -F
                // the curl command would be: curl -X POST -F file=@{YourFile} https://filesystemapp.wonderfulsky-750ba161.westus2.azurecontainerapps.io/uploadfile
                // html.Append("<form action=\"/uploadfile\" method=\"post\" enctype=\"multipart/form-data\">");
                // html.Append("<input type=\"file\" name=\"file\" />");
                // html.Append("<input type=\"submit\" value=\"Upload\" />");
                // html.Append("</form>");

                html.Append("</body></html>");

                // return web page to caller
                await context.Response.WriteAsync(html.ToString());

            }
            catch (UserErrorException e)
            {
                log.LogUserError(e.Message);
            }
            catch(Exception e)
            {
                log.HandleException(e);
            }
        }
    }



    public async Task UploadFileDelegate(HttpContext context)
    {
        using(var log = _logger.StartMethod(nameof(UploadFileDelegate), context))
        {
            try
            {
                HttpRequest request = context.Request;

                IFormFile fileContent = context.Request.Form.Files.FirstOrDefault();
                if (fileContent == null)
                {
                    throw new UserErrorException("No file content found");
                }

                FileMetadata m = new FileMetadata();
                string user_email = request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
                m.userid = user_email.Split('@')[0];
                m.filename = fileContent.FileName;
                m.contenttype = fileContent.ContentType;
                m.contentlength = fileContent.Length;                

                log.SetAttribute("request.filename", fileContent.FileName);
                log.SetAttribute("request.contenttype", fileContent.ContentType);
                log.SetAttribute("request.contentlength", fileContent.Length);

                // First step is we will write the metadata to CosmosDB
                // Here we are using Type mapping to convert our data structure
                // to a JSON document that can be stored in CosmosDB.
                if (await _cosmosDbWrapper.GetItemAsync<FileMetadata>(m.id, m.userid) != null)
                {
                    await _cosmosDbWrapper.UpdateItemAsync(m.id, m.userid, m);
                }
                else
                {
                    await _cosmosDbWrapper.AddItemAsync(m, m.userid);
                }

                // Now we write the file into a blob storage element within the container.
                // We will use one container per user to keep things organized.
                var blobStorage = new BlobStorageWrapper(_configuration);
                using (var streamReader = new StreamReader(fileContent.OpenReadStream()))
                {
                    await blobStorage.WriteBlob(m.userid, m.filename, streamReader.BaseStream);
                }

                // The POST has no response body, so we just return and the system
                // will return a 200 OK to the caller.

                // redirect to home page
                context.Response.Redirect("/");
            }
            catch (UserErrorException e)
            {
                log.LogUserError(e.Message);
            }
            catch(Exception e)
            {
                log.HandleException(e);
            }
        }
    }

    public async Task DownloadFileDelegate(HttpContext context)
    {
        using(var log = _logger.StartMethod(nameof(DownloadFileDelegate), context))
        {
            try
            {
                HttpRequest request = context.Request;

                FileMetadata m = new FileMetadata();
                string user_email = request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
                m.userid = user_email.Split('@')[0];
                m.filename = GetParameterFromList("filename", request, log);

                // Implement the download file delegate to return the file
                // contents to the caller via the HTTP response after receiving both
                // the userId and the filename to find.

                // get metadata from CosmosDB
                FileMetadata metadata = await _cosmosDbWrapper.GetItemAsync<FileMetadata>(m.id, m.userid);
                if (metadata == null)
                {
                    throw new UserErrorException("File not found");
                }

                // get file from blob storage
                var blobStorage = new BlobStorageWrapper(_configuration);
                Stream fileStream = new MemoryStream();

                // download file from blob storage to memory stream
                blobStorage.DownloadBlob(m.userid, m.filename, fileStream);

                // set response headers
                context.Response.Headers.Append("Content-Disposition", $"attachment; filename={metadata.filename}");
                context.Response.Headers.Append("Content-Type", metadata.contenttype);
                context.Response.Headers.Append("Content-Length", metadata.contentlength.ToString());

                // wait for file to be downloaded
                while (fileStream.Length != metadata.contentlength)
                {
                    await Task.Delay(100);
                }

                // return file to caller
                fileStream.Position = 0;
                await fileStream.CopyToAsync(context.Response.Body);

                // redirect to home page
                context.Response.Redirect("/");


            }
            catch(Exception e)
            {
                log.HandleException(e);
            }
        }
    }

    public async Task ListFilesDelegate(HttpContext context)
    {
        using(var log = _logger.StartMethod(nameof(ListFilesDelegate), context))
        {
            try
            {
                HttpRequest request = context.Request;

                FileMetadata m = new FileMetadata();
                m.userid = GetParameterFromList("userid", request, log);

                // Implement the list files delegate to return a list of files
                // that are associated with the userId provided in the HTTP request.

                // get metadata from CosmosDB
                string queryString = $"SELECT * FROM c WHERE c.userid = '{m.userid}'";  
                IEnumerable<FileMetadata> metadata = await _cosmosDbWrapper.GetItemsAsync<FileMetadata>(queryString);

                // return list of files to caller
                await context.Response.WriteAsync(JsonSerializer.Serialize(metadata));



            }
            catch(Exception e)
            {
                log.HandleException(e);
            }
        }
    }

    public async Task DeleteFileDelegate(HttpContext context)
    {
        using(var log = _logger.StartMethod(nameof(DeleteFileDelegate), context))
        {
            try
            {
                HttpRequest request = context.Request;

                FileMetadata m = new FileMetadata();
                string user_email = request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
                m.userid = user_email.Split('@')[0];
                m.filename = GetParameterFromList("filename", request, log);

                // Implement the delete file delegate to remove the file
                // from the storage system and the metadata from the CosmosDB database.

                // get metadata from CosmosDB
                FileMetadata metadata = await _cosmosDbWrapper.GetItemAsync<FileMetadata>(m.id, m.userid);
                if (metadata == null)
                {
                    throw new UserErrorException("File not found");
                }

                // delete file from blob storage
                var blobStorage = new BlobStorageWrapper(_configuration);
                await blobStorage.DeleteBlob(m.userid, m.filename);

                // delete metadata from CosmosDB
                await _cosmosDbWrapper.DeleteItemAsync(m.id, m.userid);

                // redirect to home page
                context.Response.Redirect("/");
            }
            catch(Exception e)
            {
                log.HandleException(e);
            }
        }
    }
}