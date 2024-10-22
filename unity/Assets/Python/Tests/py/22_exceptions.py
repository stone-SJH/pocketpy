try:
    raise 1
except TypeError:
    pass

try:
    raise KeyError
except KeyError:
    pass

x = 0
try:
    assert False
    x = 1
    assert False
except AssertionError:
    pass
assert x == 0

try:
    for i in range(5):
        raise KeyError(i)
    assert False
except KeyError:
    pass

x = 0
for i in range(5):
    try:
        for j in range(5):
            while True:
                raise KeyError(i)
                x += 3
    except KeyError:
        x += i
assert x == 10

x = 0
y = 0
for i in range(5):
    try:
        t = 1
        for j in range(5):
            while True:
                v1 = 1
                v2 = 21
                v3 = 31
                y += v1 + v2 + v3
                raise KeyError(i)
                x += 3
    except KeyError:
        z = i
        x += z

assert y == 265
assert x == 10


class A:
    def __getitem__(self, i):
        raise KeyError(i)

try:
    a = A()
    b = a[1]
    assert False
except:
    pass

try:
    a = {'1': 3, 4: None}
    x = a[1]
    assert False
except:
    pass
assert True

def f():
    try:
        raise KeyError('foo')
    except IndexError:   # will fail to catch
        assert False
    except:
        pass
    assert True

f()

def f1():
    try:
        assert 1 + 2 == 3
        try:
            a = {1: 2, 3: 4}
            x = a[0]
        except RuntimeError:
            assert False
    except IndexError:
        assert False
    assert False

try:
    f1()
    assert False
except KeyError:
    pass
#
#
assert True, "Msg"
try:
    assert False, "Msg"
    assert False
except AssertionError:
    pass

def f(a: list):
    try:
        raise ValueError
        assert False
    except:
        pass
    a[0] = 1
a = [0]
f(a)
assert a == [1]

class MyException(Exception):
    pass

class MyException2(MyException):
    pass

try:
    raise MyException2
except MyException as e:
    ok = True
except Exception:
    assert False
assert ok

#TODO: support eval mode
# ok = False
# try:
#     eval('1+')
# except SyntaxError as e:
#     assert type(e) is SyntaxError
#     ok = True
# assert ok

# finally, only
def finally_only():
    try:
        raise KeyError
    finally:
        return True

assert finally_only() is True

def finally_only_2():
    try:
        pass
    finally:
        return True

assert finally_only_2() is True

# finally, no exception
def finally_no_exception():
    ok = False
    try:
        pass
    except KeyError:
        assert False
    finally:
        ok = True
    return ok

assert finally_no_exception()

# finally, match
def finally_match():
    ok = False
    try:
        raise KeyError
    except KeyError:
        pass
    finally:
        ok = True
    return ok

assert finally_match()

# finally, no match
ok = False
def finally_no_match():
    global ok
    try:
        raise KeyError
    except IndexError:
        assert False
    finally:
        ok = True

try:
    finally_no_match()
except KeyError:
    assert ok

print ('22_exceptions passed.')
