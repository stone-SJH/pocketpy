﻿a = [1, 2, 3]
a = iter(a)

total = 0

while True:
    obj = next(a)
    if obj is StopIteration:
        break
    total += obj

assert total == 6

class Task:
    def __init__(self, n):
        self.n = n

    def __iter__(self):
        self.i = 0
        return self

    def __next__(self):
        if self.i == self.n:
            return StopIteration
        self.i += 1
        return self.i

a = Task(3)
assert sum(a) == 6

i = iter(Task(5))
assert next(i) == 1
assert next(i) == 2
assert next(i) == 3
assert next(i) == 4
assert next(i) == 5
assert next(i) == StopIteration
assert next(i) == StopIteration

print('19_iter passed.')
