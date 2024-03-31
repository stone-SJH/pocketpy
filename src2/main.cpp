#include <fstream>
#include <filesystem>
#include <iostream>
#include <sstream>

#if __has_include("pocketpy_c.h")
    #include "pocketpy_c.h"
#else
    #include "pocketpy.h"
#endif

#ifdef _WIN32

#include <windows.h>

std::string pkpy_platform_getline(bool* eof){
    HANDLE hStdin = GetStdHandle(STD_INPUT_HANDLE);
    std::wstringstream wss;
    WCHAR buf;
    DWORD read;
    while (ReadConsoleW(hStdin, &buf, 1, &read, NULL) && buf != L'\n') {
        if(eof && buf == L'\x1A') *eof = true;  // Ctrl+Z
        wss << buf;
    }
    std::wstring wideInput = wss.str();
    int length = WideCharToMultiByte(CP_UTF8, 0, wideInput.c_str(), (int)wideInput.length(), NULL, 0, NULL, NULL);
    std::string output;
    output.resize(length);
    WideCharToMultiByte(CP_UTF8, 0, wideInput.c_str(), (int)wideInput.length(), &output[0], length, NULL, NULL);
    if(!output.empty() && output.back() == '\r') output.pop_back();
    return output;
}

#else

std::string pkpy_platform_getline(bool* eof){
    std::string output;
    if(!std::getline(std::cin, output)){
        if(eof) *eof = true;
    }
    return output;
}

#endif

static int f_input(pkpy_vm* vm){
    if(!pkpy_is_none(vm, -1)){
        pkpy_CString prompt;
        bool ok = pkpy_to_string(vm, -1, &prompt);
        if(!ok) return 0;
        std::cout << prompt << std::flush;
    }
    bool eof;
    std::string output = pkpy_platform_getline(&eof);
    pkpy_push_string(vm, pkpy_string(output.c_str()));
    return 1;
}

int main(){
#if _WIN32
    SetConsoleCP(CP_UTF8);
    SetConsoleOutputCP(CP_UTF8);
#endif
    pkpy_vm* vm = pkpy_new_vm(true);

    pkpy_push_function(vm, "input(prompt=None) -> str", f_input);
    pkpy_eval(vm, "__import__('builtins')");
    pkpy_setattr(vm, pkpy_name("input"));

    const char* src = R"(
a = 1e-2
print(a)
b = 1e+3
print(b)
c = 1e4
print(c)

def test(a, b, c = " "):
    try:
        print(a)
        print(b, exp=" ")
        print(c)
    except:
        print(b, exp21=" ")
try:
    i = 1
    print(i)
    j = 2
    print(j)
    t = 3
    print(t)
    test(i, j)
except:
    i = 11
    print(i)
    j = 21
    print(j)
    t = 31
    print(t)
    print('xxx')
print('done')
)";
    const char* src2 = R"(YSA9IGInXHgyMFx4MjFceDIyXHgyM1x4MjRceDI1XHgyNlx4MjdceDI4XHgyOVx4MmFceDJiXHgyY1x4MmRceDJlXHgyZlx4MzBceDMxXHgzMlx4MzNceDM0XHgzNVx4MzZceDM3XHgzOFx4MzlceDNhXHgzYlx4M2NceDNkXHgzZVx4M2ZceDQwXHg0MVx4NDJceDQzXHg0NFx4NDVceDQ2XHg0N1x4NDhceDQ5XHg0YVx4NGJceDRjXHg0ZFx4NGVceDRmXHg1MFx4NTFceDUyXHg1M1x4NTRceDU1XHg1Nlx4NTdceDU4XHg1OVx4NWFceDViXHg1Y1x4NWRceDVlXHg1Zlx4NjBceDYxXHg2Mlx4NjNceDY0XHg2NVx4NjZceDY3XHg2OFx4NjlceDZhXHg2Ylx4NmNceDZkXHg2ZVx4NmZceDcwXHg3MVx4NzJceDczXHg3NFx4NzVceDc2XHg3N1x4NzhceDc5XHg3YVx4N2JceDdjXHg3ZFx4N2VceDdmXHg4MFx4ODFceDgyXHg4M1x4ODRceDg1XHg4Nlx4ODdceDg4XHg4OVx4OGFceDhiXHg4Y1x4OGRceDhlXHg4Zlx4OTBceDkxXHg5Mlx4OTNceDk0XHg5NVx4OTZceDk3XHg5OFx4OTlceDlhXHg5Ylx4OWNceDlkXHg5ZVx4OWZceGEwXHhhMVx4YTJceGEzXHhhNFx4YTVceGE2XHhhN1x4YThceGE5XHhhYVx4YWJceGFjXHhhZFx4YWVceGFmXHhiMFx4YjFceGIyXHhiM1x4YjRceGI1XHhiNlx4YjdceGI4XHhiOVx4YmFceGJiXHhiY1x4YmRceGJlXHhiZlx4YzBceGMxXHhjMlx4YzNceGM0XHhjNVx4YzZceGM3XHhjOFx4YzlceGNhXHhjYlx4Y2NceGNkXHhjZVx4Y2ZceGQwXHhkMVx4ZDJceGQzXHhkNFx4ZDVceGQ2XHhkN1x4ZDhceGQ5XHhkYVx4ZGJceGRjXHhkZFx4ZGVceGRmXHhlMFx4ZTFceGUyXHhlM1x4ZTRceGU1XHhlNlx4ZTdceGU4XHhlOVx4ZWFceGViXHhlY1x4ZWRceGVlXHhlZlx4ZjBceGYxXHhmMlx4ZjNceGY0XHhmNVx4ZjZceGY3XHhmOFx4ZjlceGZhXHhmYlx4ZmNceGZkXHhmZVx4ZmYnCg==)";
    const char* src3 = R"(
