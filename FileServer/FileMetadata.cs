namespace AzureFileServer.FileServer;

// This class is used to store metadata about a file and can be
// used for serializing and deserialzing the JSON data in CosmosDb
public class FileMetadata
{
    private string GenerateId()
    {
        return $"{this.userid}-{this.filename}";
    }

    // Note that "id" must be lower case for the Cosmos APIs to work
    // and for consistency, all keys are lower case
    public string id { get { return GenerateId(); } }

    public string userid { get; set; } = string.Empty;
    public string filename { get; set; } = string.Empty;
    public string contenttype { get; set; } = string.Empty;
    public long contentlength { get; set; } = 0;

    public override string ToString()
    {
        return $"id: {id}, userid: {userid}, filename: {filename}, contenttype: {contenttype}, contentlength: {contentlength}";
    }
}
