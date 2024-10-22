a = 1
b = 1
assert a is b
assert a is 1

# ignore float
#a = 1.1
#b = 1.1
#assert a is not b
#assert a is a
#assert a is not 1.1
#assert a == 1.1
#assert a == b

a = '111'
b = '111'
assert a is b

a = True
b = True
assert a is b
assert a is True
assert b is not False

a = None
b = None
assert a is b
assert a is None
assert b is None

a = [1, 'a', 2]
b = [1, 'a', 2]
assert a is not b

a = []
b = []
assert a is not b

a = {}
b = {}
assert a is not b

a = {1, 2}
b = {1, 2}
assert a is not b
print('12_is passed.')
