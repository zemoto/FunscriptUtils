local Ramp = {}
Ramp.Steps = 3

function Ramp.gui()
	Ramp.Steps, stepsChanged = ofs.SliderInt("Steps", Ramp.Steps, 1, 5)
	if stepsChanged and ofs.Undo() then
		Ramp.apply()
	end
end

function Ramp.apply()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local usePlayhead = false
	
	if not script:hasSelection() then
		usePlayhead = true
		actionCount = 2
	end
   
    local newActions = {}
	for i=1,actionCount-1 do
		local startAction = {}
		local endAction = {} 
		if usePlayhead then
			local currentTime = player.CurrentTime()
			startAction = script:closestActionBefore(currentTime + 0.015)
			endAction = script:closestActionAfter(currentTime + 0.015)
			if startAction == nil or endAction == nil then
				return
			end
		else
			startAction = script.actions[i]
			endAction = script.actions[i+1]
			if not startAction.selected or not endAction.selected then 
				goto continue
			end
		end
		
		local gap = endAction.pos - startAction.pos
		local timeGap = endAction.at - startAction.at
		
		local parts = (2 ^ (Ramp.Steps+1)) - 2
		local gapStep = gap / parts
		local newActionTimeGap = timeGap / (Ramp.Steps + 1)
		
		local time = startAction.at
		local position = startAction.pos
		
		if Ramp.Steps == 1 then 
			table.insert(newActions, {at=time + newActionTimeGap, pos=position + (gapStep * 0.5)})
		else
			for j=1,Ramp.Steps do
				time = time + newActionTimeGap
				position = position + (gapStep * (2 ^ (j-1)))
				table.insert(newActions, {at=time, pos=position})
			end
		end
		
		::continue::
	end
	
	if #newActions > 0 then
		for idx, newAction in ipairs(newActions) do
			script.actions:add(Action.new(newAction.at, newAction.pos))
		end
		
		script:sort()
		script:commit()
	end
end

return Ramp