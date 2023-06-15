assert long(123) == long('123') == 123L == 123

a = long(2)
assert a ** 0 == 1
assert a ** 60 == 1152921504606846976

assert a + 1 == 3
assert a - 1 == 1
assert a * 2 == 4
assert a // 2 == 1

assert -a == -2

assert 1 + a == 3L
assert 1 - a == -1L
assert 2 * a == 4L