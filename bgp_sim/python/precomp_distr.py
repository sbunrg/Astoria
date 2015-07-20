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
    print(len(set(exit_asns)))

    with open('alexa_top100k_as_nums.txt', 'r') as fin:
        fout = open('asprecomp/aspath_precomp' + str(l) + 'to' + str(r) + '.txt', 'w')
    
        cnt = 0;
        for line in fin.readlines():
            if cnt < l:
                cnt += 1
                continue
            if cnt >= r:
                break

            fout.write(str(cnt) + "\n")

            tmp = line.split('\t')
            if tmp[3].isdigit():
                MESSAGE = dests + " " + tmp[3] + " -q"
                for ex in exit_asns:
                    MESSAGE += " " + ex + " " + tmp[3]
                    MESSAGE += " " + tmp[3] + " " + ex
                MESSAGE += " <EOF> "

                s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                print("Connecting to " + str(port))
                s.connect((TCP_IP, port))
                s.send(MESSAGE)

                data = ""
                while True:
                    d = s.recv(BUFFER_SIZE)
                    data += d
                    if "<EOF>" in d:
                        break
                    #if len(tmp) == 0:
                        #break
                
                s.close()

                fout.write(data + "\n")
                fout.flush()

                #print("Received data: " + data)

                cnt += 1
                print(str(port) + ": " + str(cnt))

        fout.close()

'''
class compThread(threading.Thread):
    def __init__(self, threadID, name, counter):
        threading.Thread.__init__(self)
        self.threadID = threadID
        self.name = name
        self.counter = counter
    
    def run(self):
        print("Starting " + str(self.name) + " " + str(self.counter))
        # threadLock.acquire()
        l, r = self.counter
        processAlexa(l, r, int(self.name))
        # threadLock.release()
        print("Finishing " + str(self.name) + " " + str(self.counter))
'''

def main(argv):
    bgps = []
    FNULL = open(os.devnull, 'w')
    for i in range(0, 5):
        p = Popen(['mono', '../TestingApplication/bin/Release/TestingApplication.exe', 
            '-server' + str(11000 + i), '../TestingApplication/bin/Release/Cyclops_caida_new.txt'],
            stdout=FNULL, stderr=subprocess.STDOUT)
        bgps.append(p)
    
    time.sleep(5)

    '''
    threads = []
    for i in range(0, 10):
        t = compThread(i, (11000+i), (100 * i, 100 * (i+1)))
        t.start()
        threads.append(t)

    for t in threads:
        t.join()
    '''

    data = []
    for i in range(0, 5):
        data.append((11000+i, 100 * i, 100 * (i+1)))
    
    p = Pool(5)
    p.map(processAlexa, data)

    for p in bgps:
        p.terminate()

if __name__ == "__main__":
    main(sys.argv)


