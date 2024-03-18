function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs( second.pos - first.pos )
	return change / gapInSeconds
end

function sharpenImpact()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local newActions = {}
	local changesMade = false
	for i=2,actionCount-1 do
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
		
		if script:hasSelection() and (not currentAction.selected or not nextAction.selected) then
			goto continue
		end
		
		local gap = nextAction.at - currentAction.at
		if currentAction.pos ~= 0 or nextAction.pos ~= 0 or gap > 0.1 or gap < 0.04 then
			goto continue 
		end
	
		local speed = getSpeedBetweenActions(prevAction,currentAction)
		local change = math.floor(speed * 0.9 * gap / 2)
		if currentAction.pos > prevAction.pos then
			change = change * -1
		end
		
		table.insert(newActions, {at=((nextAction.at + currentAction.at)/2), pos=change})
		currentAction.pos = math.ceil(change/2.0)
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