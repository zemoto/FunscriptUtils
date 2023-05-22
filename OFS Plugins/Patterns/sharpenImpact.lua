local function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs(second.pos - first.pos)
	return change / gapInSeconds
end

function sharpenImpact()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
	
	local newActions = {}
	local changesMade = false
	for i=1,actionCount-1 do
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
		
		if not currentAction.selected or not nextAction.selected or getSpeedBetweenActions(currentAction,nextAction) > 300 or nextAction.at - currentAction.at < 0.28 then 
			goto continue
		end
		
		local newTime = nextAction.at - ((nextAction.at - currentAction.at) / 8)
		local maxDistance = math.floor(364 * (nextAction.at - newTime) + 0.5)
		print("maxDistance",maxDistance)
		if nextAction.pos > currentAction.pos then
			newPos = nextAction.pos - maxDistance
			if newPos <= currentAction.pos then
				goto continue
			end
		else
			newPos = nextAction.pos + maxDistance
			if newPos >= currentAction.pos then
				goto continue
			end
		end
		
		table.insert(newActions, {at=newTime, pos=newPos})
		changesMade = true
		
		::continue::
	end

	if changesMade then
		for idx, newAction in ipairs(newActions) do
			script.actions:add(Action.new(newAction.at, newAction.pos))
		end
		
		script:sort()
		script:commit()
	end
end

return { sharpenImpact = sharpenImpact }