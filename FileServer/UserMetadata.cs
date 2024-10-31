namespace AzureFileServer.FileServer;

// This class is used to store metadata about a file and can be
// used for serializing and deserialzing the JSON data in CosmosDb
public class UserMetadata
{
    private string GenerateId()
    {
        return $"{this.userid}";
    }

    // Note that "id" must be lower case for the Cosmos APIs to work
    // and for consistency, all keys are lower case
    public string id { get { return GenerateId(); } }

    public string email { get; set; } = string.Empty;

    public string userid { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"id: {id}, userid: {userid}, email: {email}";
    }
}
