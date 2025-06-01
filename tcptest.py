# Echo client program
import socket
import struct
from pathlib import Path

HOST = "localhost"  # The remote host
PORT = 12345  # The same port as used by the server
image = (Path(__file__).parent / "img.jpeg").read_bytes()
with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.connect((HOST, PORT))
    s.sendall(struct.pack(">I", len(image)))
    s.sendall(image)
