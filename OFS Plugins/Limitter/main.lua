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

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Green" ) then
		limitSpeed(SpeedLimit.GreenSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Yellow" ) then
		limitSpeed(SpeedLimit.YellowSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Red" ) then
		limitSpeed(SpeedLimit.RedSpeed)
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Black" ) then
		limitSpeed(SpeedLimit.BlackSpeed)
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
end

function limitSpeed(speedLimit)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local changesMade = false
	for idx, action in ipairs(script.actions) do
		if idx == 1 or ( script:hasSelection() and not action.selected ) then
			goto continue
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

			if previousAction.pos > action.pos then
				maxDistance = maxDistance * -1
			end
		
			local newPos = clamp( previousAction.pos + maxDistance, 0, 100 )
			changesMade = changesMade or action.pos ~= newPos
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
			
			local newPos = clamp( nextAction.pos + maxDistance, 0, 100 )
			changesMade = changesMade or action.pos ~= newPos
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
		script:commit()
	end
end

function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs( second.pos - first.pos )
	return change / gapInSeconds
end