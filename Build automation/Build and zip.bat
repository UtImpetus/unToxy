set msbuildpath=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
For /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c%%a%%b)
For /f "tokens=1-2 delims=/:" %%a in ('time /t') do (set mytime=%%a%%b)
%msbuildpath% /property:Configuration=Debug;Platform=x86 ./../Toxy.sln
%msbuildpath% /property:Configuration=Debug;Platform=x64 ./../Toxy.sln
XCOPY ..\Toxy\bin\x86\Debug Toxy_x86\ /s /e
XCOPY ..\Toxy\bin\x64\Debug Toxy_x64\ /s /e
del Toxy_x86\data
del Toxy_x86\config.xml
del Toxy_x64\log.txt
del Toxy_x64\data
del Toxy_x64\config.xml
del Toxy_x64\log.txt
zip Toxy_x86_%mydate%_%mytime%.zip Toxy_x86\*
zip Toxy_x64_%mydate%_%mytime%.zip Toxy_x64\*
rmdir Toxy_x86 /s /q
rmdir Toxy_x64 /s /q