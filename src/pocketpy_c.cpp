#ifndef PK_NO_EXPORT_C_API

#include "pocketpy.h"
#include "pocketpy_c.h"
#include <sstream> 
#include <regex>

using namespace pkpy;

typedef int (*LuaStyleFuncC)(VM*);

#define PK_ASSERT_N_EXTRA_ELEMENTS(n) \
    int __ex_count = count_extra_elements(vm, n); \
    if(__ex_count < n){ \
        Str msg = _S("expected at least ", n, " elements, got ", __ex_count); \
        pkpy_error(vm_handle, "StackError", pkpy_string(msg.c_str())); \
        return false; \
    }

#define PK_ASSERT_NO_ERROR() \
    if(vm->_c.error != nullptr) \
        return false;

static int count_extra_elements(VM* vm, int n){
    if(vm->callstack.empty()){
        return vm->s_data.size();
    }
    PK_ASSERT(!vm->_c.s_view.empty());
    return vm->s_data._sp - vm->_c.s_view.top().end();
}

static PyObject* stack_item(VM* vm, int index){
    PyObject** begin;
    PyObject** end = vm->s_data.end();
    if(vm->callstack.empty()){
        begin = vm->s_data.begin();
    }else{
        PK_ASSERT(!vm->_c.s_view.empty());
        begin = vm->_c.s_view.top().begin();
    }
    int size = end - begin;
    if(index < 0) index += size;
    if(index < 0 || index >= size){
        throw std::runtime_error("stack_item() => index out of range");
    }
    return begin[index];
}

#define PK_PROTECTED(__B) \
    try{ __B }  \
    catch(const Exception& e ) { \
        vm->_c.error = e.self(); \
        return false; \
    } catch(const std::exception& re){ \
        PyObject* e_t = vm->_t(vm->tp_exception); \
        vm->_c.error = vm->call(e_t, VAR(re.what())); \
        return false; \
    }

static std::string base64_encode(const std::string& in) {

    std::string out;

    int val = 0, valb = -6;
    for (unsigned char c : in) {
        val = (val << 8) + c;
        valb += 8;
        while (valb >= 0) {
            out.push_back("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"[(val >> valb) & 0x3F]);
            valb -= 6;
        }
    }
    if (valb > -6) out.push_back("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"[((val << 8) >> (valb + 8)) & 0x3F]);
    while (out.size() % 4) out.push_back('=');
    return out;
}

static std::string base64_decode(const std::string& in) {

    std::string out;

    std::vector<int> T(256, -1);
    for (int i = 0; i < 64; i++) T["ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"[i]] = i;

    int val = 0, valb = -8;
    for (unsigned char c : in) {
        if (T[c] == -1) break;
        val = (val << 6) + T[c];
        valb += 6;
        if (valb >= 0) {
            out.push_back(unsigned char((val >> valb) & 0xFF));
            valb -= 8;
        }
    }
    return out;
}


pkpy_vm* pkpy_new_vm(bool enable_os){
    return (pkpy_vm*)new VM(enable_os);
}

void pkpy_delete_vm(pkpy_vm* vm){
    return delete (VM*)vm;
}

bool pkpy_exec(pkpy_vm* vm_handle, const char* source) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* res;
    PK_PROTECTED(
        CodeObject_ code = vm->compile(source, "main.py", EXEC_MODE);
        res = vm->_exec(code, vm->_main);
    )
    return res != nullptr;
}

bool pkpy_exec_2(pkpy_vm* vm_handle, const char* source, const char* filename, int mode, const char* module){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* res;
    PyObject* mod;
    PK_PROTECTED(
        if(module == nullptr){
            mod = vm->_main;
        }else{
            mod = vm->_modules[module];     // may raise
        }
        CodeObject_ code = vm->compile(source, filename, (CompileMode)mode);
        res = vm->_exec(code, mod);
    )
    return res != nullptr;
}

