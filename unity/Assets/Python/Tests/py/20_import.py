try:
    import os
except ImportError:
    pass

def f20():
    import math as m
    assert m.pi > 3

f20()

from math import *
assert pi > 3

print('20_import passed.')
