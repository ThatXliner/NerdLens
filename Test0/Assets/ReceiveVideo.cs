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
    public float distance = 20f;
    public static byte PITCH = 0b000;
    public static byte YAW = 0b010;
    public static byte ROLL = 0b100;

    public byte lockCameraAxes = (byte)(PITCH | YAW | ROLL);

    // private const float smoothingFactor = 0.9f;
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
    private Settings settings = new Settings();
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

                            // Debug.Log($"[TCP] Received frame of length {jpegBytes.Length}");
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
            // Debug.Log($"[TCP] Rendering {jpegBytes.Length} bytes");
            if (frameTexture == null)
                frameTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);

            frameTexture.LoadImage(jpegBytes);
            displayTarget.texture = frameTexture;
        });
    }
    // So it still locks in place but it would be cool if it was configurable ig
    void UpdateSettings(Settings settings)
    {
        this.settings = settings;
        // XXX: Is UnityMainThreadDispatcher necessary here?
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            // Update your UI or game state with the new settings
            Debug.Log($"Settings updated: Distance = {settings.distance}");
            Vector3 current = cube.transform.position;
            GameObject camera = Camera.main.gameObject;
            Vector3 newPosition = camera.transform.position + Vector3.ProjectOnPlane(camera.transform.forward, new Vector3(0, 1, 0)).normalized * settings.distance;
            cube.transform.position = newPosition;
            // Ok so when it comes to rotation locking you cannot mess with
            // cube.transform.rotation since cube.transform.rotation is relative to the GameObject
            // (the cube) and not the camera nor the world. What this next line does
            // is sanely make the cube face the camera, otherwise it would technically be a
            // set distance away from the camera but not facing it (and thus not letting a perpendicular
            // straight-on view of the surface of the cube).
            cube.transform.rotation = camera.transform.rotation;
            Debug.Log($"New cube direction: {cube.transform.position.normalized}");
        });
    }
    // void LateUpdate()
    // {
    //     GameObject camera = Camera.main.gameObject;
    //     Transform cameraTransform = camera.transform;
    //     Vector3 targetDirection = cameraTransform.position - transform.position;
    //     Quaternion fullRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

    //     Vector3 euler = fullRotation.eulerAngles;

    //     if ((settings.lockCameraAxes & (1 << 0)) == 0) // Lock X (pitch)
    //         // The logic here is "if we do not specify that we're locking the X axis, then we allow it to be modified"
    //         euler.x = transform.rotation.eulerAngles.x;

    //     if ((settings.lockCameraAxes & (1 << 1)) == 0) // Lock Y (yaw)
    //         euler.y = transform.rotation.eulerAngles.y;

    //     if ((settings.lockCameraAxes & (1 << 2)) == 0) // Lock Z (roll)
    //         euler.z = transform.rotation.eulerAngles.z;

    //     // ChatGPT says I should smooth interpolate it
    //     Quaternion desired = Quaternion.Euler(euler);
    //     // TODO: make adjustable
    //     const float smoothingFactor = 0.9f; // Adjust this value to control the smoothing effect
    //     transform.rotation = Quaternion.Slerp(transform.rotation, desired, Time.deltaTime * smoothingFactor);
    // }

    void OnApplicationQuit()
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Join();
        Debug.Log("[TCP] Server stopped.");
    }
}