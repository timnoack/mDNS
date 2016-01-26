# mDNS

This is an implementation of mDNS in the Universal Windows Platform (UWP).
It's tested on Windows as well as on Windows 10 Core (Raspberry Pi 2).

Main work was done by the developers of JmDNS (http://jmdns.sourceforge.net/) and mdns.net (http://mdnsnet.cvs.sourceforge.net/).
As required by UWP networking parts are running asynchronously.

The test project allows to discover services in the network and "simulate" an Apple AirPlay receiver as well as an Apple HomeKit Accessory.

Documentation needs some updating in some places.

Usage is simple:
```
mDNS client = new mDNS();
await client.Init();

// register service for printer
props = new System.Collections.Hashtable();
props.Add("someprops", "1234");

client.RegisterService(new mDNS.ServiceInfo("_printer._tcp.local.", "EPSON WF-3620 Series", 8080, 0, 0, props));

// list printers
var printerServices = await client.List("_printer._tcp.local.");
```
