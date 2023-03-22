function impactBounce()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local impactTime = 0.05
	local impactMagnitude = 364 * impactTime -- 364 is speed limit

	local newActions = {}

	local changesMade = false
	for i=2,actionCount-1 do
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
		
		if (script:hasSelection() and not (prevAction.selected and currentAction.selected and nextAction.selected)) 
			or math.abs(prevAction.pos - currentAction.pos) < impactMagnitude*1.5 
			or math.abs(nextAction.pos - currentAction.pos) < impactMagnitude*1.5 then
			goto continue
		end
		
		table.insert(newActions, {at=currentAction.at, pos=currentAction.pos})
		table.insert(newActions, {at=currentAction.at+(impactTime*2), pos=currentAction.pos})
		
		currentAction.at = currentAction.at + impactTime
		if currentAction.pos < prevAction.pos and currentAction.pos < nextAction.pos then -- Bottom
			currentAction.pos = currentAction.pos + impactMagnitude
		elseif currentAction.pos > prevAction.pos and currentAction.pos > nextAction.pos then -- Top
			currentAction.pos = currentAction.pos - impactMagnitude
		end
		
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

return { impactBounce = impactBounce }