function fixTiming(bpm)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local beatTime = 7.5 / bpm -- seconds between every 1/32th measure

	local itemsToReview = {}
	local changesMade = false
	local totalMovement = 0
	for i=2,actionCount do
		local prev = script.actions[i-1]
		local current = script.actions[i]
		
		if script:hasSelection() and not (prev.selected and current.selected) then
			goto continue
		end
		
		prev.selected = false
		current.at = current.at + totalMovement
		local gap = current.at - prev.at
		local newGap = getCorrectGap(gap,beatTime)
		
		if math.abs(gap - newGap) + totalMovement > 0.4 * beatTime then
			table.insert(itemsToReview, i)
		end
		
		changesMade = changesMade or gap ~= newGap
		totalMovement = totalMovement - gap + newGap
		current.at = prev.at + newGap
		
		::continue::
	end
	
	-- Select the actions that were moved a lot for review
	for idx,reviewIdx in ipairs(itemsToReview) do
		script.actions[reviewIdx].selected = true;
	end

	if changesMade then
		script:commit()
	end
end

function getCorrectGap(currentGap,beatTime)
	local errorGap = currentGap % beatTime
	if errorGap > beatTime * 0.5 then
		return currentGap - errorGap + beatTime
	else
		return currentGap - errorGap
	end
end

return { fixTiming = fixTiming }