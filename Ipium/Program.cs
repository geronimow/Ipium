using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Ipium
{
    internal class Program
    {
        public static HttpListener listener;
        public static string url = "http://25.28.20.82:8000/";
        //public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "    <form method=\"get\" action=\"storage\">" +
            "      <input type=\"submit\" value=\"Storage\" {0}>" +
            "    </form>" +
            "  </body>" +
            "</html>";

        public class Block
        {
            public string BlockId { get; set; }
            public string BlockNb { get; set; }
            public string BlockInfo { get; set; }
        }

        public static List<Block> blocks = new List<Block>();
        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/storage"))
                {
                    // Write the response info
                    byte[] data = Encoding.UTF8.GetBytes("Storage good");
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    String blockId = req.QueryString["blockId"];
                    String blockNb = req.QueryString["blockNb"];
                    String blockInfo = req.QueryString["blockInfo"];
                    int blockNbParse;

                    Console.WriteLine("Storage requested");

                    if (string.IsNullOrEmpty(blockId) || blockId.Length < 64) 
                    {
                        Console.WriteLine("Erreur : L'ID de bloc est manquant dans la requête.");
                        return;
                    }

                    if (!Int32.TryParse(blockNb, out blockNbParse))
                    {
                        Console.WriteLine("Erreur : Le NB de bloc est manquant dans la requête.");
                        return;
                    }

                    if (string.IsNullOrEmpty(blockInfo))
                    {
                        Console.WriteLine("Erreur : L'info de bloc est manquant dans la requête.");
                        return;
                    }

                    if (blockNb != null && blocks.Exists(b => b.BlockNb == blockNb))
                    {
                        Console.WriteLine("Erreur : Un bloc avec le numéro " + blockNb + " existe déjà.");
                    }
                    else
                    {
                        Block newBlock = new Block
                        {
                            BlockId = blockId,
                            BlockNb = blockNb,
                            BlockInfo = blockInfo
                        };
                        //test
                        blocks.Add(newBlock);
                        Console.WriteLine("Bloc ajouté avec succès !");
                    }
                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }             
            }
        }
        static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }


}