x = "lily"
y = 16
z = 163.5

#s = f"{x+'{'+ '}' + '{x}'}, {y+y}, {z+z}"
#print(s)
d = "%s, %d, %.1f"   %[(x, y), z]
d = "%s, %d, %.1f"   %{('332)', y, z}
#s = '%sssss'%x
#s = '%sxx321321xx'%xxx23
#s = '%sxxx5azzf_%$#@^%$+@^$#@+_321x||#@|£¡#|@£¡|£¤£¡@|£¤|£¡@'%xxx23+'11'
)";
    const char* filename = "pydemo.py";
    char* out;
    bool ok;
    //pkpy_compile_to_string(vm, src2, filename, 0, true, &ok, &out);

    pkpy_exec(vm, src3);
    //pkpy_compile_to_string(vm, )
    /*


    if(argc == 1){
        void* repl = pkpy_new_repl(vm);
        bool need_more_lines = false;
        while(true){
            std::cout << (need_more_lines ? "... " : ">>> ");
            bool eof = false;
            std::string line = pkpy_platform_getline(&eof);
            if(eof) break;
            need_more_lines = pkpy_repl_input(repl, line.c_str());
        }
        pkpy_delete_vm(vm);
        return 0;
    }
    
    if(argc == 2){
        std::string argv_1 = argv[1];
        if(argv_1 == "-h" || argv_1 == "--help") goto __HELP;

        std::filesystem::path filepath(argv[1]);
        filepath = std::filesystem::absolute(filepath);
        if(!std::filesystem::exists(filepath)){
            std::cerr << "File not found: " << argv_1 << std::endl;
            return 2;
        }        
        std::ifstream file(filepath);
        if(!file.is_open()){
            std::cerr << "Failed to open file: " << argv_1 << std::endl;
            return 3;
        }
        std::string src((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
        file.close();

        // set parent path as cwd
        std::filesystem::current_path(filepath.parent_path());

        bool ok = pkpy_exec_2(vm, src.c_str(), filepath.filename().string().c_str(), 0, NULL);
        if(!ok) pkpy_clear_error(vm, NULL);
        pkpy_delete_vm(vm);
        return ok ? 0 : 1;
    }
*/
__HELP:
    std::cout << "Usage: pocketpy [filename]" << std::endl;
    return 0;
}
