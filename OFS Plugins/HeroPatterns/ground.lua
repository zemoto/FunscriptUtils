function ground()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local changesMade = false
	for i=2,actionCount do
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		
		if currentAction.pos < prevAction.pos and currentAction.pos > 0 then
			local height = currentAction.pos
			currentAction.pos = 0
			prevAction.pos = prevAction.pos - height
			changesMade = true
		end
	end
	
	if changesMade then
		script:sort()
		script:commit()
	end	
end

return { ground = ground }