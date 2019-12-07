@echo off
:: !IMPORTANT! Set the following variable to your desired size of the "thread pool" :-)
set /a sk.nop77svk.async_multibatch.cfg.thread_pool_size=2*%NUMBER_OF_PROCESSORS%
set sk.nop77svk.async_multibatch.cfg.start_minimized=no

goto :the_coordinator

:: ===============================================================================================
:coordinator_stuff
:: Use "call :async_thread <arguments>" to spawn a thread
:: !IMPORTANT! Start editing your "coordinator" stuff here

set "SOLUTION_SOURCE_PATH=%~dp0"
set ONEDRIVE_SYNC_PATH=D:\_builds@Teams
set SOLUTION_PUBLISH_PATH=D:\_builds

if not exist "%SOLUTION_SOURCE_PATH%\*.sln" (
	echo.ERROR: Solution not found in %SOLUTION_SOURCE_PATH%
	exit /b 1
)

if not exist "%SOLUTION_PUBLISH_PATH%" (
	echo.ERROR: Solution publish path %SOLUTION_PUBLISH_PATH% does not exist
	exit /b 2
)

if not exist "%ONEDRIVE_SYNC_PATH%" (
	echo.ERROR: OneDrive sync folder %ONEDRIVE_SYNC_PATH% does not exist"
	exit /b 3
)

echo.===================================================================================================
echo.== building the solution
echo.===================================================================================================

dotnet build "%SOLUTION_SOURCE_PATH%\tdvcli\tdvcli.csproj" -c Release /nowarn:AD0001
if %ERRORLEVEL% gtr 0 echo.ERROR while building the solution
if %ERRORLEVEL% gtr 0 exit /b %ERRORLEVEL%

call :async_thread "Full x64 Windows" [win][full]
call :async_thread "Full x64 Linux" [linux][full]
call :async_thread "Minimal Portable" [portable][minimal]

echo.===================================================================================================
echo.== done; waiting for the compression "threads" to finish
echo.===================================================================================================

:: !IMPORTANT! End editing your "coordinator" stuff here
exit /b 0

:: -----------------------------------------------------------------------------------------------
:thread_stuff
:: !IMPORTANT! Start editing your "thread" stuff here

echo.===================================================================================================
echo.== publishing "%~1" build
echo.===================================================================================================

set l_extra_args=
:: set l_extra_args=--no-self-contained
:: if /i "x%~1" equ "full x64 windows" set l_extra_args=--self-contained --runtime=win-x64
:: if /i "x%~1" equ "full x64 linux" set l_extra_args=--self-contained --runtime=linux-x64

dotnet publish "%SOLUTION_SOURCE_PATH%\tdvcli\tdvcli.csproj" -c Release -o "%SOLUTION_PUBLISH_PATH%\TDV CLI %~2" --no-build %l_extra_args% -p:PublishProfile="%~1"
if %ERRORLEVEL% gtr 0 echo.ERROR while publishing the solution
if %ERRORLEVEL% gtr 0 pause
if %ERRORLEVEL% gtr 0 exit /b %ERRORLEVEL%

del /q "%ONEDRIVE_SYNC_PATH%\TDV CLI %~2.7z"

pushd "%SOLUTION_PUBLISH_PATH%\TDV CLI %~2"
C:\bin\cygwin\lib\p7zip\7zr.exe a -aou -bt -r -xr!*.log "%ONEDRIVE_SYNC_PATH%\TDV CLI %~2.7z"
popd

:: !IMPORTANT! End editing your "thread" stuff here
exit /b 0

:: ===============================================================================================
:: !IMPORTANT! DO NOT EDIT STUFF AFTER THIS POINT
:the_coordinator

set g_this_script=%0
if "x%DEBUG%"=="xyes" echo.This script = %g_this_script%>&2

set l_modus_operandi=child
if "x%sk.nop77svk.async_multibatch.arg.run%"=="x" set l_modus_operandi=master
if "x%sk.nop77svk.async_multibatch.arg.child%"=="x" set l_modus_operandi=master
if "x%DEBUG%"=="xyes" echo.Current modus operandi = %l_modus_operandi%>&2

call :to_upper_case sk.nop77svk.async_multibatch.cfg.start_minimized

