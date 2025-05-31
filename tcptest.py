# Echo client program
import socket

HOST = "localhost"  # The remote host
PORT = 12345  # The same port as used by the server
with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.connect((HOST, PORT))
    assert s.recv(3) == b"\xef\xbb\xbf"
    print("connected")
    while True:
        try:
            s.sendall((input(">") + "\n").encode("utf-8"))
        except EOFError:
            s.sendall("CLOSE CONNECTION".encode("utf-8"))
            break
        data = s.recv(2048)
        print("Received", repr(data), repr(data.decode("utf-8")))
