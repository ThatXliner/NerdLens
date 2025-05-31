import socket
import struct
import time

import cv2  # pip install opencv-python
from mss import mss, tools


def connect_to_unity(host="localhost", port=12345):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((host, port))
    print(f"[Client] Connected to {host}:{port}")
    return sock


def send_jpeg(sock, jpeg_bytes):
    # Prefix each frame with a 4-byte big-endian length
    length = struct.pack(">I", len(jpeg_bytes))
    sock.sendall(length + jpeg_bytes)


def main():
    host = "localhost"  # or use '127.0.0.1'
    port = 12345

    # OpenCV video capture from screen, webcam, or file
    # cap = cv2.VideoCapture(0)  # 0 = default webcam
    # if not cap.isOpened():
    #     print("Could not open video source.")
    #     return

    sock = connect_to_unity(host, port)

    try:
        with mss() as sct:
            while True:
                print("reading capture")
                monitor = {"top": 160, "left": 160, "width": 160, "height": 135}
                frame = sct.grab(monitor)

                # # Resize for performance (optional)
                # frame = cv2.resize(frame.rgb, (640, 360))

                # # Encode frame as JPEG
                # success, jpeg = cv2.imencode(
                #     ".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), 80]
                # )
                # if not success:
                #     continue
                # print("send")
                send_jpeg(sock, tools.to_png(frame.rgb, (640, 360)))
                # send_jpeg(sock, mss.tools.to_png(frame.rgb, frame.size))
                time.sleep(1 / 30)  # ~30 FPS cap
    except (KeyboardInterrupt, EOFError):
        print("\n[Client] Stopped by user.")
    except Exception as e:
        print(f"\n[Client] Error: {e}")
    finally:
        # cap.release()
        sock.close()
        print("[Client] Connection closed.")


if __name__ == "__main__":
    main()
