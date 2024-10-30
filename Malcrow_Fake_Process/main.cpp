// Malcrow Fake Process C++
#include <windows.h>
#include <tlhelp32.h>
#include <iostream>
#include <fstream>
#include <string>
#include <sstream>
#include <filesystem>

// Function to get the executable name without the path or extension
std::wstring getExecutableName() {
    wchar_t path[MAX_PATH];
    GetModuleFileNameW(NULL, path, MAX_PATH);
    std::wstring exePath(path);
    std::wstring exeName = std::filesystem::path(exePath).stem().wstring();
    return exeName;
}

void log(const std::wstring& message) {
    static std::wstring exeName = getExecutableName();  // Get the executable name once
    std::wofstream logFile("fakeproc_logs.txt", std::ios_base::app);
    logFile << "[" << exeName << L"] - " << message << std::endl;
}

bool isProcessRunning(const wchar_t* processName) {
    HANDLE hProcessSnap;
    PROCESSENTRY32W pe32;
    bool found = false;

    hProcessSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hProcessSnap == INVALID_HANDLE_VALUE) {
        return false;
    }

    pe32.dwSize = sizeof(PROCESSENTRY32W);
    if (!Process32FirstW(hProcessSnap, &pe32)) {
        CloseHandle(hProcessSnap);
        return false;
    }

    do {
        if (wcscmp(pe32.szExeFile, processName) == 0) {
            found = true;
            break;
        }
    } while (Process32NextW(hProcessSnap, &pe32));

    CloseHandle(hProcessSnap);
    return found;
}

void monitorParentProcess() {
    log(L"Entering monitor mode.");
    while (true) {
        if (!isProcessRunning(L"Malcrow.exe")) {
            log(L"Malcrow.exe is not running. Exiting monitor mode.");
            break;
        }
        Sleep(1000);
    }
}

void cloneAndShuffleHash(const wchar_t* newName) {
    log(L"Cloning and shuffling hash.");

    wchar_t currentPath[MAX_PATH];
    GetModuleFileNameW(NULL, currentPath, MAX_PATH);

    std::ifstream src(currentPath, std::ios::binary);
    std::ofstream dst(newName, std::ios::binary);

    dst << src.rdbuf();
    dst.put('\0');  // Add null byte to shuffle the hash

    src.close();
    dst.close();

    // Execute the new clone with a specific flag to indicate it's a clone
    STARTUPINFOW si = { sizeof(si) };
    PROCESS_INFORMATION pi;
    std::wstringstream command;
    command << newName << L" --clone";

    if (CreateProcessW(NULL, const_cast<LPWSTR>(command.str().c_str()), NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi)) {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
    else {
        log(L"Failed to launch the clone.");
    }

    log(L"Clone created and executed with new name: " + std::wstring(newName));
    exit(0);
}

void deleteSelf() {
    wchar_t selfPath[MAX_PATH];
    GetModuleFileNameW(NULL, selfPath, MAX_PATH);

    DWORD processId = GetCurrentProcessId();
    std::wstringstream batchFileName;
    batchFileName << L"delete_self_" << processId << L".bat";

    std::wstringstream batchCmd;
    batchCmd << L"@echo off\n"
        << L"timeout /t 4 /nobreak\n"
        << L"del \"" << selfPath << L"\"\n"
        << L"if exist \"" << selfPath << L"\" echo Failed to delete: Access is denied >> log.txt\n"
        << L"del %0";

    wchar_t tempPath[MAX_PATH];
    GetTempPathW(MAX_PATH, tempPath);
    std::wstring batchFile = std::wstring(tempPath) + batchFileName.str();

    std::wofstream batchScript(batchFile);
    batchScript << batchCmd.str();
    batchScript.close();

    STARTUPINFOW si = { sizeof(si) };
    PROCESS_INFORMATION pi;
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;

    if (CreateProcessW(NULL, const_cast<LPWSTR>(batchFile.c_str()), NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi)) {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
    else {
        log(L"Failed to launch the batch script for self-deletion.");
    }

    log(L"Clone scheduled to delete itself.");
    exit(0);
}

int wmain(int argc, wchar_t* argv[]) {
    if (argc > 1) {
        if (wcscmp(argv[1], L"--clone") == 0) {
            // If this is a clone, log and monitor the parent process
            log(L"Running as a clone.");
            monitorParentProcess();
            deleteSelf();
        }
        else {
            // If a new name is provided, clone and shuffle hash
            cloneAndShuffleHash(argv[1]);
        }
    }
    else {
        log(L"No arguments were passed, exiting.");
        exit(0);
    }
    return 0;
}
