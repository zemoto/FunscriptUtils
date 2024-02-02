local Filler = {}
Filler.Distance = 40
Filler.Gap = 1

-- Add filler for very long gaps
function Filler.filler()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
   
	local changesMade = false
	for idx, action in ipairs(script.actions) do
		if idx >= actionCount then
			break
		end
		local nextAction = script.actions[idx+1]
			
		if ( script:hasSelection() and ( not action.selected or not nextAction.selected ) ) or 
		   ( nextAction.at - action.at <= 10 ) then
			goto continue
		end
		
		changesMade = true		
		local currentTime = action.at + Filler.Gap
		if action.pos > 0 then
			script.actions:add(Action.new(action.at + (Filler.Gap*2), 0))
			currentTime = action.at + (Filler.Gap*3)
		end
		
		local addingTop = true		
		while currentTime < nextAction.at - (Filler.Gap*3) do
			local pos = 0
			if addingTop then
				pos = Filler.Distance
			end
			script.actions:add(Action.new(currentTime, pos))
			currentTime = currentTime + Filler.Gap
			addingTop = not addingTop
		end
		
		if not addingTop then
			script.actions:add(Action.new((nextAction.at + currentTime - (Filler.Gap*2)) / 2, 0))
		end
		
		::continue::
	end
	
	if changesMade then
		script:sort()
		script:commit()
	end
end

return Filler