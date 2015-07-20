#!/usr/bin/python

with open('exit-addresses', 'r') as fin:

    fout = open('exit_ipis.txt','w')
    fout.write('begin\n')
        

    for line in fin.readlines():
        if line.startswith('ExitAddress'):
            tmp = line.split(' ')
            fout.write(tmp[1] + '\n');

    
    fout.write('end\n')
    fout.close()
