#include "cJSONw.hpp"

namespace pkpy{


static cJSON* convert_python_object_to_cjson(PyObject* obj, VM* vm);
static PyObject* convert_cjson_to_python_object(const cJSON * const item, VM* vm);


static cJSON* convert_list_to_cjson(const List& list, VM* vm){
    cJSON *cjson_list = cJSON_CreateArray();
    for(auto& element : list){
        cJSON_AddItemToArray(cjson_list, convert_python_object_to_cjson(element, vm));
    }
    return cjson_list;
}

static cJSON* convert_tuple_to_cjson(const Tuple& tuple, VM* vm){
    cJSON *cjson_list = cJSON_CreateArray();
    for(auto& element : tuple){
        cJSON_AddItemToArray(cjson_list, convert_python_object_to_cjson(element, vm));
    }
    return cjson_list;
}

static cJSON* covert_dict_to_cjson(const Dict& dict, VM* vm){
    cJSON *cjson_object = cJSON_CreateObject();
    dict.apply([&](PyObject* key, PyObject* val){
        cJSON_AddItemToObject(cjson_object, CAST(Str&, key).c_str(), convert_python_object_to_cjson(val, vm));
    });
    return cjson_object;
}

static cJSON* convert_python_object_to_cjson(PyObject* obj, VM* vm){
    Type obj_t = vm->_tp(obj);
    if (obj_t == vm->tp_int){
        return cJSON_CreateNumber(_CAST(i64, obj));
    }
    else if (obj_t == vm->tp_float){
        return cJSON_CreateNumber(_CAST(f64, obj));
    }
    else if (obj_t == vm->tp_bool){
        return cJSON_CreateBool(obj == vm->True);
    }
    else if (obj_t == vm->tp_str){
        return cJSON_CreateString(_CAST(Str&, obj).c_str());
    }
    else if (obj_t == vm->tp_dict){
        return covert_dict_to_cjson(_CAST(Dict&, obj), vm);
    }
    else if (obj_t == vm->tp_list){
        return convert_list_to_cjson(_CAST(List&, obj), vm);
    }
    else if(obj_t == vm->tp_tuple){
        return convert_tuple_to_cjson(_CAST(Tuple&, obj), vm);
    }else if(obj == vm->None){
        return cJSON_CreateNull();
    }else{
        vm->TypeError(fmt("cjson: unrecognized type ", obj_type_name(vm, obj_t)));
    }
    UNREACHABLE();
}


static PyObject* convert_cjson_to_list(const cJSON * const item, VM* vm){
    List output;
    cJSON *element = item->child;
    while(element != NULL){
        output.push_back(convert_cjson_to_python_object(element, vm));
        element = element->next;
    }
    return VAR(std::move(output));
}

static PyObject* convert_cjson_to_dict(const cJSON* const item, VM* vm){
    Dict output(vm);
    cJSON *child = item->child;
    while(child != NULL){
        const char* key = child->string;
        const cJSON *child_value = cJSON_GetObjectItemCaseSensitive(item, key);
        output.set(VAR(key), convert_cjson_to_python_object(child_value, vm));
        child = child->next;
    }
    return VAR(std::move(output));
}

static PyObject* convert_cjson_to_python_object(const cJSON * const item, VM* vm)
{
    if (cJSON_IsString(item))
    {
        return VAR(Str(item->valuestring));
    }
    else if (cJSON_IsNumber(item)){
        if(item->valuedouble != item->valueint){
            return VAR(item->valuedouble);
        }
        return VAR(item->valueint);
    }
    else if (cJSON_IsBool(item)){
        return item->valueint!=0 ? vm->True : vm->False;
    }
    else if (cJSON_IsNull(item)){
        return vm->None;
    }
    else if (cJSON_IsArray(item)){
        return convert_cjson_to_list(item, vm);
    }
    else if (cJSON_IsObject(item)){
        return convert_cjson_to_dict(item, vm);
    }
    return vm->None;
}

void add_module_cjson(VM* vm){
    PyObject* mod = vm->new_module("cjson");

    PK_LOCAL_STATIC cJSON_Hooks hooks;
    hooks.malloc_fn = pool64_alloc;
    hooks.free_fn = pool64_dealloc;
    cJSON_InitHooks(&hooks);

    vm->bind_func<1>(mod, "loads", [](VM* vm, ArgsView args){
        const Str& string = CAST(Str&, args[0]);
        cJSON *json = cJSON_ParseWithLength(string.data, string.size);
        if(json == NULL){
            const char* err = cJSON_GetErrorPtr();
            vm->TypeError(fmt("cjson: ", err));
            UNREACHABLE();
        }
        PyObject* output = convert_cjson_to_python_object(json, vm);
        cJSON_Delete(json);
        return output;
    });

    vm->bind_func<1>(mod, "dumps", [](VM* vm, ArgsView args) {
        cJSON* cjson = convert_python_object_to_cjson(args[0], vm);
        char* str = cJSON_Print(cjson);
        cJSON_Delete(cjson);
        return VAR(Str(str));
    });
}

}   // namespace pkpy