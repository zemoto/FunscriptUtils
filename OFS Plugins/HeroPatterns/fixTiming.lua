function fixTiming(bpm)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local fullBeatTiming = 240.0 / bpm

	local totalMovement = 0
	for i=2,actionCount do
		local prev = script.actions[i-1]
		local current = script.actions[i]
		
		if script:hasSelection() and not (prev.selected and current.selected) then
			goto continue
		end
		
		current.at = current.at + totalMovement
		local gap = current.at - prev.at
		
		local newGap = getCorrectGap(gap,fullBeatTiming)
		totalMovement = totalMovement - gap + newGap
		current.at = prev.at + newGap
		
		::continue::
	end

	script:commit()
end

function getCorrectGap(currentGap,fullBeatTiming)
	local closestBeatTiming = fullBeatTiming	
	local newClosest = fullBeatTiming/2
	while true do
		if math.abs(newClosest - currentGap) < math.abs(closestBeatTiming - currentGap) then
			closestBeatTiming = newClosest
			newClosest = newClosest / 2
		else
			return closestBeatTiming
		end
	end
end

return { fixTiming = fixTiming }