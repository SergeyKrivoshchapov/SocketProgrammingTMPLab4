@echo off
set PROJECT_ROOT=%CD%

if not exist "bin" mkdir bin

cd dll\task1\server && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task1_server.dll . && cd %PROJECT_ROOT%
cd dll\task1\client && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task1_client.dll . && cd %PROJECT_ROOT%
cd dll\task2\controller && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task2_controller.dll . && cd %PROJECT_ROOT%
cd dll\task2\dispatcher && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task2_dispatcher.dll . && cd %PROJECT_ROOT%
cd dll\task3\controller && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task3_controller.dll . && cd %PROJECT_ROOT%
cd dll\task3\dispatcher && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task3_dispatcher.dll . && cd %PROJECT_ROOT%

echo Done.