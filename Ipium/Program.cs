using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Ipium.Program;

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

        public static List<Block> GetAllBlocks()
        {
            List<Block> sortedBlocks = blocks.OrderByDescending(b => b.BlockId).ToList();
            return sortedBlocks;
        }

        public static Block GetLastBlock()
        {
            if (blocks.Count == 0)
            {
                return null;
            }

            List<Block> sortedBlocks = blocks.OrderByDescending(b => b.BlockId).ToList();

            Block lastBlock = sortedBlocks[0];

            return lastBlock;
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

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/allblocks"))
                {
                    List<Block> allBlocks = GetAllBlocks();
                    foreach (Block block in allBlocks)
                    {
                        Console.WriteLine("ID : " + block.BlockId);
                        Console.WriteLine("Numéro : " + block.BlockNb);
                        Console.WriteLine("Informations : " + block.BlockInfo);
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/lastblock"))
                {
                    Block lastBlock = GetLastBlock();

                    if (lastBlock == null)
                    {
                        Console.WriteLine("Aucun bloc dans la liste.");
                    }
                    else
                    {
                        Console.WriteLine("Dernier bloc - ID : " + lastBlock.BlockId);
                        Console.WriteLine("Dernier bloc - Numéro : " + lastBlock.BlockNb);  
                        Console.WriteLine("Dernier bloc - Informations : " + lastBlock.BlockInfo);
                    }
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/storage"))
                {
                    byte[] data = Encoding.UTF8.GetBytes("Storage good");
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;

                    String blockId = req.QueryString["blockId"];
                    String blockNb = req.QueryString["blockNb"];
                    String blockInfo = req.QueryString["blockInfo"];
                    int blockNbParse;

                    Console.WriteLine("Storage requested");

                    if (string.IsNullOrEmpty(blockId) || blockId.Length < 64) 
                    {
                        data = Encoding.UTF8.GetBytes("\"Erreur : L'ID de bloc est manquant dans la requête.\"");
                    }

                    if (!Int32.TryParse(blockNb, out blockNbParse))
                    {
                        data = Encoding.UTF8.GetBytes("Erreur : Le NB de bloc est manquant dans la requête.\"");
                    }

                    if (string.IsNullOrEmpty(blockInfo))
                    {
                        data = Encoding.UTF8.GetBytes("Erreur : L'info de bloc est manquant dans la requête.");
                    }

                    if (blockNb != null && blocks.Exists(b => b.BlockNb == blockNb))
                    {
                        data = Encoding.UTF8.GetBytes("Erreur : Le bloc numero \"" + blockNb + "\' existe déjà.");
                    }
                    else
                    {
                        Block newBlock = new Block
                        {
                            BlockId = blockId,
                            BlockNb = blockNb,
                            BlockInfo = blockInfo
                        };
                       
                        blocks.Add(newBlock);
                        Console.WriteLine("Bloc ajouté avec succès !");
                        data = Encoding.UTF8.GetBytes("Bloc ajouté avec succès !");
                        Console.WriteLine("ID : " + blockId);
                        Console.WriteLine("Numéro : " + blockNb);
                        Console.WriteLine("Informations : " + blockInfo);

                    }
                    // Write out to the response stream (asynchronously), then close it
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }     
                else
                {
                    byte[] data = Encoding.UTF8.GetBytes("Hello !");
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
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
