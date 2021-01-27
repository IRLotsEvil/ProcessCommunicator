using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace communication
{
    public class ThinServerClient
    {
        public HttpListener Listener { get; set; } = new HttpListener();
        public string ServerAddress { get; set; }
        public bool IsActive { get; set; } = false;
        public ThinServerClient(string ip, int port)
        {
            ServerAddress = "http://" + ip + ":" + port + "/";
            Listener.Prefixes.Add(ServerAddress);
            IsActive = true;
            new Task(() =>
            {
                Listener.Start();
                while (IsActive)
                {
                    var context = Listener.GetContext();
                    var request = context.Request;
                    var routes = request.RawUrl.TrimStart('/').Split("/");
                    if (routes.Length > 0)
                    {
                        var file = routes[1] + "." + routes[2];
                        if (routes[0] == "Image")
                        {
                            var img = System.Drawing.Image.FromStream(request.InputStream);
                            App.Current.Dispatcher.Invoke(() => ImageSent?.Invoke(this, new SentFileArgs(context.Response,img)));
                        }
                        else if (routes[0] == "File")
                        {
                            var buffered = new List<byte>();
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                while (reader.Peek() != -1) buffered.Add((byte)reader.Read());
                                App.Current.Dispatcher.Invoke(() => FileSent?.Invoke(this, new SentFileArgs(context.Response, buffered.ToArray())));
                            }
                        }
                    }
                }
                Listener.Stop();
            }).Start();
        }
        static public byte[] SendFile(string ip, int port, string filepath)
        {
            var ImageExtensions = new string[] { "jpg", "jpeg", "bmp", "png", "gif" };
            var ext = Path.GetExtension(filepath).TrimStart('.');
            return SendData("http://" + ip + ":" + port + "/" + (ImageExtensions.Contains(ext) ? "Image" : "File") + "/" + Path.GetFileNameWithoutExtension(filepath) + "/" + ext, File.ReadAllBytes(filepath));
        }
        static public byte[] SendText(string ip, int port, string text)
        {
            return SendData("http://" + ip + ":" + port + "/File/Text/txt", System.Text.Encoding.UTF8.GetBytes(text));
        }
        static private byte[] SendData(string address, byte[] contents)
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
        public virtual event EventHandler<SentFileArgs> ImageSent;
        public virtual event EventHandler<SentFileArgs> FileSent;
        public class SentFileArgs : EventArgs
        {
            private HttpListenerResponse Response { get; set; }
            public byte[] FileBuffer { get; set; }
            public System.Drawing.Image Image { get; set; }
            public SentFileArgs(HttpListenerResponse response) => Response = response;
            public SentFileArgs(HttpListenerResponse response, byte[] buffer) : this(response) => FileBuffer = buffer;
            public SentFileArgs(HttpListenerResponse response, System.Drawing.Image image) : this(response) => Image = image;
            public void Respond(byte[] buffer)
            {
                Response.ContentLength64 = buffer.Length;
                var output = Response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            public void RespondString(string value = "") => Respond(System.Text.Encoding.UTF8.GetBytes(value));
        }
    }
}
