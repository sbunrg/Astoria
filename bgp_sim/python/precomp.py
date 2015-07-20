#!/usr/bin/env python

import socket

exit_asns = []
dests = ""

with open('exit_asns.txt', 'r') as fin:
    for line in fin.readlines():
        tmp = line.split(' ')
        if tmp[0].isdigit():
            exit_asns.append(tmp[0])
            dests += tmp[0] + " "
            #break

print(dests)
print(len(set(exit_asns)))

TCP_IP = '127.0.0.1'
TCP_PORT = 11000
BUFFER_SIZE = 1000000

with open('alexa_top100k_as_nums.txt', 'r') as fin:
    fout = open('aspath_cache10k.txt', 'w')
    
    cnt = 0;
    for line in fin.readlines():
        if cnt >= 10000:
            break
        
        tmp = line.split('\t')
        if tmp[3].isdigit():
            MESSAGE = dests + " " + tmp[3] + " -q"
            for ex in exit_asns:
                MESSAGE += " " + ex + " " + tmp[3]
                MESSAGE += " " + tmp[3] + " " + ex
            MESSAGE += " <EOF> "

            s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            s.connect((TCP_IP, TCP_PORT))
            s.send(MESSAGE)
            data = s.recv(BUFFER_SIZE)
            s.close()

            fout.write(data + "\n")
            fout.flush()

            #print("Received data: " + data)

            cnt += 1
            print(cnt)

    fout.close()

