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
    public RawImage displayTarget;
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
                try
                {
                    // Block until a client connects
                    TcpClient client = listener.AcceptTcpClient();
                    Debug.Log("[TCP] Client connected.");
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
                }
                catch (SocketException se)
                {
                    Debug.LogError($"[TCP] Accept failed: {se}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TCP] Server loop error: {ex}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TCP] Listener error: {e}");
        }
        finally
        {
            listener?.Stop();
        }
    }

    void HandleClient(TcpClient client)
    {
        try
        {
            client.ReceiveTimeout = 5000; // Optional: kill dead clients
            using (NetworkStream stream = client.GetStream())
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (client.Connected && isRunning)
                {
                    try
                    {
                        int frameLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        if (frameLength <= 0 || frameLength > 10_000_000) break; // sanity check

                        byte[] jpegBytes = reader.ReadBytes(frameLength);
                        if (jpegBytes.Length != frameLength) break;

                        Debug.Log($"[TCP] Received frame of length {jpegBytes.Length}");
                        UpdateFrame(jpegBytes);
                    }
                    catch (IOException ioex)
                    {
                        Debug.LogWarning($"[TCP] Client disconnected or timed out: {ioex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[TCP] Error while reading: {ex.Message}");
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TCP] Client handling error: {e}");
        }
        finally
        {
            Debug.Log("[TCP] Cleaning up client connection.");
            client.Close();
        }
    }

    void UpdateFrame(byte[] jpegBytes)
    {
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