if %l_modus_operandi%==child (
	if "x%DEBUG%"=="xyes" echo.I'm a run %sk.nop77svk.async_multibatch.arg.run%'s child #%sk.nop77svk.async_multibatch.arg.child%>&2
	call :thread_stuff %*
	del %TEMP%\sk.nop77svk.async_multibatch.%sk.nop77svk.async_multibatch.arg.run%.%sk.nop77svk.async_multibatch.arg.child%.lck
) else (
	if "x%DEBUG%"=="xyes" echo.Thread pool size = %sk.nop77svk.async_multibatch.cfg.thread_pool_size%>&2
	set l_run_id=%RANDOM%
	if exist %TEMP%\sk.nop77svk.async_multibatch.%l_run_id%.*.lck (
		echo ERROR: There appear to be some active threads with the run id of %l_run_id%!
		exit /b 1
	)

	set l_child_id=0
	if "x%DEBUG%"=="xyes" echo.I'm a run %l_run_id%'s master>&2
	call :coordinator_stuff %*
	call :wait_for_active_threads_to_be_max 0
)
endlocal

exit /b 0

:: -----------------------------------------------------------------------------------------------
:async_thread

set /a l_maximum_threads_minus_one=sk.nop77svk.async_multibatch.cfg.thread_pool_size-1
call :wait_for_active_threads_to_be_max %l_maximum_threads_minus_one%

set /a l_child_id=l_child_id+1
echo.>%TEMP%\sk.nop77svk.async_multibatch.%l_run_id%.%l_child_id%.lck

set sk.nop77svk.async_multibatch.arg.run=%l_run_id%
set sk.nop77svk.async_multibatch.arg.child=%l_child_id%
if "x%DEBUG%"=="xyes" echo.Spawning a run id %l_run_id%'s child #%l_child_id%>&2

set l_thread_window_start_size=
if "x%sk.nop77svk.async_multibatch.cfg.start_minimized%"=="xYES" set l_thread_window_start_size=/min
if "x%sk.nop77svk.async_multibatch.cfg.start_minimized%"=="xTRUE" set l_thread_window_start_size=/min
if "x%sk.nop77svk.async_multibatch.cfg.start_minimized%"=="xY" set l_thread_window_start_size=/min
if "x%sk.nop77svk.async_multibatch.cfg.start_minimized%"=="x1" set l_thread_window_start_size=/min

set l_args=%*
start %l_thread_window_start_size% cmd.exe /c "%g_this_script% %*"
set sk.nop77svk.async_multibatch.arg.run=
set sk.nop77svk.async_multibatch.arg.child=

exit /b 0

:: -----------------------------------------------------------------------------------------------
:wait_for_active_threads_to_be_max

set l_max_no_of_active_threads=%1
if "x%l_max_no_of_active_threads%"=="x" set l_max_no_of_active_threads=0

setlocal EnableExtensions
:loop
for /f %%a in ('dir /one /b %TEMP%\sk.nop77svk.async_multibatch.%l_run_id%.*.lck 2^>nul ^| %SystemRoot%\System32\find.exe /c ".lck"') do set l_active_threads=%%a
if "x%l_active_threads%"=="x" set l_active_threads=0
if "x%DEBUG%"=="xyes" echo.Active threads = %l_active_threads%, waiting for maximum of %l_max_no_of_active_threads% active threads >&2
if %l_active_threads% gtr %l_max_no_of_active_threads% (
	if "x%DEBUG%"=="xyes" (
		%SystemRoot%\system32\timeout.exe /t 1 /nobreak >&2
	) else (
		%SystemRoot%\system32\timeout.exe /t 1 /nobreak >nul
	)
	goto :loop
)
endlocal

exit /b 0

:: -----------------------------------------------------------------------------------------------
:to_upper_case
rem ref: https://stackoverflow.com/a/34734724/3706181

if not defined %~1 exit /b 1
for %%a in ("a=A" "b=B" "c=C" "d=D" "e=E" "f=F" "g=G" "h=H" "i=I" "j=J" "k=K" "l=L" "m=M" "n=N" "o=O" "p=P" "q=Q" "r=R" "s=S" "t=T" "u=U" "v=V" "w=W" "x=X" "y=Y" "z=Z") do call set %~1=%%%~1:%%~a%%
exit /b 0
