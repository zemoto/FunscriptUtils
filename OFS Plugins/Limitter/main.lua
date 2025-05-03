-- OFS plugin to reduce the distance between actions in order to keep them all below a given speed limit
-- Works on selections, or the entire script

local SpeedLimit = {}
SpeedLimit.LowSpeed = 0
SpeedLimit.MediumSpeed = 0
SpeedLimit.OverSpeed = 0
SpeedLimit.MinSpeed = 50
SpeedLimit.DeviceMax = 400
SpeedLimit.OnlyAdjustTopActions = false
SpeedLimit.OnlyAdjustBottomActions = false
SpeedLimit.MaintainHolds = true
SpeedLimit.HoldsAllowHighSpeeds = true
SpeedLimit.SelectAdjustedActions = false

function init()
	SpeedLimit.LowSpeed = math.floor(SpeedLimit.DeviceMax * 0.625)
	SpeedLimit.MediumSpeed = math.floor(SpeedLimit.DeviceMax * 0.75)
	SpeedLimit.OverSpeed = math.floor(SpeedLimit.DeviceMax * 1.125)
end

function update(delta)
end

function gui()
	if ofs.Button(SpeedLimit.LowSpeed) then
		limitSpeed(SpeedLimit.LowSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button(SpeedLimit.MediumSpeed) then
		limitSpeed(SpeedLimit.MediumSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button(SpeedLimit.DeviceMax) then
		limitSpeed(SpeedLimit.DeviceMax)
	end
	
	ofs.SameLine()
	
	if ofs.Button(SpeedLimit.OverSpeed) then
		limitSpeed(SpeedLimit.OverSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "< "..SpeedLimit.MinSpeed ) then
		ensureMinSpeed()
	end
	
	if ofs.Button( "Delete Bad Mids" ) then
		deleteBadMidpoints()
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Ensure Long Holds" ) then
		ensureLongHolds()
	end
	
	ofs.Separator()

	SpeedLimit.OnlyAdjustTopActions, changed = ofs.Checkbox( "Only adjust top actions", SpeedLimit.OnlyAdjustTopActions )
	SpeedLimit.OnlyAdjustBottomActions, changed = ofs.Checkbox( "Only adjust bottom actions", SpeedLimit.OnlyAdjustBottomActions )
	
	SpeedLimit.MaintainHolds, maintainChanged = ofs.Checkbox( "Maintain Holds", SpeedLimit.MaintainHolds )
	if maintainChanged and not SpeedLimit.MaintainHolds then
		SpeedLimit.HoldsAllowHighSpeeds = false
	end
	
	SpeedLimit.HoldsAllowHighSpeeds, holdsAllowChanged = ofs.Checkbox( "Holds allow high speeds", SpeedLimit.HoldsAllowHighSpeeds )
	if holdsAllowChanged and SpeedLimit.HoldsAllowHighSpeeds then
		SpeedLimit.MaintainHolds = true
	end
	
	SpeedLimit.SelectAdjustedActions, changed = ofs.Checkbox( "Select adjusted actions", SpeedLimit.SelectAdjustedActions )
end

function limitSpeed(speedLimit)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local hasSelection = script:hasSelection()
	
	local actionsChanged = {}
	
	local changesMade = false
	for idx=2,actionCount do
		local action = script.actions[idx]
		if hasSelection and not action.selected then
			goto continue
		end
		
		if hasSelection and SpeedLimit.SelectAdjustedActions then
			action.selected = false
		end
		
		local previousAction = script.actions[idx - 1]
		local nextAction = nil
		if idx ~= actionCount then
			nextAction = script.actions[idx + 1]
		elseif SpeedLimit.HoldsAllowHighSpeeds then
			goto continue
		end

		local nextActionIsHold = false
		local prevActionIsHold = false
		if SpeedLimit.MaintainHolds then
			nextActionIsHold = nextAction ~= nil and nextAction.pos == action.pos
			prevActionIsHold = previousAction.pos == action.pos
		end
		
		-- A "top" action is one where it has a higher or equal position than both the previous and next action
		if SpeedLimit.OnlyAdjustTopActions and ( previousAction.pos > action.pos or ( nextAction ~= nil and nextAction.pos > action.pos ) ) then
			goto continue
		end
		
		-- A "bottom" action is one where it has a lower or equal position than both the previous and next action
		if SpeedLimit.OnlyAdjustBottomActions and ( previousAction.pos < action.pos or ( nextAction ~= nil and nextAction.pos < action.pos ) ) then
			goto continue
		end
		
		if getSpeedBetweenActions(previousAction, action) > speedLimit then
			local maxDistance = math.floor(speedLimit * (action.at - previousAction.at))
			if SpeedLimit.HoldsAllowHighSpeeds and nextActionIsHold then
				maxDistance = math.floor(speedLimit * (nextAction.at - previousAction.at))
			end

			if maxDistance > math.abs( action.pos - previousAction.pos ) then
				goto continue
			end

			if previousAction.pos > action.pos then
				maxDistance = maxDistance * -1
			end
		
			local newPos = clamp( previousAction.pos + maxDistance, 0, 100 )
			if action.pos ~= newPos then
				changesMade = true
				table.insert(actionsChanged,idx)
			end
			action.pos = newPos
		end
		
		if SpeedLimit.OnlyAdjustTopActions or SpeedLimit.OnlyAdjustBottomActions and nextAction ~= nil and getSpeedBetweenActions(action, nextAction) > speedLimit then
			local maxDistance = math.floor(speedLimit * (nextAction.at - action.at))
			if SpeedLimit.HoldsAllowHighSpeeds then
				if idx == actionCount - 1 then
					goto continue
				end
				
				local nextNextAction = script.actions[idx + 2]
				if nextNextAction.pos == nextAction.pos then
					maxDistance = math.floor(speedLimit * (nextNextAction.at - action.at))
				end
			end
			
			local newPos = math.min( action.pos, clamp( nextAction.pos + maxDistance, 0, 100 ) )
			if action.pos ~= newPos then
				changesMade = true
				table.insert(actionsChanged,idx)
			end
			action.pos = newPos
		end

		if nextActionIsHold then
			nextAction.pos = action.pos
		end
		
		if prevActionIsHold then
			previousAction.pos = action.pos
		end
		
		::continue::
	end
	
	if changesMade then
		if SpeedLimit.SelectAdjustedActions then
			for idx, actionIdx in ipairs(actionsChanged) do
				script.actions[actionIdx].selected = true
			end
		end
	
		script:commit()
	end
end

function ensureMinSpeed()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local hasSelection = script:hasSelection()
	
	local newActions = {}
	local changesMade = false
	for idx=1,actionCount-1 do
		local action = script.actions[idx]
		local nextAction = script.actions[idx + 1]
		if hasSelection and (not action.selected or not nextAction.selected) then
			goto continue2
		end
		
		local speed = getSpeedBetweenActions(action,nextAction)
		if speed >= SpeedLimit.MinSpeed or speed == 0 then
			goto continue2
		end
		
		local posChange = math.abs(nextAction.pos - action.pos)
		local timeDelta = math.max(posChange / SpeedLimit.MinSpeed, 0.05)
		
		if timeDelta > nextAction.at - action.at - 0.05 then
			goto continue2
		end
		
		changesMade = true
		
		table.insert(newActions, {at=action.at + timeDelta, pos=nextAction.pos})
		
		::continue2::
	end
	
	if changesMade then
		for idx, newAction in ipairs(newActions) do
			local newAction = Action.new(newAction.at, newAction.pos);
			newAction.selected = true
			script.actions:add(newAction)
		end
		
		script:sort()
		script:commit()
	end
end

function contains(array, val)
   for i=1,#array do
      if array[i] == val then 
         return true
      end
   end
   return false
end

function deleteBadMidpoints()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local hasSelection = script:hasSelection()
	local deletedIndexes = {}
	for idx=3,actionCount-2 do	
		local prevIdx = idx - 1
		while contains(deletedIndexes, prevIdx) do
			prevIdx = prevIdx - 1
		end
		if prevIdx < 1 then
			goto continue3
		end
		local prevAction = script.actions[prevIdx]
		
		local prevPrevIdx = prevIdx - 1
		while contains(deletedIndexes, prevPrevIdx) do
			prevPrevIdx = prevPrevIdx - 1
		end
		if prevPrevIdx < 1 then
			goto continue3
		end
		local prevPrevAction = script.actions[prevPrevIdx]
		
		local action = script.actions[idx]
		local nextAction = script.actions[idx + 1]		
		local nextNextAction = script.actions[idx + 2]		
		if hasSelection and not action.selected then
			goto continue3
		end
		
		local prevIsMid = (prevPrevAction.pos > prevAction.pos and prevAction.pos > action.pos ) or (prevPrevAction.pos < prevAction.pos and prevAction.pos < action.pos)
		local isMid = (prevAction.pos > action.pos and action.pos > nextAction.pos ) or (prevAction.pos < action.pos and action.pos < nextAction.pos)
		local nextIsMid = (action.pos > nextAction.pos and nextAction.pos > nextNextAction.pos ) or (action.pos < nextAction.pos and nextAction.pos < nextNextAction.pos)
		if not isMid then
			goto continue3
		end
		
		local prevToCurrentSpeed = getSpeedBetweenActions(prevAction,action)
		local currentToNextSpeed = getSpeedBetweenActions(action,nextAction)		
		if (prevToCurrentSpeed > SpeedLimit.DeviceMax) then
			script:markForRemoval(idx)
			table.insert(deletedIndexes, idx)
			if prevIsMid and not nextIsMid and getSpeedBetweenActions(prevAction,nextAction) > SpeedLimit.DeviceMax then
				script:markForRemoval(prevIdx)
				table.insert(deletedIndexes, prevIdx)
			end
		elseif prevToCurrentSpeed < SpeedLimit.MinSpeed and currentToNextSpeed < SpeedLimit.MinSpeed then
			script:markForRemoval(idx)
			table.insert(deletedIndexes, idx)
		end
		
		::continue3::
	end
	
	if #deletedIndexes > 0 then
		script:removeMarked()
		script:commit()
	end
end

function ensureLongHolds()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local changesMade = false
	for idx=1,actionCount-1 do
		local action = script.actions[idx]
		local nextAction = script.actions[idx + 1]
		
		if action.pos == nextAction.pos and nextAction.at - action.at > 3.001 then
			changesMade = true
			local currentTime = action.at + 3
			while currentTime <= nextAction.at - 0.001 do
				script.actions:add(Action.new(currentTime, action.pos))
				currentTime = currentTime + 3
			end			
		end
	end
	
	if changesMade then
		script:sort()
		script:commit()
	end	
end

function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs( second.pos - first.pos )
	return change / gapInSeconds
end