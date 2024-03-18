local MinMax = {}
MinMax.Max = 100
MinMax.Min = 0
MinMax.Maximize = false
MinMax.AdjustAllActions = false

function binding.minmax()
	applyMinMax()
end

function binding.smartminmax()
	applySmartMinMax()
end

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Apply Min/Max" ) then
		applyMinMax(MinMax.Min, MinMax.Max, MinMax.Maximize, MinMax.AdjustAllActions)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Smart Min/Max" ) then
		applySmartMinMax()
	end
	
	ofs.Separator()

	MinMax.Max, maxChanged = ofs.SliderInt("Max", MinMax.Max, 10, 100)
	if maxChanged then
		if MinMax.Max <= MinMax.Min then
			MinMax.Max = MinMax.Min + 1
		end
	end
	
	MinMax.Min, minChanged = ofs.SliderInt("Min", MinMax.Min, 0, 90)
	if minChanged then
		if MinMax.Min >= MinMax.Max then
			MinMax.Min = MinMax.Max - 1
		end
	end
	
	MinMax.Maximize, maximizeChanged = ofs.Checkbox( "Maximize actions", MinMax.Maximize )
	if maximizeChanged and MinMax.Maximize then
		MinMax.AdjustAllActions = false
	end
	
	MinMax.AdjustAllActions, adjustAllChanged = ofs.Checkbox( "Adjust actions relatively", MinMax.AdjustAllActions )
	ofs.Tooltip( "Adjust all actions relative to the new min/max" )
	if adjustAllChanged and MinMax.AdjustAllActions then
		MinMax.Maximize = false
	end
	
end

function applyMinMax(actionMin, actionMax, maximize, adjustAll)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if actionCount < 1 then
		return
	end
	
	local onlySelectedActions = script:hasSelection()
	local changesMade = false
	if adjustAll then		
		local oldMax = -1
		local oldMin = 101
		for idx, action in ipairs(script.actions) do
			if (onlySelectedActions and action.selected) or not onlySelectedActions then		
				oldMax = math.max(oldMax, action.pos)
				oldMin = math.min(oldMin, action.pos)
			end
		end
		
		local oldRange = oldMax - oldMin
		local newRange = actionMax - actionMin
		
		if maxChange ~= 1 or minChange ~= 1 then
			changesMade = true
			for idx, action in ipairs(script.actions) do
				if (onlySelectedActions and action.selected) or not onlySelectedActions then
					local relativePositionInOldRange = (action.pos - oldMin) / oldRange
					action.pos = math.floor(relativePositionInOldRange * newRange + MinMax.Min)
				end
			end
		end
	else
		for idx, action in ipairs(script.actions) do
			if onlySelectedActions and not action.selected then
				goto continue
			end
			local newPos = clamp( action.pos, actionMin, actionMax )
			if action.pos ~= newPos then
				action.pos = newPos
				changesMade = true
			end
			::continue::
		end
	end
	
	if maximize then
		for idx, action in ipairs(script.actions) do
			if onlySelectedActions and not action.selected then
				goto continue2
			end
		
			local prevAction = nil
			if idx > 1 then
				prevAction = script.actions[idx-1]
			end
			local nextAction = nil
			if idx < actionCount then
				nextAction = script.actions[idx+1]
			end
		
			local oldPos = action.pos
			local nextActionIsHold = nextAction ~= nil and nextAction.pos == action.pos
			if (prevAction == nil or prevAction.pos < action.pos) and (nextAction == nil or nextAction.pos <= action.pos) then -- top
				action.pos = actionMax
			elseif (prevAction == nil or prevAction.pos > action.pos) and (nextAction == nil or nextAction.pos >= action.pos) then -- bottom
				action.pos = actionMin
			elseif prevAction == nil or prevAction.pos ~= action.pos then -- hold
				action.pos = (actionMax + actionMin) / 2
			end
			
			if nextActionIsHold then
				nextAction.pos = action.pos
			end
			
			changesMade = changesMade or action.pos ~= oldPos
			
			::continue2::
		end
	end
	
	if changesMade then
		script:commit()
	end
end

function applySmartMinMax()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local onlySelectedActions = script:hasSelection()
		
	local topSpeed = 0
	local localMin = 101
	local localMax = -1
	for idx=1,actionCount-1 do
		local currentAction = script.actions[idx]
		local nextAction = script.actions[idx+1]
		
		if onlySelectedActions and not currentAction.selected then
			goto continue3
		end

		topSpeed = math.max(topSpeed, getSpeedBetweenActions(currentAction, nextAction))
		localMin = math.min(localMin, currentAction.pos)
		localMax = math.max(localMax, currentAction.pos)
		
		::continue3::
	end
	
	local maxSpeed = 400
	if topSpeed > maxSpeed then
		localMax = localMax * (maxSpeed / topSpeed)
	end
	
	applyMinMax(localMin, localMax, false, true)
end

function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs( second.pos - first.pos )
	return change / gapInSeconds
end