import socket
import threading

HOST = 'zombie-shooter-server.herokuapp.com'
PORT = 44396
BUFFER_SIZE = 4096

def read_thread():
    while True:
        try:
            data = client.recv(BUFFER_SIZE)
            if not data:
                continue
            print("Received " + data.decode('utf8'))
        except (ConnectionAbortedError, ConnectionResetError):
            break

name = input("Enter client name: ")

client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect((HOST, PORT))
client.sendall(name.encode('utf8'))

read = threading.Thread(target=read_thread)
read.start()

while True:
    try:
        data = input("Enter data to send: ")
        client.send(data.encode('utf8'))
    except KeyboardInterrupt:
        break

client.close()