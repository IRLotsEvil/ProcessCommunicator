using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Drawing;

    /// <summary>
namespace communication
{
    /// A Class that creates and communicates to a simple http server
    /// </summary>
    public class ThinServerClient
    {
        private HttpListener Listener;
        public ThinServerClient() { }
        /// <summary>
        /// Construct a new instance of ThinServerClient and Starts the Server
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public ThinServerClient(string ip, int port) => Start();
        /// <summary>
        /// Starts the Server and listens for incoming requests
        /// </summary>
        /// <param name="ip">IP Address to listen to</param>
        /// <param name="port">Port Number</param>
        public void Start(string ip = "127.0.0.1", int port = 8383)
        {
            if (Listener == null)
            {
                Listener = new HttpListener();
                Listener.Prefixes.Add("http://" + ip + ":" + port + "/");
                Listener.Start();
                void Result(IAsyncResult result)
                {
                    var listener = (HttpListener)result.AsyncState;
                    try
                    {
                        var context = Listener.EndGetContext(result);
                        var request = context.Request;
                        var routes = request.RawUrl.TrimStart('/').Split("/");
                        if (routes.Length > 0)
                        {
                            var file = routes[1] + "." + routes[2];
                            if (routes[0] == "Image")
                            {
                                var img = Image.FromStream(request.InputStream);
                                var args = new SentFileArgs(context.Response, img) { OriginalFileName = file };
                                if (ImageSent != null) App.Current.Dispatcher.Invoke(() => ImageSent(this, args)); else args.RespondString();
                            }
                            else if (routes[0] == "File")
                            {
                                var buffered = new List<byte>();
                                using (var reader = new StreamReader(request.InputStream))
                                {
                                    while (reader.Peek() != -1) buffered.Add((byte)reader.Read());
                                    var args = new SentFileArgs(context.Response, buffered.ToArray()) { OriginalFileName = file };
                                    if (FileSent != null) App.Current.Dispatcher.Invoke(() => FileSent(this, args)); else args.RespondString();
                                }
                            }
                        }
                        Listener.BeginGetContext(Result, Listener);
                    }
                    catch (ObjectDisposedException) { Listener = null; }
                }
                Listener.BeginGetContext(Result, Listener);
            }
        }

        /// <summary>
        /// Stops the ThinServerClient
        /// </summary>
        public void Stop()
        {
            Listener?.Stop();
            Listener?.Close();
        }

        /// <summary>
        /// Send a file to an active ThinServerClient
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <param name="port">Port Number</param>
        /// <param name="filepath">Path of the file to send</param>
        /// <returns>The response is returned as a byte array buffer</returns>
        static public byte[] SendFile(string filepath, string ip = "127.0.0.1", int port = 8383)
        {
            var ImageExtensions = new string[] { "jpg", "jpeg", "bmp", "png", "gif" };
            var ext = Path.GetExtension(filepath).TrimStart('.');
            return SendData("http://" + ip + ":" + port + "/" + (ImageExtensions.Contains(ext) ? "Image" : "File") + "/" + Path.GetFileNameWithoutExtension(filepath) + "/" + ext, File.ReadAllBytes(filepath));
        }
        /// <summary>
        /// Send a string to an active ThinServerClient
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <param name="port">Port Number</param>
        /// <param name="text">Text to Send</param>
        /// <returns>The response is returned as a byte array buffer</returns>
        static public byte[] SendText(string text, string ip = "127.0.0.1", int port = 8383) => SendData("http://" + ip + ":" + port + "/File/Text/txt", System.Text.Encoding.UTF8.GetBytes(text));

        /// <summary>
        /// Send a buffer of data to an active ThinServerClient
        /// </summary>
        /// <param name="address">The address of the Server</param>
        /// <param name="contents">A buffer containing the data to send</param>
        /// <returns></returns>
        static public byte[] SendData(string address, byte[] contents)
        {
            var webrequest = WebRequest.CreateHttp(address);
            webrequest.Method = "POST";
            var responseBuffer = new List<byte>();
            using (var request = webrequest.GetRequestStream())
            {
                request.Write(contents, 0, contents.Length);
                using (var response = webrequest.GetResponse())
                using (var r = new StreamReader(response.GetResponseStream()))
                    while (r.Peek() != -1) responseBuffer.Add((byte)r.Read());
            }
            return responseBuffer.ToArray();
        }
        /// <summary>
        /// Asynchronous version of the SendFile method which sends a file to an active ThinServerClient
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <param name="port">Port Number</param>
        /// <param name="filepath">Path of the file to send</param>
        /// <returns>A Task to execute asynchronously</returns>
        static public Task<byte[]> SendFileAsync(string filepath, string ip = "127.0.0.1", int port = 8383) =>new Task<byte[]>(()=>SendFile(filepath, ip, port));
        /// <summary>
        /// Asynchronous version of the SendText method which sends text to an active ThinServerClient
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <param name="port">Port Number</param>
        /// <param name="text">Text to Send</param>
        /// <returns>A Task to execute asynchronously</returns>
        static public Task<byte[]> SendTextAsync(string text, string ip = "127.0.0.1", int port = 8383) =>new Task<byte[]>(()=>SendText(text, ip, port));
        /// <summary>
        /// An event that fires when an image file has been sent to the server, to end the request you must Invoke the response method in the event args
        /// </summary>
        public virtual event EventHandler<SentFileArgs> ImageSent;
        /// <summary>
        /// An event that fires when any file or request has been sent to the server, to end the request you must Invoke the response method in the event args
        /// </summary>
        public virtual event EventHandler<SentFileArgs> FileSent;
        public class SentFileArgs : EventArgs
        {
            private HttpListenerResponse Response { get; set; }
            public byte[] FileBuffer { get; set; }
            public Image Image { get; set; }
            public string OriginalFileName { get; set; }
            public SentFileArgs(HttpListenerResponse response) => Response = response;
            public SentFileArgs(HttpListenerResponse response, byte[] buffer) : this(response) => FileBuffer = buffer;
            public SentFileArgs(HttpListenerResponse response, Image image) : this(response) => Image = image;
            /// <summary>
            /// Sends a response back to the Client
            /// </summary>
            /// <param name="buffer">A buffer containing data to send back</param>
            public void Respond(byte[] buffer)
            {
                Response.ContentLength64 = buffer.Length;
                var output = Response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            /// <summary>
            /// Sends a string reponse back to the Client encoded in UTF8
            /// </summary>
            /// <param name="value">The text to be sent</param>
            public void RespondString(string value = "") => Respond(System.Text.Encoding.UTF8.GetBytes(value));
        }
    }
}
