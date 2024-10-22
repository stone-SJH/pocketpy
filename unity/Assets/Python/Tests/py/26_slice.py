a = ("a", "b", "c", "d", "e", "f", "g", "h")
x = slice(0, -1, 2)
assert a[x] == ('a', 'c', 'e', 'g')

x= slice(None, None, None)
assert a[x] == ('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h')

x = slice(None, None, 0)
try:
    print(a[x])
except ValueError:
    pass

x = slice(None, None, -1)
assert a[x] == ('h', 'g', 'f', 'e', 'd', 'c', 'b', 'a')

x = slice(None, 1, None)
assert a[x] == ('a',)

x1 = slice(1)
assert x == x1
assert a[x1] == ('a',)

x = slice(0, 3, None)
assert a[x] == ('a', 'b', 'c')
x2 = slice(0, 3)
assert x == x2
assert a[x2] == ('a', 'b', 'c')

print('26_slice passed.')