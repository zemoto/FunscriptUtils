-- OFS plugin to reduce the distance between actions in order to keep them all below a given speed limit
-- Works on selections, or the entire script

local SpeedLimit = {}
SpeedLimit.GreenSpeed = 250
SpeedLimit.YellowSpeed = 300
SpeedLimit.RedSpeed = 364
SpeedLimit.BlackSpeed = 454
SpeedLimit.OnlyAdjustTopActions = false
SpeedLimit.MaintainHolds = true
SpeedLimit.HoldsAllowHighSpeeds = true
SpeedLimit.SelectAdjustedActions = false

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "250" ) then
		limitSpeed(SpeedLimit.GreenSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "300" ) then
		limitSpeed(SpeedLimit.YellowSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "364" ) then
		limitSpeed(SpeedLimit.RedSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "454" ) then
		limitSpeed(SpeedLimit.BlackSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "< 40" ) then
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
		
		if SpeedLimit.OnlyAdjustTopActions and nextAction ~= nil and getSpeedBetweenActions(action, nextAction) > speedLimit then
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
	
	local minSpeed = 40
	
	local newActions = {}
	local changesMade = false
	for idx=1,actionCount-1 do
		local action = script.actions[idx]
		local nextAction = script.actions[idx + 1]
		if hasSelection and (not action.selected or not nextAction.selected) then
			goto continue2
		end
		
		local speed = getSpeedBetweenActions(action,nextAction)
		if speed >= minSpeed or speed == 0 then
			goto continue2
		end
		
		local posChange = math.abs(nextAction.pos - action.pos)
		local timeDelta = math.max(posChange / minSpeed, 0.05)
		
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

function deleteBadMidpoints()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local hasSelection = script:hasSelection()
	
	local maxSpeed = 405
	local actionsToRemoveFound = false
	local newActions = {}
	local changesMade = false
	for idx=3,actionCount-2 do
		local prevPrevAction = script.actions[idx - 2]
		local prevAction = script.actions[idx - 1]
		local action = script.actions[idx]
		local nextAction = script.actions[idx + 1]
		local nextNextAction = script.actions[idx + 2]
		if hasSelection and not action.selected then
			goto continue3
		end
		
		local isMid = (prevAction.pos > action.pos and action.pos > nextAction.pos ) or (prevAction.pos < action.pos and action.pos < nextAction.pos)
		if not isMid then
			goto continue3
		end
		
		local prevIsMid = (prevPrevAction.pos > prevAction.pos and prevAction.pos > action.pos ) or (prevPrevAction.pos < prevAction.pos and prevAction.pos < action.pos)
		local nextIsMid = (action.pos > nextAction.pos and nextAction.pos > nextNextAction.pos ) or (action.pos < nextAction.pos and nextAction.pos < nextNextAction.pos)
		local prevToCurrentSpeed = getSpeedBetweenActions(prevAction,action)
		local currentToNextSpeed = getSpeedBetweenActions(action,nextAction)		
		if prevToCurrentSpeed > maxSpeed then
			script:markForRemoval(idx)
			actionsToRemoveFound = true
			local postRemovalSpeed = getSpeedBetweenActions(prevAction,nextAction)
			if postRemovalSpeed > maxSpeed and prevIsMid then
				script:markForRemoval(idx-1)
			end
		elseif currentToNextSpeed > maxSpeed and not nextIsMid then
			script:markForRemoval(idx)
			actionsToRemoveFound = true
		end
		
		::continue3::
	end
	
	if actionsToRemoveFound then
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
		
		if action.pos == nextAction.pos and nextAction.at - action.at > 3 then
			changesMade = true
			local currentTime = action.at + 3
			while currentTime < nextAction.at do
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