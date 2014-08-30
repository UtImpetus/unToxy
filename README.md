unToxy
====

This is a fork of [Toxy](https://github.com/Reverp/Toxy).

Metro-style Tox client for Windows. ([Tox](https://github.com/irungentoo/ProjectTox-Core "ProjectTox GitHub repo") is a free (as in freedom) Skype replacement.)

At this point, this client isn't feature-complete and is not ready for an official release.
Some features may be missing or are partially broken. Updates will arrive in time.

Feel free to contribute.

### Features

* Standard features like:
  - Nickname customization
  - Status customization
  - Friendlist
  - One to one conversations
  - Friendrequest listing
* Group chats
* Audio calls
* Video calls
* File transfers
* Typing detection
* DNS discovery (tox1 and tox3)
* Theme customization

### Screenshots

![Main Window](http://img-fotki.yandex.ru/get/12/32246118.21/0_7e4af_c819933_orig)

Binaries
===
Pre-compiled versions of unToxy can be found [here](http://1drv.ms/1pkwaFp "unToxy Binaries"). Those include all of the dependencies.

Things you'll need to compile
===

* The [SharpTox library](https://github.com/Impyy/SharpTox "SharpTox GitHub repo") and its dependencies. 
* The [SharpTox.Vpx library](https://github.com/Impyy/SharpTox.Vpx)
* Once you have obtained those, place libtox.dll, SharpTox.dll and SharpTox.Vpx.dll in the libs folder.

All other dependencies can be found in the packages.config file and should be downloaded by Visual Studio automatically

===
### Special Thanks

* punker76
