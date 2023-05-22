local MinMax = {}
MinMax.Max = 100
MinMax.Min = 0
MinMax.Maximize = false
MinMax.AdjustAllActions = false

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Apply Min/Max" ) then
		applyMinMax()
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

function applyMinMax()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if actionCount < 1 then
		return
	end
	
	local onlySelectedActions = script:hasSelection()
	local changesMade = false
	if MinMax.AdjustAllActions then
		local oldMax = -1
		local oldMin = 101
		for idx, action in ipairs(script.actions) do
			if (onlySelectedActions and action.selected) or not onlySelectedActions then		
				oldMax = math.max(oldMax, action.pos)
				oldMin = math.min(oldMin, action.pos)
			end
		end
		
		local oldRange = oldMax - oldMin
		local newRange = MinMax.Max - MinMax.Min
		
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
			local newPos = clamp( action.pos, MinMax.Min, MinMax.Max )
			if action.pos ~= newPos then
				action.pos = newPos
				changesMade = true
			end
			::continue::
		end
	end
	
	if MinMax.Maximize then
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
				action.pos = MinMax.Max
			elseif (prevAction == nil or prevAction.pos > action.pos) and (nextAction == nil or nextAction.pos >= action.pos) then -- bottom
				action.pos = MinMax.Min
			elseif prevAction == nil or prevAction.pos ~= action.pos then -- hold
				action.pos = (MinMax.Max + MinMax.Min) / 2
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