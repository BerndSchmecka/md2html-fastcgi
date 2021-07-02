using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace md2html
{
    class Program
    {

        static async Task Main(string[] args)
        {
            var client = new AsyncFastCGI.Client();

            client.SetPort(9123);
            client.SetBindAddress("0.0.0.0");
            client.SetMaxConcurrentRequests(256);
            client.SetConnectionTimeout(10000);
            client.SetMaxHeaderSize(16384);
            client.RequestHandler = RequestHandler;

            await client.StartAsync();
        }

        private static async Task RequestHandler(AsyncFastCGI.Input input, AsyncFastCGI.Output output){
            output.SetHttpStatus(200);
            output.SetHeader("Content-Type", "text/html");

            string requestURI = input.GetParameter("REQUEST_URI");
            string requestMethod = input.GetParameter("REQUEST_METHOD");
            string remoteAddress = input.GetParameter("REMOTE_ADDR");
            string markdownFilename = input.GetParameter("SCRIPT_FILENAME");
            string requestData = WebUtility.HtmlEncode(await input.GetContentAsync());

            FileStream fs = null; //Setting FileStream to null
            try{
                fs = File.OpenRead(markdownFilename); //Try opening Markdown file from path
            } catch (Exception e){
                Console.WriteLine($"Error reading file: {e.Message}");
                output.SetHttpStatus(404);
            }

            using(Stream s = (fs != null ? fs : new MemoryStream(Encoding.UTF8.GetBytes("File not found.")))){ //Use Markdown File (if not null), otherwise use error message.
                using(StreamReader sr = new StreamReader(s)){ //Create StreamReader to read file contents
                    MarkdownSharp.Markdown md = new MarkdownSharp.Markdown(); //Initialize Markdown to HTML Converter
                    string file_content = await sr.ReadToEndAsync(); //Read all file contents
                    await output.WriteAsync(md.Transform(file_content)); //Transform Markdown into HTML and push to the client
                }
            }

            await output.EndAsync(); //End request
        }
    }
}
