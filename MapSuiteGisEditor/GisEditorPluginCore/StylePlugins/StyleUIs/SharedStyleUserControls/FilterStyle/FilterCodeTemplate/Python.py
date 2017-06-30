	f = True
	f = feature.ColumnValues["COLUMN_NAME"] == "MATCH_VALUE"
	f = f and (float(feature.ColumnValues["COLUMN_NAME"])> 0 or float(feature.ColumnValues["COLUMN_NAME"]) <= 1)
	f = f and feature.ColumnValues["COLUMN_NAME"] != "MATCH_VALUE"
	return f