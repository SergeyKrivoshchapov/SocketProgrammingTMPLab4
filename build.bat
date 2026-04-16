@echo off
set PROJECT_ROOT=%CD%

if not exist "bin" mkdir bin

cd dll\task1\server && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task1_server.dll export.go server.go && cd %PROJECT_ROOT%
cd dll\task1\client && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task1_client.dll export.go client.go && cd %PROJECT_ROOT%
cd dll\task2\controller && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task2_controller.dll export.go controller.go && cd %PROJECT_ROOT%
cd dll\task2\dispatcher && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task2_dispatcher.dll export.go client.go && cd %PROJECT_ROOT%
cd dll\task3\controller && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task3_controller.dll export.go controller.go && cd %PROJECT_ROOT%
cd dll\task3\dispatcher && go build -buildmode=c-shared -o %PROJECT_ROOT%\bin\task3_dispatcher.dll export.go client.go && cd %PROJECT_ROOT%

cd cmd\task1_test_server && go build -o %PROJECT_ROOT%\bin\test_task1_server.exe && cd %PROJECT_ROOT%
cd cmd\task1_test_client && go build -o %PROJECT_ROOT%\bin\test_task1_client.exe && cd %PROJECT_ROOT%
cd cmd\task2_test_controller && go build -o %PROJECT_ROOT%\bin\test_task2_controller.exe && cd %PROJECT_ROOT%
cd cmd\task2_test_dispatcher && go build -o %PROJECT_ROOT%\bin\test_task2_dispatcher.exe && cd %PROJECT_ROOT%
cd cmd\task3_test_controller && go build -o %PROJECT_ROOT%\bin\test_task3_controller.exe && cd %PROJECT_ROOT%
cd cmd\task3_test_dispatcher && go build -o %PROJECT_ROOT%\bin\test_task3_dispatcher.exe && cd %PROJECT_ROOT%

echo Done.