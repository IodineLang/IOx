@echo off
pushd iodine
call :pup
call build_iodine.bat
popd
call :pup
pause
exit

:pup
git pull origin master
git submodule init
git submodule update
git submodule status
exit /b 0