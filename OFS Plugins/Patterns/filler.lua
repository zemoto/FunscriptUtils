function filler(distance,gap)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
   
	local changesMade = false
	for idx, action in ipairs(script.actions) do
		if idx >= actionCount then
			break
		end
		local nextAction = script.actions[idx+1]
		
		-- Add filler for very long gaps
		if nextAction.at - action.at > 10 then
			changesMade = true
			
			local currentTime = action.at + gap
			if action.pos > 0 then
				script.actions:add(Action.new(action.at + (gap*2), 0))
				local currentTime = action.at + (gap*3)
			end
			
			local addingTop = true
			
			while currentTime < nextAction.at - (gap*2) do
				local pos = 0
				if addingTop then
					pos = distance
				end
				script.actions:add(Action.new(currentTime, pos))
				currentTime = currentTime + gap
				addingTop = not addingTop
			end
			
			if not addingTop then
				script.actions:add(Action.new((nextAction.at + currentTime - gap) / 2, 0))
			end
		end
	end
	
	if changesMade then
		script:sort()
		script:commit()
	end
end

return { filler = filler }