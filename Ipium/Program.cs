using System;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
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

        public class Transaction
        {
          
        }
        public class Block
        {
            public String Index { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string PreviousHash { get; set; }
            public string Hash { get; set; }
            public int Nonce { get; set; }
            public List<Transaction> Transactions { get; set; } = new List<Transaction>();

            public void CalculateHash()
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    string data = Index.ToString() + Timestamp.ToString() + PreviousHash + Nonce.ToString();
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                    Hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
                }
            }
        }

        public static List<Block> GetAllBlocks()
        {
            List<Block> sortedBlocks = blocks.OrderByDescending(b => b.Index).ToList();
            return sortedBlocks;
        }

        public static Block GetLastBlock()
        {
            if (blocks.Count == 0)
            {
                return null;
            }

            List<Block> sortedBlocks = blocks.OrderByDescending(b => b.Index).ToList();

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
                        Console.WriteLine("Numéro : " + block.Index);
                        Console.WriteLine("Hash : " + block.Hash);
                        Console.WriteLine("Transactions : " + block.Transactions);
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
                        Console.WriteLine("Dernier bloc - Numéro : " + lastBlock.Index);
                        Console.WriteLine("Dernier bloc - Hash : " + lastBlock.Hash);  
                        Console.WriteLine("Dernier bloc - Transactions : " + lastBlock.Transactions);
                    }
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/storage"))
                {
                    byte[] data = Encoding.UTF8.GetBytes("Storage good");
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;

                    String blockIndex = req.QueryString["blockIndex"];
                    String blockHash = req.QueryString["blockHash"];
                    String blockTransactions = req.QueryString["blockTransactions"];
                    int blockNbParse;

                    if (string.IsNullOrEmpty(blockIndex) || blockIndex.Length < 64) 
                    {
                        data = Encoding.UTF8.GetBytes("\"Erreur : L'indexe de bloc est manquant dans la requête.\"");
                    }

                    if (!Int32.TryParse(blockHash, out blockNbParse))
                    {
                        data = Encoding.UTF8.GetBytes("Erreur : Le Hash de bloc est manquant dans la requête.\"");
                    }

                    if (string.IsNullOrEmpty(blockTransactions))
                    {
                        data = Encoding.UTF8.GetBytes("Erreur : La transaction de bloc est manquant dans la requête.");
                    }

                    if (blockHash != null && blocks.Exists(b => b.Hash == blockHash))
                    {
                        data = Encoding.UTF8.GetBytes("Erreur : Le bloc numero \"" + blockHash + "\' existe déjà.");
                    }
                    else
                    {
                        Block newBlock = new Block
                        {
                            Index = blockIndex,
                            Hash = blockHash,
                            Transactions = blockTransactions
                        };
                       
                        blocks.Add(newBlock);
                        Console.WriteLine("Bloc ajouté avec succès !");
                        data = Encoding.UTF8.GetBytes("Bloc ajouté avec succès !");
                        Console.WriteLine("ID : " + blockIndex);
                        Console.WriteLine("Numéro : " + blockHash);
                        Console.WriteLine("Informations : " + blockTransactions);

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
            // Create a Http server and start listening for incoming connection

            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            //Generatin genesis block
            var blockGenesis = new Block();
            blockGenesis.Index = "0";
            blockGenesis.Timestamp = DateTime.Now;
            blockGenesis.PreviousHash = "0000000000000000000000000000000000000000000000000000000000000000";
            blockGenesis.Nonce = 0;
            blockGenesis.CalculateHash();

            blocks.Add(blockGenesis);


            // Close the listener
            listener.Close();
        }
    }


}
