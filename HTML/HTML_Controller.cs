
using System;
using System.Text;

// metadata
using AzureFileServer.FileServer;

namespace AzureFileServer.HTML;
public class HTMLPageController
{
    public void AddCSS(StringBuilder html)
    {
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
    }

    public void AddBody(StringBuilder html, IEnumerable<FileMetadata> metadata, string user)
    {
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

                html.Append("</body>");
    }

    public void BuildHomePage(StringBuilder html, IEnumerable<FileMetadata> metadata, string user)
    {
                html.Append("<html>");
                html.Append("<head>");
                AddCSS(html);
                html.Append("</head>");
                AddBody(html, metadata, user);
                html.Append("</html>");
    }


}