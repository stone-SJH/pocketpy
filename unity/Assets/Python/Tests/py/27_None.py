print(1, 2, '3232', '1111', 3.3, sep = None, end = None)
print(None)
print(None, 'Test', None, end = None)

def A27t(a, kw1 = 30):
    return a + kw1

assert A27t(10) == 40
assert A27t(10, kw1 = 20) == 30
assert A27t(10, kw1 = None) == 40
try:
    r = A27t(None)
    assert False
except TypeError:
    pass

assert None.__repr__() == 'None'
assert None.__str__() == 'None'
assert repr(None.__repr__).startswith('<method-wrapper \'__repr__\' of NoneType')
assert repr(None.__str__).startswith('<method-wrapper \'__str__\' of NoneType')

try:
    a = None()
except TypeError:
    pass

a = None
assert a.__eq__(None)
assert a.__eq__(1) == NotImplemented
assert a.__eq__(1.0) == NotImplemented
assert a.__eq__('fff') == NotImplemented

assert 1 == None is False
assert None == None
assert None is None

print('27_None passed.')
