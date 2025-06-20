using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
class Settings
{
    public float distance;
    // public int lives;
    // public float health;

    public static Settings CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<Settings>(jsonString);
    }

    // Given JSON input:
    // {"name":"Dr Charles","lives":3,"health":0.8}
    // this example will return a PlayerInfo object with
    // name == "Dr Charles", lives == 3, and health == 0.8f.
}


public class ReceiveVideo : MonoBehaviour
{
    private TcpListener listener;
    private Thread listenerThread;
    private volatile bool isRunning = false;
    public int port = 12345;
    public RawImage displayTarget;
    private Texture2D frameTexture = null;
    public GameObject cube;
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
                        byte messageType = reader.ReadByte();
                        if (messageType.Equals(0)) // Keep-alive message
                        {
                            Debug.Log("[TCP] Received keep-alive message");
                        }
                        else if (messageType.Equals(1))
                        {
                            int frameLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            if (frameLength <= 0 || frameLength > 10_000_000) break; // sanity check

                            byte[] jpegBytes = reader.ReadBytes(frameLength);
                            if (jpegBytes.Length != frameLength) break;

                            Debug.Log($"[TCP] Received frame of length {jpegBytes.Length}");
                            UpdateFrame(jpegBytes);
                        }
                        else if (messageType.Equals(2))
                        {
                            Debug.Log("[TCP] Received change settings");
                            int characterLengths = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            if (characterLengths <= 0 || characterLengths > 10_000_000) break; // sanity check
                            char[] settingsString = reader.ReadChars(characterLengths); // Read the characters but do nothing with them
                            Settings settings = Settings.CreateFromJSON(new string(settingsString));
                            UpdateSettings(settings);
                        }
                        else
                        {
                            Debug.LogWarning($"[TCP] Unknown message type: {messageType}");
                            // Optionally, you could send an error response back to the client
                        }

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
            displayTarget.SetNativeSize(); // sets width/height based on texture
        });
    }
    // So it still locks in place but it would be cool if it was configurable ig
    void UpdateSettings(Settings settings)
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            // Update your UI or game state with the new settings
            Debug.Log($"Settings updated: Distance = {settings.distance}");
            Vector3 current = cube.transform.position;
            // Debug.Log($"[TCP] Current cube position: {current}");
            GameObject camera = Camera.main.gameObject;
            // Debug.Log($"[TCP] Camera located");
            cube.transform.position = camera.transform.position + camera.transform.forward * settings.distance;
            // TODO: Lock axis
            cube.transform.rotation = camera.transform.rotation;
            // Debug.Log($"[TCP] Modified cube transform");
            // For example, you could update a TextMeshProUGUI component:
            // someTextMeshProUGUI.text = $"Distance: {settings.distance}";
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