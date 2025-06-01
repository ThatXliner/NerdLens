using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiveVideo : MonoBehaviour
{
    private TcpListener listener;
    private Thread listenerThread;
    private volatile bool isRunning = false;
    public int port = 12345;
    public RawImage displayTarget; // assign this in the Inspector
    private Texture2D frameTexture = null;


    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
                Debug.Log("[TCP] Client connected, waiting for data...");
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
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (true)
                {
                    int frameLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    if (frameLength <= 0) break;
                    byte[] jpegBytes = reader.ReadBytes(frameLength);
                    if (jpegBytes.Length < frameLength) break;
                    Debug.Log($"[TCP] Received frame of length {jpegBytes.Length}");
                    UpdateFrame(jpegBytes);
                }
                Debug.Log("[TCP] Client disconnected or sent invalid data.");
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
    void UpdateFrame(byte[] jpegBytes)
    {
        // Run texture update on the main thread
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log($"[TCP] Rendering {jpegBytes.Length} bytes");
            if (frameTexture == null)
                frameTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);

            frameTexture.LoadImage(jpegBytes);
            displayTarget.texture = frameTexture;
        });
    }
    void OnApplicationQuit()
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Join();
        Debug.Log("[TCP] Server stopped.");
    }
}