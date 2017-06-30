load_assembly("mscorlib")
load_assembly("ThinkGeo.MapSuite")
               
def match(feature, features)
    [expression]
 end
                                                                                                                         
def getMatchFeatures(featuresToMatch)
	matchFeatures = System::Collections::ObjectModel::Collection[ThinkGeo::MapSuite::Core::Feature].new
	featuresToMatch.each do |f|
		begin
			if match(f, featuresToMatch)
				matchFeatures<<f
			end
		rescue

		end
	end
	return matchFeatures
end

return getMatchFeatures(Features)