import asyncio
import threading
from events import Events

class Player:
    def __init__(self, reader, writer, buffer_size):
        self.name = None
        self.buffer_size = buffer_size
        self.reader = reader
        self.writer = writer
        self.events = Events(('on_receive', 'on_disconnect'))

        self.event_loop = asyncio.get_event_loop()
        self.event_loop.create_task(self.read())
        
    async def read(self):
        self.name = (await self.reader.read(self.buffer_size)).decode('utf8')
        while True:
            try:
                data = await self.reader.read(self.buffer_size)
                if not data:
                    continue
                self.events.on_receive(self, data)
            except (ConnectionAbortedError, ConnectionResetError):
                self.events.on_disconnect(self)
                break

    def send(self, data):
        try:
            self.writer.write(data)
        except (ConnectionAbortedError, ConnectionResetError):
            self.events.on_disconnect(self)

    def __eq__(self, other):
        if (isinstance(other, Player)):
            return self.name == other.name
        return False

    def __ne__(self, other):
        return not self.__eq__(other)