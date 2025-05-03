local MatchPatternEx = {}
MatchPatternEx.HeroMax = 100

function MatchPatternEx.apply()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions

	local changesMade = false
	for i=2,actionCount-2 do
		local prevAction = script.actions[i-1]
		local action = script.actions[i]
		local nextAction = script.actions[i+1]
		local nextNextAction = script.actions[i+2]
		
		if action.pos == 0 and action.pos == nextAction.pos and nextAction.at - action.at < 1 then
			changesMade = true
			nextAction.pos = MatchPatternEx.HeroMax
		end
	end
	
	if changesMade then
		script:commit()
	end
end

return MatchPatternEx