Toxy [![Build Status](http://jenkins.impy.me/job/Toxy%20x86/badge/icon)](http://jenkins.impy.me/job/Toxy%20x86/)
====

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
* Voice chats
* File transfers
* Typing detection
* DNS discovery (tox1 and tox3)
* Theme customization

### Screenshots

![Main Window](http://reverbs.pw/i/46f5a9.png)

Binaries
===
Pre-compiled versions of Toxy can be found [here](http://1drv.ms/1pkwaFp "Toxy Binaries"). Those include all of the dependencies.

Things you'll need to compile
===

* The [SharpTox library](https://github.com/Impyy/SharpTox "SharpTox GitHub repo") and its dependencies. Once you have obtained those, place libtox.dll and SharpTox.dll in the libs folder.

All other dependencies can be found in the packages.config file and should be downloaded by Visual Studio automatically

===
### Special Thanks

* Punker
