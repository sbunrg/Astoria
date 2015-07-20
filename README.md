# Astoria

Astoria is a **research prototype** AS-aware Tor client. It is constantly
changing and should not be considered secure enough for browsing sensitive
content.

Find the paper that describes the internals of Astoria here: [Arxiv](http://arxiv.org/abs/1505.05173). 

It may happen from time-to-time, that
the details described in the paper will be out-of-date with the current 
client. We will make effort to prevent this from happening too often.

## Disclaimer 

We follow the principles of the [CRAPL
licence](http://matt.might.net/articles/crapl/) and take no responsibility for
any thing that might happen to you, your system, or your network when you use
Astoria.

## Astoria Installation

Astoria consists of two parts -- a client (a modified version of the original 
Tor client) and a path prediction tool. 

###Building the Astoria Tor client

Since Astoria is a fork of the original Tor client (and has large similarities 
to it), the installation process is identical.


> sh autogen.sh && ./configure && make && make install


###Path Prediction Toolkit

The toolkit is ready to go. You just need to have "mono" installed. 

## Running Astoria

First, you need to get the path prediction toolkit running. 
In ./bgp_sim: 

> mono TestingApplication/bin/Release/TestingApplication.exe -server11000 TestingApplication/bin/Release/Cyclops_caida_cons.txt precomp/US-precomp367.txt cache/exit_asns.txt

Cyclops_caida_cons.txt is the Caida AS topology to be used while performing 
prediction. US-precomp367.txt is some precomputed data you can use to make
initialization faster. You can find more precomputed cache files for 10
countries in the precomp folder.

Initialization takes about 15 minutes.

Once the path prediction toolkit is up and running, you can start the Tor
client.

In ./astoria-tor-client/src/or: 

> ./tor

Remember, the longer you use Astoria, the faster it gets. Expect the first few
pages you load to take a couple of minutes.

## Support

For support, please send an email to any of the authors.
We will follow the principle of best-effort support, without providing any
guarantees.
