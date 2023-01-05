function hardMode()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions

	local newActions = {}
	
	for i=1,actionCount do	
		current = script.actions[i]
		current.pos = 0
		
		if i == actionCount then
			break
		end
		
		nextAction = script.actions[i+1]
		
		local gap = nextAction.at - current.at
		
		local desiredGap = 0
		if i == actionCount-2 then		
			desiredGap = script.actions[i+2].at - nextAction.at
		else
			desiredGap = gap
		end

		local newTime = current.at
		if desiredGap <= gap then
			newTime = nextAction.at - (desiredGap/2)
		else
			newTime = nextAction.at - (gap/2)
		end
		
		table.insert(newActions, {at=newTime, pos=100})
	end
	
	for idx, newAction in ipairs(newActions) do
		script.actions:add(Action.new(newAction.at, newAction.pos))
	end
	
	script:sort()
	script:commit()
end

return { hardMode = hardMode }