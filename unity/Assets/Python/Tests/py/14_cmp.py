assert 1<2
assert 1+1==2
assert 2+1>=2

assert 1<2<3
assert 1<2<3<4
assert 1<2<3<4<5

assert 1<1+1<3
assert 1<1+1<3<4
assert 1<1+1<3<2+2<5

a = [1,2,3]
assert a[0] < a[1] < a[2]
assert a[0]+1 == a[1] < a[2]
assert a[0]+1 == a[1] < a[2]+1 < 5

assert (4>3<2) == False

#dict compare
a = {i: i**2 for i in range(10)}
assert a == {0: 0, 1: 1, 2: 4, 3: 9, 4: 16, 5: 25, 6: 36, 7: 49, 8: 64, 9: 81}

a = {i: i**2 for i in range(10) if i % 2 == 0}
assert a == {0: 0, 2: 4, 4: 16, 6: 36, 8: 64}

b = {k:v for k,v in a.items()}
assert b == a

#list compare
a = [i for i in range(10)]
assert a == list(range(10))

a = [i for i in range(10) if i % 2 == 0]
assert a == [0, 2, 4, 6, 8]

a = [i**3 for i in range(10) if i % 2 == 0]
assert a == [0, 8, 64, 216, 512]

a = [1, 2, 3, 4]
assert a.pop() == 4
assert a == [1, 2, 3]
assert a.pop(0) == 1
assert a == [2, 3]
assert a.pop(-2) == 2
assert a == [3]

a = []
a.sort()
assert len(a) == 0
assert a == []

a = [1]
a.sort()
assert len(a) == 1
assert a == [1]

a = [1, 2, 3, 4]
assert reversed(a) == [4, 3, 2, 1]
assert a == [1, 2, 3, 4]
a = (1, 2, 3, 4)
assert reversed(a) == [4, 3, 2, 1]
assert a == (1, 2, 3, 4)
a = '1234'
assert reversed(a) == ['4', '3', '2', '1']
assert a == '1234'

assert reversed([]) == []
assert reversed('') == []
assert reversed('测试') == ['试', '测']

a = [
    [(i,j) for j in range(10) if j % 2 == 0]
    for i in range(10) if i % 2 == 1
]

assert a == [[(1, 0), (1, 2), (1, 4), (1, 6), (1, 8)], [(3, 0), (3, 2), (3, 4), (3, 6), (3, 8)], [(5, 0), (5, 2), (5, 4), (5, 6), (5, 8)], [(7, 0), (7, 2), (7, 4), (7, 6), (7, 8)], [(9, 0), (9, 2), (9, 4), (9, 6), (9, 8)]]

#set compare
a = {i for i in range(10)}
assert a == set(range(10))

a = {i for i in range(10) if i % 2 == 0}
assert a == {0, 2, 4, 6, 8}

a = {i**3 for i in range(10) if i % 2 == 0}
assert a == {0, 8, 64, 216, 512}

a = {(i,i+1) for i in range(5)}
assert a == {(0, 1), (1, 2), (2, 3), (3, 4), (4, 5)}

print('14_cmp passed.')
