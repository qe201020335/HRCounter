import socket as s
from time import sleep

k = s.socket(s.AF_INET, s.SOCK_DGRAM)

# broadcast
# k.setsockopt(s.SOL_SOCKET,s.SO_BROADCAST,1)
# a = "255.255.255.255"
a = "localhost"
port = 9000

while True:
    for i in range(60, 180):
        k.sendto(b"/hr\0,i\0\0" + i.to_bytes(4, byteorder='big', signed=True), (a, port))
        sleep(0.5)
