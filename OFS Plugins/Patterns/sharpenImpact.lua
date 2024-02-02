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
		if currentAction.pos ~= nextAction.pos or gap > 0.1 or gap < 0.04 then
			goto continue 
		end
	
		local change = math.floor(360 * gap / 2)
		if currentAction.pos > prevAction.pos then
			change = change * -1
		end	
		
		table.insert(newActions, {at=((nextAction.at + currentAction.at)/2), pos=(currentAction.pos + change)})
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