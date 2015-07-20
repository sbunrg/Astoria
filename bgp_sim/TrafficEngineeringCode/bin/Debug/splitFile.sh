#!/bin/bash

#
# script to split the output of the traffic
# engineering code into per iteration outputs.
#

for i in {1..30}; do

grep "^$i " trafficThroughSecureProvider.bak | sed 's/:://' > "iteration$i.txt"

done