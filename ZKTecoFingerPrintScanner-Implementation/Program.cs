using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ZKTecoFingerPrintScanner_Implementation
{
    static class Program
    {
        private static Form _fingerprintForm;
        private static readonly object _formLock = new object();
        private static ManualResetEvent _uiReady = new ManualResetEvent(false);
        private static volatile bool _serviceRunning = true;
        private static TcpListener _server;

        [STAThread]
        private static void Main(string[] args)
        {
            // Crear un formulario oculto para mantener el contexto de UI
            Form hiddenForm = null;
            var uiThread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                hiddenForm = new Form() { ShowInTaskbar = false, Opacity = 0 };
                _uiReady.Set(); // Indicar que la UI está lista
                Application.Run(hiddenForm);
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();

            // Esperar a que la UI esté lista
            _uiReady.WaitOne();

            // Iniciar el servidor TCP en otro hilo
            Thread serverThread = new Thread(StartTcpServer);
            serverThread.IsBackground = true;
            serverThread.Start();

            // Mantener la aplicación principal corriendo
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void StartTcpServer()
        {
            try
            {
                var port = 15000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                _server = new TcpListener(localAddr, port);
                _server.Start();

                while (_serviceRunning)
                {
                    Console.WriteLine("Esperando por solicitudes...");
                    using (TcpClient client = _server.AcceptTcpClient())
                    {
                        Console.WriteLine("Conectado!");

                        NetworkStream stream = client.GetStream();
                        byte[] buffer = new byte[1024];
                        int bytes = stream.Read(buffer, 0, buffer.Length);
                        string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytes);
                        Console.WriteLine("Mensaje Recibido: " + httpRequest);

                        string httpResponse = ProcessRequest(httpRequest);

                        byte[] msg = Encoding.UTF8.GetBytes(httpResponse);
                        stream.Write(msg, 0, msg.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                _server?.Stop();
            }
        }

        private static string ProcessRequest(string httpRequest)
        {
            // Determinar qué endpoint se está llamando
            if (httpRequest.Contains("GET /start "))
            {
                return StartService();
            }
            else if (httpRequest.Contains("GET /stop "))
            {
                return StopService();
            }
            else if (httpRequest.Contains("GET /call "))
            {
                return CallFingerprintReader();
            }
            else
            {
                return "HTTP/1.1 404 Not Found\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><body><h1>Endpoint no encontrado</h1></body></html>";
            }
        }

        private static string StartService()
        {
            if (!_serviceRunning)
            {
                _serviceRunning = true;
                Thread serverThread = new Thread(StartTcpServer);
                serverThread.IsBackground = true;
                serverThread.Start();
                return "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><body><h1>Servicio iniciado</h1></body></html>";
            }
            return "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><body><h1>Servicio ya estaba en ejecución</h1></body></html>";
        }

        private static string StopService()
        {
            if (_serviceRunning)
            {
                _serviceRunning = false;
                _server?.Stop();
                return "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><body><h1>Servicio detenido</h1></body></html>";
            }
            return "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><body><h1>Servicio ya estaba detenido</h1></body></html>";
        }

        private static string CallFingerprintReader()
        {
            lock (_formLock)
            {
                if (_fingerprintForm != null && !_fingerprintForm.IsDisposed)
                {
                    if (_fingerprintForm.IsHandleCreated)
                    {
                        _fingerprintForm.Invoke((Action)(() =>
                        {
                            _fingerprintForm.BringToFront();
                            _fingerprintForm.WindowState = FormWindowState.Normal;
                            _fingerprintForm.Focus();
                        }));
                    }
                    return "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><body><h1>Lector ya abierto</h1></body></html>";
                }
                else
                {
                    Application.OpenForms[0]?.Invoke((Action)(() =>
                    {
                        _fingerprintForm = new Master();
                        _fingerprintForm.FormClosed += (s, e) => _fingerprintForm = null;
                        _fingerprintForm.Show();
                    }));
                    return "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html><html><body><h1>Lector iniciado</h1></body></html>";
                }
            }
        }
    }
}