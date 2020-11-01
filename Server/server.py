import socket
import asyncio
import signal
import sys

from player import Player

HOST = ''
PORT = 5000
BUFFER_SIZE = 4096

players = []

def handle_receive(player, message):
    print("Received message from " + player.name + ": " + message.decode('utf8'))
    for connected_player in players:
        if connected_player != player:
            connected_player.send(message)

def handle_disconnect(player):
    player.writer.close()
    players.remove(player)

async def handle_client(reader, writer):
    player = Player(reader, writer, BUFFER_SIZE)
    players.append(player)
    print("Received connection")

    player.events.on_receive += handle_receive
    player.events.on_disconnect += handle_disconnect

def signal_handler(*args):
    for player in players:
        player.writer.close()
    loop.close()

if __name__ == '__main__':
    print("Starting server")
    
    loop = asyncio.get_event_loop()
    task = loop.create_task(asyncio.start_server(handle_client, HOST, PORT))

    for signal_type in [signal.SIGINT, signal.SIGTERM]:
        signal.signal(signal_type, signal_handler)

    loop.run_forever()