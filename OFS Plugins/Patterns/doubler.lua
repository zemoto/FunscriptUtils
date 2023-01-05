function doubler()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
   
	local newActions = {}
	local changesMade = false
	for idx, action in ipairs(script.actions) do
		local i0 = clamp(idx - 2, 1, actionCount)
		local i1 = clamp(idx - 1, 1, actionCount)
		local i2 = clamp(idx, 1, actionCount)
		local start = script.actions[i0]
		local middle = script.actions[i1]
        local endAction = script.actions[i2]
		
		if not start.selected or not endAction.selected or i0 == i1 or i1 == i2 then 
			goto continue
		end
		
		local time1 = (start.at + middle.at) / 2
		local time2 = (middle.at + endAction.at) / 2
		local pos = middle.pos
		
		middle.pos = (start.pos + endAction.pos) / 2
		
		table.insert(newActions, {at=time1, pos=pos})
		table.insert(newActions, {at=time2, pos=pos})
		
		start.selected = false
		middle.selected = false
		
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

return { doubler = doubler }