bool pkpy_dup(pkpy_vm* vm_handle, int n){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, n);
        vm->s_data.push(item);
    )
    return true;
}

bool pkpy_pop(pkpy_vm* vm_handle, int n){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(n)
    vm->s_data.shrink(n);
    return true;
}

bool pkpy_pop_top(pkpy_vm* vm_handle){
    VM* vm = (VM*)vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    vm->s_data.pop();
    return true;
}

bool pkpy_dup_top(pkpy_vm* vm_handle){
    VM* vm = (VM*)vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    vm->s_data.push(vm->s_data.top());
    return true;
}

bool pkpy_rot_two(pkpy_vm* vm_handle){
    VM* vm = (VM*)vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(2)
    std::swap(vm->s_data.top(), vm->s_data.second());
    return true;
}

int pkpy_stack_size(pkpy_vm* vm_handle){
    VM* vm = (VM*)vm_handle;
    PK_ASSERT_NO_ERROR()
    if(vm->callstack.empty()){
        return vm->s_data.size();
    }
    if(vm->_c.s_view.empty()) exit(127);
    return vm->s_data._sp - vm->_c.s_view.top().begin();
}

// int
bool pkpy_push_int(pkpy_vm* vm_handle, int value) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* res;
    PK_PROTECTED(
        // int may overflow so we should protect it
        res = py_var(vm, value);
    )
    vm->s_data.push(res);
    return true;
}

bool pkpy_is_int(pkpy_vm* vm_handle, int i){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        return is_int(stack_item(vm, i));
    )
}

bool pkpy_to_int(pkpy_vm* vm_handle, int i, int* out){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        *out = py_cast<int>(vm, item);
    )
    return true;
}

// float
bool pkpy_push_float(pkpy_vm* vm_handle, double value) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* res = py_var(vm, value);
    vm->s_data.push(res);
    return true;
}

bool pkpy_is_float(pkpy_vm* vm_handle, int i){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        return is_float(item);
    )
}

bool pkpy_to_float(pkpy_vm* vm_handle, int i, double* out){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        *out = py_cast<double>(vm, item);
    )
    return true;
}

// bool
bool pkpy_push_bool(pkpy_vm* vm_handle, bool value) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    vm->s_data.push(value ? vm->True : vm->False);
    return true;
}

bool pkpy_is_bool(pkpy_vm* vm_handle, int i){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        return is_non_tagged_type(item, vm->tp_bool);
    )
}

bool pkpy_to_bool(pkpy_vm* vm_handle, int i, bool* out){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        *out = py_cast<bool>(vm, item);
    )
    return true;
}

// string
bool pkpy_push_string(pkpy_vm* vm_handle, pkpy_CString value) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* res = py_var(vm, value);
    vm->s_data.push(res);
    return true;
}

bool pkpy_is_string(pkpy_vm* vm_handle, int i){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        return is_non_tagged_type(item, vm->tp_str);
    )
}

bool pkpy_to_string(pkpy_vm* vm_handle, int i, pkpy_CString* out){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        const Str& s = py_cast<Str&>(vm, item);
        *out = s.c_str();
    )
    return true;
}

// void_p
bool pkpy_push_voidp(pkpy_vm* vm_handle, void* value) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* res = py_var(vm, value);
    vm->s_data.push(res);
    return true;
}

bool pkpy_is_voidp(pkpy_vm* vm_handle, int i){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        return is_non_tagged_type(item, VoidP::_type(vm));
    )
}

bool pkpy_to_voidp(pkpy_vm* vm_handle, int i, void** out){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        VoidP& vp = py_cast<VoidP&>(vm, item);
        *out = vp.ptr;
    )
    return true;
}

// none
bool pkpy_push_none(pkpy_vm* vm_handle) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    vm->s_data.push(vm->None);
    return true;
}

bool pkpy_is_none(pkpy_vm* vm_handle, int i){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* item = stack_item(vm, i);
        return item == vm->None;
    )
}

