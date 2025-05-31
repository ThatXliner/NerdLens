using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TCPServerBehaviour : MonoBehaviour
{
    private TcpListener listener;
    private Thread listenerThread;
    private volatile bool isRunning = false;
    public int port = 12345;

    void Start()
    {
        isRunning = true;
        listenerThread = new Thread(ServerLoop);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    void ServerLoop()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Debug.Log($"[TCP] Server listening on port {port}");

            while (isRunning)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(100); // prevent tight loop
                    continue;
                }

                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
        }
        catch (SocketException se)
        {
            Debug.LogError($"[TCP] Socket exception: {se}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TCP] Server exception: {e}");
        }
    }

    void HandleClient(TcpClient client)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                string name = reader.ReadLine();
                if (string.IsNullOrEmpty(name)) return;

                string response = $"Hello from iPhone, {name}\n";
                writer.WriteLine(response);
                writer.Flush();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TCP] Client handling error: {e}");
        }
        finally
        {
            client.Close();
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Join();
        Debug.Log("[TCP] Server stopped.");
    }
}