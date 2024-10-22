x = "lily"
y = 16
z = 163.532

s = '%sssss'%x
assert s == 'lilyssss'
d = '%d1ssss'%y
assert d == '161ssss'
d = '%f1ssss'%y
assert d == '16.0000001ssss'
d = '%.6f1ssss'%y
assert d == '16.0000001ssss'
d = '%.3f2ssss'%y
assert d == '16.0002ssss'
f = 'ssss%.2f'%z
assert f == 'ssss163.53'
f = 'ssss%.5f'%z
assert f == 'ssss163.53200'
f = 'ssss%d'%z
assert f == 'ssss163'
f = 'ssss%s11+ss%'%z
assert f == 'ssss163.53211+ss%'

try:
    f = 'ssss%d'%x
    assert False
except TypeError:
    pass

try:
    f = 'fdafd%.4f'%x
    assert False
except TypeError:
    pass

g = {1, 2, 3}
s = '%sssss'%g
assert s == '{1, 2, 3}ssss'
l = [1, '222', '%s']
s = '%sssss'%l
assert s == "[1, '222', '%s']ssss"
t = (1, 2, 3)
s = "%s+321,%s,3214%s"%t
assert s == '1+321,2,32143'
t = (1, 3.4214, '32132155%%kkk3+%$#@')
s = "%d+321,%.2f,3214%s"%t
assert s == '1+321,3.42,321432132155%%kkk3+%$#@'
s = "%.4f+321,%d,3214%s"%t
assert s == '1.0000+321,3,321432132155%%kkk3+%$#@'
try:
    s = "%.4f+321,%d,3214%f"%t
    assert False
except TypeError:
    pass

d = "%s, %d, %.1f"%(x, y, z)
assert d == 'lily, 16, 163.5'
d = "%s, %d, %.1f"   %('332)', y, z)
assert d == '332), 16, 163.5'
d = "%s, %d, %.1f"   %('332)'+x+x, y+y*y, z-z+y*z)
assert d == '332)lilylily, 272, 2616.5'

try:
    d = "%s, %d, %.1f"   %('332)'+x+x, y+y*y-x, z-z+y*z)
    assert False
except TypeError:
    pass
d = "%s, %d, %.1f"%([x,x,x,x,y,y,y,y,z,z,z,z], y, z)
assert d == "['lily', 'lily', 'lily', 'lily', 16, 16, 16, 16, 163.532, 163.532, 163.532, 163.532], 16, 163.5"
d = "%s, %d, %.1f"%({(x,x,x),(y,y,y,y),(z,z,z,z)}, y, z)
assert d == "{('lily', 'lily', 'lily'), (16, 16, 16, 16), (163.532, 163.532, 163.532, 163.532)}, 16, 163.5"

d = "%s"   %[(x, y), z]
assert d == "[('lily', 16), 163.532]"
d = "%s"   %{('332)'), y, z}
assert d == "{'332)', 16, 163.532}"

try:
    d = "%s, %d, %.1f"  %[x,y,z]
    assert False
except TypeError:
    pass

try:
    d = "%s, %d, %.1f"  %{x,y,z}
    assert False
except TypeError:
    pass

assert 'ssss%s11+ss%'%z.startswith('ssss') is True
assert "%s, %d, %.1f"%(x, y, z).startswith('lily') is True

s = 'ssss%s11+ss%'%z+'qerw%s'%y
assert s == 'ssss163.53211+ss%qerw16'

s = 'ssss%s11+ss%'%z+x
assert s == 'ssss163.53211+ss%lily'

try:
    s = 'ssss%s11+ss%'%z+y
    assert False
except TypeError:
    pass


print('28_cstring passed.')