// null
bool pkpy_push_null(pkpy_vm* vm_handle) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    vm->s_data.push(PY_NULL);
    return true;
}

struct TempViewPopper{
    VM* vm;
    bool used;

    TempViewPopper(VM* vm): vm(vm), used(false) {}

    void restore() noexcept{
        if(used) return;
        vm->_c.s_view.pop();
        used = true;
    }

    ~TempViewPopper(){ restore(); }
};

// function
static PyObject* c_function_wrapper(VM* vm, ArgsView args) {
    LuaStyleFuncC f = lambda_get_userdata<LuaStyleFuncC>(args.begin());
    PyObject** curr_sp = vm->s_data._sp;

    vm->_c.s_view.push(args);
    TempViewPopper _tvp(vm);
    int retc = f(vm);       // may raise, _tvp will handle this via RAII
    _tvp.restore();

    // propagate_if_errored
    if (vm->_c.error != nullptr){
        PyObject* e_obj = PK_OBJ_GET(Exception, vm->_c.error).self();
        vm->_c.error = nullptr;
        vm->_error(e_obj);
        return nullptr;
    }
    PK_ASSERT(retc == vm->s_data._sp-curr_sp);
    if(retc == 0) return vm->None;
    if (retc == 1) return vm->s_data.popx();
    ArgsView ret_view(curr_sp, vm->s_data._sp);
    return py_var(vm, ret_view.to_tuple());
}

bool pkpy_push_function(pkpy_vm* vm_handle, const char* sig, pkpy_CFunction f) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* f_obj;
    PK_PROTECTED(
        f_obj = vm->bind(
            nullptr,
            sig,
            nullptr,
            c_function_wrapper,
            f
        );
    )
    vm->s_data.push(f_obj);
    return true;
}

// special push
bool pkpy_push_module(pkpy_vm* vm_handle, const char* name) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        PyObject* module = vm->new_module(name);
        vm->s_data.push(module);
    )
    return true;
}

// some opt
bool pkpy_getattr(pkpy_vm* vm_handle, pkpy_CName name) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    PyObject* o = vm->s_data.top();
    o = vm->getattr(o, StrName(name), false);
    if(o == nullptr) return false;
    vm->s_data.top() = o;
    return true;
}

bool pkpy_setattr(pkpy_vm* vm_handle, pkpy_CName name) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(2)
    PyObject* a = vm->s_data.top();
    PyObject* val = vm->s_data.second();
    PK_PROTECTED(
        vm->setattr(a, StrName(name), val);
    )
    vm->s_data.shrink(2);
    return true;
}

//get global will also get bulitins
bool pkpy_getglobal(pkpy_vm* vm_handle, pkpy_CName name) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* o = vm->_main->attr().try_get(StrName(name));
    if (o == nullptr) {
        o = vm->builtins->attr().try_get(StrName(name));
        if (o == nullptr) return false;
    }
    vm->s_data.push(o);
    return true;
}

bool pkpy_setglobal(pkpy_vm* vm_handle, pkpy_CName name) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    vm->_main->attr().set(StrName(name), vm->s_data.popx());
    return true;
}

bool pkpy_eval(pkpy_vm* vm_handle, const char* source) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_PROTECTED(
        CodeObject_ co = vm->compile(source, "<eval>", EVAL_MODE);
        PyObject* ret = vm->_exec(co, vm->_main);
        vm->s_data.push(ret);
    )
    return true;
}

bool pkpy_unpack_sequence(pkpy_vm* vm_handle, int n) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    auto _lock = vm->heap.gc_scope_lock();
    PK_PROTECTED(
        PyObject* _0 = vm->py_iter(vm->s_data.popx());
        for(int i=0; i<n; i++){
            PyObject* _1 = vm->py_next(_0);
            if(_1 == vm->StopIteration) vm->ValueError("not enough values to unpack");
            vm->s_data.push(_1);
        }
        if(vm->py_next(_0) != vm->StopIteration) vm->ValueError("too many values to unpack");
    )
    return true;
}

