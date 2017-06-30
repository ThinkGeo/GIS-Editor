f = true
f = feature.ColumnValues["COLUMN_NAME"] == "MATCH_VALUE"
f = (f and (Float(feature.ColumnValues["COLUMN_NAME"])> 0 or Float(feature.ColumnValues["COLUMN_NAME"]) <= 1))
f = (f and feature.ColumnValues["COLUMN_NAME"] != "MATCH_VALUE")
return f