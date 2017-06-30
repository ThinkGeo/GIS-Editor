import clr
clr.AddReference("mscorlib")
clr.AddReference("ThinkGeo.MapSuite")

from System.Collections.ObjectModel import *
from ThinkGeo.MapSuite import *

def match(feature, features):
[expression]

resultFeatures = Collection[Feature]()
for f in Features:
	try:
		if match(f, Features): resultFeatures.Add(f)
	except:
		pass
resultFeatures