bool pkpy_get_unbound_method(pkpy_vm* vm_handle, pkpy_CName name){
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    PyObject* o = vm->s_data.top();
    PyObject* self;
    PK_PROTECTED(
        o = vm->get_unbound_method(o, StrName(name), &self);
    )
    vm->s_data.pop();
    vm->s_data.push(o);
    vm->s_data.push(self);
    return true;
}

bool pkpy_py_repr(pkpy_vm* vm_handle) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    PyObject* item = vm->s_data.top();
    PK_PROTECTED(
        item = vm->py_repr(item);
    )
    vm->s_data.top() = item;
    return true;
}

bool pkpy_py_str(pkpy_vm* vm_handle) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(1)
    PyObject* item = vm->s_data.top();
    PK_PROTECTED(
        item = vm->py_str(item);
    )
    vm->s_data.top() = item;
    return true;
}

/* Error Handling */
bool pkpy_error(pkpy_vm* vm_handle, const char* name, pkpy_CString message) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PyObject* e_t = vm->_main->attr().try_get_likely_found(name);
    if(e_t == nullptr){
        e_t = vm->builtins->attr().try_get_likely_found(name);
        if(e_t == nullptr){
            e_t = vm->_t(vm->tp_exception);
            std::cerr << "[warning] pkpy_error(): " << Str(name).escape() << " not found, fallback to 'Exception'" << std::endl;
        }
    }
    vm->_c.error = vm->call(e_t, VAR(message));
    return false;
}

bool pkpy_check_error(pkpy_vm* vm_handle) {
    VM* vm = (VM*) vm_handle;
    return vm->_c.error != nullptr;
}

bool pkpy_clear_error(pkpy_vm* vm_handle, char** message) {
    VM* vm = (VM*) vm_handle;
    // no error
    if (vm->_c.error == nullptr) return false;
    Exception& e = PK_OBJ_GET(Exception, vm->_c.error);
    if (message != nullptr)
        *message = strdup(e.summary().c_str());
    else
        std::cout << e.summary() << std::endl;
    vm->_c.error = nullptr;
    if(vm->callstack.empty()){
        vm->s_data.clear();
    }else{
        if(vm->_c.s_view.empty()) exit(127);
        vm->s_data.reset(vm->_c.s_view.top().end());
    }
    return true;
}

bool pkpy_vectorcall(pkpy_vm* vm_handle, int argc) {
    VM* vm = (VM*) vm_handle;
    PK_ASSERT_NO_ERROR()
    PK_ASSERT_N_EXTRA_ELEMENTS(argc + 2)
    PyObject* res;
    PK_PROTECTED(
        res = vm->vectorcall(argc);
    )
    vm->s_data.push(res);
    return true;
}
/*****************************************************************/
void pkpy_free(void* p){
    free(p);
}

pkpy_CName pkpy_name(const char* name){
    return StrName(name).index;
}

pkpy_CString pkpy_name_to_string(pkpy_CName name){
    return StrName(name).c_str();
}

void pkpy_set_output_handler(pkpy_vm* vm_handle, pkpy_COutputHandler handler){
    VM* vm = (VM*) vm_handle;
    vm->_stdout = handler;
}

void pkpy_set_import_handler(pkpy_vm* vm_handle, pkpy_CImportHandler handler){
    VM* vm = (VM*) vm_handle;
    vm->_import_handler = handler;
}

void* pkpy_new_repl(pkpy_vm* vm_handle){
    return new REPL((VM*)vm_handle);
}

bool pkpy_repl_input(void* r, const char* line){
    return ((REPL*)r)->input(line);
}

void pkpy_delete_repl(void* repl){
    delete (REPL*)repl;
}

