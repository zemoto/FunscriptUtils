function raise()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local changesMade = false
	for i=3,actionCount-2 do
		local prevPrevAction = script.actions[i-2]
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
		local nextNextAction = script.actions[i+2]
				
		local beforeHold = nextAction.pos == 0 and nextNextAction.pos == 0
		local preHold = currentAction.pos == 0 and nextAction.pos == 0
		local postHold = prevAction.pos == 0 and currentAction.pos == 0
		local afterHold = prevPrevAction.pos == 0 and prevAction.pos == 0
		
		if not beforeHold and not preHold and not postHold and not afterHold then
			currentAction.pos = currentAction.pos + 5
			changesMade = true
		end
	end
	
	if changesMade then	
		script:commit()
	end
end

return { raise = raise }