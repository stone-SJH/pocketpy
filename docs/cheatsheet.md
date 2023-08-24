---
icon: log
title: 'Cheat sheet'
order: 22
---

## Basics

Setup pocketpy

```cpp
#include "pocketpy.h"
```

Create a python virtual machine

```cpp
VM* vm = new VM();
```

Dispose a python virtual machine

```cpp
delete vm;
```

Execute a source string

```cpp
vm->exec("print('Hello!')");
```

Evaluate a source string

```cpp
PyObject* obj = vm->eval("123");
std::cout << CAST(i64, obj);  // 123
```

Compile a source string into a code object

```cpp
CodeObject_ co = vm->compile("print('Hello!')", "main.py", EXEC_MODE);
```

Execute a compiled code object

```cpp
try{
    vm->_exec(co);    // may throw
}catch(Exception& e){
    std::cerr << e.summary() << std::endl;
}
```

## Interop with native types

Create primitive objects

```cpp
PyObject* obj;
obj = VAR(1);			// create a int
obj = VAR(1.0);		// create a float
obj = VAR("123");	// create a string
obj = VAR(true);	// create a bool
```

Create a tuple object

```cpp
// obj = (1, 1.0, '123')
Tuple t(3);
t[0] = VAR(1);
t[1] = VAR(1.0);
t[2] = VAR("123");
PyObject* obj = VAR(std::move(t));
```

Create a list object

```cpp
// obj = [1, 1.0, '123']
List t;
t.push_back(VAR(1));
t.push_bask(VAR(1.0));
t.push_back(VAR("123"));
PyObject* obj = VAR(std::move(t));
```

Create a dict object

```cpp
// obj = {'x': 1, 'y': '123'}
Dict d(vm);
d.set(VAR('x'), VAR(1));
d.set(VAR('y'), VAR('123'));
PyObject* obj = VAR(std::move(d));
```

Get native types from python objects

```cpp
PyObject* obj;
i64 a = CAST(i64, obj);
f64 b = CAST(f64, obj);
Str& c = CAST(Str&, obj);			// reference cast
bool d = CAST(bool, obj);

Tuple& e = CAST(Tuple&, obj);	// reference cast
List& f = CAST(List&, obj);		// reference cast
Dict& g = CAST(Dict&, obj);		// reference cast
```

## Access python types

Access built-in python types

```cpp
PyObject* int_t = vm->_t(vm->tp_int);
PyObject* float_t = vm->_t(vm->tp_float);
PyObject* object_t = vm->_t(vm->tp_object);
PyObject* tuple_t = vm->_t(vm->tp_tuple);
PyObject* list_t = vm->_t(vm->tp_list);
```

Access extended python types

```cpp
PyObject* voidp_t = VoidP::_type(vm);
```

## Access attributes

Check an object supports attribute access

```cpp
PyObject* obj;
bool ok = !is_tagged(obj) && obj->is_attr_valid();
```

```python
class MyClass:
  def __init__(self, x, y):
    self.x = x
    self.y = y

  def sum(self):
    return self.x + self.y
```

Get and set attributes

```cpp
PyObject* obj = vm->exec("MyClass(1, 2)");
PyObject* x = vm->getattr(obj, "x");	// obj.x
vm->setattr(obj, "x", VAR(3));				// obj.x = 3
```

## Call python functions

```python
def add(a, b):
  return a + b 
```

```cpp
PyObject* f_add = vm->eval("add");
PyObject* ret = vm->call(f_add, VAR(1), VAR(2));
std::cout << CAST(i64, ret);	// 3
```

## Call python methods

```cpp
PyObject* obj = vm->exec("MyClass(1, 2)");
PyObject* ret = vm->call_method(obj, "sum");
std::cout << CAST(i64, ret);	// 3
```

## Cache python names

```cpp
// cache the name "add" to avoid string-based lookup
const static StrName m_sum("sum");
PyObject* ret = vm->call_method(obj, m_sum);
```

## Bind native functions

```cpp
vm->bind(obj, "add(a: int, b: int) -> int", [](VM* vm, ArgsView args){
    int a = CAST(int, args[0]);
    int b = CAST(int, args[1]);
    return VAR(a + b);
});

// or you can provide a docstring
vm->bind(obj,
    "add(a: int, b: int) -> int",
    "add two integers", [](VM* vm, ArgsView args){
    int a = CAST(int, args[0]);
    int b = CAST(int, args[1]);
    return VAR(a + b);
});
```

## Bind native properties

```cpp
    // getter and setter of property `x`
    vm->bind_property(type, "x: int",
      [](VM* vm, ArgsView args){
          Point& self = CAST(Point&, args[0]);
          return VAR(self.x);
      },
      [](VM* vm, ArgsView args){
          Point& self = CAST(Point&, args[0]);
          self.x = CAST(int, args[1]);
          return vm->None;
      });
```