void replace_all(std::string& str, const std::string& from, const std::string& to) {
    if (from.empty())
        return;
    size_t start_pos = 0;
    while ((start_pos = str.find(from, start_pos)) != std::string::npos) {
        str.replace(start_pos, from.length(), to);
        start_pos += to.length(); // In case 'to' contains 'from', like replacing 'x' with 'yx'
    }
}

void pkpy_compile_to_string(pkpy_vm* vm_handle, const char* source, const char* filename, int mode, bool unknown_global_scope, bool* ok, char** out) {
    VM* vm = (VM*)vm_handle;
    pkpy_clear_error(vm_handle, NULL);

    try {
        std::string decoded = base64_decode(source);

        std::vector<std::string> s_20_7f = { " ", "!", "\"", "#", "$", " % ", " & ", "'", "(", ")", "*", "+", ",", "-", ".", "/", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ":", ";", "<", "=", ">", "?", "@", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "[", "\\", "]", "^", "_", "`", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "{", "|", "}", "~", ""};
        std::vector<std::string> s_80_ff = {"€", "", "‚", "ƒ", "„", "…", "†", "‡", "ˆ", "‰", "Š", "‹", "Œ", "", "Ž", "", "", "‘", "’", "“", "”", "•", "–", "—", "˜", "™", "š", "›", "œ", "", "ž", "Ÿ", " ", "¡", "¢", "£", "¤", "¥", "¦", "§", "¨", "©", "ª", "«", "¬", "­", "®", "¯", "°", "±", "²", "³", "´", "µ", "¶", "·", "¸", "¹", "º", "»", "¼", "½", "¾", "¿", "À", "Á", "Â", "Ã", "Ä", "Å", "Æ", "Ç", "È", "É", "Ê", "Ë", "Ì", "Í", "Î", "Ï", "Ð", "Ñ", "Ò", "Ó", "Ô", "Õ", "Ö", "×", "Ø", "Ù", "Ú", "Û", "Ü", "Ý", "Þ", "ß", "à", "á", "â", "ã", "ä", "å", "æ", "ç", "è", "é", "ê", "ë", "ì", "í", "î", "ï", "ð", "ñ", "ò", "ó", "ô", "õ", "ö", "÷", "ø", "ù", "ú", "û", "ü", "ý", "þ", "ÿ"};

        std::regex pattern("\\\\x([0-9A-Fa-f]{2})");
        std::smatch matches;
        std::vector<std::string> to_replace;
        
        std::string tmp = decoded;
        while (std::regex_search(tmp, matches, pattern)) {
            for (size_t i = 1; i < matches.size(); ++i) {
                std::string hex = matches.str(i);
                to_replace.push_back(hex);
            }
            tmp = matches.suffix().str();
        }

        for (int i = 0; i < to_replace.size(); i++) {
            std::string hex = to_replace[i];
            int decimal = std::stoi(hex, nullptr, 16);
            if (decimal >= 32 && decimal <= 127) {
                std::string changed = s_20_7f[decimal - 32];
                replace_all(decoded, "\\x" + hex, changed);
            }
            else if (decimal >= 128 && decimal <= 255) {
                std::string changed = s_80_ff[decimal - 128];
                replace_all(decoded, "\\x" + hex, changed);
            }
        }
        CodeObject_ code = vm->compile(decoded, filename, (CompileMode)mode, unknown_global_scope);
        std::string compiled = code->serialize(vm).str();
        std::string encoded = base64_encode(code->serialize(vm).str());
        *out = strdup(encoded.c_str());
        *ok = true;
    }
    catch (Exception& e) {
        *ok = false;
        std::string encoded = base64_encode(e.summary().str());
        *out = strdup(encoded.c_str());
    }
    catch (std::exception& e) {
        *ok = false;
        std::string encoded = base64_encode(e.what());
        *out = strdup(encoded.c_str());
    }
    catch (...) {
        *ok = false;
        std::string encoded = base64_encode("unknown error");
        *out = strdup(encoded.c_str());
    }
}

#endif // PK_NO_EXPORT_C_API