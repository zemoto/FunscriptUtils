local matchPattern = {}

function matchPattern.easy()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions

	local newActions = {}
	local actionsAdded = 0
	
	local changesMade = false
	for i=1,actionCount-2 do
		current = script.actions[i]
		nextAction = script.actions[i+1]
		
		local gap = nextAction.at - current.at
		local nextGap = script.actions[i+2].at - nextAction.at
		
		if math.abs( nextGap - gap ) > 0.05 * nextGap and nextGap < gap and gap - nextGap > 0.075 then
			changesMade = true
			table.insert(newActions, {at=(nextAction.at - nextGap), pos=current.pos})
		end
		
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

function matchPattern.hard()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions

	local newActions = {}
	local actionsAdded = 0
	
	local changesMade = false
	for i=1,actionCount-3 do
		current = script.actions[i]
		if current.pos ~= 0 then
			goto continue
		end
		
		movementAction = script.actions[i+1]
		nextAction = script.actions[i+2]
		
		local gap = movementAction.at - current.at
		local nextGap = script.actions[i+3].at - nextAction.at
		
		if math.abs( nextGap - gap ) > 0.05 * nextGap and nextGap < gap and gap - nextGap > 0.038 then
			local newTime1
			
			changesMade = true
			movementAction.at = nextAction.at - nextGap
			table.insert(newActions, {at=(movementAction.at-nextGap), pos=0})
		end
		
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

return { matchPattern = matchPattern }