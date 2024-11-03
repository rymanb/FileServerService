
using AzureFileServer.Azure;
using AzureFileServer.Utils;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.RegularExpressions;

using AzureFileServer.HTML;

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
                m.userid = GetParameterFromList("userid", request, log);
                m.filename = fileContent.FileName;
                // replace any non a-z, A-Z, 0-9 or _ or . with nothing
                m.filename = Regex.Replace(m.filename, "[^a-zA-Z0-9_.]", "");

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
                m.userid = GetParameterFromList("userid", request, log);
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

                // get userid parameter from request
                string userid = GetParameterFromList("userid", request, log);

                // Implement the list files delegate to return a list of files
                // that are associated with the userId provided in the HTTP request.

                // get metadata from CosmosDB
                IEnumerable<FileMetadata> metadata = await GetMetadataFromCosmosDb(userid);

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
                m.userid = GetParameterFromList("userid", request, log);
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

            }
            catch(Exception e)
            {
                log.HandleException(e);
            }
        }
    }

    private async Task<IEnumerable<FileMetadata>> GetMetadataFromCosmosDb(string userId)
    {
        string queryString = $"SELECT * FROM c WHERE c.userid = '{userId}'";
        return await _cosmosDbWrapper.GetItemsAsync<FileMetadata>(queryString);
    }
}