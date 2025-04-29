using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZKTecoFingerPrintScanner_Implementation
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        //static void Main()
        private static void Main(string[] args)
        {
            TcpListener server = null;
            

            try
            {
                var port = 15000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                server = new TcpListener(localAddr, port);

                server.Start();

                while (true)
                {
                    Console.WriteLine("Esperando por solicitudes... ");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Conectado!");


                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytes);
                    Console.WriteLine("Mensaje Recibido" + httpRequest);

                    string httpResponse = "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><head><title>Prueba</title></head><body><h1>Prueba</h1><p>Esto es una prueba</p></body></html>";
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Master());

                    //string httpResponse = ""; // CONSOLA.ZKTecoFingerPrintScanner_Implementation.Init();
                    byte[] msg = Encoding.UTF8.GetBytes(httpResponse);
                    stream.Write(msg, 0, msg.Length);

                    client.Close();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Error {0}", e);
            }


            //DETIENE EL SERVIDOR 
            finally
            {
                server.Stop();
            }
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Master());
        }
    }
}
