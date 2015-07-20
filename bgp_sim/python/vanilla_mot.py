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
PORT = 11000
BUFFER_SIZE = 1000000

def countAttackers(src, ent, ext, dst, log):
    MESSAGE = src + " " + ent + " " + ext + " " + dst + " -q";
    MESSAGE += " " + src + " " + ent
    MESSAGE += " " + ent + " " + src
    MESSAGE += " " + ext + " " + dst
    MESSAGE += " " + dst + " " + ext
    MESSAGE += " <EOFc>"

    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    print("Connecting to " + str(PORT))
    s.connect((TCP_IP, PORT))
    s.send(MESSAGE)

    data = ""
    while True:
        d = s.recv(BUFFER_SIZE)
        data += d
        if len(d) == 0:
            break
        if "<EOFs>" in d:
            break
    s.close()

    #print("Received data: " + data)
    log.write(data + "\n")
    log.flush()

    # Separating sets
    arr = data.split('-\n')
    print("returned : " + str(len(arr)))
    assert len(arr) == 5

    #print(arr)

    first = []
    for s in arr[0].split("\n"):
        #print(s)
        if not s.startswith("ASes") and s != '':
            first.append(int(s))
    for s in arr[1].split("\n"):
        if not s.startswith("ASes") and s != '':
            first.append(int(s))

    second = []
    for s in arr[2].split("\n"):
        if not s.startswith("ASes") and s != '':
            second.append(int(s))
    for s in arr[3].split("\n"):
        if not s.startswith("ASes") and s != '':
            second.append(int(s))

    # Intersection
    return len(set(first) & set(second))


def main(argv):
    
    countries = ["US"];
    #countries = ["BR", "CN", "DE", "ES", "FR", "GB", "IR", "IT", "RU", "US"]
    for C in countries:
        summary = {}
        #sout = open("vanilla_motivation/vm-US-sum.txt", "w")
        sout = open("uniform_motivation/vm-" + C + "-sum.txt", "w")
        fout = open("uniform_motivation/vm-" + C + ".txt", "w")
        log = open("uniform_motivation/vm-" + C + "-log.txt", "w")
        with open('/home/ostarov/motivation/uniform/'+ C + '-logs/' + C + '-circuits-uniform.log', 'r') as fin:
            website = None
            requests = 0
            attacked = 0
            num = 0
            for line in fin.readlines():
                if line == "\n": 
                    continue
                if line.startswith("Visiting"):
                    if website != None:
                        sout.write(website + "\t" + str(requests) + "\t" + str(attacked) + "\n")
                        sout.flush()
                        requests = 0
                        attacked = 0
                    website = line.strip()
                    fout.write(website + "\n")
                    fout.flush()
                    num += 1
                    print(num)
                elif website != None:
                    tmp = line.replace("]", "[").split("[")
                    print(tmp)
                    if len(tmp) == 9:
                        fout.write(tmp[1] + "\t" + tmp[3] + "\t" + tmp[5] + "\t" + tmp[7] + "\t")
                        attackers = countAttackers(tmp[1], tmp[3], tmp[5], tmp[7], log);
                        fout.write(str(attackers) + "\n");
                        fout.flush()
                        requests += 1
                        if attackers > 0:
                            attacked += 1
                        #break
        log.close()
        fout.close()
        sout.close()

    
if __name__ == "__main__":
    main(sys.argv)


