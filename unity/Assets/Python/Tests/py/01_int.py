# test int literals
assert 0xffff == 65535
assert 0xAAFFFF == 11206655
assert 0x7fffffff == 2147483647
assert -0xffff == -65535
assert -0xAAFFFF == -11206655
assert -0x7fffffff == -2147483647

# test oct literals
assert 0o1234 == 668
assert 0o17777777777 == 2147483647
assert -0o1234 == -668
assert -0o17777777777 == -2147483647

# test binary literals
assert 0b10010 == 18
assert -0b10010 == -18
assert -0b11111 == -31

# test == != >= <= < >
assert -1 == -1
assert -1 != 65535
assert -1 != 1
assert -1 >= -1
assert -1 <= -1
assert -1 < 1
assert -1 > -2

# test + - * % ** //
assert -1 + 1 == 0
assert -1 - 1 == -2
assert 4 * -1 == -4
assert 5 % 2 == 1
assert 2 ** 3 == 8
assert 4 // 2 == 2
assert 5 // 2 == 2

# test += -= *= //=
x = 3
x += 1
assert x == 4
x -= 1
assert x == 3
x *= 2
assert x == 6
x //= 2
assert x == 3

# test __str__, __repr__
assert str(1) == '1'
assert repr(1) == '1'

# test int()
assert int(1) == 1
assert int(1.0) == 1
assert int(1.1) == 1
assert int(1.9) == 1
assert int(-1.9) == -1
assert int(1.5) == 1
assert int(-1.5) == -1
assert int("123") == 123

# test >> << & | ^
assert 12 >> 1 == 6
assert 12 << 1 == 24
assert 12 & 1 == 0
assert 12 | 1 == 13
assert 12 ^ 1 == 13

# test high precision int pow
assert 4**13 == 67108864
assert (-4)**13 == -67108864

# test __invert__
assert ~3 == -4
assert ~-3 == 2
assert ~0 == -1

assert not 1 < 2 > 3

try:
    1 // 0
except ZeroDivisionError:
    pass

try:
    1 % 0
except ZeroDivisionError:
    pass

# test inc dec
a = 1

++a
assert a == 2
++a; ++a; --a;
assert a == 3

def f(a):
    ++a
    ++a
    --a
    return a

assert f(3) == 4
assert f(-2) == -1

print('01_int passed.')
