#!/usr/bin/env python

import sys
import socket
import subprocess
from subprocess import Popen
import threading
import time
from multiprocessing import Pool
import os

TCP_IP = '127.0.0.1'
BUFFER_SIZE = 1000000

def processAlexa(x):
    port, l, r = x
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
    print("LEN: " + str(len(set(exit_asns))))
    #485

    ipis = []
    with open('US-dests.txt', 'r') as fin:
        for line in fin.readlines():
            tmp = line[line.index("[")+1: line.index("]")]
            #print(tmp)
            ipis.append(tmp)
            #break
    print("LEN: " + str(len(set(ipis))))
    #367

    #fout = open('asprecomp/aspath_precomp' + str(l) + 'to' + str(r) + '.txt', 'w')
    fout = open('US-precomp' + str(len(set(ipis)))  + '.txt', 'w')

    cnt = 0;
    for tmp in set(ipis):
        print(tmp)
        MESSAGE = dests + " " + tmp + " -q"
        for ex in set(exit_asns):
            MESSAGE += " " + ex + " " + tmp
            MESSAGE += " " + tmp + " " + ex
        MESSAGE += " <EOFc>"

        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        print("Connecting to " + str(port))
        s.connect((TCP_IP, port))
        s.send(MESSAGE)

        data = ""
        while True:
            d = s.recv(BUFFER_SIZE)
            data += d
            if "<EOFs>" in d:
                break
        s.close()

        fout.write(data + "\n")
        fout.flush()

        if cnt == 0:
            print("Received data: " + data)

        cnt += 1
        print(str(port) + ": " + str(cnt))

    fout.close()


def main(argv):
    bgps = []
    FNULL = open(os.devnull, 'w')
    for i in range(0, 1):
        p = Popen(['mono', '../TestingApplication/bin/Release/TestingApplication.exe', 
            '-server' + str(11000 + i), '../TestingApplication/bin/Release/Cyclops_caida_cons.txt'],
            stdout=FNULL, stderr=subprocess.STDOUT)
        bgps.append(p)
    
    time.sleep(5)
    
    '''
    data = []
    for i in range(0, 1):
        data.append((11000+i, 100 * i, 100 * (i+1)))
    
    p = Pool(1)
    p.map(processAlexa, data)
    '''

    processAlexa((11000, 0, 0))

    for p in bgps:
        p.terminate()

    
if __name__ == "__main__":
    main(sys.argv)


